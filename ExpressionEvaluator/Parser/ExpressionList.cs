using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class ExpressionList : Expression
    {
        public ExpressionList()
        {
            Expressions = new List<Expression>();
        }
        public List<Expression> Expressions { get; set; }
    }
}