using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Domain;

namespace ExpressionEvaluator.Tests
{
    [TestClass]
    public class ScopeTests
    {

        [TestMethod]
        public void ScopeCompile()
        {
            var helper = new Helper();
            var str = "availableMethod(1)";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.ScopeCompile<Helper>();
            ret(helper);
        }

        [TestMethod]
        public void ScopeCompileCall()
        {
            var helper = new Helper();
            var str = "availableMethod(1)";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.ScopeCompileCall<Helper>();
            ret(helper);
        }

        [TestMethod]
        public void ScopeCompileTypedResultTypedParam()
        {
            var scope = new ClassA() { x = 1 };
            var target = new CompiledExpression<int>("x");
            target.ScopeCompile<ClassA>();
        }

        [TestMethod]
        public void ScopeCompileTypedResultObjectParam()
        {
            var scope = new ClassA() { x = 1 };
            var target = new CompiledExpression<int>("1");
            target.ScopeCompile();
        }
    }
}