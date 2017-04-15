using System.Diagnostics;
using System.Dynamic;
using ExpressionEvaluator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UnitTestProject1.Domain;

namespace UnitTestProject1
{
    [TestClass]
    public class DynamicsPerformanceTest
    {
        [TestMethod]
        public void DynamicsAssignmentTest()
        {
            var expr = "settings.showAsteriskMessage = true;";
            expr += "settings.showStatisticallySignificantExplanation = page.HasSignificantScore;";
            expr += "rowHeightNum = helper.getRowHeight(data);";
            expr += "rowHeight = rowHeightNum.ToString() + \"px\";";
            expr += "barHeight = (rowHeightNum - 3).ToString() + \"px\";";
            expr += "showPaging = count > 1;";
            dynamic scope = new ExpandoObject();
            scope.rowHeightNum = 10;
            scope.count = 2;
            scope.page = new Page();
            scope.settings = new ExpandoObject();
            var p = new CompiledExpression { StringToParse = expr };
            p.ExpressionType = ExpressionType.StatementList;
            var f = p.ScopeCompile<ExpandoObject>();
            f(scope);
            Assert.AreEqual(true, scope.settings.showAsteriskMessage);
            Assert.AreEqual("10px", scope.rowHeight);
            Assert.AreEqual("7px", scope.barHeight);
            Assert.AreEqual(true, scope.showPaging);
        }

        [TestMethod]
        public void DynamicsTest()
        {
            dynamic scope = new ExpandoObject();
            var fc = new FunctionCache();

            scope.Property1 = 5;
            scope.Property2 = 6;
            var expression = "Property1 + Property2";

            var st = new Stopwatch();
            st.Start();
            for (var x = 0; x < 1000000; x++)
            {
                var fn = fc.GetCachedFunction(expression);
                fn(scope);
            }
            st.Stop();
            Debug.WriteLine("{0}", st.ElapsedMilliseconds);
        }

        [TestMethod]
        public void GenericTest()
        {
            var scope = new Scope();
            var fc = new StaticFunctionCache<Scope>();

            scope.Property1 = 5;
            scope.Property2 = 6;
            var expression = "Property1 + Property2";

            var st = new Stopwatch();
            st.Start();
            for (var x = 0; x < 1000000; x++)
            {
                var fn = fc.GetCachedFunction(expression);
                fn(scope);
            }
            st.Stop();
            Debug.WriteLine("{0}", st.ElapsedMilliseconds);
        }


    }
}