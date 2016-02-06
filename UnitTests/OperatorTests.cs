using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Domain;

namespace ExpressionEvaluator.Tests
{
    [TestClass]
    public class OperatorTests
    {

        [TestMethod]
        public void TernaryOperator()
        {
            TestHelpers.TestOperator("3 == 2 ? 4 : 5 == 5 ? 3 : 2", 3 == 2 ? 4 : 5 == 5 ? 3 : 2);
        }

        [TestMethod]
        public void OperatorPrecedence()
        {
            TestHelpers.TestOperator("1 + 2 * 3", 1 + 2 * 3);
        }

        [TestMethod]
        public void BracketGrouping()
        {
            TestHelpers.TestOperator("((1 + (3 - 1)) * (12 / 4))", ((1 + (3 - 1)) * (12 / 4)));
        }

        [TestMethod]
        public void AddImplicitIntegersReturnsInteger()
        {
            TestHelpers.TestOperator("1 + 1", 1 + 1, typeof(System.Int32));
        }

        [TestMethod]
        public void Add()
        {
            TestHelpers.TestOperator("1 + 1", 1 + 1);
        }

        [TestMethod]
        public void AddMultiple()
        {
            TestHelpers.TestOperator("1 + 1 + 1", 1 + 1 + 1);
        }

        [TestMethod]
        public void AdditiveMixed1()
        {
            TestHelpers.TestOperator("1 + 2 - 3", 1 + 2 - 3);
        }

        [TestMethod]
        public void AdditiveMixed2()
        {
            TestHelpers.TestOperator("1 - 2 + 3", 1 - 2 + 3);
        }

        [TestMethod]
        public void Subtract()
        {
            TestHelpers.TestOperator("1 - 1", 1 - 1);
        }

        [TestMethod]
        public void Multiply()
        {
            TestHelpers.TestOperator("4 * 3", 4 * 3);
        }

        [TestMethod]
        public void Divide()
        {
            TestHelpers.TestOperator("6 / 3", 6 / 3);
        }

        [TestMethod]
        public void Modulo()
        {
            TestHelpers.TestOperator("9 % 2", 9 % 2);
        }

        [TestMethod]
        public void Equal()
        {
            TestHelpers.TestOperator("1 == 1", 1 == 1);
        }

        [TestMethod]
        public void NotEqual()
        {
            TestHelpers.TestOperator("1 != 2", 1 != 2);
        }

        [TestMethod]
        public void UnaryNegation()
        {
            TestHelpers.TestOperator("-1", -1);
        }

        [TestMethod]
        public void And()
        {
            TestHelpers.TestOperator("true && false", true && false);
        }

        [TestMethod]
        public void Or()
        {
            TestHelpers.TestOperator("true || false", true || false);
        }

        [TestMethod]
        public void Xor()
        {
            TestHelpers.TestOperator("false ^ true", false ^ true);
        }

        [TestMethod]
        public void Not()
        {
            TestHelpers.TestOperator("!true", !true);
        }

        [TestMethod]
        public void NumericPromotion()
        {
            var t = new TypeRegistry();

            TestHelpers.TestOperator("1.5m + 2", 1.5m + 2, typeof(decimal));
            TestHelpers.TestOperator("1.5m - 2", 1.5m - 2, typeof(decimal));
            TestHelpers.TestOperator("2 * 1.5m", 2 * 1.5m, typeof(decimal));
            TestHelpers.TestOperator("1.5m / 2", 1.5m / 2, typeof(decimal));
            TestHelpers.TestOperator("15m % 2", 15m % 2, typeof(decimal));
            TestHelpers.TestOperator("1.5m * 2", 1.5m * 2, typeof(decimal));
            TestHelpers.TestOperator("1.5m / 2", 1.5m / 2, typeof(decimal));
            TestHelpers.TestOperator("1.5m + 2f", null, null, typeof(InvalidOperationException));
            TestHelpers.TestOperator("1.5d - 2m", null, null, typeof(InvalidOperationException));

            TestHelpers.TestOperator("64L & 2", 64L & 2, typeof(long));
            TestHelpers.TestOperator("64L | 2", 64L | 2, typeof(long));
            TestHelpers.TestOperator("64L ^ 2", 64L ^ 2, typeof(long));

            TestHelpers.TestOperator("64 & 2L", 64 & 2L, typeof(long));
            TestHelpers.TestOperator("64 | 2L", 64 | 2L, typeof(long));
            TestHelpers.TestOperator("64 ^ 2L", 64 ^ 2L, typeof(long));

            TestHelpers.TestOperator("1.5d + 2f", 1.5d + 2f, typeof(double));

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
            TestHelpers.TestOperator("64UL - 2", 64UL - 2, typeof(ulong));
            // This should pass since both of them are constants
            TestHelpers.TestOperator("ulongValue - 2", ulongValue - 2, typeof(ulong), null, t);
            // This should pass, since we still passing constants 
            TestHelpers.TestOperator("ulongValueC - intValueC", ulongValueC - intValueC, null, null, t);
            // WARNING! even though we registered variables into the typeregistry, the typeregistry 
            // will expose them to the compiler as CONSTANTS and implicit conversion treats them as such!
            TestHelpers.TestOperator("ulongValue - intValue", 64ul - 2, null, null, t);
            // This should fail as we compiled the symbols as parameters so implicit conversion works
            // on the actual values!!!
            TestHelpers.TestOperator("ulongValue - intValue", null, null, typeof(InvalidOperationException), t, expression => expression.Compile<Func<ulong, int, object>>("ulongValue", "intValue")(ulongValue, intValue));

            TestHelpers.TestOperator("64UL + 2L", 64UL + 2L, typeof(ulong));
            TestHelpers.TestOperator("64UL + -2L", null /* 64UL + -2L*/, null, typeof(InvalidOperationException));

            var results = shortValue + sbyteValue;
            // expect type int
            TestHelpers.TestOperator("a + b", results, typeof(int), null, null, expression => expression.Compile<Func<short, sbyte, int>>("a", "b")(shortValue, sbyteValue));

            //TestHelpers.TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            //TestHelpers.TestExpression("1.5m / 2", 1.5m / 2, typeof(decimal));
            //TestHelpers.TestExpression("1.5m * 2", 1.5m / 2);
            //TestHelpers.TestExpression("30d % 2", 1.5m / 2);
            //TestHelpers.TestExpression("1.5m / 2", 1.5m / 2);

        }
        [TestMethod]
        public void Assignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 0, "x = 1", 1);
        }

        [TestMethod]
        public void AddAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 1, "x += 9", 10);
        }

        [TestMethod]
        public void SubtractAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 10, "x -= 4", 6);
        }

        [TestMethod]
        public void MultiplyAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 6, "x *= 5", 30);
        }

        [TestMethod]
        public void DivideAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 30, "x /= 2", 15);
        }

        [TestMethod]
        public void ModuloAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 15, "x %= 13", 2);
        }

        [TestMethod]
        public void LeftShiftAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 2, "x <<= 4", 32);
        }

        [TestMethod]
        public void RightShiftAssignment()
        {
            TestHelpers.TestAssignment<ClassA, int>(scope => scope.x, scope => scope.x = 32, "x >>= 1", 16);
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