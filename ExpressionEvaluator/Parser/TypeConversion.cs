using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Converter = System.Convert;

namespace ExpressionEvaluator.Parser
{
    internal class TypeConversion
    {
        private static Dictionary<Type, List<Type>> ImplicitNumericConversions = new Dictionary<Type, List<Type>>();

        //private static bool PromoteNumericWithAlternate(Type referenceType, ref Expression le, ref Expression re, Type[] alternateTypes, Type targetType)
        //{
        //    if (le.Type == referenceType && alternateTypes.Contains(re.Type))
        //    {
        //        re = Expression.Convert(re, targetType);
        //        return true;
        //    }

        //    if (re.Type == referenceType && alternateTypes.Contains(le.Type))
        //    {
        //        le = Expression.Convert(le, targetType);
        //        return true;
        //    }

        //    return false;

        //}

        //private static bool PromoteNumeric(Type referenceType, Type thisOperandType, ref Expression otherOperand, Type[] invalidTypes = null)
        //{
        //    if (thisOperandType == referenceType && otherOperand.Type != referenceType)
        //    {
        //        if (invalidTypes != null && invalidTypes.Contains(otherOperand.Type))
        //        {
        //            throw new InvalidNumericPromotionException();
        //        }

        //        otherOperand = Expression.Convert(otherOperand, referenceType);
        //        return true;
        //    }

        //    return false;
        //}


        //private static bool PromoteNumeric(Type referenceType, ref Expression le, ref Expression re, Type[] invalidTypes = null)
        //{
        //    return PromoteNumeric(referenceType, le.Type, ref re, invalidTypes) ||
        //    PromoteNumeric(referenceType, re.Type, ref le, invalidTypes);
        //}

        //private static bool PromoteNumericInt(ref Expression le, ref Expression re)
        //{
        //    le = Expression.Convert(le, typeof(int));
        //    re = Expression.Convert(re, typeof(int));
        //    return true;
        //}


        // 7.3.6.2 Binary numeric promotions
        //public static bool BinaryNumericPromotion(ExpressionType expressionType, ref Expression le, ref Expression re)
        //{
        //    try
        //    {
        //        // Binary numeric promotion occurs for the operands of the predefined +, –, *, /, %, &, |, ^, ==, !=, >, <, >=, and <= binary operators. Binary numeric promotion implicitly converts both operands to a common type which, in case of the non-relational operators, also becomes the result type of the operation. Binary numeric promotion consists of applying the following rules, in the order they appear here:
        //        switch (expressionType)
        //        {
        //            case ExpressionType.Add:
        //            case ExpressionType.AddChecked:
        //            case ExpressionType.Subtract:
        //            case ExpressionType.SubtractChecked:
        //            case ExpressionType.Multiply:
        //            case ExpressionType.MultiplyChecked:
        //            case ExpressionType.Divide:
        //            case ExpressionType.Modulo:
        //            case ExpressionType.And:
        //            case ExpressionType.Or:
        //            case ExpressionType.ExclusiveOr:
        //            case ExpressionType.Equal:
        //            case ExpressionType.NotEqual:
        //            case ExpressionType.GreaterThan:
        //            case ExpressionType.LessThan:
        //            case ExpressionType.GreaterThanOrEqual:
        //            case ExpressionType.LessThanOrEqual:
        //                if (le.Type.IsNumericType() && re.Type.IsNumericType())
        //                {
        //                    // •	If either operand is of type decimal, the other operand is converted to type decimal, or a binding-time error occurs if the other operand is of type float or double.
        //                    return PromoteNumeric(typeof(decimal), ref le, ref re, new Type[] { typeof(float), typeof(double) }) ||
        //                        // •	Otherwise, if either operand is of type double, the other operand is converted to type double.
        //                    PromoteNumeric(typeof(double), ref le, ref re) ||
        //                        // •	Otherwise, if either operand is of type float, the other operand is converted to type float.
        //                    PromoteNumeric(typeof(float), ref le, ref re) ||
        //                        // •	Otherwise, if either operand is of type ulong, the other operand is converted to type ulong, or a binding-time error occurs if the other operand is of type sbyte, short, int, or long.
        //                    PromoteNumeric(typeof(ulong), ref le, ref re, new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long) }) ||
        //                        // •	Otherwise, if either operand is of type long, the other operand is converted to type long.
        //                    PromoteNumeric(typeof(long), ref le, ref re) ||
        //                        // •	Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, both operands are converted to type long.
        //                    PromoteNumericWithAlternate(typeof(uint), ref le, ref re, new Type[] { typeof(sbyte), typeof(short), typeof(int) }, typeof(long)) ||
        //                        // •	Otherwise, if either operand is of type uint, the other operand is converted to type uint.
        //                    PromoteNumeric(typeof(uint), ref le, ref re) ||
        //                        // •	Otherwise, both operands are converted to type int.
        //                    PromoteNumericInt(ref le, ref re)
        //                   ;
        //                }
        //                // Note that the first rule disallows any operations that mix the decimal type with the double and float types. The rule follows from the fact that there are no implicit conversions between the decimal type and the double and float types.
        //                // Also note that it is not possible for an operand to be of type ulong when the other operand is of a signed integral type. The reason is that no integral type exists that can represent the full range of ulong as well as the signed integral types.
        //                break;
        //        }
        //    }
        //    catch (InvalidNumericPromotionException)
        //    {
        //        throw new Exception(string.Format("Cannot apply operator {0} to operands of type {1} and {2}", expressionType, le.Type, re.Type));
        //    }

