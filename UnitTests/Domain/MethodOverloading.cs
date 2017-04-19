using System;

namespace ExpressionEvaluator.UnitTests.Domain
{
    public class MethodOverloading
    {
        public int MethodCalled { get; set; }

        public double sum(double i, double t)
        {
            MethodCalled = 1;
            var result = 0d;
            return result;
        }

        public double sum(double i, int t)
        {
            MethodCalled = 2;
            var result = 0d;
            return result;
        }

        public int sum(int i, int t)
        {
            MethodCalled = 3;
            var result = 0;
            return result;
        }

        public int sum(int i1, int i2, int i3, int i4, int i5)
        {
            MethodCalled = 4;
            var result = 0;
            return result;
        }


        public double sum(double i, params double[] nums)
        {
            MethodCalled = 5;
            var result = 0d;
            foreach (var num in nums)
            {
                result += num;
            }
            return result;
        }

        public float sum(float i, params float[] nums)
        {
            MethodCalled = 6;
            var result = 0f;
            foreach (var num in nums)
            {
                result += num;
            }
            return result;
        }

        public float sum(params int[] nums)
        {
            MethodCalled = 7;
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
}