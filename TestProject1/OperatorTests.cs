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