using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class ArgumentContext
    {
        public Expression Expression { get; set; }
        public string Name { get; set; }
    }

    public class MethodInvocationContext
    {
        public bool IsExtensionMethod { get; set; }
        public bool IsStaticMethod { get; set; }
        public Expression ThisParameter { get; set; }
        public TypeOrGeneric Method { get; set; }
        public IEnumerable<MethodInfo> MethodCandidates { get; set; }
        public CSharp4Parser.ArgumentContext[] ArgumentContext { get; set; }
        public Type Type { get; set; }
        public CSharpEvalVisitor Visitor { get; set; }
        public Expression Instance { get; set; }

        public List<TypeInferenceBounds> TypeInferenceBoundsList { get; set; }

        public Expression GetInvokeMethodExpression()
        {
            var appMembers = new List<ApplicableFunctionMember>();
            IEnumerable<Argument> args = null;

            foreach (var F in MethodCandidates)
            {
                if (!F.IsGenericMethod)
                {
                    var argsList = new List<Argument>();

                    foreach (var argument in ArgumentContext)
                    {
                        var argExpression = (ArgumentExpression)Visitor.Visit(argument);
                        argsList.Add(new Argument()
                        {
                            Expression = argExpression.Expression,
                            Name = argExpression.Name,
                            IsNamedArgument = argExpression.Name != null
                        });
                    }

                    args = argsList;

                    var afm = IsApplicableFunctionMember(F, args);
                    if (afm != null)
                    {
                        appMembers.Add(afm);
                    }
                }
                else
                {
                    args = ApplyTypeInference(F);

                    if (args != null)
                    {
                        var afm = IsApplicableFunctionMember(F, args);
                        if (afm != null)
                        {
                            appMembers.Add(afm);
                        }
                    }
                }
            }

            // based on TypeInferenceBoundsList, get the best types and re-visit the expressions

            Type[] typeArgs = null;
            var genericMethods = appMembers.Where(x => x.Member.IsGenericMethod).ToList();

            Dictionary<ApplicableFunctionMember, List<TypeInferrence>> methodTypeInferences = new Dictionary<ApplicableFunctionMember, List<TypeInferrence>>();

            if (genericMethods.Any() && Method.TypeArgs == null)
            {
                foreach (var genericMethod in genericMethods)
                {
                    var inferences = MethodResolution.TypeInference(genericMethod, args);
                    if (inferences != null)
                    {
                        methodTypeInferences.Add(genericMethod, inferences);
                    }
                }
            }

            var applicableMemberFunction = MethodResolution.OverloadResolution(appMembers, args);

            if (applicableMemberFunction != null)
            {
                var parameterInfos = applicableMemberFunction.Member.GetParameters();
                var argExps = args.Select(x => x.Expression).ToList();

                ParameterInfo paramArrayParameter = parameterInfos.FirstOrDefault(p => p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0);
                List<Expression> newArgs2 = null;

                if (Method.TypeArgs != null)
                {
                    typeArgs = Method.TypeArgs.ToArray();
                }
                else
                {
                    typeArgs = new Type[0];
                }

                if (paramArrayParameter != null)
                {
                    newArgs2 = argExps.Take(parameterInfos.Length - 1).ToList();
                    var paramArgs2 = argExps.Skip(parameterInfos.Length - 1).ToList();

                    var typeArgCount = parameterInfos.Count(x => x.ParameterType.IsGenericParameter);
                    if (typeArgs == null && typeArgCount > 0) typeArgs = new Type[typeArgCount];


                    for (var i = 0; i < parameterInfos.Length - 1; i++)
                    {
                        if (parameterInfos[i].ParameterType.IsGenericParameter)
                        {
                            var genericParameterPosition = parameterInfos[i].ParameterType.GenericParameterPosition;
                            typeArgs[genericParameterPosition] = newArgs2[i].Type;
                        }
                        newArgs2[i] = TypeConversion.Convert(newArgs2[i], parameterInfos[i].ParameterType);
                    }

                    var targetType = paramArrayParameter.ParameterType.GetElementType();

                    if (targetType == null)
                    {
                        var ga = paramArrayParameter.ParameterType.GetGenericArguments();
                        if (ga.Any())
                        {
                            targetType = ga.Single();
                        }
                    }

                    if (targetType != null)
                    {
                        newArgs2.Add(Expression.NewArrayInit(targetType,
                                                             paramArgs2.Select(x => TypeConversion.Convert(x, targetType))));
                    }

                }
                else
                {
                    newArgs2 = argExps.ToList();
                    var typeArgCount = parameterInfos.Count(x => x.ParameterType.IsGenericParameter || x.ParameterType.IsGenericType && x.ParameterType.GetGenericArguments().Any(y => y.IsGenericParameter));
                    if (typeArgs == null && typeArgCount > 0) typeArgs = new Type[typeArgCount];

                    for (var i = 0; i < parameterInfos.Length; i++)
                    {
                        if (parameterInfos[i].ParameterType.IsGenericParameter)
                        {
                            var genericParameterPosition = parameterInfos[i].ParameterType.GenericParameterPosition;
                            typeArgs[genericParameterPosition] = newArgs2[i].Type;
                        }
                        if (parameterInfos[i].ParameterType.IsGenericType)
                        {
                            var genericArgs = parameterInfos[i].ParameterType.GetGenericArguments();
                            var genericArgParameters = genericArgs.Where(y => y.IsGenericParameter).ToList();
                            if (genericArgParameters.Any())
                            {
                                foreach (var genericArgParameter in genericArgParameters)
                                {
                                    var genericArgParameterPosition = genericArgParameter.GenericParameterPosition;
                                    typeArgs[genericArgParameterPosition] = typeof(int);
                                }
                            }
                        }
                        newArgs2[i] = TypeConversion.Convert(newArgs2[i], parameterInfos[i].ParameterType);
                    }
                }

                if (applicableMemberFunction.Member.ContainsGenericParameters)
                {
                    if (IsStaticMethod)
                    {
                        return Expression.Call(Type, Method.Identifier, typeArgs, newArgs2.ToArray());
                    }
                    else
                    {
                        return Expression.Call(Instance, Method.Identifier, typeArgs, newArgs2.ToArray());
                    }
                }

                if (IsStaticMethod)
                {
                    return Expression.Call(null, applicableMemberFunction.Member, newArgs2.ToArray());
                }
                else
                {
                    return Expression.Call(Instance, applicableMemberFunction.Member, newArgs2.ToArray());
                }
            }

            return null;
            //return Expression.Call(primaryExpression, applicableMemberFunction.Member, args.Select(x => x.Expression));
        }


        private void InferTypes(IEnumerable<TypeInferenceBounds> bounds, Type type, Type target)
        {
            if (type.IsGenericParameter)
            {
                foreach (var typeInferenceBound in bounds)
                {
                    if (type == typeInferenceBound.TypeArgument)
                    {
                        typeInferenceBound.Bounds.Add(target);
                    }
                }
            }
            else
            {
                var x = typeof(List<>);
                var y = typeof(IEnumerable<>) == type;

                if (type.IsGenericType && target.IsGenericType && target.IsAssignableToGenericType(type))
                {
                    foreach (var argumentTypeZip in target.GetGenericArguments().Zip(type.GetGenericArguments(),
                        (type1, type2) => new { type1, type2 }))
                    {
                        InferTypes(bounds, argumentTypeZip.type2, argumentTypeZip.type1);
                    }
                }
            }

        }

        private IEnumerable<Argument> ApplyTypeInference(MethodInfo method)
        {
            var args = new List<Argument>();
            var methodParameters = method.GetParameters();

            if (methodParameters.Length != ArgumentContext.Length + (IsExtensionMethod ? 1 : 0)) return null;

            TypeInferenceBoundsList = method.GetGenericArguments()
                .Select(
                t =>
                new TypeInferenceBounds()
                {
                    TypeArgument = t,
                    Bounds = new List<Type>()
                })
                .ToList();

            // Process non-lambdas
            foreach (var parameterInfo in methodParameters.Where(
                m =>
                    m.ParameterType.IsGenericType && !m.ParameterType.IsDelegate() &&
                    !(m.ParameterType.BaseType == typeof(LambdaExpression))))
            {
                try
                {
                    Type targetType = null;
                    Expression expression;

                    if (IsExtensionMethod)
                    {
                        if (parameterInfo.Position == 0)
                        {
                            //    //InferTypes(bounds, parameterInfo, methodInvocationContext.ThisParameter);
                            expression = ThisParameter;
                        }
                        else
                        {
                            expression = Visitor.Visit(ArgumentContext[parameterInfo.Position - 1]);
                        }
                    }
                    else
                    {
                        expression = Visitor.Visit(ArgumentContext[parameterInfo.Position]);
                    }

                    var type = parameterInfo.ParameterType;
                    InferTypes(TypeInferenceBoundsList, type, expression.Type);
                    args.Add(new Argument() { Expression = expression });
                }
                catch (Exception)
                {
                    return null;
                }
            }

            // Process lambdas
            foreach (var parameterInfo in methodParameters.Where(
                m => m.ParameterType.IsGenericType && m.ParameterType.IsDelegate()))
            {
                try
                {
                    Type targetType = null;

                    Visitor.CurrentParameterInfo = parameterInfo;

                    Expression expression;

                    if (IsExtensionMethod)
                    {
                        expression = Visitor.Visit(ArgumentContext[parameterInfo.Position - 1]);
                    }
                    else
                    {
                        expression = Visitor.Visit(ArgumentContext[parameterInfo.Position]);
                    }
                    var type = parameterInfo.ParameterType;
                    InferTypes(TypeInferenceBoundsList, type, expression.Type);
                    args.Add(new Argument() { Expression = expression });
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
            return args;
        }

        public static ApplicableFunctionMember IsApplicableFunctionMember(MethodInfo F, IEnumerable<Argument> argList)
        {
            bool isMatch = true;
            bool isParamArray = false;
            bool isExpanded = false;
            List<Argument> args = argList.ToList();

            //        A function member is said to be an applicable function member with respect to an argument list A when all of the following are true:

            //•	Each argument in A corresponds to a parameter in the function member declaration as described in §7.5.1.1, and any parameter to which no argument 
            //  corresponds is an optional parameter.

            //•	For each argument in A, the parameter passing mode of the argument (i.e., value, ref, or out) is identical to the parameter passing mode of the 
            //  corresponding parameter, and
            //  o	for a value parameter or a parameter array, an implicit conversion (§6.1) exists from the argument to the type of the corresponding parameter, or
            //  o	for a ref or out parameter, the type of the argument is identical to the type of the corresponding parameter. After all, a ref or out parameter is 
            //      an alias for the argument passed.

            //For a function member that includes a parameter array, if the function member is applicable by the above rules, it is said to be applicable in its normal form. 
            //If a function member that includes a parameter array is not applicable in its normal form, the function member may instead be applicable in its expanded form:
            //•	The expanded form is constructed by replacing the parameter array in the function member declaration with zero or more value parameters of the element type 
            //  of the parameter array such that the number of arguments in the argument list A matches the total number of parameters. If A has fewer arguments than the number 
            //  of fixed parameters in the function member declaration, the expanded form of the function member cannot be constructed and is thus not applicable.

            //•	Otherwise, the expanded form is applicable if for each argument in A the parameter passing mode of the 
            //  argument is identical to the parameter passing mode of the corresponding parameter, and
            //  o	for a fixed value parameter or a value parameter created by the expansion, an implicit conversion (§6.1) exists from the type of the 
            //      argument to the type of the corresponding parameter, or
            //  o	for a ref or out parameter, the type of the argument is identical to the type of the corresponding parameter.

            // for non-named arguments:

            int argCount = 0;
            foreach (ParameterInfo pInfo in F.GetParameters())
            {
                bool haveArg = argCount < args.Count();

                if (pInfo.IsOut || pInfo.ParameterType.IsByRef)
                {
                    if (!haveArg)
                    {
                        isMatch = false;
                    }
                    else if (pInfo.IsOut)
                    {
                        if (args[argCount].ParameterPassingMode != ParameterPassingModeEnum.Out)
                        {
                            isMatch = false;
                        }
                    }
                    else if (pInfo.ParameterType.IsByRef)
                    {
                        if (args[argCount].ParameterPassingMode != ParameterPassingModeEnum.ByRef)
                        {
                            isMatch = false;
                        }
                    }

                    // Step 4 (technically)
                    // Check types if either are a ref type. Must match exactly
                    String argTypeStr = args[argCount].Expression.Type.FullName;
                    Type paramType = F.GetParameters()[argCount].ParameterType;
                    String paramTypeStr = paramType.ToString().Substring(0, paramType.ToString().Length - 1);

                    if (argTypeStr != paramTypeStr)
                    {
                        isMatch = false;
                    }

                }
                else
                {
                    if (pInfo.IsOptional)
                    {
                        // If an argument for this parameter position was specified, check its type
                        if (haveArg && !MethodResolution.HasImplicitConversion(args[argCount].Expression, args[argCount].Expression.Type, pInfo.ParameterType))
                        {
                            isMatch = false;
                        }
                    }
                    else if (pInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                    { // Check ParamArray arguments
                        isParamArray = true;

                        if (argCount < args.Count)
                        {
                            isExpanded = true;
                            var elementType = pInfo.ParameterType.GetElementType();

                            for (int j = pInfo.Position; j < args.Count; j++)
                            {
                                if (!MethodResolution.HasImplicitConversion(args[j].Expression, args[j].Expression.Type, elementType))
                                {
                                    isMatch = false;
                                }
                                argCount++;
                            }
                        }


                        break;
                    }
                    else
                    { // Checking non-optional, non-ParamArray arguments
                        if (!haveArg || !MethodResolution.HasImplicitConversion(args[argCount].Expression, args[argCount].Expression.Type, pInfo.ParameterType))
                        {
                            isMatch = false;
                        }
                    }
                }

                if (!isMatch)
                {
                    break;
                }

                argCount++;
            }

            if (isMatch && argCount < args.Count)
                isMatch = false;

            if (!isMatch) return null;

            if (isParamArray)
            {
                return new ApplicableFunctionMember() { IsExpanded = isExpanded, IsParamArray = true, Member = F };
            }

            return new ApplicableFunctionMember() { Member = F };
        }


    }
}