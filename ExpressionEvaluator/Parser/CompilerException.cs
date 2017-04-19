using System;

namespace ExpressionEvaluator.Parser
{
    [Serializable]
    public class CompilerException : Exception
    {
        public CompilerException()
        {
        }

        public CompilerException(string message)
            : base(message)
        {
        }
    }

    [Serializable]
    public class MemberResolutionException : CompilerException
    {
        public string MemberName { get; private set; }
        public Type Type { get; private set; }

        public MemberResolutionException(string memberName, Type type)  
        {
            MemberName = memberName;
            Type = type;
        }

        public override string ToString()
        {
            return String.Format("Cannot resolve member {0} on type {1}", MemberName, Type.Name);
        }
    }
}