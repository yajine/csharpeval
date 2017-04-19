using System.Collections;
using System.Collections.Generic;

namespace ExpressionEvaluator.UnitTests.Domain
{
    public class IteratorTest
    {
        public bool result { get; set; }
        public NumEnum value { get; set; }
        public int number { get; set; }
        public int number2 { get; set; }
        public IEnumerable<string> Words;
        public IEnumerable objectIterator;
        public string[] stringIterator;
        public dynamic Value;
    }
}