using System.Collections;
using System.Collections.Generic;

namespace UnitTestProject1.Domain
{
    public class objHolder
    {
        public bool result { get; set; }
        public NumEnum value { get; set; }
        public int number { get; set; }
        public int number2 { get; set; }
        public IEnumerable<string> iterator;
        public IEnumerable objectIterator;
        public string[] stringIterator;
    }
}