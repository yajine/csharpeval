using System;
using System.Text;
using ExpressionEvaluator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExpressionEvaluator;

namespace ExpressionEvaluator.Tests
{
    [TestClass]
    public class LiteralParsingTests
    {
        [TestMethod]
        [ExpectedException(typeof(ExpressionParseException))]
        public void ParseInvalidNumericThrowsException()
        {
            var str = "2.55DX";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
        }


        [TestMethod]
        public void ParseDSuffixReturnsDouble()
        {
            var str = "2.5D";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Double));
            Assert.IsTrue(Convert.ToDouble(ret) == 2.5D);
        }

        [TestMethod]
        public void ImplicitNumericCasting()
        {
            var str = "2.5D + 1";
            var c = new CompiledExpression(str);
            var ret = c.Eval();

            Assert.IsTrue(ret.GetType() == typeof(System.Double));
            Assert.IsTrue(Convert.ToDouble(ret) == 3.5D);

            c.StringToParse = "1 + 2.5d";
            ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Double));
            Assert.IsTrue(Convert.ToDouble(ret) == 3.5d);
        }



        [TestMethod]
        public void ParseFSuffixReturnsSingle()
        {
            var str = "2.5F";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Single));
            Assert.IsTrue(Convert.ToSingle(ret) == 2.5F);
        }

        [TestMethod]
        public void ParseMSuffixReturnsDecimal()
        {
            var str = "2.5M";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Decimal));
            Assert.IsTrue(Convert.ToDecimal(ret) == 2.5M);
        }

        [TestMethod]
        public void ParseLSuffixReturnsLong()
        {
            var str = "2L";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Int64));
            Assert.IsTrue(Convert.ToInt64(ret) == 2L);
        }

        [TestMethod]
        public void MixedNumericTypes()
        {
            var reg = new TypeRegistry();
            reg.RegisterType("Math", typeof(Math));
            var exp = "(1*2) + (0.8324057*1)";
            var expression = new CompiledExpression(exp) { TypeRegistry = reg };
            var value = expression.Eval();
        }

        [TestMethod]
        public void DateType()
        {
            var t = new TypeRegistry();
            t.RegisterDefaultTypes();
            var _compiledExpr = new CompiledExpression("DateTime.Now.ToString('dd/MM/yyyy')") { TypeRegistry = t };
            var vv = _compiledExpr.Eval();
        }

    }
}
