using System;
using System.Collections.Generic;

namespace UnitTestProject1.Domain
{
    public class MyClass
    {
        public int X { get; set; }
        public List<int> Y { get; set; }
        public int[] Z { get; set; }
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

        public int this[int index]
        {
           get { return index; }
        }

        public int this[string index]
        {
            get { return index.Length; }
        }


    }
}