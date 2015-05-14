using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Domain;

namespace ExpressionEvaluator.Tests
{
    [TestClass]
    public class OperatorTests
    {

        [TestMethod]
        public void Assignment()
        {
            var str = "c.x = 1";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var cont = new Container();
            c.TypeRegistry.RegisterSymbol("c", cont);
            c.TypeRegistry.RegisterType("p", typeof(Math));
            var ret = c.Eval();
            Assert.AreEqual(ret, 1);
            Assert.AreEqual(ret, cont.x);
        }


        [TestMethod]
        public void TernaryOperator()
        {
            var str = "3 == 2 ? 4 : 5 == 5 ? 3 : 2";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            var y = 3 == 2 ? 4 : 5 == 5 ? 3 : 2;
            Assert.AreEqual(ret, 3);
            Assert.AreEqual(ret, y);
        }

        [TestMethod]
        public void OperatorPrecedence()
        {
            var str = "1 + 2 * 3";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            var y = 1 + 2 * 3;
            Assert.AreEqual(ret, 7);
            Assert.AreEqual(ret, y);
        }

        [TestMethod]
        public void BracketGrouping()
        {
            var str = "(1 + 2) * 3";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            var y = (1 + 2) * 3;
            Assert.AreEqual(ret, 9);
            Assert.AreEqual(ret, y);
        }

        [TestMethod]
        public void AddImplicitIntegersReturnsInteger()
        {
            var str = "1 + 1";
            var c = new CompiledExpression(str);
            var ret = c.Eval();
            Assert.IsTrue(ret.GetType() == typeof(System.Int32));
            Assert.IsTrue(Convert.ToInt32(ret) == 2);
        }

        [TestMethod]
        public void Add()
        {
            var str = "1 + 1";
            var c = new CompiledExpression<int>(str);
            var ret = c.Eval();
            Assert.IsTrue(ret == 2);
        }

        [TestMethod]
        public void Subtract()
        {
            var str = "1 - 1";
            var c = new CompiledExpression<int>(str);
            var ret = c.Eval();
            Assert.IsTrue(Convert.ToInt32(ret) == 0);
        }


        [TestMethod]
        public void UnaryNegation()
        {
            var x = -1 - 10;
            var str = "-1 - 10";
            var c = new CompiledExpression<int>(str);
            var ret = c.Eval();
            Assert.IsTrue(Convert.ToInt32(ret) == x);
        }

        private void TestExpression(string expr, object expected, Type expectedType = null, Type expectedException = null, TypeRegistry typeRegistry = null, Func<CompiledExpression, object> compiler = null)
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

