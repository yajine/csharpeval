using System.Collections.Generic;
using System.Reflection;

namespace ExpressionEvaluator.Parser
{
    public class CompilationContext
    {
        List<Assembly> _assemblies = new List<Assembly>();
        List<string> _namespaces = new List<string>();

        public List<Assembly> Assemblies { get { return _assemblies; } }
        public List<string> Namespaces { get { return _namespaces; } }
    }
}