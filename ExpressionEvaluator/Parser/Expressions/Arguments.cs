using System.Collections.Generic;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class Arguments : PrimaryExpressionPart
    {
        public List<Argument> Values { get; set; }
    }
}