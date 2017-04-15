using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using ExpressionEvaluator.Parser.Expressions;
using Microsoft.CSharp.RuntimeBinder;
using Binder = Microsoft.CSharp.RuntimeBinder.Binder;

namespace ExpressionEvaluator.Parser
{
    internal class ExpressionHelper
    {
        private static readonly Type StringType = typeof(string);

        private static Expression ConvertToString(Expression instance)
        {
            return Expression.Call(typeof(Convert), "ToString", null, instance);
        }

        public static Expression Add(Expression le, Expression re)
        {
            if (le.Type == StringType || re.Type == StringType)
            {
                if (le.Type != typeof(string)) le = ConvertToString(le);
                if (re.Type != typeof(string)) re = ConvertToString(re);
                return Expression.Add(le, re, StringType.GetMethod("Concat", new Type[] { le.Type, re.Type }));
            }
            return Expression.Add(le, re);
        }

        public static Expression GetPropertyIndex(Expression le, IEnumerable<Expression> args)
        {
            Expression instance = null;
            Type type = null;

            var isDynamic = false;
            var isRuntimeType = false;

            if (le.Type.Name == "RuntimeType")
            {
                isRuntimeType = true;
                type = ((Type)((ConstantExpression)le).Value);
            }
            else
            {
                type = le.Type;
                instance = le;
                isDynamic = IsDynamic(le);
            }

            if (isDynamic)
            {
                var expArgs = new List<Expression>() { le };
                expArgs.AddRange(args);

                var indexedBinder = Binder.GetIndex(
                    CSharpBinderFlags.None,
                    type,
                    expArgs.Select(x => CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null))
                    );

                return Expression.Dynamic(indexedBinder, typeof(object), expArgs);

            }
            else
            {
                if (type.BaseType == typeof(System.Array))
                {
                    return Expression.ArrayAccess(le, args);
                }

                var defaultMembers = type.GetCustomAttributes(typeof(DefaultMemberAttribute), true);

                if (defaultMembers.Length > 0)
                {
                    foreach (var defaultMember in defaultMembers)
                    {
                        var pi = type.GetProperty(((DefaultMemberAttribute)defaultMember).MemberName, args.Select(x => x.Type).ToArray());

                        if (pi == null)
                        {
                            throw new CompilerException(string.Format("No default member found on type '{0}' that matches the given arguments", type.Name));
                        }

                        return Expression.Property(le, pi, args);
                    }
                }


                var interfaces = le.Type.GetInterfaces();

                foreach (var @interface in interfaces)
                {

                    foreach (PropertyInfo pi in @interface.GetProperties())
                    {
                        var indexParameters = pi.GetIndexParameters();

                        if (indexParameters.Length > 0)
                        {
                            var position = 0;
                            var argMatch = 0;
                            foreach (var expression in args)
                            {

                                var indexParameter = indexParameters[position];
                                if (indexParameter.Position == position && indexParameter.ParameterType == expression.Type)
                                {
                                    argMatch++;
                                }
                                position++;
                            }

                            if (argMatch == indexParameters.Length)
                            {
                                return Expression.Property(le, pi, args);
                            }
                            //var indexer = le.Type.GetProperties().SingleOrDefault(x => x.Name == "Item");
                            //if (indexer == null)
                            //{
                            //    var me = ((MemberExpression)le);
                            //    throw new CompilerException(string.Format("The member '{0}' does not implement the index accessor", me.Member.Name));
                            //}
                        }
                    }
                }

                //if (interfaces.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
                //    interfaces.Any(t => t == typeof(IDictionary)))
                //{
                //    var indexer = le.Type.GetProperties().SingleOrDefault(x => x.Name == "Item");
                //    if (indexer == null)
                //    {
                //        var me = ((MemberExpression)le);
                //        throw new CompilerException(string.Format("The member '{0}' does not implement the index accessor", me.Member.Name));
                //    }
                //    return Expression.Property(le, indexer, args);
                //}

                // Alternative, note that we could even look for the type of parameters, if there are indexer overloads.
                PropertyInfo indexerpInfo = (from p in le.Type.GetDefaultMembers().OfType<PropertyInfo>()
                                                 // This check is probably useless. You can't overload on return value in C#.
                                             where p.PropertyType == typeof(int)
                                             let q = p.GetIndexParameters()
                                             // Here we can search for the exact overload. Length is the number of "parameters" of the indexer, and then we can check for their type.
                                             where q.Length == args.Count() && q.Join(args, r => r.ParameterType, a => a.Type, (info, expression) => info).Count() == q.Count()
                                             select p).Single();

                return Expression.Property(le, indexerpInfo, args);
            }

        }

        public static Expression Assign(Expression le, Expression re, Dictionary<string, Type> dynamicTypeLookup = null)
        {
            var type = le.Type;
            var isDynamic = type.IsDynamicOrObject();

            if (le.NodeType == System.Linq.Expressions.ExpressionType.Call)
            {
                var mc = (MethodCallExpression)le;
                if (mc.Method.Name == "getVar")
                {
                    var ce = (ConstantExpression)mc.Arguments[0];
                    var method1 = new TypeOrGeneric() { Identifier = "setVar" };
                    var args1 = new List<Expression>() { Expression.Constant(ce.Value, typeof(string)), re };
                    if (dynamicTypeLookup != null)
                    {
                        if (dynamicTypeLookup.ContainsKey((string)ce.Value))
                        {
                            dynamicTypeLookup[(string)ce.Value] = re.Type;
                        }
                        else
                        {
                            dynamicTypeLookup.Add((string)ce.Value, re.Type);
                        }
                    }
                    return GetMethod(mc.Object, method1, args1.Select(x => new ArgumentExpression() { Expression = x }).ToList(), false, null);
                }
            }

            if (type.IsDynamic() || le.NodeType == System.Linq.Expressions.ExpressionType.Dynamic)
            {
                var dle = (DynamicExpression)le;
                var membername = ((GetMemberBinder)dle.Binder).Name;
                var instance = dle.Arguments[0];

                var binder = Binder.SetMember(
                    CSharpBinderFlags.None,
                    membername,
                    type,
                    new[]
                        {
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                        }
                    );

                return Expression.Dynamic(binder, typeof(object), instance, re);

            }

            TypeConversion.ImplicitConversion(ref re, le.Type);

            return Expression.Assign(le, re);
        }

