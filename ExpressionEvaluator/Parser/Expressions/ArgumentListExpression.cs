using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class ArgumentListExpression : Expression
    {
        public ArgumentListExpression()
        {
            ArgumentList = new List<ArgumentExpression>();
        }

        public void Add(ArgumentExpression expression)
        {
            ArgumentList.Add(expression);
        }

        public List<ArgumentExpression> ArgumentList{ get; set; }
    }
}