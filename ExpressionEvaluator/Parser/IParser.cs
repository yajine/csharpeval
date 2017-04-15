using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ExpressionEvaluator.Parser
{
    public interface IParser
    {
        Dictionary<string, Type> DynamicTypeLookup { get; set; }
        Expression Expression { get; set; }
        string ExpressionString { get; set; }
        ExpressionType ExpressionType { get; set; }
        List<ParameterExpression> ExternalParameters { get; set; }
        object Global { get; set; }
        Type ReturnType { get; set; }
        TypeRegistry TypeRegistry { get; set; }
        CompilationContext Context { get; set; }

        Expression Parse(Expression scope, bool isCall = false);
    }
}