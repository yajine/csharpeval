using System;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{
    public static class TestHelpers
    {

        public static void TestAssignment<TScope, TResult>(Expression<Func<TScope, TResult>> property, Action<TScope> initializer, string expression,
            TResult expectedResult) where TScope : new()
        {
            var scope = new TScope();
            if (initializer != null) initializer(scope);
            var exp = new CompiledExpression<TResult>(expression);
            Func<TScope, TResult> func;
            func = exp.ScopeCompile<TScope>();
            func(scope);
            Assert.AreEqual(expectedResult, property.Compile()(scope));
        }

        public static void TestOperator<T>(string expression, T expectedResult)
        {
            var c = new CompiledExpression<T>(expression);
            Assert.AreEqual(expectedResult, c.Eval());
        }

        public static void TestOperator(string expression, object expectedResult, Type expectedType)
        {
            var c = new CompiledExpression(expression);
            var result = c.Eval();
            Assert.AreEqual(expectedType, result.GetType());
            Assert.AreEqual(expectedResult, result);
        }

        public static void TestOperator(string expr, object expected, Type expectedType = null, Type expectedException = null, TypeRegistry typeRegistry = null, Func<CompiledExpression, object> compiler = null)
        {
            try
            {
                var c = new CompiledExpression(expr) { TypeRegistry = typeRegistry };
                object actual = null;
                actual = compiler == null ? c.Eval() : compiler(c);
                if (expectedException != null)
                {
                    Assert.Fail("Expected Exception of type {0}", expectedException.Name);
                }
                Assert.AreEqual(expected, actual);
                if (expectedType != null)
                {
                    Assert.AreEqual(expectedType, actual.GetType());
                }
            }
            catch (Exception e)
            {
                if (expectedException != null && e.GetType() != typeof(AssertFailedException))
                {
                    Assert.AreEqual(expectedException, e.GetType());
                }
                else
                {
                    throw;
                }
            }

        }


    }
}