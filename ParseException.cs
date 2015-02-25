using System;

namespace ExpressionEvaluator
{
    public class ParseException : Exception
    {
        public string Expression { get; private set; }
        
        public ParseException(string expression, string message) : base(message)
        {
            Expression = expression;
        }

        public ParseException(string expression, string message, Exception innerException)
            : base(message, innerException)
        {
            Expression = expression;
        }
    }
}