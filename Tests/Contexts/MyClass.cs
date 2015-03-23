using System;
using System.Collections.Generic;

namespace Tests.Contexts
{
    public class MyClass
    {
        public int X { get; set; }
        public List<int> Y { get; set; }
        public Func<bool> Value { get; set; }
        public void Foo()
        {
            X++;
        }

        public void Foo(int value)
        {
            X += value;
        }

        public int Bar(int value)
        {
            return value * 2;
        }
    }
}