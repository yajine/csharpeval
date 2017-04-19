using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{ 
    public class MethodInvocationContext
    {
        public MethodInvocationContext()
        {
            Arguments = new List<ArgumentExpression>();
        }

        public bool IsExtensionMethod { get; set; }
        public bool IsStaticMethod { get; set; }
        public Expression ThisParameter { get; set; }
        public TypeOrGeneric Method { get; set; }
        public IEnumerable<MethodInfo> MethodCandidates { get; set; }
        public Type Type { get; set; }
        public Expression Instance { get; set; }
        public IList<ArgumentExpression> Arguments { get; set; }
        public bool IsCall { get; set; }
        public List<TypeInferenceBounds> TypeInferenceBoundsList { get; set; }

        public Expression GetDynamicInvokeMethodExpression()
        {
            var expArgs = new List<Expression> { Instance };

            expArgs.AddRange(Arguments.Select(x => x.Expression));

            if (IsCall)
            {
                var binderMC = Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                    Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.ResultDiscarded,
                    Method.Identifier,
                    null,
                    Type,
                    expArgs.Select(x => Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null))
                    );

                return Expression.Dynamic(binderMC, typeof(void), expArgs);
            }

            var binderM = Microsoft.CSharp.RuntimeBinder.Binder.InvokeMember(
                Microsoft.CSharp.RuntimeBinder.CSharpBinderFlags.None,
                Method.Identifier,
                null,
                Type,
                expArgs.Select(x => Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfoFlags.None, null))
                );

            return Expression.Dynamic(binderM, typeof(object), expArgs);
    }

        public Expression GetInvokeMethodExpression()
        {
            if (ExpressionHelper.IsDynamic(Instance))
            {
                return GetDynamicInvokeMethodExpression();
            }

            var appMembers = new List<ApplicableFunctionMember>();
            //IEnumerable<Argument> args = null;

            foreach (var F in MethodCandidates)
            {
                if (!F.IsGenericMethod)
                {
                    //var argsList = new List<Argument>();

                    //foreach (var argument in ArgumentContext)
                    //{
                    //    var argExpression = (ArgumentExpression)Visitor.Visit(argument);
                    //    argsList.Add(new Argument()
                    //    {
                    //        Expression = argExpression.Expression,
                    //        Name = argExpression.Name,
                    //        IsNamedArgument = argExpression.Name != null
                    //    });
                    //}

                    //args = argsList;

                    var afm = IsApplicableFunctionMember(F, Arguments);
                    if (afm != null)
                    {
                        appMembers.Add(afm);
                    }
                }
                else
                {
                    //args = ApplyTypeInference(F);

                    //if (args != null)
                    //{
                    //    var afm = IsApplicableFunctionMember(F, Arguments);
                    //    if (afm != null)
                    //    {
                    //        appMembers.Add(afm);
                    //    }
                    //}
                }
            }

            // based on TypeInferenceBoundsList, get the best types and re-visit the expressions

            Type[] typeArgs = null;
            var genericMethods = appMembers.Where(x => x.Member.IsGenericMethod).ToList();

            Dictionary<ApplicableFunctionMember, List<TypeInferrence>> methodTypeInferences = new Dictionary<ApplicableFunctionMember, List<TypeInferrence>>();

            if (genericMethods.Any() && (Method.TypeArgs == null || !Method.TypeArgs.Any()))
            {
                foreach (var genericMethod in genericMethods)
                {
                    var inferences = MethodResolution.TypeInference(genericMethod, Arguments);
                    if (inferences != null)
                    {
                        methodTypeInferences.Add(genericMethod, inferences);
                    }
                }
            }

            var applicableMemberFunction = MethodResolution.OverloadResolution(appMembers, Arguments);

            if (applicableMemberFunction != null)
            {
                var parameterInfos = applicableMemberFunction.Member.GetParameters();
                var argExps = Arguments.Select(x => x.Expression).ToList();

                ParameterInfo paramArrayParameter = parameterInfos.FirstOrDefault(p => p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0);
                List<Expression> newArgs2 = null;

                if (Method.TypeArgs != null && Method.TypeArgs.Any())
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

//        7.5.2.1 The first phase
//For each of the method arguments Ei:
//•	If Ei is an anonymous function, an explicit parameter type inference (§7.5.2.7) is made from Ei to Ti
//•	Otherwise, if Ei has a type U and xi is a value parameter then a lower-bound inference is made from U to Ti.
//•	Otherwise, if Ei has a type U and xi is a ref or out parameter then an exact inference is made from U to Ti. 
//•	Otherwise, no inference is made for this argument.
//7.5.2.2 The second phase
//The second phase proceeds as follows:
//•	All unfixed type variables Xi which do not depend on (§7.5.2.5) any Xj are fixed (§7.5.2.10).
//•	If no such type variables exist, all unfixed type variables Xi are fixed for which all of the following hold:
//o	There is at least one type variable Xj that depends on Xi
//o	Xi has a non-empty set of bounds
//•	If no such type variables exist and there are still unfixed type variables, type inference fails. 
//•	Otherwise, if no further unfixed type variables exist, type inference succeeds.
//•	Otherwise, for all arguments Ei with corresponding parameter type Ti where the output types (§7.5.2.4) contain unfixed type variables Xj but the input types (§7.5.2.3) do not, an output type inference (§7.5.2.6) is made from Ei to Ti. Then the second phase is repeated.
//7.5.2.3 Input types
//If E is a method group or implicitly typed anonymous function and T is a delegate type or expression tree type then all the parameter types of T are input types of E with type T.
//7.5.2.4  Output types
//If E is a method group or an anonymous function and T is a delegate type or expression tree type then the return type of T is an output type of E with type T.
//7.5.2.5 Dependence
//An unfixed type variable Xi depends directly on an unfixed type variable Xj if for some argument Ek with type Tk Xj occurs in an input type of Ek with type Tk and Xi occurs in an output type of Ek with type Tk.
//Xj depends on Xi if Xj depends directly on Xi or if Xi depends directly on Xk and Xk depends on Xj. Thus “depends on” is the transitive but not reflexive closure of “depends directly on”.
//7.5.2.6 Output type inferences
//An output type inference is made from an expression E to a type T in the following way:
//•	If E is an anonymous function with inferred return type  U (§7.5.2.12) and T is a delegate type or expression tree type with return type Tb, then a lower-bound inference (§7.5.2.9) is made from U to Tb.
//•	Otherwise, if E is a method group and T is a delegate type or expression tree type with parameter types T1…Tk and return type Tb, and overload resolution of E with the types T1…Tk yields a single method with return type U, then a lower-bound inference is made from U to Tb.
//•	Otherwise, if E is an expression with type U, then a lower-bound inference is made from U to T.
//•	Otherwise, no inferences are made.
//7.5.2.7 Explicit parameter type inferences
//An explicit parameter type inference is made from an expression E to a type T in the following way:
//•	If E is an explicitly typed anonymous function with parameter types U1…Uk and T is a delegate type or expression tree type with parameter types V1…Vk then for each Ui an exact inference (§7.5.2.8) is made from Ui to the corresponding Vi.
//7.5.2.8 Exact inferences
//An exact inference from a type U to a type V is made as follows:
//•	If V is one of the unfixed Xi then U is added to the set of exact bounds for Xi.
//•	Otherwise, sets V1…Vk and U1…Uk are determined by checking if any of the following cases apply:
//•	V is an array type V1[…] and U is an array type U1[…]  of the same rank
//•	V is the type V1? and U is the type U1?
//•	V is a constructed type C<V1…Vk> and U is a constructed type C<U1…Uk> 
//If any of these cases apply then an exact inference is made from each Ui to the corresponding Vi.
//•	Otherwise no inferences are made.
//7.5.2.9 Lower-bound inferences
//A lower-bound inference from a type U to a type V is made as follows:
//•	If V is one of the unfixed Xi then U is added to the set of lower bounds for Xi.
//•	Otherwise, if V is the type V1? and U is the type U1? then a lower bound inference is made from U1 to V1.
//•	Otherwise, sets U1…Uk and V1…Vk are determined by checking if any of the following cases apply:
//•	V is an array type V1[…]and U is an array type U1[…] (or a type parameter whose effective base type is U1[…]) of the same rank
//•	V is one of IEnumerable<V1>, ICollection<V1> or IList<V1> and U is a one-dimensional array type U1[](or a type parameter whose effective base type is U1[]) 
//•	V is a constructed class, struct, interface or delegate type C<V1…Vk> and there is a unique type C<U1…Uk> such that U (or, if U is a type parameter, its effective base class or any member of its effective interface set) is identical to, inherits from (directly or indirectly), or implements (directly or indirectly) C<U1…Uk>.
//(The “uniqueness” restriction means that in the case interface C<T>{} class U: C<X>, C<Y>{}, then no inference is made when inferring from U to C<T> because U1 could be X or Y.)
//If any of these cases apply then an inference is made from each Ui to the corresponding Vi as follows:
//•	If  Ui is not known to be a reference type then an exact inference is made
//•	Otherwise, if U is an array type then a lower-bound inference is made
//•	Otherwise, if V is C<V1…Vk> then inference depends on the i-th type parameter of C:
//•	If it is covariant then a lower-bound inference is made.
//•	If it is contravariant then an upper-bound inference is made.
//•	If it is invariant then an exact inference is made.
//•	Otherwise, no inferences are made.
//7.5.2.10 Upper-bound inferences
//An upper-bound inference from a type U to a type V is made as follows:
//•	If V is one of the unfixed Xi then U is added to the set of upper bounds for Xi.
//•	Otherwise, sets V1…Vk and U1…Uk are determined by checking if any of the following cases apply:
//•	U is an array type U1[…]and V is an array type V1[…]of the same rank
//•	U is one of IEnumerable<Ue>, ICollection<Ue> or IList<Ue> and V is a one-dimensional array type Ve[]
//•	U is the type U1? and V is the type V1?
//•	U is constructed class, struct, interface or delegate type C<U1…Uk> and V is a class, struct, interface or delegate type which is identical to, inherits from (directly or indirectly), or implements (directly or indirectly) a unique type C<V1…Vk>
//(The “uniqueness” restriction means that if we have interface C<T>{} class V<Z>: C<X<Z>>, C<Y<Z>>{}, then no inference is made when inferring from C<U1> to V<Q>. Inferences are not made from U1 to either X<Q> or Y<Q>.)
//If any of these cases apply then an inference is made from each Ui to the corresponding Vi as follows:
//•	If  Ui is not known to be a reference type then an exact inference is made
//•	Otherwise, if V is an array type then an upper-bound inference is made
//•	Otherwise, if U is C<U1…Uk> then inference depends on the i-th type parameter of C:
//•	If it is covariant then an upper-bound inference is made.
//•	If it is contravariant then a lower-bound inference is made.
//•	If it is invariant then an exact inference is made.
//•	Otherwise, no inferences are made.	
//7.5.2.11 Fixing
//An unfixed type variable Xi with a set of bounds is fixed as follows:
//•	The set of candidate types Uj starts out as the set of all types in the set of bounds for Xi.
//•	We then examine each bound for Xi in turn: For each exact bound U of Xi all types Uj which are not identical to U are removed from the candidate set. For each lower bound U of Xi all types Uj to which there is not an implicit conversion from U are removed from the candidate set. For each upper bound U of Xi all types Uj from which there is not an implicit conversion to U are removed from the candidate set.
//•	If among the remaining candidate types Uj there is a unique type V from which there is an implicit conversion to all the other candidate types, then Xi is fixed to V.
//•	Otherwise, type inference fails.
//7.5.2.12 Inferred return type
//The inferred return type of an anonymous function F is used during type inference and overload resolution. The inferred return type can only be determined for an anonymous function where all parameter types are known, either because they are explicitly given, provided through an anonymous function conversion or inferred during type inference on an enclosing generic method invocation. 
//The inferred result type is determined as follows:
//•	If the body of F is an expression that has a type, then the inferred result type of F is the type of that expression.
//•	If the body of F is a block and the set of expressions in the block’s return statements has a best common type T (§7.5.2.14), then the inferred result type of F is T.
//•	Otherwise, a result type cannot be inferred for F.
//The inferred return type is determined as follows:
//•	If F is async and the body of F is either an expression classified as nothing (§7.1), or a statement block where no return statements have expressions, the inferred return type is System.Threading.Tasks.Task
//•	If F is async and has an inferred result type T, the inferred return type is System.Threading.Tasks.Task<T>.
//•	If F is non-async and has an inferred result type T, the inferred return type is T.
//•	Otherwise a return type cannot be inferred for F.
//As an example of type inference involving anonymous functions, consider the Select extension method declared in the System.Linq.Enumerable class:
//namespace System.Linq
//{
//	public static class Enumerable
//	{
//		public static IEnumerable<TResult> Select<TSource,TResult>(
//			this IEnumerable<TSource> source,
//			Func<TSource,TResult> selector)
//		{
//			foreach (TSource element in source) yield return selector(element);
//		}
//	}
//}
//Assuming the System.Linq namespace was imported with a using clause, and given a class Customer with a Name property of type string, the Select method can be used to select the names of a list of customers:
//List<Customer> customers = GetCustomerList();
//IEnumerable<string> names = customers.Select(c => c.Name);
//The extension method invocation (§7.6.5.2) of Select is processed by rewriting the invocation to a static method invocation:
//IEnumerable<string> names = Enumerable.Select(customers, c => c.Name);
//Since type arguments were not explicitly specified, type inference is used to infer the type arguments. First, the customers argument is related to the source parameter, inferring T to be Customer. Then, using the anonymous function type inference process described above, c is given type Customer, and the expression c.Name is related to the return type of the selector parameter, inferring S to be string. Thus, the invocation is equivalent to
//Sequence.Select<Customer,string>(customers, (Customer c) => c.Name)
//and the result is of type IEnumerable<string>.
//The following example demonstrates how anonymous function type inference allows type information to “flow” between arguments in a generic method invocation. Given the method:
//static Z F<X,Y,Z>(X value, Func<X,Y> f1, Func<Y,Z> f2) {
//	return f2(f1(value));
//}
//Type inference for the invocation:
//double seconds = F("1:15:30", s => TimeSpan.Parse(s), t => t.TotalSeconds);
//proceeds as follows: First, the argument "1:15:30" is related to the value parameter, inferring X to be string. Then, the parameter of the first anonymous function, s, is given the inferred type string, and the expression TimeSpan.Parse(s) is related to the return type of f1, inferring Y to be System.TimeSpan. Finally, the parameter of the second anonymous function, t, is given the inferred type System.TimeSpan, and the expression t.TotalSeconds is related to the return type of f2, inferring Z to be double. Thus, the result of the invocation is of type double.


        

        public static ApplicableFunctionMember IsApplicableFunctionMember(MethodInfo F, IList<ArgumentExpression> argList)
        {
            bool isMatch = true;
            bool isParamArray = false;
            bool isExpanded = false;
            //List<Argument> args = argList.ToList();

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
                bool haveArg = argCount < argList.Count();

                if (pInfo.IsOut || pInfo.ParameterType.IsByRef)
                {
                    if (!haveArg)
                    {
                        isMatch = false;
                    }
                    else if (pInfo.IsOut)
                    {
                        if (argList[argCount].ParameterPassingMode != ParameterPassingModeEnum.Out)
                        {
                            isMatch = false;
                        }
                    }
                    else if (pInfo.ParameterType.IsByRef)
                    {
                        if (argList[argCount].ParameterPassingMode != ParameterPassingModeEnum.ByRef)
                        {
                            isMatch = false;
                        }
                    }

                    // Step 4 (technically)
                    // Check types if either are a ref type. Must match exactly
                    String argTypeStr = argList[argCount].Expression.Type.FullName;
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
                        if (haveArg && !MethodResolution.HasImplicitConversion(argList[argCount].Expression, argList[argCount].Expression.Type, pInfo.ParameterType))
                        {
                            isMatch = false;
                        }
                    }
                    else if (pInfo.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
                    { // Check ParamArray arguments
                        isParamArray = true;

                        if (argCount < argList.Count)
                        {
                            isExpanded = true;
                            var elementType = pInfo.ParameterType.GetElementType();

                            for (int j = pInfo.Position; j < argList.Count; j++)
                            {
                                if (!MethodResolution.HasImplicitConversion(argList[j].Expression, argList[j].Expression.Type, elementType))
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
                        if (!haveArg || !MethodResolution.HasImplicitConversion(argList[argCount].Expression, argList[argCount].Expression.Type, pInfo.ParameterType))
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

            if (isMatch && argCount < argList.Count)
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