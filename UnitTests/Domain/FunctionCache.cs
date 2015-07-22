using System;
using System.Collections.Generic;
using System.Dynamic;
using ExpressionEvaluator;

namespace UnitTestProject1.Domain
{
    public class FunctionCache 
    {
        private readonly Dictionary<string, Func<ExpandoObject, object>> _functionRegistry;

        public FunctionCache()
        {
            _functionRegistry = new Dictionary<string, Func<ExpandoObject, object>>();
        }

        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }

        public Func<ExpandoObject, object> GetCachedFunction(string expression)
        {
            Func<ExpandoObject, object> f;
            if (!_functionRegistry.TryGetValue(expression, out f))
            {
                CacheMisses++;
                var p = new CompiledExpression { StringToParse = expression };

                f = p.ScopeCompile<ExpandoObject>();
                _functionRegistry.Add(expression, f);
            }
            else
            {
                CacheHits++;
            }
            return f;
        }

    }
}