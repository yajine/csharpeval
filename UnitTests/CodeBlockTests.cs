using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Domain;

namespace ExpressionEvaluator.Tests
{
    [TestClass]
    public class CodeBlockTests
    {
        [TestMethod]
        public void LocalImplicitVariables()
        {
            var registry = new TypeRegistry();

            object obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "var x = new objHolder(); x.number = 3; x.number++; var varname = 23; varname++; obj.number = varname -  x.number;", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            var result = cc.Eval();
        }

        [TestMethod]
        public void DoWhileLoop()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };

            do { obj.number2++; } while (obj.number2 < 10);

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "do{ obj.number++; } while (obj.number < 10);", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(obj.number2, obj.number);
        }


        [TestMethod]
        public void WhileLoop()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };

            while (obj.number2 < 10) { obj.number2++; }

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "while (obj.number < 10) { obj.number++; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(obj.number2, obj.number);
        }


        [TestMethod]
        public void ForLoop()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            for (var i = 0; i < 10; i++) { obj.number2++; }

            var cc = new CompiledExpression() { StringToParse = "for(var i = 0; i < 10; i++) { Debug.WriteLine(i.ToString()); obj.number++; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(obj.number2, obj.number);
        }

        [TestMethod]
        public void ForLoopWithMultipleIterators()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            for (int i = 0, j = 0; i < 10; i++, j++) { obj.number2 = j; }

            var cc = new CompiledExpression() { StringToParse = "for(int i = 0, j = 0; i < 10; i++, j++) { obj.number = j; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(9, obj.number);
            Assert.AreEqual(obj.number2, obj.number);
        }

        [TestMethod]
        public void ForEachLoop()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { iterator = new List<string>() { "Hello", "there", "world" } };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            //var iterator = new List<string>() { "Hello", "there", "world" };
            //var enumerator = iterator.GetEnumerator();
            //while (enumerator.MoveNext())
            //{
            //    var word = enumerator.Current;
            //    Debug.WriteLine(word);
            //}

            var cc = new CompiledExpression() { StringToParse = "foreach(var word in obj.iterator) { Debug.WriteLine(word); }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
        }

        [TestMethod]
        public void ForEachLoopNoBlock()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { iterator = new List<string>() { "Hello", "there", "world" } };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            //var iterator = new List<string>() { "Hello", "there", "world" };
            //var enumerator = iterator.GetEnumerator();
            //while (enumerator.MoveNext())
            //{
            //    var word = enumerator.Current;
            //    Debug.WriteLine(word);
            //}

            var cc = new CompiledExpression() { StringToParse = "foreach(var word in obj.iterator) Debug.WriteLine(word);", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
        }


        [TestMethod]
        public void ForEachLoopArray()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { stringIterator = new[] { "Hello", "there", "world" } };

            //foreach (var word in obj.stringIterator) { Debug.WriteLine(word); }

            var enumerator = obj.stringIterator.GetEnumerator();
            while (enumerator.MoveNext())
            {
                string word = (string)enumerator.Current;
                Debug.WriteLine(word);
            }

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "foreach(var word in obj.stringIterator) { Debug.WriteLine(word); }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
        }


        [TestMethod]
        public void ForLoopWithContinue()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };
            var obj2 = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            for (var i = 0; i < 10; i++) { obj2.number++; if (i > 5) continue; obj2.number2++; }

            var cc = new CompiledExpression() { StringToParse = "for(var i = 0; i < 10; i++) { obj.number++; if(i > 5) continue; obj.number2++; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(obj2.number, obj.number);
            Assert.AreEqual(obj2.number2, obj.number2);
        }

        [TestMethod]
        public void ForLoopWithBreak()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };
            var obj2 = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            for (var i = 0; i < 10; i++) { obj2.number++; if (i > 5) break; obj2.number2++; }

            var cc = new CompiledExpression() { StringToParse = "for(var i = 0; i < 10; i++) { obj.number++; if(i > 5) break; obj.number2++; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(obj2.number, obj.number);
            Assert.AreEqual(obj2.number2, obj.number2);
        }

        [TestMethod]
        public void WhileLoopWithBreak()
        {
            var registry = new TypeRegistry();

            var obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "while (obj.number < 10) { obj.number++; if(obj.number == 5) break; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(5, obj.number);
        }

        [TestMethod]
        public void NestedWhileLoopWithBreak()
        {
            var registry = new TypeRegistry();
            var obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("Debug", typeof(Debug));
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();
            var cc = new CompiledExpression() { StringToParse = "while (obj.number < 10) { Debug.WriteLine(obj.number.ToString()); obj.number++; while (obj.number2 < 10) { Debug.WriteLine(obj.number2.ToString()); obj.number2++; if(obj.number2 == 5) break;  }  if(obj.number == 5) break; }", TypeRegistry = registry };
            cc.ExpressionType = ExpressionType.StatementList;
            cc.Eval();
            Assert.AreEqual(5, obj.number);
            Assert.AreEqual(10, obj.number2);
        }


        [TestMethod]
        public void IfThenElseStatementList()
        {
            var a = new ClassA() { x = 1 };
            var t = new TypeRegistry();
            t.RegisterSymbol("a", a);
            var p = new CompiledExpression { StringToParse = "if (a.x == 1) a.y = 2; else { a.y = 3; } a.z = a.y;", TypeRegistry = t };
            p.ExpressionType = ExpressionType.StatementList;
            var f = p.Eval();
            Assert.AreEqual(a.y, 2);
            Assert.AreEqual(a.y, a.z);
        }

        [TestMethod]
        public void SwitchStatement()
        {
            var a = new ClassA() { x = 1 };
            var t = new TypeRegistry();

            for (a.x = 1; a.x < 7; a.x++)
            {
                switch (a.x)
                {
                    case 1:
                    case 2:
                        Debug.WriteLine("Hello");
                        break;
                    case 3:
                        Debug.WriteLine("There");
                        break;
                    case 4:
                        Debug.WriteLine("World");
                        break;
                    default:
                        Debug.WriteLine("Undefined");
                        break;
                }
            }

            t.RegisterSymbol("Debug", typeof(Debug));
            var p = new CompiledExpression { StringToParse = "switch(x) { case 1: case 2: Debug.WriteLine(\"Hello\"); break; case 3: Debug.WriteLine(\"There\"); break; case 4: Debug.WriteLine(\"World\"); break; default: Debug.WriteLine(\"Undefined\"); break; }", TypeRegistry = t };
            p.ExpressionType = ExpressionType.StatementList;
            var func = p.ScopeCompile<ClassA>();
            for (a.x = 1; a.x < 7; a.x++)
            {
                func(a);
            }
        }



        //[TestMethod]
        //public void Return()
        //{
        //    var t = new TypeRegistry();

        //    var p = new CompiledExpression<bool> { StringToParse = "return true;", TypeRegistry = t };
        //    p.ExpressionType = CompiledExpressionType.StatementList;
        //    Assert.AreEqual(true, p.Compile()());

        //    p.StringToParse = "var x = 3; if (x == 3) { return true; } return false;";
        //    Assert.AreEqual(true, p.Compile()());

        //    p.StringToParse = "var x = 2; if (x == 3) { return true; } ";
        //    Assert.AreEqual(true, p.Compile()());

        //    p.StringToParse = "var x = true; x;";
        //    Assert.AreEqual(true, p.Compile()());
        //}

        //[TestMethod]
        //public void SwitchReturn()
        //{
        //    var a = new ClassA() { x = 1 };
        //    var t = new TypeRegistry();

        //    var p = new CompiledExpression { StringToParse = "var retval = 'Exit'; switch(x) { case 1: case 2: return 'Hello'; case 3: return 'There'; case 4: return 'World'; default: return 'Undefined'; } return retval;", TypeRegistry = t };
        //    p.ExpressionType = CompiledExpressionType.StatementList;
        //    var func = p.ScopeCompile<ClassA>();
        //    for (a.x = 1; a.x < 7; a.x++)
        //    {
        //        Debug.WriteLine(func(a));
        //    }
        //}


    }
}