using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class LocalVariableDeclaratorExpression : Expression
    {
        public string Identifer { get; set; }
        public Expression Expression { get; set; }
    }
}