        public static Type[] InferTypes(MethodInfo methodInfo, List<Expression> args)
        {
            var parameterinfoes = methodInfo.GetParameters();

            var X = methodInfo.GetGenericArguments().Select(x => new TypeVariable() { Name = x.Name }).ToArray();
            var S = new Type[X.Length];
            var T = parameterinfoes.Select(p => p.ParameterType).ToList();

            var lookup = X.ToDictionary(x => x.Name);

            // Phase 1
            // 7.5.2.1
            // For each of the method arguments ei:
            // An explicit argument type inference (§26.3.3.7) is made from ei with type Ti if ei is a lambda expression, an anonymous method, or a method group.
            // An output type inference (§26.3.3.6) is made from ei with type Ti if ei is not a lambda expression, an anonymous method, or a method group.
            for (var i = 0; i < args.Count; i++)
            {
                var ei = args[i];
                var Ti = T[i];
                if (ei.NodeType == System.Linq.Expressions.ExpressionType.Lambda)
                {
                    var lambda = ((LambdaExpression)ei);
                    // 7.5.2.7 Explicit argument type inferences
                    // An explicit argument type inference is made from an expression e with type T in the following way:
                    // If e is an explicitly typed lambda expression or anonymous method with argument types U1...Uk and T is a delegate type with parameter types V1...Vk then for each Ui an exact inference (§26.3.3.8) is made from Ui for the corresponding Vi.

                    var x = lambda.Parameters.Select(p => p.Type).Zip(Ti.GetGenericArguments(), (type, type1) =>
                        {
                            ExactInference(type, type1, lookup);
                            return 1;
                        }).ToList();


                }
                else
                {
                    // An output type inference is made from an expression e with type T in the following way:
                    // If e is a lambda or anonymous method with inferred return type U (§26.3.3.11) and T is a delegate type with return type Tb, then a lower-bound inference (§26.3.3.9) is made from U for Tb.
                    // Otherwise, if e is a method group and T is a delegate type with parameter types T1...Tk and overload resolution of e with the types T1...Tk yields a single method with return type U, then a lower-bound inference is made from U for Tb.
                    // Otherwise, if e is an expression with type U, then a lower-bound inference is made from U for T.
                    LowerBoundInference(ei.Type, Ti, lookup);

                    // Otherwise, no inferences are made.
                }
            }






            // Phase 2
            // (26.3.3.2)
            // 

            return null;
        }

        public static void ExactInference(Type U, Type V, Dictionary<string, TypeVariable> lookup)
        {
            // 7.5.2.8 Exact inferences
            // An exact inference from a type U for a type V is made as follows:
            //var U = e.Type;
            //var V = forType;
            //If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
            TypeVariable tv;
            if (lookup.TryGetValue(V.Name, out tv))
            {
                if (!tv.IsFixed) tv.Bounds.Add(U);
            }
            // Otherwise, if U is an array type Ue[...] and V is an array type Ve[...] of the same rank then an exact inference from Ue to Ve is made.
            // Otherwise, if V is a constructed type C<V1...Vk> and U is a constructed type C<U1...Uk> then an exact inference is made from each Ui to the corresponding Vi.
            // Otherwise, no inferences are made.
        }

        public static void LowerBoundInference(Type U, Type V, Dictionary<string, TypeVariable> lookup)
        {
            //7.5.2.9 Lower-bound inferences
            //A lower-bound inference from a type U for a type V is made as follows:
            //var U = e.Type;
            //var V = forType;
            //If V is one of the unfixed Xi then U is added to the set of bounds for Xi.
            TypeVariable tv;
            if (lookup.TryGetValue(V.Name, out tv))
            {
                if (!tv.IsFixed) tv.Bounds.Add(U);
            }
            //Otherwise if U is an array type Ue[...] and V is either an array type Ve[...] of the same rank, 
            if ((U.IsArray && V.IsArray && U.GetArrayRank() == V.GetArrayRank() ||
                 // or if U is a one-dimensional array type Ue[]and V is one of IEnumerable<Ve>, ICollection<Ve> or IList<Ve> then:
                 U.IsArray && U.GetArrayRank() == 1 &&
                 (V.IsAssignableFrom(typeof(IEnumerable<>)) || V.IsAssignableFrom(typeof(ICollection<>)) ||
                  V.IsAssignableFrom(typeof(IList<>)))
                ))
            {
                //If Ue is known to be a reference type then a lower-bound inference from Ue to Ve is made.
                var x = 1;
                //Otherwise, an exact inference from Ue to Ve is made.
            }
            //Otherwise if V is a constructed type C<V1...Vk> and there is a unique set of types U1...Uk such that a standard implicit conversion exists from U to C<U1...Uk> then an exact inference is made from each Ui for the corresponding Vi.
            if (V.IsGenericType && U.IsGenericType)
            {
                var x = U.GetGenericArguments().Zip(V.GetGenericArguments(), (type, type1) =>
                    {
                        ExactInference(type, type1, lookup);
                        return 1;
                    }).ToList();

            }
            //Otherwise, no inferences are made.
        }

