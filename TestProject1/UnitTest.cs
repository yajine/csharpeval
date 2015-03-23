using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
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
            catch (ParseException exception)
            {
                var regex = new Regex("Cannot resolve member \"(\\w\\S+)\" on type \"(\\w\\S+)\"");
                var m = regex.Match(((ExpressionParseException)exception.InnerException).Message);
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
            catch (ParseException exception)
            {
                var regex = new Regex("Cannot resolve member \"(\\w\\S+)\" on type \"(\\w\\S+)\"");
                var m = regex.Match(((ExpressionParseException)exception.InnerException).Message);
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
        [ExpectedException(typeof(ParseException))]
        public void ExpressionException()
        {
            var c = new CompiledExpression();
            c.StringToParse = "(1 + 2))";
            var result = c.Eval();
        }


        [TestMethod]
        [ExpectedException(typeof(ParseException))]
        public void ExpressionException2()
        {
            var c = new CompiledExpression();
            c.StringToParse = "25L +";
            var result = c.Eval();
        }

        [TestMethod]
        [ExpectedException(typeof(ParseException))]
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


    }
}