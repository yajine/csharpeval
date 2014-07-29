using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ExpressionEvaluator.Tokens;
using System.Linq;
#if !NET35
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder;
#endif //!NET35


namespace ExpressionEvaluator.Operators
{
    internal class OpFuncServiceProviders
    {
        public static Expression MethodOperatorFunc(
            OpFuncArgs args
            )
        {
            string nextToken = ((MemberToken)args.T).Name;
            Expression le = args.ExprStack.Pop();

            Expression result = ((MethodOperator)args.Op).Func(args.T.IsFunction, le, nextToken, args.Args);

            return result;
        }

        public static Expression TypeOperatorFunc(
            OpFuncArgs args
            )
        {
            Expression le = args.ExprStack.Pop();
            return ((TypeOperator)args.Op).Func(le, args.T.Type);
        }

        public static Expression UnaryOperatorFunc(
            OpFuncArgs args
            )
        {
            Expression le = args.ExprStack.Pop();
            // perform implicit conversion on known types

#if !NET35
            if (le.Type.IsDynamic())
            {
                return DynamicUnaryOperatorFunc(le, args.Op.ExpressionType);
            }
            else
            {
#endif //!NET35
                return ((UnaryOperator) args.Op).Func(le);
#if !NET35
            }
#endif //!NET35
        }

        public static Expression BinaryOperatorFunc(
            OpFuncArgs args
            )
        {
            Expression re = args.ExprStack.Pop();
            Expression le = args.ExprStack.Pop();
            // perform implicit conversion on known types
#if !NET35
            var isDynamic = le.Type.GetInterfaces().Contains(typeof(IDynamicMetaObjectProvider)) ||
                le.Type == typeof(Object);


            if (le.Type.IsDynamic() && re.Type.IsDynamic())
            {
                return DynamicBinaryOperatorFunc(le, re, args.Op.ExpressionType);
            }
            else
            {
#endif //!NET35
                TypeConversion.Convert(ref le, ref re);

                return ((BinaryOperator)args.Op).Func(le, re);
#if !NET35
            }
#endif //!NET35
        }

#if !NET35
        private static Expression DynamicUnaryOperatorFunc(Expression le, ExpressionType expressionType)
        {
            var expArgs = new List<Expression>() { le };

            var binderM = Binder.UnaryOperation(CSharpBinderFlags.None, expressionType, le.Type, new CSharpArgumentInfo[]
		            {
			            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
			            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
		            });

            return Expression.Dynamic(binderM, typeof(object), expArgs);
        }

        private static Expression DynamicBinaryOperatorFunc(Expression le, Expression re, ExpressionType expressionType)
        {
            var expArgs = new List<Expression>() { le, re };

            var binderM = Binder.BinaryOperation(CSharpBinderFlags.None, expressionType, le.Type, new CSharpArgumentInfo[]
		            {
			            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null),
			            CSharpArgumentInfo.Create(CSharpArgumentInfoFlags.None, null)
		            });

            return Expression.Dynamic(binderM, typeof(object), expArgs);
        }
#endif //!NET35


        public static Expression TernaryOperatorFunc(OpFuncArgs args)
        {
            Expression falsy = args.ExprStack.Pop();
            Expression truthy = args.ExprStack.Pop();
            Expression condition = args.ExprStack.Pop();

            if (condition.Type != typeof (bool))
            {
                condition = Expression.Convert(condition, typeof(bool));
            }
            
            // perform implicit conversion on known types ???
            TypeConversion.Convert(ref falsy, ref truthy);
            return ((TernaryOperator)args.Op).Func(condition, truthy, falsy);
        }

        public static Expression TernarySeparatorOperatorFunc(OpFuncArgs args)
        {
            return args.ExprStack.Pop();
        }

    }
}