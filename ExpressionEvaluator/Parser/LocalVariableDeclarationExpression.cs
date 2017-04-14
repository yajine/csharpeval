using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class LocalVariableDeclarationExpression : Expression
    {
        public LocalVariableDeclarationExpression()
        {
            Variables = new List<ParameterExpression>();
            Initializers = new List<Expression>();
        }
        public List<ParameterExpression> Variables { get; set; }
        public List<Expression> Initializers { get; set; }
    }
}