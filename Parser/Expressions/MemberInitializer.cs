using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class MemberInitializer
    {
        public string Identifier { get; set; }
        public Expression Value { get; set; }
    }
}