        private static bool IsDynamic(Expression expr)
        {
            return (expr.NodeType == System.Linq.Expressions.ExpressionType.Dynamic) || expr.Type.IsDynamic() || (expr.NodeType == System.Linq.Expressions.ExpressionType.Call && ((MethodCallExpression)expr).Method.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(DynamicAttribute), true).Length > 0);
        }

        public static Expression GetProperty(Expression le, string membername)
        {
            // remove leading dot
            //membername = membername.Substring(1);

            Expression instance = null;
            Type type = null;

            var isDynamic = false;
            var isRuntimeType = false;

            if (le.Type.Name == "RuntimeType")
            {
                isRuntimeType = true;
                type = ((Type)((ConstantExpression)le).Value);
            }
            else
            {

                type = le.Type;
                instance = le;
                isDynamic = IsDynamic(le);

                if (!isDynamic)
                {
                    var prop = type.GetProperty(membername);
                    if (prop != null)
                    {
                        if (prop.GetCustomAttributes(false).Any(x => x.GetType() == typeof(DynamicAttribute)))
                        {
                            isDynamic = true;
                        }
                    }
                    else
                    {
                        var fieldInfo = type.GetField(membername);
                        if (fieldInfo != null)
                        {
                            if (fieldInfo.GetCustomAttributes(false).Any(x => x.GetType() == typeof(DynamicAttribute)))
                            {
                                isDynamic = true;
                            }
                        }
                    }
                }

            }

            if (isDynamic)
            {
                var binder = Binder.GetMember(
                    CSharpBinderFlags.None,
                    membername,
                    type,
                    new[] { CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null) }
                    );

                Expression result = Expression.Dynamic(binder, typeof(object), instance);

                return result;
            }
            else
            {
                Expression exp = null;

                var propertyInfo = type.GetProperty(membername);
                if (propertyInfo != null)
                {
                    exp = Expression.Property(instance, propertyInfo);

                    if (propertyInfo.GetCustomAttributes(typeof(ExpressionContainerAttribute), false).Any())
                    {
                        throw new ExpressionContainerException(exp);
                        //var x = propertyInfo.GetValue(((MemberExpression)instance).Member, BindingFlags.Instance, null, null, null);
                    }

                }
                else
                {
                    var fieldInfo = type.GetField(membername);
                    if (fieldInfo != null)
                    {
                        exp = Expression.Field(instance, fieldInfo);
                    }
                }

                return exp;
            }

            throw new Exception();
        }

        public static Expression GetMethod(Expression le,
            TypeOrGeneric member,
            IList<ArgumentExpression> args,
            bool isCall,
            CompilationContext context
            )
        {
            Expression instance = null;
            Type type = null;

            var isDynamic = false;
            var isRuntimeType = false;

            var membername = member.Identifier;
            if (typeof(Type).IsAssignableFrom(le.Type))
            {
                isRuntimeType = true;
                type = ((Type)((ConstantExpression)le).Value);
            }
            else
            {
                type = le.Type;
                instance = le;
                isDynamic = IsDynamic(le);
            }

            if (isDynamic)
            {
                var expArgs = new List<Expression> { instance };

                expArgs.AddRange(args.Select(x => x.Expression));

                if (isCall)
                {
                    var binderMC = Binder.InvokeMember(
                        CSharpBinderFlags.ResultDiscarded,
                        membername,
                        null,
                        type,
                        expArgs.Select(x => CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null))
                        );

                    return Expression.Dynamic(binderMC, typeof(void), expArgs);
                }

                var binderM = Binder.InvokeMember(
                    CSharpBinderFlags.None,
                    membername,
                    null,
                    type,
                    expArgs.Select(x => CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null))
                    );

                return Expression.Dynamic(binderM, typeof(object), expArgs);
            }
            else
            {
                var method = MethodInvokeExpression(type, instance, member, args);

                if (method == null)
                {
                    var extensionmethodArgs = new List<ArgumentExpression>() { new ArgumentExpression() { Expression = instance } };
                    extensionmethodArgs.AddRange(args);

                    foreach (var @namespace in context.Namespaces)
                    {
                        foreach (var assembly in context.Assemblies)
                        {
                            var q = from t in assembly.GetTypes()
                                    where t.IsClass && t.Namespace == @namespace
                                    select t;
                            foreach (var t in q)
                            {
                                // Should try an Extension Method call here...
                                method = MethodInvokeExpression(t, null, member, extensionmethodArgs);
                                if (method != null)
                                    return method;
                            }
                        }
                    }
                }

                return method;
            }

            return null;
        }




        public static Expression MethodInvokeExpression(Type type, Expression instance, TypeOrGeneric member,
                                                   IList<ArgumentExpression> args)
        {
            var candidates = MethodResolution.GetCandidateMembers(type, member.Identifier);

            var applicableMembers = MethodResolution.GetApplicableMembers(candidates, args).ToList();

            return ResolveApplicableMembers(type, instance, applicableMembers, member, args);
        }

        public static Expression ResolveApplicableMembers(Type type, Expression instance, IEnumerable<ApplicableFunctionMember> applicableMembers, TypeOrGeneric member, IList<ArgumentExpression> args)
        {
            Type[] typeArgs = null;
            var genericMethods = applicableMembers.Where(x => x.Member.IsGenericMethod).ToList();

            Dictionary<ApplicableFunctionMember, List<TypeInferrence>> methodTypeInferences = new Dictionary<ApplicableFunctionMember, List<TypeInferrence>>();

            if (genericMethods.Any() && member.TypeArgs == null)
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

            var applicableMemberFunction = MethodResolution.OverloadResolution(applicableMembers, args);

            if (applicableMemberFunction != null)
            {
                var parameterInfos = applicableMemberFunction.Member.GetParameters();
                var argExps = args.Select(x => x.Expression).ToList();

                ParameterInfo paramArrayParameter = parameterInfos.FirstOrDefault(p => p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0);
                List<Expression> newArgs2 = null;

                if (member.TypeArgs != null)
                {
                    typeArgs = member.TypeArgs.ToArray();
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
                    if (instance == null)
                    {
                        return Expression.Call(type, member.Identifier, typeArgs, newArgs2.ToArray());
                    }
                    else
                    {
                        return Expression.Call(instance, member.Identifier, typeArgs, newArgs2.ToArray());
                    }
                }


                return Expression.Call(instance, applicableMemberFunction.Member, newArgs2.ToArray());
            }


            return null;

        }

        //private static Expression OldMethodHandler(Type type, Expression instance, TypeOrGeneric member,
        //                                           List<Expression> args)
        //{
        //    var argTypes = args.Select(x => x.Type).ToArray();
        //    var membername = member.Identifier;

        //    // Look for an exact match
        //    var methodInfo = type.GetMethod(membername, argTypes);

        //    if (methodInfo == null)
        //    {
        //        methodInfo = type.GetInterfaces()
        //                         .Select(m => m.GetMethod(membername, argTypes)).FirstOrDefault(i => i != null);
        //    }

        //    if (methodInfo != null)
        //    {
        //        var parameterInfos = methodInfo.GetParameters();

        //        for (int i = 0; i < parameterInfos.Length; i++)
        //        {
        //            args[i] = TypeConversion.Convert(args[i], parameterInfos[i].ParameterType);
        //        }

        //        return Expression.Call(instance, methodInfo, args);
        //    }

        //    // assume params

        //    var methodInfos = type.GetMethods().Where(x => x.Name == membername);
        //    var matchScore = new List<Tuple<MethodInfo, int, bool>>();

        //    foreach (var info in methodInfos.OrderByDescending(m => m.GetParameters().Count()))
        //    {
        //        var parameterInfos = info.GetParameters();
        //        var lastParam = parameterInfos.Last();
        //        var newArgs = args.Take(parameterInfos.Length - 1).ToList();
        //        var paramArgs = args.Skip(parameterInfos.Length - 1).ToList();
        //        var hasParams = false;
        //        int i = 0;
        //        int k = 0;

        //        foreach (var expression in newArgs)
        //        {
        //            k += TypeConversion.CanConvert(expression.Type, parameterInfos[i].ParameterType);
        //            i++;
        //        }

        //        if (k > 0)
        //        {
        //            if (Attribute.IsDefined(lastParam, typeof(ParamArrayAttribute)))
        //            {
        //                k +=
        //                    paramArgs.Sum(
        //                        arg => TypeConversion.CanConvert(arg.Type, lastParam.ParameterType.GetElementType()));
        //                hasParams = true;
        //            }
        //        }

        //        matchScore.Add(new Tuple<MethodInfo, int, bool>(info, k, hasParams));
        //    }

        //    var info2 = matchScore.OrderBy(x => x.Item2).FirstOrDefault(x => x.Item2 >= 0 && x.Item3);

        //    if (info2 != null)
        //    {
        //        var parameterInfos2 = info2.Item1.GetParameters();
        //        var lastParam2 = parameterInfos2.Last();
        //        var newArgs2 = args.Take(parameterInfos2.Length - 1).ToList();
        //        var paramArgs2 = args.Skip(parameterInfos2.Length - 1).ToList();


        //        for (int i = 0; i < parameterInfos2.Length - 1; i++)
        //        {
        //            newArgs2[i] = TypeConversion.Convert(newArgs2[i], parameterInfos2[i].ParameterType);
        //        }

        //        var targetType = lastParam2.ParameterType.GetElementType();

        //        if (targetType == null)
        //        {
        //            var ga = lastParam2.ParameterType.GetGenericArguments();
        //            if (ga.Any())
        //            {
        //                targetType = ga.Single();
        //            }
        //        }

        //        if (targetType != null)
        //        {
        //            newArgs2.Add(Expression.NewArrayInit(targetType,
        //                                                 paramArgs2.Select(x => TypeConversion.Convert(x, targetType))));
        //        }

        //        return Expression.Call(instance, info2.Item1, newArgs2);
        //    }

        //    info2 = matchScore.OrderBy(x => x.Item2).FirstOrDefault(x => x.Item2 >= 0);

        //    if (info2 != null)
        //    {
        //        var parameterInfos2 = info2.Item1.GetParameters();
        //        var newArgs2 = args.Take(parameterInfos2.Length).ToList();


        //        for (int i = 0; i < parameterInfos2.Length; i++)
        //        {
        //            newArgs2[i] = TypeConversion.Convert(newArgs2[i], parameterInfos2[i].ParameterType);
        //        }

        //        return Expression.Call(instance, info2.Item1, newArgs2);
        //    }

        //    return null;
        //}

        public static Expression ParseRealLiteral(string token)
        {
            var m = Regex.Match(token, "(-?(?:\\d+)?(?:.\\d+)?)(d|f|m)?", RegexOptions.IgnoreCase);
            var suffix = "";

            Type ntype = null;
            object val = null;

            if (m.Success)
            {
                token = m.Groups[1].Value;

                if (m.Groups[2].Success)
                {
                    suffix = m.Groups[2].Value;
                }


                if (suffix.Length > 0)
                {
                    switch (suffix.ToLower())
                    {
                        case "d":
                            ntype = typeof(Double);
                            val = double.Parse(token, CultureInfo.InvariantCulture);
                            break;
                        case "f":
                            ntype = typeof(Single);
                            val = float.Parse(token, CultureInfo.InvariantCulture);
                            break;
                        case "m":
                            ntype = typeof(Decimal);
                            val = decimal.Parse(token, CultureInfo.InvariantCulture);
                            break;
                    }

                }
                else
                {
                    ntype = typeof(Double);
                    val = double.Parse(token, CultureInfo.InvariantCulture);
                }
            }
            return Expression.Constant(val, ntype);
        }


        public static Expression ParseIntLiteral(string token)
        {
            var m = Regex.Match(token, "(-?\\d+)(ul|lu|l|u)?", RegexOptions.IgnoreCase);
            string suffix = "";

            if (m.Success)
            {
                token = m.Groups[1].Value;

                if (m.Groups[2].Success)
                {
                    suffix = m.Groups[2].Value;
                }

                Type ntype = null;
                object val = null;

                if (suffix.Length > 0)
                {
                    switch (suffix.ToLower())
                    {
                        case "l":
                            ntype = typeof(Int64);
                            val = long.Parse(token, CultureInfo.InvariantCulture);
                            break;
                        case "u":
                            ntype = typeof(UInt32);
                            val = uint.Parse(token, CultureInfo.InvariantCulture);
                            break;
                        case "ul":
                        case "lu":
                            ntype = typeof(UInt64);
                            val = ulong.Parse(token, CultureInfo.InvariantCulture);
                            break;
                    }

                }
                else
                {
                    int intval;
                    uint uintval;
                    long longval;
                    ulong ulongval;
                    if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out intval))
                    {
                        val = intval;
                        ntype = typeof(Int32);
                    }
                    else if (uint.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out uintval))
                    {
                        val = uintval;
                        ntype = typeof(UInt32);
                    }
                    else if (long.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out longval))
                    {
                        val = longval;
                        ntype = typeof(Int64);
                    }
                    else if (ulong.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulongval))
                    {
                        val = ulongval;
                        ntype = typeof(UInt64);
                    }
                    else
                    {
                        throw new Exception("Ambiguous invocation: -(decimal), -(double), -(float)");
                    }


                }
                return Expression.Constant(val, ntype);
            }
            throw new Exception("Invalid int literal");
        }

        public static Expression ParseDateLiteral(string token)
        {
            token = token.Substring(1, token.Length - 2);
            return Expression.Constant(DateTime.Parse(token));
        }

        public static Expression ParseHexLiteral(string token)
        {
            var m = Regex.Match(token, "(0[x][0-9|a-f]+)(l|u|ul|lu)?", RegexOptions.IgnoreCase);
            string suffix = "";

            if (m.Success)
            {
                token = m.Groups[1].Value;

                if (m.Groups[2].Success)
                {
                    suffix = m.Groups[2].Value;
                }

                Type ntype = typeof(Int32);
                object val = null;

                if (suffix.Length > 0)
                {
                    switch (suffix.ToLower())
                    {
                        case "l":
                            ntype = typeof(Int64);
                            val = Convert.ToInt64(token, 16);
                            break;
                        case "u":
                            ntype = typeof(UInt32);
                            val = Convert.ToUInt32(token, 16);
                            break;
                        case "ul":
                        case "lu":
                            ntype = typeof(UInt64);
                            val = Convert.ToUInt64(token, 16);
                            break;
                    }
                }
                else
                {
                    ntype = typeof(Int32);
                    val = Convert.ToInt32(token, 16);
                }

                return Expression.Constant(val, ntype);
            }
            return null;
        }


        public static Expression ParseCharLiteral(string token)
        {
            token = token.Substring(2, token.Length - 3);
            return Expression.Constant(Convert.ToChar(token), typeof(char));
        }

        private const string HexChars = "0123456789abcdefABCDEF";

        public static string Unescape(string txt)
        {
            if (string.IsNullOrEmpty(txt)) { return txt; }
            StringBuilder retval = new StringBuilder(txt.Length);
            for (int ix = 0; ix < txt.Length;)
            {
                int jx = txt.IndexOf('\\', ix);
                if (jx < 0 || jx == txt.Length - 1) jx = txt.Length;
                retval.Append(txt, ix, jx - ix);
                if (jx >= txt.Length) break;
                var skip = 2;
                switch (txt[jx + 1])
                {
                    case '\'': retval.Append('\''); break;  // Single quote
                    case '"': retval.Append('\"'); break;  // Double quote
                    case '\\': retval.Append('\\'); break; // Don't escape
                    case '0': retval.Append('\0'); break;  // null character
                    case 'a': retval.Append('\a'); break;  // Alert
                    case 'b': retval.Append('\b'); break;  // Backspace
                    case 'f': retval.Append('\f'); break;  // Form feed
                    case 'n': retval.Append('\n'); break;  // New line
                    case 'r': retval.Append('\r'); break;  // Carriage return
                    case 't': retval.Append('\t'); break;  // Horizontal Tab
                    case 'u': // Unicode Character
                        var unicode = "";
                        {
                            int i;
                            for (i = 0; jx + 2 + i < txt.Length && i < 4; i++)
                            {
                                var chr = txt[jx + 2 + i];
                                if (HexChars.Contains(chr))
                                {
                                    unicode = unicode + chr;
                                }
                                else
                                {
                                    throw new Exception("Invalid literal character");
                                }
                            }
                            if (i < 4)
                            {
                                throw new Exception("Invalid literal character");
                            }
                            skip += 4;
                            retval.Append(char.ToString((char)ushort.Parse(unicode, NumberStyles.AllowHexSpecifier)));
                        }
                        break;
                    case 'x': // Hex
                        {
                            var hex = "";
                            var i = 0;
                            while (jx + 2 + i < txt.Length)
                            {
                                var chr = txt[jx + 2 + i];
                                if (HexChars.Contains(chr))
                                {
                                    hex = hex + chr;
                                }
                                else
                                {
                                    break;
                                }
                                i++;
                            }
                            if (i == 0)
                            {
                                throw new Exception("Invalid literal character");
                            }
                            skip += i;
                            retval.Append(char.ToString((char)ushort.Parse(hex, NumberStyles.AllowHexSpecifier)));
                        }
                        break;

                    case 'v': retval.Append('\v'); break;  // Vertical Tab
                    default:
                        throw new Exception("Invalid literal character");
                }
                ix = jx + skip;
            }
            return retval.ToString();
        }

        public static Expression ParseStringLiteral(string token)
        {
            token = token.Substring(1, token.Length - 2);
            token = Unescape(token);
            return Expression.Constant(token, typeof(string));
        }

        public static Expression UnaryOperator(Expression le, System.Linq.Expressions.ExpressionType expressionType)
        {
            // perform implicit conversion on known types

            if (le.Type.IsDynamicOrObject())
            {
                return DynamicUnaryOperator(le, expressionType);
            }
            else
            {
                return GetUnaryOperator(le, expressionType);
            }
        }

        public static Expression BinaryOperator(Expression le, Expression re, System.Linq.Expressions.ExpressionType expressionType)
        {
            // perform implicit conversion on known types

            if (IsDynamic(le) || IsDynamic(re))
            {
                if (expressionType == System.Linq.Expressions.ExpressionType.OrElse)
                {
                    le = Expression.IsTrue(Expression.Convert(le, typeof(bool)));
                    expressionType = System.Linq.Expressions.ExpressionType.Or;
                    return Expression.Condition(le, Expression.Constant(true),
                                                Expression.Convert(
                                                    DynamicBinaryOperator(Expression.Constant(false), re, expressionType),
                                                    typeof(bool)));
                }


                if (expressionType == System.Linq.Expressions.ExpressionType.AndAlso)
                {
                    le = Expression.IsFalse(Expression.Convert(le, typeof(bool)));
                    expressionType = System.Linq.Expressions.ExpressionType.And;
                    return Expression.Condition(le, Expression.Constant(false),
                                                Expression.Convert(
                                                    DynamicBinaryOperator(Expression.Constant(true), re, expressionType),
                                                    typeof(bool)));
                }

                return DynamicBinaryOperator(le, re, expressionType);
            }
            else
            {

                re = TypeConversion.EnumConversion(ref re);
                le = TypeConversion.EnumConversion(ref le);

                var ret = re.Type;
                var let = le.Type;

                TypeConversion.ImplicitConversion(ref le, ret);
                TypeConversion.ImplicitConversion(ref re, let);
                //TypeConversion.BinaryNumericPromotion(expressionType, ref le, ref re);
                //le = TypeConversion.DynamicConversion(re, le);
                return GetBinaryOperator(le, re, expressionType);
            }
        }

        public static Expression GetUnaryOperator(Expression le, System.Linq.Expressions.ExpressionType expressionType)
        {
            switch (expressionType)
            {

                case System.Linq.Expressions.ExpressionType.Negate:
                    return Expression.Negate(le);

                case System.Linq.Expressions.ExpressionType.UnaryPlus:
                    return Expression.UnaryPlus(le);

                case System.Linq.Expressions.ExpressionType.NegateChecked:
                    return Expression.NegateChecked(le);

                case System.Linq.Expressions.ExpressionType.Not:
                    return Expression.Not(le);

                case System.Linq.Expressions.ExpressionType.Decrement:
                    return Expression.Decrement(le);

                case System.Linq.Expressions.ExpressionType.Increment:
                    return Expression.Increment(le);

                case System.Linq.Expressions.ExpressionType.OnesComplement:
                    return Expression.OnesComplement(le);

                case System.Linq.Expressions.ExpressionType.PreIncrementAssign:
                    return Expression.PreIncrementAssign(le);

                case System.Linq.Expressions.ExpressionType.PreDecrementAssign:
                    return Expression.PreDecrementAssign(le);

                case System.Linq.Expressions.ExpressionType.PostIncrementAssign:
                    return Expression.PostIncrementAssign(le);

                case System.Linq.Expressions.ExpressionType.PostDecrementAssign:
                    return Expression.PostDecrementAssign(le);

                default:
                    throw new ArgumentOutOfRangeException("expressionType");
            }
        }

        public static Expression GetBinaryOperator(Expression le, Expression re, System.Linq.Expressions.ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case System.Linq.Expressions.ExpressionType.Add:
                    return Add(le, re);

                case System.Linq.Expressions.ExpressionType.And:
                    return Expression.And(le, re);

                case System.Linq.Expressions.ExpressionType.AndAlso:
                    return Expression.AndAlso(le, re);

                case System.Linq.Expressions.ExpressionType.Coalesce:
                    return Expression.Coalesce(le, re);

                case System.Linq.Expressions.ExpressionType.Divide:
                    return Expression.Divide(le, re);

                case System.Linq.Expressions.ExpressionType.Equal:
                    return Expression.Equal(le, re);

                case System.Linq.Expressions.ExpressionType.ExclusiveOr:
                    return Expression.ExclusiveOr(le, re);

                case System.Linq.Expressions.ExpressionType.GreaterThan:
                    return Expression.GreaterThan(le, re);

                case System.Linq.Expressions.ExpressionType.GreaterThanOrEqual:
                    return Expression.GreaterThanOrEqual(le, re);

                case System.Linq.Expressions.ExpressionType.LeftShift:
                    return Expression.LeftShift(le, re);

                case System.Linq.Expressions.ExpressionType.LessThan:
                    return Expression.LessThan(le, re);

                case System.Linq.Expressions.ExpressionType.LessThanOrEqual:
                    return Expression.LessThanOrEqual(le, re);

                case System.Linq.Expressions.ExpressionType.Modulo:
                    return Expression.Modulo(le, re);

                case System.Linq.Expressions.ExpressionType.Multiply:
                    return Expression.Multiply(le, re);

                case System.Linq.Expressions.ExpressionType.NotEqual:
                    return Expression.NotEqual(le, re);

                case System.Linq.Expressions.ExpressionType.Or:
                    return Expression.Or(le, re);

                case System.Linq.Expressions.ExpressionType.OrElse:
                    return Expression.OrElse(le, re);

                case System.Linq.Expressions.ExpressionType.Power:
                    return Expression.Power(le, re);

                case System.Linq.Expressions.ExpressionType.RightShift:
                    return Expression.RightShift(le, re);

                case System.Linq.Expressions.ExpressionType.Subtract:
                    return Expression.Subtract(le, re);

                case System.Linq.Expressions.ExpressionType.Assign:
                    return Expression.Assign(le, re);

                case System.Linq.Expressions.ExpressionType.AddAssign:
                    return Expression.AddAssign(le, re);

                case System.Linq.Expressions.ExpressionType.AndAssign:
                    return Expression.AndAssign(le, re);

                case System.Linq.Expressions.ExpressionType.DivideAssign:
                    return Expression.DivideAssign(le, re);

                case System.Linq.Expressions.ExpressionType.ExclusiveOrAssign:
                    return Expression.ExclusiveOrAssign(le, re);

                case System.Linq.Expressions.ExpressionType.LeftShiftAssign:
                    return Expression.LeftShiftAssign(le, re);

                case System.Linq.Expressions.ExpressionType.ModuloAssign:
                    return Expression.ModuloAssign(le, re);

                case System.Linq.Expressions.ExpressionType.MultiplyAssign:
                    return Expression.MultiplyAssign(le, re);

                case System.Linq.Expressions.ExpressionType.OrAssign:
                    return Expression.OrAssign(le, re);

                case System.Linq.Expressions.ExpressionType.PowerAssign:
                    return Expression.PowerAssign(le, re);

                case System.Linq.Expressions.ExpressionType.RightShiftAssign:
                    return Expression.RightShiftAssign(le, re);

                case System.Linq.Expressions.ExpressionType.SubtractAssign:
                    return Expression.SubtractAssign(le, re);

                case System.Linq.Expressions.ExpressionType.AddAssignChecked:
                    return Expression.AddAssignChecked(le, re);

                case System.Linq.Expressions.ExpressionType.MultiplyAssignChecked:
                    return Expression.MultiplyAssignChecked(le, re);

                case System.Linq.Expressions.ExpressionType.SubtractAssignChecked:
                    return Expression.SubtractAssignChecked(le, re);

                default:
                    throw new ArgumentOutOfRangeException("expressionType");
            }
        }

        private static Expression DynamicUnaryOperator(Expression le, System.Linq.Expressions.ExpressionType expressionType)
        {
            var expArgs = new List<Expression>() { le };

            var binderM = Binder.UnaryOperation(CSharpBinderFlags.None, expressionType, le.Type,
                                                new CSharpArgumentInfo[]
                                                    {
                                                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                                                    });

            return Expression.Dynamic(binderM, typeof(object), expArgs);
        }

        private static Expression DynamicBinaryOperator(Expression le, Expression re, System.Linq.Expressions.ExpressionType expressionType)
        {
            var expArgs = new List<Expression>() { le, re };


            var binderM = Binder.BinaryOperation(CSharpBinderFlags.None, expressionType, le.Type,
                                                 new CSharpArgumentInfo[]
                                                     {
                                                         CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                                         CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
                                                     });

            return Expression.Dynamic(binderM, typeof(object), expArgs);
        }


        public static Expression Condition(Expression condition, Expression ifTrue, Expression ifFalse)
        {
            if (condition.NodeType == System.Linq.Expressions.ExpressionType.Dynamic)
            {
                var expArgs = new List<Expression>() { condition };

                var binderM = Binder.UnaryOperation(CSharpBinderFlags.None, System.Linq.Expressions.ExpressionType.IsTrue, condition.Type,
                                                new CSharpArgumentInfo[]
                                                    {
                                                        CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
                                                    });

                condition = Expression.Dynamic(binderM, typeof(bool), expArgs);
            }

            TypeConversion.ImplicitConversion(ref condition, typeof(bool));
            var x = TypeConversion.ImplicitConversion(ref ifTrue, ifFalse.Type) || TypeConversion.ImplicitConversion(ref ifFalse, ifTrue.Type);
            return Expression.Condition(condition, ifTrue, ifFalse);
        }

        public static Expression New(Type t, ArgumentListExpression argumentList)
        {
            ConstructorInfo constructorInfo;

            if (argumentList == null)
            {
                var p = t.GetConstructors();
                constructorInfo = p.First(x => !x.GetParameters().Any());

                return Expression.New(constructorInfo, null);
            }
            constructorInfo = t.GetConstructor(argumentList.ArgumentList.Select(arg => arg.Expression.Type).ToArray());

            return Expression.New(constructorInfo, argumentList.ArgumentList.Select(x => x.Expression));
        }

        public static Expression Switch(LabelTarget breakTarget, Expression switchCase, List<SwitchCase> switchBlock)
        {
            var defaultCase = switchBlock.SingleOrDefault(x => x.TestValues[0].Type == typeof(void));
            var cases = switchBlock.Where(x => x.TestValues[0].Type != typeof(void)).ToArray();

            foreach (var @case in cases)
            {
                if (@case.Body.NodeType != System.Linq.Expressions.ExpressionType.Block) continue;
                var caseBlock = (BlockExpression)@case.Body;
                if (caseBlock.Expressions.Last().NodeType != System.Linq.Expressions.ExpressionType.Goto)
                {
                    throw new Exception("Break statement is missing");
                }
            }

            Expression switchExp = null;

            if (defaultCase == null)
            {
                switchExp = Expression.Switch(switchCase, cases);
            }
            else
            {
                switchExp = Expression.Switch(switchCase, defaultCase.Body, cases);
            }

            return Expression.Block(new[] {
                switchExp,
                Expression.Label(breakTarget)
            });

        }

        public static Expression ForEach(LabelTarget exitLabel, LabelTarget continueLabel, ParameterExpression parameter, Expression iterator, Expression body)
        {
            var enumerator = GetMethod(iterator, new TypeOrGeneric() { Identifier = "GetEnumerator" }, new List<ArgumentExpression>(), false, null);

            var enumParam = Expression.Variable(enumerator.Type);
            var assign = Expression.Assign(enumParam, enumerator);

            var localVar = new LocalVariableDeclarationExpression();
            localVar.Variables.Add(enumParam);
            localVar.Initializers.Add(assign);

            var condition = GetMethod(enumParam, new TypeOrGeneric() { Identifier = "MoveNext" }, new List<ArgumentExpression>(), false, null);

            var expressions = new List<Expression>();
            var variables = new List<ParameterExpression>();

            variables.Add(parameter);

            var current = GetProperty(enumParam, "Current");

            if (current.Type == typeof(object) && parameter.Type != typeof(object))
            {
                current = Expression.Convert(current, parameter.Type);
            }

            expressions.Add(Assign(parameter, current));
            expressions.Add(body);

            var newbody = Expression.Block(variables, expressions);
            return While(exitLabel, continueLabel, localVar, condition, newbody);
        }

        public static Expression For(LabelTarget exitLabel, LabelTarget continueLabel, Expression initializer, Expression condition, ExpressionList iterator, Expression body)
        {
            var initializations = new List<Expression>();
            var localVars = new List<ParameterExpression>();
            var loopbody = new List<Expression>();

            if (initializer != null)
            {
                if (initializer.GetType() == typeof(LocalVariableDeclarationExpression))
                {
                    var lvd = (LocalVariableDeclarationExpression)initializer;
                    localVars.AddRange(lvd.Variables);
                    initializations.AddRange(lvd.Initializers);
                }
                else
                {
                    initializations.AddRange(((ExpressionList)initializer).Expressions);
                }
            }

            var loopblock = new List<Expression>();

            if (condition != null)
            {
                loopblock.Add(Expression.IfThen(Expression.Not(condition), Expression.Goto(exitLabel)));
            }

            loopblock.Add(body);
            loopblock.Add(Expression.Label(continueLabel));

            if (iterator != null)
            {
                loopblock.AddRange(iterator.Expressions);
            }

            var loop = Expression.Loop(Expression.Block(loopblock));

            loopbody.AddRange(initializations);
            loopbody.Add(loop);
            loopbody.Add(Expression.Label(exitLabel));

            var block = Expression.Block(localVars, loopbody);
            return block;
        }
        
        public static Expression DoWhile(LabelTarget breakTarget, LabelTarget continueLabel, Expression body, Expression boolean)
        {
            var block = Expression.Block(
                new Expression[] {
                    Expression.Loop(
                        Expression.Block(
                            new Expression[] {
                                body,
                                Expression.Label(continueLabel),
                                Expression.IfThen(Expression.Not(boolean),Expression.Goto(breakTarget))
                            })),
                    Expression.Label(breakTarget)
                });
            return block;
        }

        public static Expression While(LabelTarget breakTarget, LabelTarget continueLabel, LocalVariableDeclarationExpression setup, Expression boolean, Expression body)
        {
            var loopBody = new List<Expression>();
            if (setup != null)
            {
                loopBody.AddRange(setup.Initializers);
            }
            loopBody.Add(Expression.Loop(
                        Expression.Block(
                            new Expression[] {
                                Expression.IfThen(Expression.Not(boolean),Expression.Goto(breakTarget)),
                                body,
                                Expression.Label(continueLabel)
                            })));
            loopBody.Add(Expression.Label(breakTarget));
            if (setup != null)
            {

                return Expression.Block(setup.Variables, loopBody);
            }
            return Expression.Block(loopBody);
        }
    }
}