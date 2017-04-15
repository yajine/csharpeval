using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ExpressionEvaluator.Parser.Expressions;

namespace ExpressionEvaluator.Parser
{

    public class TypeInferrence
    {
        public ArgumentExpression Argument { get; set; }
        public ParameterInfo Parameter { get; set; }
        public Type[] UnfixedBounds { get; set; }
        public Type FixedType { get; set; }
    }

    public class TypeParameter
    {
        public TypeParameter()
        {
            LowerBounds = new List<Type>();
            Unfixed = new List<Type>();
        }
        public string Name { get; set; }
        public List<Type> LowerBounds { get; set; }
        public List<Type> Unfixed { get; set; }
    }

    internal class MethodResolution
    {
        private static Dictionary<Type, List<Type>> NumConv = new Dictionary<Type, List<Type>> {
            {typeof(sbyte), new List<Type> { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) }},
            {typeof(byte), new List<Type> { typeof(short) ,typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double) ,typeof(decimal)}},
            {typeof(short), new List<Type> { typeof(int) ,typeof(long), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(ushort), new List<Type> {typeof(int), typeof(uint), typeof(long), typeof(ulong),typeof(float), typeof(double), typeof(decimal)}},
            {typeof(int), new List<Type> { typeof(long), typeof(float), typeof(double), typeof(decimal) }},
            {typeof(uint), new List<Type> { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) }},
            {typeof(long), new List<Type> { typeof(float), typeof(double), typeof(decimal)}},
            {typeof(ulong), new List<Type> { typeof(float), typeof(double), typeof(decimal)}},
            {typeof(char), new List<Type> { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)}},
            {typeof(float), new List<Type> { typeof(double)}}
        };

        private static Type[] GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => String.Equals(t.Namespace, nameSpace, StringComparison.Ordinal)).ToArray();
        }


        public static List<TypeInferrence> SecondPhase(ApplicableFunctionMember member, IEnumerable<ArgumentExpression> arguments)
        {
            return null;
        }

        //        7.5.2.9 Lower-bound inferences
        public void MakeLowerBoundInference(TypeParameter X, Type U, Type V)
        {
            //A lower-bound inference from a type U to a type V is made as follows:
            //•	If V is one of the unfixed Xi then U is added to the set of lower bounds for Xi.
            if (X.Unfixed.Contains(V))
            {
                X.LowerBounds.Add(U);
            }

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

        }



        //7.5.2.1 The first phase
        public static List<TypeInferrence> FirstPhase(ApplicableFunctionMember member, IEnumerable<ArgumentExpression> arguments)
        {
            var genericArgs = member.Member.GetGenericArguments().ToList();
            var parameters = member.Member.GetParameters().ToList();
            var typeParameters = genericArgs.Select(x => new TypeParameter());

            var E = arguments.ToList();

            //            For each of the method arguments Ei:
            for (var i = 0; i < E.Count(); i++)
            {
                if (E[i].Expression.NodeType == System.Linq.Expressions.ExpressionType.Lambda)
                {
                    //•	If Ei is an anonymous function, an explicit parameter type inference (§7.5.2.7) is made from Ei to Ti
                }
                else if (parameters[i].ParameterType.IsValueType)
                {
                    //•	Otherwise, if Ei has a type U and xi is a value parameter then a lower-bound inference is made from U to Ti.
                }
                else if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    //•	Otherwise, if Ei has a type U and xi is a ref or out parameter then an exact inference is made from U to Ti. 
                }
            }
            //•	Otherwise, no inference is made for this argument.

            return null;
        }


        // 7.5.2 Type inference 
        // http://blogs.msdn.com/b/ericlippert/archive/2012/10/02/how-do-we-ensure-that-method-type-inference-terminates.aspx
        // Add bounds to type parameters based on all non-lambda arguments, and all lambda arguments where the delegate type has no type parameters in its inputs.
        // Loop
        //   Is every type parameter fixed?
        //      Type inference has succeeded. Terminate the algorithm.
        //   Is there any lambda argument converted to a delegate type where the inputs of the delegate type are all known and the output type involves an unfixed type parameter?
        //      Deduce the return type of all such lambdas and make inferences that add bounds to the corresponding delegate's output types.
        //   Is there any unfixed, bounded type parameter that does not appear in an output type of a delegate that has unfixed input types?
        //      Fix all such type parameters and go back to the top of the loop.
        //   Is there any unfixed, bounded type parameter such that an unfixed type parameter depends on it, directly or indirectly?
        //      Fix all such type parameters and go back to the top of the loop.
        //   If we make it here then we failed to make progress; we have just as many fixed type parameters as we started with. Type inference fails. Terminate the algorithm.
        public static List<TypeInferrence> TypeInference(ApplicableFunctionMember member, IEnumerable<ArgumentExpression> arguments)
        {
            return null;
        }

        public static bool CanConvertType(object value, bool isLiteral, Type from, Type to)
        {
            // null literal conversion 6.1.5
            //if (value == null)
            //{
            //    return IsNullableType(to);
            //}

            // identity conversion 6.1.1
            if (@from.GetHashCode().Equals(to.GetHashCode()))
                return true;

            // implicit constant expressions 6.1.9
            if (isLiteral)
            {
                bool canConv = false;

                dynamic num = value;
                if (@from == typeof(int))
                {
                    switch (Type.GetTypeCode(to))
                    {
                        case TypeCode.SByte:
                            if (num >= SByte.MinValue && num <= SByte.MaxValue)
                                canConv = true;
                            break;
                        case TypeCode.Byte:
                            if (num >= Byte.MinValue && num <= Byte.MaxValue)
                                canConv = true;
                            break;
                        case TypeCode.Int16:
                            if (num >= Int16.MinValue && num <= Int16.MaxValue)
                                canConv = true;
                            break;
                        case TypeCode.UInt16:
                            if (num >= UInt16.MinValue && num <= UInt16.MaxValue)
                                canConv = true;
                            break;
                        case TypeCode.UInt32:
                            if (num >= UInt32.MinValue && num <= UInt32.MaxValue)
                                canConv = true;
                            break;
                        case TypeCode.UInt64:
                            if (num >= 0)
                                canConv = true;
                            break;
                    }
                }
                else if (@from == typeof(long))
                {
                    if (to == typeof(ulong))
                    {
                        if (num >= 0)
                            canConv = true;
                    }
                }

                if (canConv)
                    return true;
            }

            // string conversion
            // TODO: check if this is necessary
            if (@from == typeof(string))
            {
                if (to == typeof(object))
                    return true;
                else
                    return false;
            }


            // implicit nullable conversion 6.1.4
            if (IsNullableType(to))
            {

                if (IsNullableType(@from))
                {

                    // If the source value is null, then just return successfully (because the target value is a nullable type)
                    if (value == null)
                    {
                        return true;
                    }

                }

                return CanConvertType(value, isLiteral, Nullable.GetUnderlyingType(@from), Nullable.GetUnderlyingType(to));

            }

            // implicit enumeration conversion 6.1.3
            long longTest = -1;

            if (isLiteral && to.IsEnum && Int64.TryParse(value.ToString(), out longTest))
            {
                if (longTest == 0)
                    return true;
            }

            // implicit reference conversion 6.1.5
            if (!@from.IsValueType && !to.IsValueType)
            {
                bool? irc = ImpRefConv(value, @from, to);
                if (irc.HasValue)
                    return irc.Value;
            }

            // implicit numeric conversion 6.1.2
            try
            {
                object fromObj = null;
                double dblTemp;
                decimal decTemp;
                char chrTemp;
                fromObj = Activator.CreateInstance(@from);

                if (Char.TryParse(fromObj.ToString(), out chrTemp) || Double.TryParse(fromObj.ToString(), out dblTemp) || Decimal.TryParse(fromObj.ToString(), out decTemp))
                {
                    if (NumConv.ContainsKey(@from) && NumConv[@from].Contains(to))
                        return true;
                    else
                        return CrawlThatShit(to.GetHashCode(), @from, new List<int>());
                }
                else
                {
                    return CrawlThatShit(to.GetHashCode(), @from, new List<int>());
                }
            }
            catch
            {
                return CrawlThatShit(to.GetHashCode(), @from, new List<int>());
            }

            return false;
        }

        // 7.5.3.1 Applicable Function Member

        public static ApplicableFunctionMember IsApplicableFunctionMember(MethodInfo F, IList<ArgumentExpression> argList)
        {
            bool isMatch = true;
            bool isParamArray = false;
            bool isExpanded = false;
            //        A function member is said to be an applicable function member with respect to an argument list A when all of the following are true:

            //•	Each argument in A corresponds to a parameter in the function member declaration as described in §7.5.1.1, and any parameter to which no argument corresponds is an optional parameter.

            //•	For each argument in A, the parameter passing mode of the argument (i.e., value, ref, or out) is identical to the parameter passing mode of the corresponding parameter, and
            //  o	for a value parameter or a parameter array, an implicit conversion (§6.1) exists from the argument to the type of the corresponding parameter, or
            //  o	for a ref or out parameter, the type of the argument is identical to the type of the corresponding parameter. After all, a ref or out parameter is an alias for the argument passed.

            //For a function member that includes a parameter array, if the function member is applicable by the above rules, it is said to be applicable in its normal form. If a function member that includes a parameter array is not applicable in its normal form, the function member may instead be applicable in its expanded form:
            //•	The expanded form is constructed by replacing the parameter array in the function member declaration with zero or more value parameters of the element type of the parameter array such that the number of arguments in the argument list A matches the total number of parameters. If A has fewer arguments than the number of fixed parameters in the function member declaration, the expanded form of the function member cannot be constructed and is thus not applicable.

            //•	Otherwise, the expanded form is applicable if for each argument in A the parameter passing mode of the argument is identical to the parameter passing mode of the corresponding parameter, and
            //  o	for a fixed value parameter or a value parameter created by the expansion, an implicit conversion (§6.1) exists from the type of the argument to the type of the corresponding parameter, or
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
                        if (haveArg && !HasImplicitConversion(argList[argCount].Expression, argList[argCount].Expression.Type, pInfo.ParameterType))
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
                                if (!HasImplicitConversion(argList[j].Expression, argList[j].Expression.Type, elementType))
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
                        if (!haveArg || !HasImplicitConversion(argList[argCount].Expression, argList[argCount].Expression.Type, pInfo.ParameterType))
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

        //// 6.1.1
        public static bool HasIdentityConversion(Type T1, Type T2)
        {
            return T1.GetHashCode() == T2.GetHashCode();
        }

        //// 6.1.2
        public static bool HasImplicitNumericConversion(Type T1, Type T2)
        {
            if (T1 == typeof(sbyte) && (T2 == typeof(short) || T2 == typeof(int) || T2 == typeof(long) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(byte) && (T2 == typeof(short) || T2 == typeof(ushort) || T2 == typeof(int) || T2 == typeof(uint) || T2 == typeof(long) || T2 == typeof(ulong) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(short) && (T2 == typeof(int) || T2 == typeof(long) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(ushort) && (T2 == typeof(int) || T2 == typeof(uint) || T2 == typeof(long) || T2 == typeof(ulong) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(int) && (T2 == typeof(long) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(uint) && (T2 == typeof(long) || T2 == typeof(ulong) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(long) && (T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(ulong) && (T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(char) && (T2 == typeof(ushort) || T2 == typeof(int) || T2 == typeof(uint) || T2 == typeof(long) || T2 == typeof(ulong) || T2 == typeof(float) || T2 == typeof(double) || T2 == typeof(decimal))) return true;
            if (T1 == typeof(float) && (T2 == typeof(double))) return true;
            return false;
        }

        // 6.1.3 Implicit enumeration conversions
        public static bool HasImplicitEnumerationConversion(Type T1, Type T2)
        {
            // An implicit enumeration conversion permits the decimal-integer-literal 0 to be converted to any enum-type and to any 
            // nullable-type whose underlying type is an enum-type. In the latter case the conversion is evaluated by converting to 
            // the underlying enum-type and wrapping the result (§4.1.10).
            return false;
        }

        // 6.1.4 Implicit nullable conversions
        public static bool HasImplicitNullableConversion(Type S, Type T)
        {
            // Predefined implicit conversions that operate on non-nullable value types can also be used with nullable forms of those types. For each of the predefined 
            // implicit identity and numeric conversions that convert from a non-nullable value type S to a non-nullable value type T, the following implicit nullable conversions exist:
            //•	An implicit conversion from S? to T?.
            //•	An implicit conversion from S to T?.
            // Evaluation of an implicit nullable conversion based on an underlying conversion from S to T proceeds as follows:
            if (T.IsNullable())
            {
                if (S.IsNullable())
                {
                    //•	If the nullable conversion is from S? to T?:
                    // o	If the source value is null (HasValue property is false), the result is the null value of type T?.
                    // o	Otherwise, the conversion is evaluated as an unwrapping from S? to S, followed by the underlying conversion from S to T, followed by a wrapping (§4.1.10) from T to T?.
                    return HasImplicitConversion(null, Nullable.GetUnderlyingType(S), Nullable.GetUnderlyingType(T));

                }
                else
                {
                    //•	If the nullable conversion is from S to T?, the conversion is evaluated as the underlying conversion from S to T followed by a wrapping from T to T?.
                    return HasImplicitConversion(null, S, Nullable.GetUnderlyingType(T));
                }
            }
            return false;
        }


        /// 6.1.5 Null literal conversions
        public static bool HasNullLiteralConversion(Expression E, Type T1)
        {
            // An implicit conversion exists from the null literal to any nullable type. This conversion produces the null value (§4.1.10) of the given nullable type.
            return T1.IsNullable() && ((E.NodeType == System.Linq.Expressions.ExpressionType.Constant) && ((ConstantExpression)E).Value == null);
        }

        public static bool IsDelegate(Type t)
        {
            return typeof(Delegate).IsAssignableFrom(t.BaseType);
        }

        public static bool IsReferenceType(Type T)
        {
            return T.IsArray || T.IsClass || T.IsInterface || IsDelegate(T);
        }

        // 6.1.6 Implicit reference conversions
        public static bool HasImplicitReferenceConversion(Type S, Type T)
        {
            return
                //•	From any reference-type to object and dynamic.
            (S.IsReferenceType() && T.IsDynamicOrObject()) ||
                //•	From any class-type S to any class-type T, provided S is derived from T.
            (S.IsClass && T.IsClass && S.IsDerivedFrom(T)) ||
                //•	From any class-type S to any interface-type T, provided S implements T.
            (S.IsClass && T.IsInterface && S.Implements(T)) ||
                //•	From any interface-type S to any interface-type T, provided S is derived from T.
            (S.IsInterface && T.IsInterface && S.IsDerivedFrom(T)) ||
                //•	From an array-type S with an element type SE to an array-type T with an element type TE, provided all of the following are true:
                //o	S and T differ only in element type. In other words, S and T have the same number of dimensions.
                //o	Both SE and TE are reference-types.
                //o	An implicit reference conversion exists from SE to TE.
            (S.IsArray && T.IsArray && (
                (S.GetArrayRank() == T.GetArrayRank()) &&
                (S.GetElementType().IsReferenceType() && T.GetElementType().IsReferenceType()) &&
                (HasImplicitReferenceConversion(S.GetElementType(), T.GetElementType()))
            )) ||
                //•	From any array-type to System.Array and the interfaces it implements.

            //•	From a single-dimensional array type S[] to System.Collections.Generic.IList<T> and its base interfaces, provided that there is an implicit identity or reference conversion from S to T.


            //•	From any delegate-type to System.Delegate and the interfaces it implements.

            //•	From the null literal to any reference-type.

            //•	From any reference-type to a reference-type T if it has an implicit identity or reference conversion to a reference-type T0 and T0 has an identity conversion to T.

            //•	From any reference-type to an interface or delegate type T if it has an implicit identity or reference conversion to an 
                // interface or delegate type T0 and T0 is variance-convertible (§13.1.3.2) to T.

            //•	Implicit conversions involving type parameters that are known to be reference types. See §6.1.10 for more details on implicit conversions involving type parameters.

            //The implicit reference conversions are those conversions between reference-types that can be proven to always succeed, and therefore require no checks at run-time.

            S.IsClass && T.IsInterface && S.IsGenericType && S.GetGenericTypeDefinition().Implements(T);

            //Reference conversions, implicit or explicit, never change the referential identity of the object being converted. In other words, while a reference conversion may change the type of the reference, it never changes the type or value of the object being referred to.
        }

        //6.1.7 Boxing conversions
        //A boxing conversion permits a value-type to be implicitly converted to a reference type. A boxing conversion exists from any non-nullable-value-type to object and dynamic, to System.ValueType and to any interface-type implemented by the non-nullable-value-type. Furthermore an enum-type can be converted to the type System.Enum.
        //A boxing conversion exists from a nullable-type to a reference type, if and only if a boxing conversion exists from the underlying non-nullable-value-type to the reference type.
        //A value type has a boxing conversion to an interface type I if it has a boxing conversion to an interface type I0 and I0 has an identity conversion to I.
        //A value type has a boxing conversion to an interface type I if it has a boxing conversion to an interface or delegate type I0 and I0 is variance-convertible (§13.1.3.2) to I.
        //Boxing a value of a non-nullable-value-type consists of allocating an object instance and copying the value-type value into that instance. A struct can be boxed to the type System.ValueType, since that is a base class for all structs (§11.3.2).
        //Boxing a value of a nullable-type proceeds as follows:
        //•	If the source value is null (HasValue property is false), the result is a null reference of the target type.
        //•	Otherwise, the result is a reference to a boxed T produced by unwrapping and boxing the source value.
        //Boxing conversions are described further in §4.3.1.

        public static bool HasImplicitBoxingConversions(Expression E, Type T2)
        {
            if (E.Type.IsValueType)
            {
                if (T2 == typeof (object))
                {
                    return true;
                }
            }
            return false;
        }

        // 6.1.9 Implicit constant expression conversions
        public static bool HasImplicitConstantExpressionConversions(Expression E, Type T2)
        {
            //An implicit constant expression conversion permits the following conversions:
            //•	A constant-expression (§7.19) of type int can be converted to type sbyte, byte, short, ushort, uint, or ulong, provided the value of the constant-expression 
            //is within the range of the destination type.
            if (E != null && E.NodeType == System.Linq.Expressions.ExpressionType.Constant)
            {
                if (E.Type == typeof(int))
                {
                    var value = (int)((ConstantExpression)E).Value;
                    return (T2 == typeof(sbyte) && value >= sbyte.MinValue && value <= sbyte.MaxValue) ||
                    (T2 == typeof(byte) && value >= byte.MinValue && value <= byte.MaxValue) ||
                    (T2 == typeof(short) && value >= short.MinValue && value <= short.MaxValue) ||
                    (T2 == typeof(ushort) && value >= ushort.MinValue && value <= ushort.MaxValue) ||
                    (T2 == typeof(uint) && value >= uint.MinValue && value <= uint.MaxValue) ||
                    (T2 == typeof(ulong) && (ulong)value >= ulong.MinValue && (ulong)value <= ulong.MaxValue);
                }
                if (E.Type == typeof(long))
                {
                    //•	A constant-expression of type long can be converted to type ulong, provided the value of the constant-expression is not negative.
                    var value = (long)((ConstantExpression)E).Value;
                    return (T2 == typeof(ulong) && value >= 0);
                }
            }
            return false;
        }

        // 6.1.10 Implicit conversions involving type parameters
        public static bool HasImplicitConversionInvolvingTypeParameters(Type T1, Type T2)
        {
            return T2.IsGenericParameter && true;
            //The following implicit conversions exist for a given type parameter T:
            //•	From T to its effective base class C, from T to any base class of C, and from T to any interface implemented by C. At run-time, 
            // if T is a value type, the conversion is executed as a boxing conversion. 
            // Otherwise, the conversion is executed as an implicit reference conversion or identity conversion.

            //•	From T to an interface type I in T’s effective interface set and from T to any base interface of I. 
            // At run-time, if T is a value type, the conversion is executed as a boxing conversion. Otherwise, the 
            // conversion is executed as an implicit reference conversion or identity conversion.

            //•	From T to a type parameter U, provided T depends on U (§10.1.5). At run-time, if U is a value type, then T and U are 
            // necessarily the same type and no conversion is performed. Otherwise, if T is a value type, the conversion is executed as a boxing conversion. 
            // Otherwise, the conversion is executed as an implicit reference conversion or identity conversion.

            //•	From the null literal to T, provided T is known to be a reference type.

            //•	From T to a reference type I if it has an implicit conversion to a reference type S0 and S0 has an identity conversion to S. 
            // At run-time the conversion is executed the same way as the conversion to S0.

            //•	From T to an interface type I if it has an implicit conversion to an interface or delegate type I0 and I0 is variance-convertible 
            // to I (§13.1.3.2). At run-time, if T is a value type, the conversion is executed as a boxing conversion. Otherwise, the
            // conversion is executed as an implicit reference conversion or identity conversion.

            //If T is known to be a reference type (§10.1.5), the conversions above are all classified as implicit reference conversions (§6.1.6). 
            // If T is not known to be a reference type, the conversions above are classified as boxing conversions (§6.1.7).

        }

        // This is a hack, not in the specification(?), not fully implemented right now. need to check type parameters
        private static bool HasDelegateConversion(Type T1, Type T2)
        {
            if (T1.IsDelegate() && T2.IsDelegate())
            {
                return true;
            }
            return false;
        }


        public static bool HasImplicitConversion(Expression E, Type T1, Type T2)
        {
            return HasIdentityConversion(T1, T2) ||
                HasImplicitNumericConversion(T1, T2) ||
                HasImplicitNullableConversion(T1, T2) ||
                HasNullLiteralConversion(E, T2) ||
                HasImplicitReferenceConversion(T1, T2) ||
                HasImplicitBoxingConversions(E, T2) ||
                HasImplicitConstantExpressionConversions(E, T2) ||
                HasImplicitConversionInvolvingTypeParameters(T1, T2) || 
                HasDelegateConversion(T1, T2);
        }

        //7.5.3.5 Better conversion target
        public static bool IsBetterConversionTarget(Expression E, Type T1, Type T2)
        {
            // Given two different types T1 and T2, T1 is a better conversion target than T2 if at least one of the following holds:
            // •	An implicit conversion from T1 to T2 exists, and no implicit conversion from T2 to T1 exists
            return HasImplicitConversion(E, T1, T2) && !HasImplicitConversion(E, T2, T1) ||
                // •	T1 is a signed integral type and T2 is an unsigned integral type. Specifically:
            (
                //o	T1 is sbyte and T2 is byte, ushort, uint, or ulong
                (T1 == typeof(sbyte) && (T2 == typeof(byte) || T2 == typeof(ushort) || T2 == typeof(uint) || T2 == typeof(ulong))) ||
                //o	T1 is short and T2 is ushort, uint, or ulong
                (T1 == typeof(short) && (T2 == typeof(ushort) || T2 == typeof(uint) || T2 == typeof(ulong))) ||
                //o	T1 is int and T2 is uint, or ulong
                (T1 == typeof(int) && (T2 == typeof(uint) || T2 == typeof(ulong))) ||
                //o	T1 is long and T2 is ulong
                (T1 == typeof(long) && T2 == typeof(ulong))
            );
        }

        // 7.5.3.3 Better conversion from expression
        public static bool BetterConversion(Expression E, Type T1, Type T2)
        {
            // Given an implicit conversion C1 that converts from an expression E to a type T1, and an implicit conversion C2 
            // that converts from an expression E to a type T2, C1 is a better conversion than C2 if at least one of the following holds:
            var S = E.Type;
            //•	E has a type S and an identity conversion exists from S to T1 but not from S to T2
            if (HasIdentityConversion(S, T1) && !HasIdentityConversion(S, T2)) return true;
            //•	E is not an anonymous function and T1 is a better conversion target than T2 (§7.5.3.5)
            if (E.NodeType != System.Linq.Expressions.ExpressionType.Lambda)
            {
                return IsBetterConversionTarget(E, T1, T2);
            }
            else
            {
                //•	E is an anonymous function, T1 is either a delegate type D1 or an expression tree type Expression<D1>, T2 is either a delegate type D2 or an expression tree type Expression<D2> and one of the following holds:
                //o	D1 is a better conversion target than D2
                //o	D1 and D2 have identical parameter lists, and one of the following holds:
                //•	D1 has a return type Y1, and D2 has a return type Y2, an inferred return type X exists for E in the context of that parameter list (§7.5.2.12), and the conversion from X to Y1 is better than the conversion from X to Y2
                //•	E is async, D1 has a return type Task<Y1>, and D2 has a return type Task<Y2>, an inferred return type Task<X> exists for E in the context of that parameter list (§7.5.2.12), and the conversion from X to Y1 is better than the conversion from X to Y2
                //•	D1 has a return type Y, and D2 is void returning
            }
            return false;
        }

        public static Type[] GetExpandedParameterTypes(ParameterInfo[] parameters, int argCount)
        {
            var expandedParameterTypes = new Type[argCount];
            ParameterInfo currentParameter = null;

            ParameterInfo paramArrayParameter =
                parameters.FirstOrDefault(p => p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0);

            for (int i = 0; i < argCount; i++)
            {
                if (i < paramArrayParameter.Position)
                {
                    expandedParameterTypes[i] = parameters[i].ParameterType;
                }
                else
                {
                    expandedParameterTypes[i] = paramArrayParameter.ParameterType.GetElementType();
                }
            }


            return expandedParameterTypes;
        }

        public static int GetBetterFunctionMember(ApplicableFunctionMember MP, ApplicableFunctionMember MQ, IEnumerable<ArgumentExpression> argList)
        {
            //        For the purposes of determining the better function member, a stripped-down argument list A is constructed containing just the argument expressions themselves in the order they appear in the original argument list.

            //Parameter lists for each of the candidate function members are constructed in the following way: 
            //•	The expanded form is used if the function member was applicable only in the expanded form.
            //•	Optional parameters with no corresponding arguments are removed from the parameter list

            //•	The parameters are reordered so that they occur at the same position as the corresponding argument in the argument list.
            int implicitPoints = 0, typePoints = 0;

            var Pparams = MP.Member.GetParameters();
            var Qparams = MQ.Member.GetParameters();

            Type[] P = null, Q = null;

            bool firstCollorary = true;
            bool secondCollorary = false;

            //Given an argument list A with a set of argument expressions { E1, E2, ..., EN } and two applicable function members MP and MQ 
            // with parameter types { P1, P2, ..., PN } and { Q1, Q2, ..., QN }, MP is defined to be a better function member than MQ if

            //When performing this evaluation, if MP or MQ is applicable in its expanded form, then PX or QX refers to a parameter in the expanded form of the parameter list.
            var E = argList.Select(x => x.Expression).ToList();
            var argCount = E.Count();

            if (MP.IsParamArray && MP.IsExpanded)
            {
                P = GetExpandedParameterTypes(Pparams, argCount);
            }
            else
            {
                P = Pparams.Select(x => x.ParameterType).ToArray();
            }

            if (MQ.IsParamArray && MQ.IsExpanded)
            {
                Q = GetExpandedParameterTypes(Qparams, argCount);
            }
            else
            {
                Q = Qparams.Select(x => x.ParameterType).ToArray();
            }

            for (int x = 0; x < argCount; x++)
            {
                //•	for each argument, the implicit conversion from EX to QX is not better than the implicit conversion from EX to PX, and
                firstCollorary = firstCollorary & !BetterConversion(E[x], Q[x], P[x]);
                //•	for at least one argument, the conversion from EX to PX is better than the conversion from EX to QX.
                secondCollorary = secondCollorary | BetterConversion(E[x], P[x], Q[x]);
            }

            if (firstCollorary && secondCollorary) return 1;

            return -1;


            //In case the parameter type sequences {P1, P2, …, PN} and {Q1, Q2, …, QN} are equivalent (i.e. each Pi has an identity conversion to the corresponding Qi), the following tie-breaking rules are applied, in order, to determine the better function member. 

            //•	If MP is a non-generic method and MQ is a generic method, then MP is better than MQ.

            //•	Otherwise, if MP is applicable in its normal form and MQ has a params array and is applicable only in its expanded form, then MP is better than MQ.

            //•	Otherwise, if MP has more declared parameters than MQ, then MP is better than MQ. This can occur if both methods have params arrays and are applicable only in their expanded forms.

            //•	Otherwise if all parameters of MP have a corresponding argument whereas default arguments need to be substituted for at least one optional parameter in MQ then MP is better than MQ. 

            //•	Otherwise, if MP has more specific parameter types than MQ, then MP is better than MQ. Let {R1, R2, …, RN} and {S1, S2, …, SN} represent the uninstantiated and unexpanded parameter types of MP and MQ. MP’s parameter types are more specific than MQ’s if, for each parameter, RX is not less specific than SX, and, for at least one parameter, RX is more specific than SX:

            //o	A type parameter is less specific than a non-type parameter.
            //o	Recursively, a constructed type is more specific than another constructed type (with the same number of type arguments) if at least one type argument is more specific and no type argument is less specific than the corresponding type argument in the other.
            //o	An array type is more specific than another array type (with the same number of dimensions) if the element type of the first is more specific than the element type of the second.

            //•	Otherwise if one member is a non-lifted operator and  the other is a lifted operator, the non-lifted one is better.
            //•	Otherwise, neither function member is better.
            return 0;
        }


        // 7.5.3
        public static ApplicableFunctionMember OverloadResolution(IEnumerable<ApplicableFunctionMember> candidates, IEnumerable<ArgumentExpression> A)
        {
            //            7.5.3 Overload resolution
            //•	Given the set of applicable candidate function members, the best function member in that set is located. 
            // If the set contains only one function member, then that function member is the best function member. 
            if (candidates.Count() == 1) return candidates.First();

            var points = candidates.Select(x => new CandidatePoints { Candidate = x, Points = 0 }).ToList();

            // Otherwise, the best function member is the one function member that is better than all other function members with respect to the given argument list, 
            // provided that each function member is compared to all other function members using the rules in §7.5.3.2. 
            // If there is not exactly one function member that is better than all other function members, then the function member invocation is ambiguous and a binding-time error occurs.
            foreach (var point in points)
            {
                foreach (var candidate in candidates)
                {
                    if (point.Candidate != candidate)
                    {
                        point.Points += GetBetterFunctionMember(point.Candidate, candidate, A);
                    }
                }
            }

            if (points.Count == 0) return null;

            return points.OrderByDescending(x => x.Points).First().Candidate;
        }

        // 7.5.3.1
        public static IEnumerable<ApplicableFunctionMember> GetApplicableMembers(IEnumerable<MethodInfo> candidates, IList<ArgumentExpression> A)
        {

            // paramater matching && ref C# lang spec section 7.5.1.1
            var appMembers = new List<ApplicableFunctionMember>();

            // match each param with an arg. 
            //List<CallArgMod> paramMods;
            foreach (var F in candidates)
            {
                var afm = IsApplicableFunctionMember(F, A);
                if (afm != null)
                {
                    appMembers.Add(afm);
                }

                //if (M.TypeArgs == null || !M.TypeArgs.Any())
                //{
                //    if (!F.IsGenericMethod)
                //    {
                //        if (IsApplicableFunctionMember(F, A)) appMembers.Add(F);
                //    }
                //    else // (F.IsGenericMethod)
                //    {
                //        //InferTypes(F, A);
                //        if (IsApplicableFunctionMember(F, A)) appMembers.Add(F);
                //    }

                //}
                //if (F.IsGenericMethod && M.TypeArgs != null && M.TypeArgs.Any())
                //{
                //    if (IsApplicableFunctionMember(F, A)) appMembers.Add(F);
                //}
            }

            // • The set of candidate methods is reduced to contain only methods from the most derived types: For each method C.F in the set, where C is the type in which the method F is declared, 
            // all methods declared in a base type of C are removed from the set. Furthermore, if C is a class type other than object, all methods declared in an interface type are removed from the
            // set. (This latter rule only has affect when the method group was the result of a member lookup on a type parameter having an effective base class other than object and a
            // non-empty effective interface set.)
            //foreach (var F in appMembers.ToList())
            //{
            //    var C = F.DeclaringType;
            //    var baseTypeMethods = C.BaseType.GetMethods();
            //}

            //•	If the resulting set of candidate methods is empty, then further processing along the following steps are abandoned, and instead an attempt is made to 
            //  process the invocation as an extension method invocation (§7.6.5.2). If this fails, then no applicable methods exist, and a binding-time error occurs. 
            if (!appMembers.Any()) return Enumerable.Empty<ApplicableFunctionMember>();

            //•	The best method of the set of candidate methods is identified using the overload resolution rules of §7.5.3. If a single best method cannot be identified, the method invocation is ambiguous, and a binding-time error occurs. When performing overload resolution, the parameters of a generic method are considered after substituting the type arguments (supplied or inferred) for the corresponding method type parameters.

            //•	Final validation of the chosen best method is performed:
            //o	The method is validated in the context of the method group: If the best method is a static method, the method group must have resulted from a simple-name or a member-access through a type. If the best method is an instance method, the method group must have resulted from a simple-name, a member-access through a variable or value, or a base-access. If neither of these requirements is true, a binding-time error occurs.
            //o	If the best method is a generic method, the type arguments (supplied or inferred) are checked against the constraints (§4.4.4) declared on the generic method. If any type argument does not satisfy the corresponding constraint(s) on the type parameter, a binding-time error occurs.
            //Once a method has been selected and validated at binding-time by the above steps, the actual run-time invocation is processed according to the rules of function member invocation described in §7.5.4.
            //The intuitive effect of the resolution rules described above is as follows: To locate the particular method invoked by a method invocation, start with the type indicated by the method invocation and proceed up the inheritance chain until at least one applicable, accessible, non-override method declaration is found. Then perform type inference and overload resolution on the set of applicable, accessible, non-override methods declared in that type and invoke the method thus selected. If no method was found, try instead to process the invocation as an extension method invocation.



            return appMembers;
            //}
        }

        public static bool? ImpRefConv(object value, Type from, Type to)
        {
            bool? success = null;

            if (@from == to)
                // identity
                success = true;

            else if (to == typeof(object))
                // ref -> object
                success = true;

            else if (value == null)
                // null literal -> Ref-type
                success = !to.IsValueType;

            else if (false)
                // ref -> dynamic (6.1.8)
                // figure out how to do this
                ;

            else if (@from.IsArray && to.IsArray)
            {
                // Array-type -> Array-type
                bool sameRank = (@from.GetArrayRank() == to.GetArrayRank());
                bool bothRef = (!@from.GetElementType().IsValueType && !to.GetElementType().IsValueType);
                bool? impConv = ImpRefConv(value, @from.GetElementType(), to.GetElementType());
                success = (sameRank && bothRef && impConv.GetValueOrDefault(false));
            }

                // Conversion involving type parameters (6.1.10)
            else if (to.IsGenericParameter)
            {

                //if ( fromArg.GetType().Name.Equals(to.Name)) {
                if (to.GenericParameterAttributes != GenericParameterAttributes.None)
                {

                    if ((int)(to.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
                    {
                        ;
                    }
                }
                else
                {
                }


                /*genArg.GetGenericParameterConstraints();
                genArg.GenericParameterAttributes;*/
                //if( mi.GetGenericArguments()[?]
                //var t = a.GetType().GetMethod("Foo", BindingFlags.Public | BindingFlags.Instance).GetGenericArguments()[0].GetGenericParameterConstraints();//.GenericParameterAttributes;
            }

                // Boxing Conversions (6.1.7)
            else if (@from.IsValueType && !to.IsValueType)
            {
                return IsBoxingConversion(@from, to);
            }

            else if ((@from.IsClass && to.IsClass) || (@from.IsClass && to.IsInterface) || (@from.IsInterface && to.IsInterface))
                // class -> class  OR  class -> interface  OR  interface -> interface
                success = CrawlThatShit(to.GetHashCode(), @from, new List<int>());

            else if (@from.IsArray && CrawlThatShit(to.GetHashCode(), typeof(Array), new List<int>()))
            {
                // Array-type -> System.array
                return true;
            }

            else if (@from.IsArray && @from.GetArrayRank() == 1 && to.IsGenericType && CrawlThatShit(to.GetHashCode(), typeof(IList<>), new List<int>()))
                // Single dim array -> IList<>
                success = ImpRefConv(value, @from.GetElementType(), to.GetGenericTypeDefinition());



            return success;
        }

        // TODO: Rename this method

        ///
        /// <summary>
        ///		Recursive method to traverse through the class hierarchy in an attempt to determine if the current object may be converted
        ///		to the target type, based on it's hash code.
        /// </summary>
        /// 
        /// <param name="target">The hashCode value of the target object</param>
        /// <param name="current">The object to be converted.</param>
        /// <param name="visitedTypes">The list of visited types. This is an optimization parameter.</param>
        /// 
        /// <returns>True if the object can be converted to an object matching the hashCode property of target, false otherwise</returns>
        /// 
        public static bool CrawlThatShit(int target, Type current, List<int> visitedTypes)
        {
            int curHashCode = current.GetHashCode();

            // Optimization
            if (visitedTypes.Contains(curHashCode))
            {
                return false;
            }

            bool found = (curHashCode == target);
            visitedTypes.Add(curHashCode);

            if (!found && current.BaseType != null)
            {
                found = CrawlThatShit(target, current.BaseType, visitedTypes);
            }

            if (!found)
            {
                if (current.GetInterfaces() != null)
                {
                    foreach (Type iface in current.GetInterfaces())
                    {
                        if (CrawlThatShit(target, iface, visitedTypes))
                        {
                            found = true;
                            break;
                        }

                    }
                }
            }

            return found;
        }

        ///
        /// <summary>
        ///		Determines if the passed type is a nullable type
        /// </summary>
        /// 
        /// <param name="t">The type to check</param>
        /// 
        /// <returns>True if the type is a nullable type, false otherwise</returns>
        ///
        public static bool IsNullableType(Type t)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        ///
        /// <summary>
        ///		Determines if a boxing conversion exists between the passed object and the type
        /// </summary>
        /// 
        /// <param name="from">The type to convert</param>
        /// <param name="to">The type to attempt to convert the object to.</param>
        /// 
        /// <returns>True if a boxing conversion exists between the object and the type, false otherwise</returns>
        /// 
        public static bool IsBoxingConversion(Type from, Type to)
        {
            if (IsNullableType(@from))
            {
                @from = Nullable.GetUnderlyingType(@from);
            }

            if (to == typeof(ValueType) || to == typeof(object))
            {
                return true;
            }

            if (CrawlThatShit(to.GetHashCode(), @from, new List<int>()))
            {
                return true;
            }

            if (@from.IsEnum && to == typeof(Enum))
            {
                return true;
            }
            return false;
        }

        public static List<MethodInfo> GetCandidateMembers(Type type, string membername)
        {
            // Find members that match on name
            var results = GetMethodInfos(type, membername);

            foreach (var iinterface in type.GetInterfaces())
            {
                results.AddRange(GetMethodInfos(iinterface, membername));
            }

            // Traverse through class hierarchy
            while (type != typeof(object) && type.BaseType != null)
            {
                type = type.BaseType;
                results.AddRange(GetMethodInfos(type, membername));
            }

            return results;
        }

        private static Func<MethodInfo, bool> IsVirtual = (mi) => (mi.Attributes & MethodAttributes.Virtual) != 0;
        private static Func<MethodInfo, bool> HasVTable = (mi) => (mi.Attributes & MethodAttributes.VtableLayoutMask) != 0;

        private static BindingFlags findFlags = BindingFlags.NonPublic |
                                                BindingFlags.Public |
                                                BindingFlags.Static |
                                                BindingFlags.Instance |
                                                BindingFlags.InvokeMethod |
                                                BindingFlags.OptionalParamBinding |
                                                BindingFlags.DeclaredOnly;


        public static List<MethodInfo> GetMethodInfos(Type env, string memberName)
        {
            return env.GetMethods(findFlags).Where(mi => mi.Name == memberName && (!IsVirtual(mi) || HasVTable(mi))).ToList();
        }
    }
}