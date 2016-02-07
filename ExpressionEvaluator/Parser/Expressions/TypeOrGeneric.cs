using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class TypeOrGeneric
    {
        public string Identifier { get; set; }
        public List<Type> TypeArgs { get; set; }
        public override string ToString()
        {
            return Identifier;
        }
    }

    public class LambdaParameter
    {
        public string Identifier { get; set; }
        public List<Type> TypeArgs { get; set; }
        public Type Hint { get; set; } 
        public ParameterExpression Expression { get; set; }
        public override string ToString()
        {
            return Identifier;
        }
    }

}