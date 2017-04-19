using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExpressionEvaluator.Parser;
using ExpressionEvaluator.UnitTests.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{

    public static class StaticMethodTest
    {
        public static int StaticMethod()
        {
            return 42;
        }
    }

    public class ParamArrayTest
    {
        public int ParamArraySum(params int[] values)
        {
            var sum = 0;
            foreach (var value in values)
            {
                sum += value;
            }
            return sum;
        }
    }

    public class RegisterTest
    {

    }

    public class IndexTest
    {
        public string this[string index]
        {
            get
            {
                return "Hello world " + index;
            }
        }
    }

    public class PropertyNameTest
    {
        public int FooBar { get; set; }
        public int _FooBar { get; set; }
        public int Foo_Bar { get; set; }
        public int FooBar_ { get; set; }
    }

    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void FailOnNonExistentMethodResolution()
        {
            try
            {
                var test = new MemberResolutionTest();
                // Non-invocable member cannot be used like a method
                //test.Foobaz();
                var expression = "test.Foobaz()";
                var t = new TypeRegistry();
                t.RegisterSymbol("test", test);
                var c = new CompiledExpression(expression) { TypeRegistry = t };
                var ret = c.Eval();
                Assert.Fail();
            }
            catch (MemberResolutionException exception)
            {
                Assert.AreEqual("Foobaz", exception.MemberName);
                Assert.AreEqual(typeof(MemberResolutionTest), exception.Type);
            }
        }

        [TestMethod]
        public void FailOnNonExistentPropertyResolution()
        {
            try
            {
                var test = new MemberResolutionTest();
                // Only assignment, call, increment, decrement and new object expressions can be used as a statement
                //test.Foobar;
                // Cannot assign method group to implicitly-typed variable
                //var x = test.Foobar;
                var expression = "var x = test.Foobar";
                var t = new TypeRegistry();
                t.RegisterSymbol("test", test);
                var c = new CompiledExpression(expression) { TypeRegistry = t };
                var ret = c.Eval();
                Assert.Fail();
            }
            catch (MemberResolutionException exception)
            {
                Assert.AreEqual("Foobar", exception.MemberName);
                Assert.AreEqual(typeof(MemberResolutionTest), exception.Type);
            }
        }

        [TestMethod]
        public void UnderscoreInMemberNames()
        {
            var test = new PropertyNameTest
            {
                _FooBar = 21,
                Foo_Bar = 39,
                FooBar_ = 42
            };
            DefaultTest(test, "test._FooBar", test._FooBar);
            DefaultTest(test, "test.Foo_Bar", test.Foo_Bar);
            DefaultTest(test, "test.FooBar_", test.FooBar_);
        }

        [TestMethod]
        public void New()
        {
            var expected = typeof(ObjectCreationTest);
            var expression = "new testType()";
            var t = new TypeRegistry();
            t.RegisterType("testType", expected);
            var c = new CompiledExpression(expression) { TypeRegistry = t };
            var ret = c.Eval();
            var actual = ret.GetType();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MethodOverLoading()
        {
            var controlScope = new MethodOverloading();
            var testScope = new MethodOverloading();

            var exp = new CompiledExpression();
            Func<MethodOverloading, object> func;

            controlScope.sum(1, 2, 3, 4, 5, 6, 7, 8);

            exp.StringToParse = "sum(1, 2, 3, 4, 5, 6, 7, 8)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(float i, params float[] nums) 
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);

            controlScope.sum(1, 2);

            exp.StringToParse = "sum(1, 2)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(int,int) is called
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);

            controlScope.sum(1.0d, 2.0d);

            exp.StringToParse = "sum(1.0d, 2.0d)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(double, double) is called
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);

            controlScope.sum(1, 2.0d);

            exp.StringToParse = "sum(1,2.0d)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(double, double) is called (no matching int, double)
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);
        }

        [TestMethod]
        public void MethodOverLoading2()
        {
            var controlScope = new MethodOverloading();
            var testScope = new MethodOverloading();

            var exp = new CompiledExpression();
            Func<MethodOverloading, object> func;

            controlScope.sum(1.0d, 2.0d);

            exp.StringToParse = "sum(1.0d, 2.0d)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(double, double) is called
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);
        }


        [TestMethod]
        public void MethodParamArray()
        {
            var controlScope = new MethodOverloading();
            var testScope = new MethodOverloading();

            var exp = new CompiledExpression();
            Func<MethodOverloading, object> func;

            controlScope.sum(1, 2, 3, 4, 5, 6, 7, 8);

            exp.StringToParse = "sum(1, 2, 3, 4, 5, 6, 7, 8)";
            func = exp.ScopeCompile<MethodOverloading>();
            func(testScope);
            // expect sum(double, double) is called (no matching int, double)
            Assert.AreEqual(controlScope.MethodCalled, testScope.MethodCalled);
            Debug.Print("{0}", testScope.MethodCalled);
        }

        //[TestMethod]
        //public void Lambda()
        //{
        //    var tr = new TypeRegistry();
        //    tr.RegisterType("Enumerable", typeof(Enumerable));
        //    var data = new MyClass();
        //    data.Y = new List<int>() { 1, 2, 3, 4, 5, 4, 4, 3, 4, 2 };
        //    var c9 = new CompiledExpression() { StringToParse = "Enumerable.Where<int>(Y, (y) => y == 4)", TypeRegistry = tr };
        //    var f9 = c9.ScopeCompile<MyClass>();

        //    Console.WriteLine(data.X);
        //    f9(data);
        //    Console.WriteLine(data.X);
        //}

        [TestMethod]
        public void CompileToGenericFunc()
        {
            var data = new MyClass();
            data.Y = new List<int>() { 1, 2, 3, 4, 5, 4, 4, 3, 4, 2 };
            var c9 = new CompiledExpression() { StringToParse = "y == 4" };
            var f9 = c9.Compile<Func<int, bool>>("y");
            Assert.AreEqual(4, data.Y.Where(f9).Count());
        }


        [TestMethod]
        public void NullableType()
        {
            var expression = new CompiledExpression()
            {
                TypeRegistry = new TypeRegistry()
            };

            int? argument1 = 5;
            var argument2 = new Fact()
            {
                Count = 5
            };

            expression.TypeRegistry.RegisterSymbol("Argument1", argument1, typeof(int?));
            expression.TypeRegistry.RegisterSymbol("Argument2", argument2);

            var x = argument2.Count != null;
            var y = null != argument2.Count;

            expression.StringToParse = "null != Argument2.Count";
            expression.Eval();

            // Works
            expression.StringToParse = "Argument2.Count != null";
            expression.Eval();

            // Fails with NullReferenceException
            expression.StringToParse = "Argument1 != null";
            expression.Eval();
        }


        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ExpressionException()
        {
            var c = new CompiledExpression();
            c.StringToParse = "(1 + 2))";
            var result = c.Eval();
        }

        [TestMethod]
        public void GenericMethodCall()
        {
            var p1 = new Parametro("A", 12);
            var p2 = new Parametro("B", 13);
            var expected = p1.Valor<int>() + p2.Valor<int>();
            var t = new TypeRegistry();
            t.RegisterSymbol("p1", p1);
            t.RegisterSymbol("p2", p2);
            var c = new CompiledExpression() { TypeRegistry = t };
            c.StringToParse = "p1.Valor<int>() + p2.Valor<int>()";
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void GenericMethodCall2()
        {
            var p1 = new Parametro("A", 12);
            var expected = p1.Valor2(255);
            var t = new TypeRegistry();
            t.RegisterSymbol("p1", p1);
            var c = new CompiledExpression() { TypeRegistry = t };
            c.StringToParse = "p1.Valor2(255)";
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GenericMethodCall3()
        {
            var p1 = new Parametro("A", 12);
            var expected1 = p1.Valor3(1, 255, 128f);
            var expected2 = p1.Valor3(2, 255L, 128d);
            var expected3 = p1.Valor3(3, 255f, 128L);
            var t = new TypeRegistry();
            t.RegisterSymbol("p1", p1);
            var c = new CompiledExpression() { TypeRegistry = t };
            c.StringToParse = "p1.Valor3(1, 255, 128f)";
            var actual1 = c.Eval();
            c.StringToParse = "p1.Valor3(2, 255L, 128d)";
            var actual2 = c.Eval();
            c.StringToParse = "p1.Valor3(3, 255f, 128L)";
            var actual3 = c.Eval();
            Assert.AreEqual(expected1, actual1);
            Assert.AreEqual(expected1.GetType(), actual1.GetType());
            Assert.AreEqual(expected2, actual2);
            Assert.AreEqual(expected2.GetType(), actual2.GetType());
            Assert.AreEqual(expected3, actual3);
        }


        [TestMethod]
        public void ExtensionMethods()
        {
            IEnumerable<int> p1 = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var expected1 = p1.Count();
            var t = new TypeRegistry();
            var context = new CompilationContext();

            var dom = AppDomain.CreateDomain("");
            var syscore = dom.Load(new AssemblyName("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089"));

            //var syscore = Assembly.Load("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=B77A5C561934E089");
            //var syscore = Assembly.GetAssembly(typeof(System.Linq.Enumerable));
            context.Assemblies.Add(syscore);
            context.Namespaces.Add("System.Linq");

            t.RegisterSymbol("p1", p1);
            var c = new CompiledExpression() { TypeRegistry = t, Context = context };
            //c.StringToParse = "p1.Count()";
            //var actual1 = c.Eval();
            //Assert.AreEqual(expected1, actual1);

            c.StringToParse = "p1.Count(x => x >= 5)";
            var expected2 = p1.Count(x => x >= 5);
            var actual2 = c.Eval();
            Assert.AreEqual(expected2, actual2);

            //c.StringToParse = "p1.Where(x => x % 2 == 0)";
            //var expected3 = p1.Where(x => x % 2 == 0);
            //var actual3 = c.Eval();
            //Assert.AreEqual(expected3, actual3);

            AppDomain.Unload(dom);
        }


        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ExpressionException2()
        {
            var c = new CompiledExpression();
            c.StringToParse = "25L +";
            var result = c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ExpressionException3()
        {
            var c = new CompiledExpression();
            c.StringToParse = "25.L";
            var result = c.Eval();
        }

        [TestMethod]
        public void ListAndArrayIndexers()
        {
            var a = new MyClass() { Y = new List<int>() { 1, 45, 88, 22 }, Z = new[] { 7, 11, 33, 65 } };
            var t = new TypeRegistry();
            t.RegisterSymbol("a", a);
            var c = new CompiledExpression() { TypeRegistry = t };

            // Access List item by index
            c.StringToParse = "a.Y[3]";
            var result = c.Eval();
            Assert.AreEqual(a.Y[3], result);

            // Access array item by index
            c.StringToParse = "a.Z[1]";
            result = c.Eval();
            Assert.AreEqual(a.Z[1], result);
        }

        [TestMethod]
        public void CustomIndexers()
        {
            var a = new MyClass();
            var t = new TypeRegistry();
            t.RegisterSymbol("a", a);
            var c = new CompiledExpression() { TypeRegistry = t };

            c.StringToParse = "a[3]";
            var result = c.Eval();
            Assert.AreEqual(result, 3);

            c.StringToParse = "a[\"Hello World\"]";
            result = c.Eval();
            Assert.AreEqual(result, 11);
        }


        [TestMethod]
        public void ExpandoObjects()
        {
            dynamic A = new ExpandoObject();
            dynamic B = new ExpandoObject();
            A.Num1 = 1000;
            B.Num2 = 50;

            var t = new TypeRegistry();
            t.RegisterSymbol("A", A);
            t.RegisterSymbol("B", B);
            var c = new CompiledExpression() { TypeRegistry = t };
            c.StringToParse = "A.Num1 - B.Num2";
            var result = c.Eval();
            Assert.AreEqual(result, 950);

        }

        [TestMethod]
        public void EscapeCharacters()
        {
            var expected = "\\\"\a\b\f\r\n\t\v\0";
            var expression = "\"\\\\\\\"\\a\\b\\f\\r\\n\\t\\v\\0\"";
            DefaultTest(expression, expected);
        }

        [TestMethod]
        public void EmbeddedUnicodeStrings()
        {
            var expected = "\u8ba1\u7b97\u673a\u2022\u7f51\u7edc\u2022\u6280\u672f\u7c7b";
            var expression = "\"\\u8ba1\\u7b97\\u673a\\u2022\\u7f51\\u7edc\\u2022\\u6280\\u672f\\u7c7b\"";
            DefaultTest(expression, expected);
        }

        [TestMethod]
        public void EmbeddedHexStrings()
        {
            var expected = "\x010\x045\x32\x12\x1002\x444\x333\x232\x11\x0";
            var expression = "\"\\x010\\x045\\x32\\x12\\x1002\\x444\\x333\\x232\\x11\\x0\"";
            DefaultTest(expression, expected);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidEscapeCharacter()
        {
            var c = new CompiledExpression("\"\\c\"");
            c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidUnicodeLiteral()
        {
            var c = new CompiledExpression("\"\\u123\"");
            c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidHexiteral()
        {
            var c = new CompiledExpression("\"\\x\"");
            c.Eval();
        }

        //[TestMethod]
        //public void BinaryOperatorNullTypeInference()
        //{
        //    var val = 10d;
        //    var nullval = null;
        //    var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.Expression };
        //    c.StringToParse = "null != 10d";
        //    var x = nullval != val;
        //    var result = c.Eval();
        //}

        [TestMethod]
        public void ImplicitlyTypedArray()
        {
            var expected = new[] { "1", "2", "3", "4" };
            var str = "new[]{\"1\", \"2\", \"3\", \"4\"}";
            var c = new CompiledExpression(str);
            var actual = (string[])c.Eval();
            Assert.AreEqual(expected.Length, actual.Length);
            Assert.AreEqual(expected.GetType(), actual.GetType());
        }

        [TestMethod]
        public void RegisterSymbol()
        {
            var test = new RegisterTest();
            var expected = test.GetType();
            var str = "test";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            c.TypeRegistry.RegisterSymbol("test", test);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual.GetType());
        }


        [TestMethod]
        public void ParamArray()
        {
            var test = new ParamArrayTest();
            var expected = test.ParamArraySum(1, 1, 1, 1, 1);
            var str = "test.ParamArraySum(1, 1, 1, 1, 1)";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            c.TypeRegistry.RegisterSymbol("test", test);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void StaticMethod()
        {
            var expected = StaticMethodTest.StaticMethod();
            var str = "s.StaticMethod()";
            var t = new TypeRegistry();
            t.RegisterType("s", typeof(StaticMethodTest));
            var c = new CompiledExpression(str) { TypeRegistry = t };
            var actual = c.Eval();
            Assert.AreEqual(actual, expected);
        }

        [TestMethod]
        public void CustomIndex()
        {
            var test = new IndexTest();
            DefaultTest(test, "test[\"Foobar\"]", test["Foobar"]);
        }

        [TestMethod]
        public void DictionaryIndex()
        {
            var test = new Dictionary<string, string> { ["Foobar"] = "Hello world" };
            DefaultTest(test, "test[\"Foobar\"]", test["Foobar"]);
        }

        [TestMethod]
        public void ListIndex()
        {
            var test = new List<int> { 13, 21, 42 };
            DefaultTest(test, "test[2]", test[2]);
        }


        private void DefaultTest(string expression, object expected)
        {
            var c = new CompiledExpression(expression);
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        private void DefaultTest(object testObject, string expression, object expected)
        {
            var t = new TypeRegistry();
            t.RegisterSymbol("test", testObject);
            var c = new CompiledExpression(expression) { TypeRegistry = t };
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        private void BasicTest(Action<TypeRegistry> registration, string expression, object expected)
        {
            var t = new TypeRegistry();
            registration(t);
            var c = new CompiledExpression(expression) { TypeRegistry = t };
            var actual = c.Eval();
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void Lambda()
        {
            // this one is working as expected!
            const int key = 1;
            TypeRegistry t = new TypeRegistry();
            IList<int> dict = new List<int> { key };
            t.RegisterSymbol("FooBar", dict);
            CompiledExpression s = new CompiledExpression("FooBar[0]");
            s.TypeRegistry = t;
            Assert.AreEqual(1, s.Eval());
        }

        class MyContext
        {
            public readonly MyContext ctx;

            public bool NumEq(int a, int b)
            {
                return a == b;
            }

            public string Text(string text)
            {
                return text;
            }

            public int GetFirst()
            {
                var r = new Random();

                return r.Next();
            }

            public int GetSecond()
            {
                var r = new Random();

                return r.Next();
            }

        }

        [TestMethod]
        public void DirectTernaryNestingExpressionEvaluatorExperiment()
        {
            var expression = GetNestedTernary(10);
            var compiled = new CompiledExpression<dynamic>(expression);
            var result = compiled.ScopeCompile<MyContext>();
            //for (int i = 0; i < 10; i++)
            //    {
            //        var expression = GetNestedTernary(i);
            //        var compiled = new CompiledExpression<dynamic>(expression);
            //        var result = compiled.ScopeCompile<MyContext>();
            //    }
        }

        private string GetNestedTernary(int nestingCount)
        {
            var ternaryStart = "(ctx.NumEq(ctx.GetFirst(),ctx.GetSecond())?ctx.Text(\"B\"):";
            var ternary = "";

            if (nestingCount > 0)
            {
                ternary = ternaryStart + GetNestedTernary(nestingCount - 1) + ")";
            }
            else
            {
                ternary = ternaryStart + "ctx.Text(\"C\"))";
            }

            return ternary;
        }


        [TestMethod]
        public void DirectIfNestingExpressionEvaluatorExperiment()
        {
            //for (int i = 0; i < 100; i++)
            //{
            var expr = "string result; " + GetNestedIfDirect(1) + "result;";

            var expression = new CompiledExpression<dynamic>(expr);
            //var result = compiled.ScopeCompile<MyContext>();

            expression.ExpressionType = ExpressionType.StatementList;
            expression.TypeRegistry = new TypeRegistry();
            expression.TypeRegistry.RegisterDefaultTypes();
            expression.TypeRegistry.RegisterSymbol("ctx", new MyContext());
            var result = expression.Compile();

            //}
        }

        private string GetNestedIfDirect(int nestingCount)
        {
            var ternaryStart = @" if(ctx.NumEq(ctx.GetFirst(),ctx.GetSecond())){ result= ctx.Text(""B"");}else{";
            var ternary = "";

            if (nestingCount > 0)
            {
                ternary = ternaryStart + GetNestedIfDirect(nestingCount - 1) + "}";
            }
            else
            {
                ternary = ternaryStart + @"result= ctx.Text(""C"");}";
            }

            return ternary;
        }
    }

}