using System;
using System.Collections;
using System.Dynamic;
using ExpressionEvaluator.UnitTests.Domain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{
    public class DynamicContainer
    {
        public dynamic a { get; set; }
        public dynamic b { get; set; }
        public dynamic c(dynamic d, dynamic e)
        {
            return d + e;
        }
    }

    [TestClass]
    public class DynamicTests
    {
        [TestMethod]
        public void MethodOnDynamicProperty()
        {
            dynamic invoice = new ExpandoObject();
            invoice.Date = DateTime.Now;
            var val = invoice.Date.ToString("mm/dd/yyyy");
            var reg = new TypeRegistry();
            reg.RegisterSymbol("invoice", invoice);
            var expression = new CompiledExpression("invoice.Date.ToString(\"mm/dd/yyyy\")");
            expression.TypeRegistry = reg;
            var result2 = expression.Eval();
        }

        [TestMethod]
        public void BinaryOperatorsOnDynamicObjects()
        {
            var c = new DynamicContainer();
            c.a = 1;
            c.b = 2;
            var actual = c.a + c.b - c.c(c.a * c.b, c.c(c.a, c.b));
            var compiler = new CompiledExpression {  StringToParse = "a + b - c(a * b, c(a, b))" };
            var f = compiler.ScopeCompile<DynamicContainer>();
            var result = f(c);
            Assert.AreEqual(actual, result);
        }


        [TestMethod]
        public void DynamicsUnboxingTest()
        {
            var bd = new BoxedDecimal() { CanWithdraw = false, AmountToWithdraw = 12m };
            dynamic e = bd;

            var isOverDrawn = e.AmountToWithdraw > 10m;
            Assert.IsTrue(isOverDrawn);

            var t = new TypeRegistry();
            t.RegisterSymbol("e", e);
            var compiler = new CompiledExpression { TypeRegistry = t, StringToParse = "e.AmountToWithdraw > 10m" };
            compiler.Compile();
            var result = (bool)compiler.Eval();
            Assert.IsTrue(result);

        }


        [TestMethod]
        public void DynamicsTest()
        {
            //
            // Expando Objects
            //
            dynamic myObj = new ExpandoObject();
            myObj.User = "testUser";
            var t = new TypeRegistry();
            t.RegisterSymbol("myObj", myObj);
            var compiler = new CompiledExpression { TypeRegistry = t, StringToParse = "myObj.User" };
            compiler.Compile();
            var result = compiler.Eval();

            Assert.AreEqual(result, "testUser"); //test pass

            //
            // Dynamic Objects
            //
            IList testList = new ArrayList();
            testList.Add(new NameValue<string>() { Name = "User", Value = "testUserdynamic" });
            testList.Add(new NameValue<string>() { Name = "Password", Value = "myPass" });
            dynamic dynamicList = new PropertyExtensibleObject(testList);

            Assert.AreEqual(dynamicList.User, "testUserdynamic"); //test pass 
            var tr = new TypeRegistry();
            tr.RegisterSymbol("dynamicList", dynamicList);

            compiler = new CompiledExpression { TypeRegistry = tr, StringToParse = "dynamicList.User" };
            compiler.Compile();
            result = compiler.Eval();

            Assert.AreEqual(result, "testUserdynamic");

        }


        [TestMethod]
        public void DynamicValue()
        {
            var registry = new TypeRegistry();
            var obj = new IteratorTest() { Value = "aa" };
            registry.RegisterSymbol("obj", obj);
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "obj.Value == \"aa\"", TypeRegistry = registry };
            var ret = cc.Eval();
            Assert.AreEqual(true, ret);

            obj.Value = 10;
            var test = obj.Value == 10;
            cc = new CompiledExpression() { StringToParse = "obj.Value == 10", TypeRegistry = registry };
            ret = cc.Eval();
            Assert.AreEqual(true, ret);

            obj.Value = 10.0;
            cc = new CompiledExpression() { StringToParse = "obj.Value == 10", TypeRegistry = registry };
            ret = cc.Eval();
            Assert.AreEqual(true, ret);

            obj.Value = 10.0;
            cc = new CompiledExpression() { StringToParse = "obj.Value = 5", TypeRegistry = registry };
            ret = cc.Eval();
            Assert.AreEqual(5, obj.Value);

            obj.Value = 10;
            cc = new CompiledExpression() { StringToParse = "obj.Value == 10.0", TypeRegistry = registry };
            ret = cc.Eval();
            Assert.AreEqual(true, ret);
        }

        [TestMethod]
        public void DynamicMethodReturnType()
        {
            var typeRegistry = new TypeRegistry();
            dynamic obj = new Entity { Name = "MyName" };
            typeRegistry.RegisterSymbol("obj", obj);
            typeRegistry.RegisterSymbol("entity", new EntityLoader());

            var entity = new EntityLoader();
            Console.WriteLine("Expected: " + entity.GetParent(obj).Name);
            ValidateExpression<string>(typeRegistry, "entity.GetParent(obj).Name");
        }

        private static void ValidateExpression<T>(TypeRegistry typeRegistry, string expression)
        {
            var compiler = new CompiledExpression<string> { TypeRegistry = typeRegistry, StringToParse = expression };
            compiler.Compile();
            var result = compiler.Eval();
            Console.WriteLine(expression + " :: " + result);
        }

    }

    public class Entity
    {
        public string Name { get; set; }
    }

    public class EntityLoader
    {
        public dynamic GetParent(dynamic entity)
        {
            return new Entity { Name = "Parent of " + entity.Name };
        }
    }


}