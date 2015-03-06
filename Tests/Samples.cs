using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using ExpressionEvaluator;

namespace Tests
{
    public static class Samples
    {
        public static void Indexers()
        {
            var idx = new Indexers()
            {
                a = new List<int>() { 1, 2, 3, 4, 5 },
                b = new List<int>() { 1, 2, 3, 4, 5 },
                c = new List<int>() { 1, 2, 3, 4, 5 }.ToArray()
            };
            var ce1 = new CompiledExpression<int>() { StringToParse = "a[0]" };
            var res1 = ce1.ScopeCompile<Indexers>()(idx);
            //var ce2 = new CompiledExpression<int>() { StringToParse = "b[0]" };
            //var res2 = ce2.ScopeCompile<Indexers>()(idx);
            var ce3 = new CompiledExpression<int>() { StringToParse = "c[1]" };
            var res3 = ce3.ScopeCompile<Indexers>()(idx);
        }

        public static void Mono()
        {
            
        }

        public static void Sample1()
        {
            var obj2 = new objHolder2();
            obj2.Value = 5;
            Type t = Type.GetType("Mono.Runtime");
            if (t != null)
                Console.WriteLine("Mono.Runtime detected");
            else
                Console.WriteLine("Not Mono");

            var sobj = new Sub() { y = 3 };

            var tt = new Super() { DataContext = sobj, x = 2, y = true, z = true };

            tt.setVar("z", 1);

            //var a = tt.y && tt.z;
            var nn = new Sub() { x = new List<int>() { 1, 2, 3, 4, 5 } };
            object aa = new List<Sub>();

            var ee = new CompiledExpression<bool>() { StringToParse = "test = x > 1; test2 = test && true; test2;", ExpressionType = CompiledExpressionType.StatementList, DynamicTypeLookup = tt.types };
            // ee.DynamicTypeLookup.Add("z", typeof(float));
            var xx = ee.ScopeCompile<Super>();
            var yx = xx(tt);

            var rr = new CompiledExpression<int>() { StringToParse = "z(aa)", ExpressionType = CompiledExpressionType.Expression, TypeRegistry = new TypeRegistry() };
            // ee.DynamicTypeLookup.Add("z", typeof(float));
            rr.TypeRegistry.Add("aa", aa);
            var uu = rr.ScopeCompile<Sub>();
            var ff = uu(nn);


            var exp1 = "var x = new List<IImportedValue>(); ";
            exp1 += "for(int i = 0; i < 27; i++){";
            exp1 += "x.Add(new ImportedValue());";
            exp1 += "}";
            exp1 += "Console.WriteLine(x.Count);";
            exp1 += "int z = Trend(x);";
            exp1 += "x.Count;";

            {
                var reg1 = new TypeRegistry();
                reg1.RegisterType("ImportedValue", typeof(ImportedValue));
                reg1.RegisterType("IImportedValue", typeof(IImportedValue));
                reg1.RegisterType("List<IImportedValue>", typeof(List<IImportedValue>));
                reg1.RegisterType("List<ImportedValue>", typeof(List<ImportedValue>));
                reg1.RegisterType("Console", typeof(Console));
                var test1 = new Test();
                var x1 = new List<ImportedValue>();
                var ce1 = new CompiledExpression<int>() { StringToParse = exp1, TypeRegistry = reg1, ExpressionType = CompiledExpressionType.StatementList };
                var res1 = ce1.ScopeCompile<Test>()(test1);
            }


            var exp = "@ATADJ( @MAX( @SUBTR(@PR( 987043 ) , @AMT( 913000 ) ) , @MULT( @PR( 987043 ) , 0.20f ) ) , 60f ) ";
            var util = new Utility();
            var reg = new TypeRegistry();
            reg.RegisterSymbol("util", util);
            exp = exp.Replace("@", "util.");
            var ce = new CompiledExpression() { StringToParse = exp, TypeRegistry = reg };
            var res = ce.Eval();

            var registry1 = new TypeRegistry();
            var registry = new TypeRegistry();

            object obj = new objHolder() { result = false, value = NumEnum.Two };

            registry.RegisterSymbol("obj", obj);
            registry.RegisterType("objHolder", typeof(objHolder));
            registry.RegisterDefaultTypes();

            var cc = new CompiledExpression() { StringToParse = "var x = new objHolder(); x.number = 3; x.number++; var varname = 23; varname++; obj.number = varname -  x.number;", TypeRegistry = registry };
            cc.ExpressionType = CompiledExpressionType.StatementList;
            var result = cc.Eval();


            var field = new ValueHolder() { Value = "405" };
            var query = new ValueHolder() { Value = "405" };
            var ra11 = Convert.ToInt32(field.Value) >= Convert.ToInt32(query.Value);
            registry1.RegisterSymbol("field", field);
            registry1.RegisterSymbol("query", query);
            registry1.RegisterDefaultTypes();
            var c11 = new CompiledExpression() { StringToParse = "Convert.ToInt32(field.Value) >= Convert.ToInt32(query.Value)", TypeRegistry = registry1 };
            var x11 = c11.Compile();
            var r11 = x11();

            var cy = new CompiledExpression() { StringToParse = "int.Parse(\"1000\")", TypeRegistry = registry };
            var resulty = cy.Eval();

            var x = new List<String>() { "Hello", "There", "World" };
            dynamic scope = new ExpandoObject();
            scope.x = x;
            var data = new MyClass { Value = () => false, Y = new List<int>() { 1, 2, 3, 4, 4, 5, 6, 4, 2, 3 } };
            var item = new MyClass { Value = () => true };
            scope.data = data;
            scope.item = item;
            scope.i = 1;
            var a = scope.data.Value() && scope.item.Value();
            //var b = !scope.data.Value() || scope.item.Value();

            registry.RegisterSymbol("data", data);


            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            var pi = Convert.ToString(3.141592654);
            var xs = 2d;
            var pipi = 3.141592654.ToString();
            var c0 = new CompiledExpression() { StringToParse = "3.141592654.ToString()" };
            var pi2 = c0.Eval();

            var p = scope.x[0];

            // (data.Value && !item.Value) ? 'yes' : 'no'
            var c = new CompiledExpression() { StringToParse = "data.Foo(30 + data.Bar(10))", TypeRegistry = registry };
            Console.WriteLine(data.X);
            c.Eval();
            //c.Call();
            Console.WriteLine(data.X);

            var c8 = new CompiledExpression() { StringToParse = "data.X  + '%'", TypeRegistry = registry };
            Console.WriteLine(data.X);
            var cr = c8.Eval();
            Console.WriteLine(data.X);


            var c1 = new CompiledExpression() { StringToParse = "Foo()" };
            var f1 = c1.ScopeCompileCall<MyClass>();
            Console.WriteLine(data.X);
            f1(data);
            Console.WriteLine(data.X);


            var qq = (25.82).ToString("0.00", new CultureInfo("fr-FR")) + "px";
            var test = "(25.82).ToString('0.00') + 'px'";
            var cx = new CompiledExpression() { StringToParse = "int.Parse('25.82', new CultureInfo(\"fr-FR\"))" };


            var c2 = new CompiledExpression() { StringToParse = "data.Foo();" };
            var y = 12 + "px";
            var f2 = c2.ScopeCompileCall();
            Console.WriteLine(scope.data.X);
            f2(scope);
            Console.WriteLine(scope.data.X);
        }
    }
}