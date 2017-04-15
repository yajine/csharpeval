using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class SwitchBlockExpression : Expression
    {
        public SwitchBlockExpression()
        {
            Cases = new List<SwitchCase>();
        }
        public List<SwitchCase> Cases { get; set; }
    }
}
