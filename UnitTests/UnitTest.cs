using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ExpressionEvaluator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1;
using UnitTestProject1.Domain;

namespace ExpressionEvaluator.Tests
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void UnavailableMethodThrowsException()
        {
            try
            {
                var str = "var x = helper.availableMethod(someparameter);\r\nvar y = helper.unavailableMethod(someparameter);";
                var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry(), ExpressionType = CompiledExpressionType.StatementList };
                var helper = new Helper();
                var someparameter = 1;
                c.TypeRegistry.RegisterSymbol("helper", helper);
                c.TypeRegistry.RegisterSymbol("someparameter", someparameter);
                var ret = c.Eval();
                Assert.Fail();
            }
            catch (ExpressionParseException exception)
            {
                var regex = new Regex("Cannot resolve member \"(\\w\\S+)\" on type \"(\\w\\S+)\"");
                var m = regex.Match(exception.Message);
                Assert.AreEqual(m.Groups[1].Value, "unavailableMethod");
                Assert.AreEqual(m.Groups[2].Value, "Helper");
            }
        }

        [TestMethod]
        public void UnavailablePropertyThrowsException()
        {
            try
            {
                var str = "var x = helper.availableProperty;\r\nvar y = helper.unavailableProperty;";
                var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry(), ExpressionType = CompiledExpressionType.StatementList };
                var helper = new Helper() { availableProperty = 1 };
                var someparameter = 1;
                c.TypeRegistry.RegisterSymbol("helper", helper);
                var ret = c.Eval();
                Assert.Fail();
            }
            catch (ExpressionParseException exception)
            {
                var regex = new Regex("Cannot resolve member \"(\\w\\S+)\" on type \"(\\w\\S+)\"");
                var m = regex.Match(exception.Message);
                Assert.AreEqual(m.Groups[1].Value, "unavailableProperty");
                Assert.AreEqual(m.Groups[2].Value, "Helper");
            }
        }

        [TestMethod]
        public void UnderscoreVariables()
        {
            var str = "1 | VARIABLE_NAME | _VARNAME";
            var t = new TypeRegistry();
            t.RegisterSymbol("VARIABLE_NAME", 16);
            t.RegisterSymbol("_VARNAME", 32);
            var c = new CompiledExpression(str) { TypeRegistry = t };
            var ret = c.Eval();
        }

        [TestMethod]
        public void New()
        {
            var str = "new TestClass(123)";
            var t = new TypeRegistry();
            t.RegisterType("TestClass", typeof(TestClass));
            var c = new CompiledExpression<TestClass>(str) { TypeRegistry = t };
            var ret = c.Eval();
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
        [ExpectedException(typeof(ExpressionParseException))]
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
            var c = new CompiledExpression() { TypeRegistry = t, Context = context};
            //c.StringToParse = "p1.Count()";
            //var actual1 = c.Eval();
            //Assert.AreEqual(expected1, actual1);

            c.StringToParse = "p1.Count(x => x >= 5)";
            var actual2 = c.Eval();
            Assert.AreEqual(expected2, actual2);

            
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
        [ExpectedException(typeof(ExpressionParseException))]
        public void ExpressionException2()
        {
            var c = new CompiledExpression();
            c.StringToParse = "25L +";
            var result = c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(ExpressionParseException))]
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
            Assert.AreEqual(result, 22);

            // Access array item by index
            c.StringToParse = "a.Z[1]";
            result = c.Eval();
            Assert.AreEqual(result, 11);
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

        public class Z
        {
            public string z { get; set; }
        }

        private string CreateEmbeddedString(string text)
        {
            return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        [TestMethod]
        public void Escaping()
        {
            var x = "//table[@id=\"ct_lookup\"]/descendant::tr[last()]/th[2]/descendant::button[@type=\"submit\"]";
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            var w = CreateEmbeddedString(x);
            Assert.AreNotEqual(x, w);
            c.StringToParse = "var x = \"" + w + "\";";
            var z = new Z { z = x };
            var func = c.ScopeCompile<Z>();
            var result = func(z);
            Assert.AreEqual(x, result);
        }

        [TestMethod]
        public void EmbeddedUnicodeStrings()
        {
            var x = "\u8ba1\u7b97\u673a\u2022\u7f51\u7edc\u2022\u6280\u672f\u7c7b";
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\u8ba1\\u7b97\\u673a\\u2022\\u7f51\\u7edc\\u2022\\u6280\\u672f\\u7c7b\";";
            var z = new Z { z = x };
            var func = c.ScopeCompile<Z>();
            var result = func(z);
            Assert.AreEqual(x, result);
        }

        [TestMethod]
        public void EmbeddedHexStrings()
        {
            var x = "\x010\x045\x32\x12\x1002\x444\x333\x232\x11\x0";
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\x010\\x045\\x32\\x12\\x1002\\x444\\x333\\x232\\x11\\x0\";";
            var z = new Z { z = x };
            var func = c.ScopeCompile<Z>();
            var result = func(z);
            Assert.AreEqual(x, result);
        }

        [TestMethod]
        public void EmbeddedEscapeStrings()
        {
            var x = "\a\b\f\r\n\t\v\0";
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\a\\b\\f\\r\\n\\t\\v\\0\";";
            var z = new Z { z = x };
            var func = c.ScopeCompile<Z>();
            var result = func(z);
            Assert.AreEqual(x, result);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidEscapeLiteral()
        {
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\c\";";
            var result = c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidUnicodeLiteral()
        {
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\u123\";";
            var result = c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InvalidHexiteral()
        {
            var c = new CompiledExpression() { ExpressionType = CompiledExpressionType.StatementList };
            c.StringToParse = "var x = \"\\x\";";
            var result = c.Eval();
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
        public void StringJoin()
        {
  
            var str = "String.Join(\",\", new[]{1, 2, 3, 4})";
            var c = new CompiledExpression(str) { TypeRegistry = new TypeRegistry() };
            var ret = c.Eval();
            Assert.AreEqual(ret, "1,2,3,4");
        }

        public class IndexableType
        {
            public string this[string name] => "Hello world";
        }

        [TestMethod]
        public void CustomIndexer()
        {
            TypeRegistry t = new TypeRegistry();
            t.RegisterSymbol("MyObject", new IndexableType());
            CompiledExpression s = new CompiledExpression("MyObject[\"asdf\"]");
            s.TypeRegistry = t;
            Assert.AreEqual("Hello world", s.Eval());
        }

        [TestMethod]
        public void DictionaryIndexer()
        {
            const string key = "asdf";
            const string helloWorld = "Hello world";

            TypeRegistry t = new TypeRegistry();
            Dictionary<string, string> dict = new Dictionary<string, string> { [key] = helloWorld };
            t.RegisterSymbol("FooBar", dict);
            CompiledExpression s = new CompiledExpression("FooBar[\"asdf\"]");
            s.TypeRegistry = t;
            Assert.AreEqual(helloWorld, s.Eval());
        }

        [TestMethod]
        public void ListIndexer()
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

    }
}