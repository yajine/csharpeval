using System.Collections.Generic;
using System.Linq;

namespace Tests.Contexts
{
    public class Test
    {
        public double Trend(IList<IImportedValue> test)
        {
            return test.Average(x => x.Value);
        }
    }
}