        //    return false;
        //}

        readonly Dictionary<Type, int> _typePrecedence = null;
        static readonly TypeConversion Instance = new TypeConversion();
        /// <summary>
        /// Performs implicit conversion between two expressions depending on their type precedence
        /// </summary>
        /// <param name="le"></param>
        /// <param name="re"></param>
        internal static void Convert(ref Expression le, ref Expression re)
        {
            if (Instance._typePrecedence.ContainsKey(le.Type) && Instance._typePrecedence.ContainsKey(re.Type))
            {
                if (Instance._typePrecedence[le.Type] > Instance._typePrecedence[re.Type]) re = Expression.Convert(re, le.Type);
                if (Instance._typePrecedence[le.Type] < Instance._typePrecedence[re.Type]) le = Expression.Convert(le, re.Type);
            }
        }

        /// <summary>
        /// Performs implicit conversion on an expression against a specified type
        /// </summary>
        /// <param name="le"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static Expression Convert(Expression le, Type type)
        {
            if (Instance._typePrecedence.ContainsKey(le.Type) && Instance._typePrecedence.ContainsKey(type))
            {
                if (Instance._typePrecedence[le.Type] < Instance._typePrecedence[type]) return Expression.Convert(le, type);
            }
            if (le.Type.IsNullable() && Nullable.GetUnderlyingType(le.Type) == type)
            {
                le = Expression.Property(le, "Value");
            }
            if (type.IsNullable() && Nullable.GetUnderlyingType(type) == le.Type)
            {
                le = Expression.Convert(le, type);
            }
            if (type == typeof(object))
            {
                return Expression.Convert(le, type);
            }
            if (le.Type == typeof(object))
            {
                return Expression.Convert(le, type);
            }
            return le;
        }

        /// <summary>
        /// Compares two types for implicit conversion
        /// </summary>
        /// <param name="from">The source type</param>
        /// <param name="to">The destination type</param>
        /// <returns>-1 if conversion is not possible, 0 if no conversion necessary, +1 if conversion possible</returns>
        internal static int CanConvert(Type from, Type to)
        {
            if (Instance._typePrecedence.ContainsKey(@from) && Instance._typePrecedence.ContainsKey(to))
            {
                return Instance._typePrecedence[to] - Instance._typePrecedence[@from];
            }
            else
            {
                if (@from == to) return 0;
                if (to.IsAssignableFrom(@from)) return 1;
            }
            return -1;
        }

        // 6.1.6 Implicit Reference Conversions
        public static bool ReferenceConversion(ref Expression src, Type destType)
        {
            //if (!src.Type.IsValueType)
            //{
            //    if (src.Type.IsSubclassOf(dest.Type))
            //    {
            //        src = Expression.Convert(src, dest.Type);
            //    }
            //}
            return false;
        }

        // 6.1.7 Boxing Conversions
        // A boxing conversion permits a value-type to be implicitly converted to a reference type. A boxing conversion exists from any non-nullable-value-type to object and dynamic, 
        // to System.ValueType and to any interface-type implemented by the non-nullable-value-type. 
        // Furthermore an enum-type can be converted to the type System.Enum.
        // A boxing conversion exists from a nullable-type to a reference type, if and only if a boxing conversion exists from the underlying non-nullable-value-type to the reference type.
        // A value type has a boxing conversion to an interface type I if it has a boxing conversion to an interface type I0 and I0 has an identity conversion to I.

        public static bool BoxingConversion(ref Expression src, Type destType)
        {
            if (src.Type.IsValueType && !src.Type.IsNullable() && destType.IsDynamicOrObject())
            {
                src = Expression.Convert(src, destType);
                return true;
            }
            return false;
        }


        //6.1.4 Nullable Type conversions
        public static bool NullableConverion(ref Expression src, Type destType)
        {
            if (src.Type.IsNullable() && Nullable.GetUnderlyingType(src.Type) == destType)
            {
                src = Expression.Property(src, "Value");
                return true;
            }
            if (destType.IsNullable() && Nullable.GetUnderlyingType(destType) == src.Type)
            {
                src = Expression.Convert(src, destType);
                return true;
            }
            return false;
        }


        // 6.1.5 Null literal conversions
        // An implicit conversion exists from the null literal to any nullable type. 
        // This conversion produces the null value (§4.1.10) of the given nullable type.
        public static bool NullLiteralConverion(ref Expression src, Type destType)
        {
            if (src.NodeType == System.Linq.Expressions.ExpressionType.Constant && src.Type == typeof(object) && ((ConstantExpression)src).Value == null && destType.IsNullable())
            {
                src = Expression.Constant(Activator.CreateInstance(destType), destType);
                return true;
            }
            return false;
        }

