using System.Collections.Generic;
using System.Linq;

namespace Tests.Contexts
{
    public class Utility
    {
        public float ATADJ(float value, float adj)
        {
            return value;
        }

        public float MAX(float value1, float value2)
        {
            return new List<float> { value1, value2 }.Max();
        }

        public float MAX(params float[] value)
        {
            return value.Max();
        }

        public float SUBTR(float left, float right)
        {
            return left - right;
        }

        public float MULT(float left, float right)
        {
            return left * right;
        }

        public float PR(float value)
        {
            return value;
        }

        public float AMT(float value)
        {
            return value;
        }
    }
}