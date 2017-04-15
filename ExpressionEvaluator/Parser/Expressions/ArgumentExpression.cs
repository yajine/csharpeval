using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class ArgumentExpression : Expression
    {
        public Expression Expression { get; set; }
        public string Name { get; set; }
        public bool IsNamedArgument { get; set; }
        public int Position{ get; set; }
        public ParameterPassingModeEnum ParameterPassingMode { get; set; }
    }
}