using System;
using ExpressionEvaluator;

namespace Tests
{
    public class c
    {
        public double sum(double i, double t)
        {
            var result = 0d;
            return result;
        }

        public double sum(double i, int t)
        {
            var result = 0d;
            return result;
        }

        public int sum(int i, int t)
        {
            var result = 0;
            return result;
        }

        public int sum(int i1, int i2, int i3, int i4, int i5)
        {
            var result = 0;
            return result;
        }


        public double sum(double i, params double[] nums)
        {
            var result = 0d;
            foreach (var num in nums)
            {
                result += num;
            }
            return result;
        }

        public float sum(float i, params float[] nums)
        {
            var result = 0f;
            foreach (var num in nums)
            {
                result += num;
            }
            return result;
        }


        public int yes()
        {
            return 1234;
        }

        public bool no
        {
            get { return false; }
        }

        public int fix(int x)
        {
            return x + 1;
        }

        public int func(Predicate<int> t)
        {
            return t(5) ? 1 : 2;
        }
    }

    public class c2
    {
        public int yes()
        {
            return 1234;
        }

        public bool no
        {
            get { return true; }
        }

        public int fix(int x)
        {
            return x + 1;
        }

        public int sum(params int[] nums)
        {
            var result = 0;
            foreach (var num in nums)
            {
                result -= num;
            }
            return result;
        }

    }



    class Program
    {

        static void Main(string[] args)
        {
            //var x = new List<String>() { "Hello", "There", "World" };
            //string[] x = new string[] { "Hello", "There", "World" };
            //dynamic scope = new ExpandoObject();
            //scope.x = x;
            //var p = scope.x[0];
            int x = 1;
            int y = 2;

            CompiledExpression c = new CompiledExpression() { StringToParse = "x + y == 3" };
            c.RegisterType("x", x); 
            c.RegisterType("y", y);
            var f = c.Compile();

            Console.WriteLine(f());



            Console.ReadLine();
        }
    }
}
