using System.Collections.Generic;

namespace Tests
{
    public class Sub
    {
        public int y { get; set; }
        public List<int> x { get; set; }
        public int z(IEnumerable<Sub> sub)
        {
            return 1;
        }
    }
}