        public static Expression EnumConversion(ref Expression src)
        {
            if (typeof(Enum).IsAssignableFrom(src.Type))
            {
                return Expression.Convert(src, Enum.GetUnderlyingType(src.Type));
            }
            return src;
        }

        // 6.1 Implicit Conversions
        public static bool ImplicitConversion(ref Expression src, Type destType)
        {
            return src.Type != destType && (
                    (destType.IsNumericType() && src.Type.IsNumericType() && ImplicitNumericConversion(ref src, destType)) ||
                    NullableConverion(ref src, destType) ||
                    NullLiteralConverion(ref src, destType) ||
                    ReferenceConversion(ref src, destType) ||
                    BoxingConversion(ref src, destType) ||
                    DynamicConversion(ref src, destType) ||
                    ImplicitConstantConversion(ref src, destType)
                );
        }

        // 6.1.9 Implicit constant expression conversions
        public static bool ImplicitConstantConversion(ref Expression src, Type destType)
        {
            //An implicit constant expression conversion permits the following conversions:
            if (src.NodeType == System.Linq.Expressions.ExpressionType.Constant)
            {
                //•	A constant-expression (§7.19) of type int can be converted to type sbyte, byte, short, ushort, uint, or ulong, provided the value of the constant-expression is within the range of the destination type.
                if (src.Type == typeof(int))
                {
                    var value = (int)((ConstantExpression)src).Value;
                    if (destType == typeof (sbyte))
                    {
                        if (value >= SByte.MinValue && value <= SByte.MinValue)
                        {
                            src = Expression.Convert(src, typeof(sbyte));
                            return true;                         
                        }
                    }
                    if (destType == typeof(byte))
                    {
                        if (value >= Byte.MinValue && value <= Byte.MaxValue)
                        {
                            src = Expression.Convert(src, typeof(byte));
                            return true;                         
                        }
                    }
                    if (destType == typeof(short))
                    {
                        if (value >= Int16.MinValue && value <= Int16.MaxValue)
                        {
                            src = Expression.Convert(src, typeof(short));
                            return true;                         
                        }
                    }
                    if (destType == typeof(ushort))
                    {
                        if (value >= UInt16.MinValue && value <= UInt16.MaxValue)
                        {
                            src = Expression.Convert(src, typeof(ushort));
                            return true;                         
                        }
                    }
                    if (destType == typeof(uint))
                    {
                        if (value >= UInt32.MinValue && value <= UInt32.MaxValue)
                        {
                            src = Expression.Convert(src, typeof(uint));
                            return true;                         
                        }
                    }
                    if (destType == typeof(ulong))
                    {
                        if (value >= 0 && Converter.ToUInt64(value) <= UInt64.MaxValue)
                        {
                            src = Expression.Convert(src, typeof(ulong));
                            return true;
                        }
                    }
                }
                //•	A constant-expression of type long can be converted to type ulong, provided the value of the constant-expression is not negative.
                if (src.Type == typeof(long))
                {
                    var value = (long)((ConstantExpression)src).Value;
                    if (destType == typeof(ulong))
                    {
                        if (value >= 0)
                        {
                            src = Expression.Convert(src, typeof(ulong));
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static Type GetBaseCommonType(IEnumerable<Expression> expressions)
        {
            Type baseType = null;

            foreach (var expression in expressions)
            {
                if (baseType == null)
                {
                    baseType = expression.Type;
                }
                else
                {
                    switch (CanConvert(expression.Type, baseType))
                    {
                        case 1:
                            baseType = expression.Type;
                            break;
                        case -1:
                            throw new Exception(string.Format("Cannot convert between types {0} and {1}", baseType.Name, expression.Type.Name));
                    }
                }
            }

            return baseType;
        }

        public static bool DynamicConversion(ref Expression src, Type destType)
        {
            if (src.Type.IsObject())
            {
                src = Expression.Convert(src, destType);
                return true;
            }
            return false;
        }

        // 6.1.2 Implicit numeric conversions

        public static bool ImplicitNumericConversion(ref Expression src, Type target)
        {
            List<Type> allowed;
            if (ImplicitNumericConversions.TryGetValue(src.Type, out allowed))
            {
                if (allowed.Contains(target))
                {
                    src = Expression.Convert(src, target);
                    return true;
                }
                return false;
            }
            return false;
        }

        TypeConversion()
        {
            _typePrecedence = new Dictionary<Type, int>
            {
                    {typeof (object), 0},
                    {typeof (bool), 1},
                    {typeof (byte), 2},
                    {typeof (int), 3},
                    {typeof (short), 4},
                    {typeof (long), 5},
                    {typeof (float), 6},
                    {typeof (double), 7}
                };

            ImplicitNumericConversions.Add(typeof(sbyte), new List<Type>() { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(byte), new List<Type>() { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(short), new List<Type>() { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(ushort), new List<Type>() { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(int), new List<Type>() { typeof(long), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(uint), new List<Type>() { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(long), new List<Type>() { typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(ulong), new List<Type>() { typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(char), new List<Type>() { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) });
            ImplicitNumericConversions.Add(typeof(float), new List<Type>() { typeof(double) });
        }
    }
}