using ExpressionEvaluator.UnitTests.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{
    [TestClass]
    public class ScopeTests
    {

        [TestMethod]
        public void ScopeCompile()
        {
            var test = new MemberResolutionTest();
            var str = "Foobar()";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.ScopeCompile<MemberResolutionTest>();
            ret(test);
        }

        [TestMethod]
        public void ScopeCompileCall()
        {
            var test = new MemberResolutionTest();
            var str = "Foobar()";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.ScopeCompileCall<MemberResolutionTest>();
            ret(test);
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