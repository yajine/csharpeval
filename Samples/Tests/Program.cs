using System;
using System.Linq.Expressions;

namespace Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            Samples.Dynamictest();
            Console.ReadLine();
            Samples.ParameterObjectCasts();
            Console.ReadLine();
            Samples.DefaultTypes();
            Samples.Culture();
            Samples.CodeBlocks();
            //Samples.Sample1();
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();
        }
    }
}
