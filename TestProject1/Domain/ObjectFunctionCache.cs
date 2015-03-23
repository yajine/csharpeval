using System;
using System.Collections.Generic;
using ExpressionEvaluator;

namespace UnitTestProject1.Domain
{
    public class ObjectFunctionCache
    {
        private readonly Dictionary<string, Func<object, object>> _functionRegistry;

        public ObjectFunctionCache()
        {
            _functionRegistry = new Dictionary<string, Func<object, object>>();
        }

        public int CacheHits { get; private set; }
        public int CacheMisses { get; private set; }

        public Func<object, object> GetCachedFunction(string expression)
        {
            Func<object, object> f;
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