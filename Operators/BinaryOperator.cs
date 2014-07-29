using System;
using System.Linq.Expressions;
#if !NET35
using Microsoft.CSharp.RuntimeBinder;
#endif //!NET35

namespace ExpressionEvaluator.Operators
{
    internal class BinaryOperator : Operator<Func<Expression, Expression, Expression>>
    {
        public BinaryOperator(string value, int precedence, bool leftassoc,
                              Func<Expression, Expression, Expression> func, ExpressionType expressionType)
            : base(value, precedence, leftassoc, func)
        {
            Arguments = 2;
            ExpressionType = expressionType;
        }

    }


    internal class TernaryOperator : Operator<Func<Expression, Expression, Expression, Expression>>
    {
        public TernaryOperator(string value, int precedence, bool leftassoc,
                              Func<Expression, Expression, Expression, Expression> func)
            : base(value, precedence, leftassoc, func)
        {
            Arguments = 3;
        }
    }
}