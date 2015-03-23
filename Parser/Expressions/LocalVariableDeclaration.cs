using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class LocalVariableDeclaration : DeclarationStatement
    {
        public LocalVariableDeclaration()
        {
            Variables = new List<ParameterExpression>();
            Initializers = new List<Expression>();
        }

        public List<ParameterExpression> Variables { get; set; }
        public List<Expression> Initializers { get; set; }
    }
}