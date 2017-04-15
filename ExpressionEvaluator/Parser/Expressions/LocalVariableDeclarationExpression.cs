using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
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

    public class NamespaceOrTypeExpression : Expression
    {
        public string Identifier { get; set; }
        public Type DetectedType {get;set;}
    }
}