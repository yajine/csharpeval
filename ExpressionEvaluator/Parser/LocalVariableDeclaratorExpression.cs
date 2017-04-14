using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class LocalVariableDeclaratorExpression : Expression
    {
        public string Identifer { get; set; }
        public Expression Expression { get; set; }
    }
}