        [TestMethod]
        public void NumericPromotion()
        {
            var t = new TypeRegistry();

            TestExpression("1.5m + 2", 1.5m + 2, typeof(decimal));
            TestExpression("1.5m - 2", 1.5m - 2, typeof(decimal));
            TestExpression("2 * 1.5m", 2 * 1.5m, typeof(decimal));
            TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            TestExpression("15m % 2", 15m % 2, typeof(decimal));
            TestExpression("1.5m * 2", 1.5m * 2, typeof(decimal));
            TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            TestExpression("1.5m + 2f", null, null, typeof(InvalidOperationException));
            TestExpression("1.5d - 2m", null, null, typeof(InvalidOperationException));

            TestExpression("64L & 2", 64L & 2, typeof(long));
            TestExpression("64L | 2", 64L | 2, typeof(long));
            TestExpression("64L ^ 2", 64L ^ 2, typeof(long));

            TestExpression("64 & 2L", 64 & 2L, typeof(long));
            TestExpression("64 | 2L", 64 | 2L, typeof(long));
            TestExpression("64 ^ 2L", 64 ^ 2L, typeof(long));

            TestExpression("1.5d + 2f", 1.5d + 2f, typeof(double));

            ulong ulongValue = 64ul;
            int intValue = 2;
            sbyte sbyteValue = 64;
            short shortValue = 64;


            const ulong ulongValueC = 64ul;
            const int intValueC = 2;
            const long longValueC = 2;
            const long negLongValueC = -2;

            // permitted
            var aa = ulongValueC + longValueC;

            // var bb = ulongValueC + negLongValueC;

            t.Add("ulongValue", ulongValue);
            t.Add("intValue", intValue);

            t.Add("ulongValueC", ulongValueC);
            t.Add("intValueC", intValueC);
            // This should pass since these are constants
            TestExpression("64UL - 2", 64UL - 2, typeof(ulong));
            // This should pass since both of them are constants
            TestExpression("ulongValue - 2", ulongValue - 2, typeof(ulong), null, t);
            // This should pass, since we still passing constants 
            TestExpression("ulongValueC - intValueC", ulongValueC - intValueC, null, null, t);
            // WARNING! even though we registered variables into the typeregistry, the typeregistry 
            // will expose them to the compiler as CONSTANTS and implicit conversion treats them as such!
            TestExpression("ulongValue - intValue", 64ul - 2, null, null, t);
            // This should fail as we compiled the symbols as parameters so implicit conversion works
            // on the actual values!!!
            TestExpression("ulongValue - intValue", null, null, typeof(InvalidOperationException), t, expression => expression.Compile<Func<ulong, int, object>>("ulongValue", "intValue")(ulongValue, intValue));

            TestExpression("64UL + 2L", 64UL + 2L, typeof(ulong));
            TestExpression("64UL + -2L", null /* 64UL + -2L*/, null, typeof(InvalidOperationException));

            var results = shortValue + sbyteValue;
            // expect type int
            TestExpression("a + b", results, typeof(int), null, null, expression => expression.Compile<Func<short, sbyte, int>>("a", "b")(shortValue, sbyteValue));

            //TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            //TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            //TestExpression("1.5m * 2", 1.5m / 2);
            //TestExpression("30d % 2", 1.5m / 2);
            //TestExpression("1.5m / 2", 1.5m / 2);

        }


        [TestMethod]
        public void AssignmentOperators()
        {
            var classA = new ClassA();

            var exp = new CompiledExpression();
            Func<ClassA, object> func;

            exp.StringToParse = "x = 1";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(1, classA.x);

            exp.StringToParse = "x += 9";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(10, classA.x);

            exp.StringToParse = "x -= 4";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(6, classA.x);

            exp.StringToParse = "x *= 5";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(30, classA.x);

            exp.StringToParse = "x /= 2";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(15, classA.x);

            exp.StringToParse = "x %= 13";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(2, classA.x);

            exp.StringToParse = "x <<= 4";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(32, classA.x);

            exp.StringToParse = "x >>= 1";
            func = exp.ScopeCompile<ClassA>();
            func(classA);
            Assert.AreEqual(16, classA.x);
        }


        [TestMethod]
        public void OverloadedBinaryOperators()
        {
            var registry = new TypeRegistry();
            var target = new CompiledExpression() { TypeRegistry = registry };

            var x = new TypeWithOverloadedBinaryOperators(3);
            registry.RegisterSymbol("x", x);

            string y = "5";
            Assert.IsFalse(x == y);
            target.StringToParse = "x == y";
            Assert.IsFalse(target.Compile<Func<string, bool>>("y")(y));

            y = "3";
            Assert.IsTrue(x == y);
            target.StringToParse = "x == y";
            Assert.IsTrue(target.Compile<Func<string, bool>>("y")(y));

            target.StringToParse = "x == \"4\"";
            Assert.IsFalse(target.Compile<Func<bool>>()());
            target.StringToParse = "x == \"3\"";
            Assert.IsTrue(target.Compile<Func<bool>>()());
        }

        struct TypeWithOverloadedBinaryOperators
        {
            private int _value;

            public TypeWithOverloadedBinaryOperators(int value)
            {
                _value = value;
            }

            public static bool operator ==(TypeWithOverloadedBinaryOperators instance, string value)
            {
                return instance._value.ToString().Equals(value);
            }

            public static bool operator !=(TypeWithOverloadedBinaryOperators instance, string value)
            {
                return !instance._value.ToString().Equals(value);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;
                if (obj is TypeWithOverloadedBinaryOperators)
                {
                    return this._value.Equals(((TypeWithOverloadedBinaryOperators)obj)._value);
                }
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return _value.GetHashCode();
            }
        }
    }
}