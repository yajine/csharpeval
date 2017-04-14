using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public class ArgumentExpression : Expression
    {
        public Expression Expression { get; set; }
        public string Name { get; set; }

        public int Position{ get; set; }
        public bool Ref { get; set; }
        public bool Out{ get; set; }
    }
}