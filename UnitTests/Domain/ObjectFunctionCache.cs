using System;
using System.Collections.Generic;
using ExpressionEvaluator;

namespace UnitTestProject1.Domain
{
    public class ObjectFunctionCache<T>
    {
        private readonly Dictionary<string, Func<T, object>> _functionRegistry;

        public ObjectFunctionCache()
        {
            _functionRegistry = new Dictionary<string, Func<T, object>>();
        }

        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }

        public Func<T, object> GetCachedFunction(string expression)
        {
            Func<T, object> f;
            if (!_functionRegistry.TryGetValue(expression, out f))
            {
                CacheMisses++;
                var p = new CompiledExpression { StringToParse = expression };

                f = p.ScopeCompile<T>();
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