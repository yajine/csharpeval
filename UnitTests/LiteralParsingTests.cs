using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{
    [TestClass]
    public class LiteralParsingTests
    {
        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ParseInvalidNumericThrowsException()
        {
            var str = "2.55DX";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
        }



        [TestMethod]
        public void ImplicitNumericCasting()
        {
            var expression = "2.5D + 1";
            var expected = 2.5D + 1;
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.IsTrue(actual is double);
            Assert.AreEqual(expected, actual);

            expression = "1 + 2.5D";
            expected = 1 + 2.5D;
            c = new CompiledExpression(expression);
            actual = c.Eval();
            Assert.IsTrue(actual is double);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ExplicitDecimal()
        {
            var expression = "2.5M";
            var expected = 2.5M;
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);

            expression = "2.5m";
            expected = 2.5m;
            c = new CompiledExpression(expression);
            actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ExplicitDouble()
        {
            var expression = "2.5D";
            var expected = 2.5D;
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);

            expression = "2.5d";
            expected = 2.5d;
            c = new CompiledExpression(expression);
            actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void ExplicitSingle()
        {
            var expression = "2.5F";
            var expected = 2.5F;
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);

            expression = "2.5f";
            expected = 2.5f;
            c = new CompiledExpression(expression);
            actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ExplicitLong()
        {
            var expression = "25L";
            var expected = 25L;
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);

            expression = "25l";
            expected = 25l;
            c = new CompiledExpression(expression);
            actual = c.Eval();
            Assert.AreEqual(expected, actual);
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
            var _compiledExpr = new CompiledExpression("DateTime.Now.ToString(\"dd/MM/yyyy\")") { TypeRegistry = t };
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
            var expression = "-9223372036854775808";
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual, "Input: <{0}>", expression);
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
        public void DoubleLowestMinimum()
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
    }
}
