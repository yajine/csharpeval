using System;
using System.Collections.Generic;
using ExpressionEvaluator;

namespace UnitTestProject1.Domain
{
    public class FunctionCache 
    {
        private readonly Dictionary<string, Func<dynamic, object>> _functionRegistry;

        public FunctionCache()
        {
            _functionRegistry = new Dictionary<string, Func<dynamic, object>>();
        }

        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }

        public Func<dynamic, object> GetCachedFunction(string expression)
        {
            Func<dynamic, object> f;
            if (!_functionRegistry.TryGetValue(expression, out f))
            {
                CacheMisses++;
                var p = new CompiledExpression { StringToParse = expression };

                f = p.ScopeCompile();
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