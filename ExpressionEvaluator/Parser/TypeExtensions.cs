using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ExpressionEvaluator.Parser
{
    internal static class TypeExtensions
    {
        private static readonly List<Type> NumericTypes = new List<Type>()
            {
                typeof (sbyte),
                typeof (byte),
                typeof (short),
                typeof (ushort),
                typeof (int),
                typeof (uint),
                typeof (long),
                typeof (ulong),
                typeof (char),
                typeof (float),
                typeof (double),
                typeof (decimal)
            };

        public static bool IsNumericType(this Type type)
        {
            return NumericTypes.Contains(type);
        }

        public static bool IsDynamicOrObject(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(IDynamicMetaObjectProvider)) ||
                   type == typeof(Object);
        }

        public static bool IsDelegate(this Type t)
        {
            return typeof(Delegate).IsAssignableFrom(t.BaseType);
        }

        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition().Name == genericType.Name)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition().Name == genericType.Name)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
        }

        public static bool IsReferenceType(this Type T)
        {
            return T.IsArray || T.IsClass || T.IsInterface || T.IsDelegate();
        }

        public static bool IsDerivedFrom(this Type T, Type superClass)
        {
            return superClass.IsAssignableFrom(T);
        }

        public static bool Implements(this Type T, Type interfaceType)
        {
            return T.GetInterfaces().Any(x =>
            {
                return x.Name == interfaceType.Name;
            });
        }

        //public static bool HasGenericParameter(this Type T)
        //{
        //    return T.GetInterfaces().Any(x =>
        //    {
        //        return x.Name == interfaceType.Name;
        //    });
        //}

        public static bool IsDynamic(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(IDynamicMetaObjectProvider));
        }

        public static bool IsObject(this Type type)
        {
            return type == typeof(Object);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

    }

    //internal static class ExpressionExtensions
    //{
    //    public static bool IsDynamicOrObject(this Type type)
    //    {
    //        return type.GetInterfaces().Contains(typeof(IDynamicMetaObjectProvider)) ||
    //               type == typeof(Object);
    //    }

    //    public static bool IsDynamic(this Expression type)
    //    {
    //        return type.GetInterfaces().Contains(typeof(IDynamicMetaObjectProvider));
    //    }

    //    public static bool IsObject(this Type type)
    //    {
    //        return type == typeof(Object);
    //    }
    //}
}