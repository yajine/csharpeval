using System;
using System.Collections.Generic;

namespace ExpressionEvaluator.Parser
{
    public class TypeInferenceBounds
    {
        public Type TypeArgument { get; set; }
        public List<Type> Bounds { get; set; }
    }
}