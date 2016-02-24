using System;
using System.Globalization;
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
            var expected = DateTime.Now.ToString("dd/MM/yyyy");
            var actual = _compiledExpr.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Long()
        {
            var expected = 123456789012345;
            var str = "123456789012345";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }


        [TestMethod]
        public void ULongMax()
        {
            var expected = 18446744073709551615;
            var str = "18446744073709551615";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }


        [TestMethod]
        public void LongMin()
        {
            var expected = -9223372036854775808;
            var str = "-9223372036854775808";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }

        [TestMethod]
        public void LongInvalidMin()
        {
            // ambiguous invocation -(decimal|double|float)
            //var expected = -9223372036854775809;
            try
            {
                var str = "-9223372036854775809";
                var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
                var ret = c.Eval();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("Ambiguous invocation"));
            }
            //Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }

        [TestMethod]
        public void Double()
        {
            var expected = -9223372036854775809d;
            var str = "-9223372036854775809d";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var actual = c.Eval();
            Assert.AreEqual(expected, actual, "Input: <{0}>", str);
        }
        
        [TestMethod]
        public void LongMax()
        {
            var expected = 9223372036854775807;
            var str = "9223372036854775807";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }

        [TestMethod]
        public void VerbatimStringLiteral()
        {
            var str = "@\"this is \\a test\"";
            var expected = @"this is \a test";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(expected, ret, "Input: <{0}>", str);
        }
    }
}
