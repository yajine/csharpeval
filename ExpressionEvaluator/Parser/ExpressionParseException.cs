using System;
using Antlr4.Runtime;

namespace ExpressionEvaluator.Parser
{
    [Serializable]
    public class ExpressionParseException : Exception
    {
        private ITokenStream _tokenStream;

        public ExpressionParseException(string message, ITokenStream tokenStream)
            : base(string.Format("{0} at line {1} char {2}", message, tokenStream.Lt(-1).Line, tokenStream.Lt(-1).Column))
        {
            this._tokenStream = tokenStream;
        }
    }
}