using System;
using System.Reflection;

namespace ExpressionEvaluator.Parser.Expressions
{
    public class ApplicableFunctionMember
    {
        public MethodInfo Member { get; set; }
        public bool IsParamArray { get; set; }
        public bool IsExpanded { get; set; }
        public int ParamArrayPosition { get; set; }
        public Type ParamArrayElementType { get; set; }
        public Type ParamArrayElementLength { get; set; }
    }
}