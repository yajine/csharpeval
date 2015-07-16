using System;

namespace ExpressionEvaluator.Parser
{
    [Serializable]
    public class CompilerException : Exception
    {
        public CompilerException(string message)
            : base(message)
        {
        }
    }
}