using System;
using System.Linq;
using Antlr4.Runtime;

namespace ExpressionEvaluator.Parser
{
    public class ErrorListener : BaseErrorListener
    {
        public override void SyntaxError(IRecognizer recognizer, IToken offendingSymbol, int line, int charPositionInLine, string msg,
            RecognitionException e)
        {
            throw new Exception(msg);
        }
    }
}
