namespace ExpressionEvaluator.UnitTests.Domain
{
    public class ObjectCreationTest
    {
        public int Value { get; private set; }

        public ObjectCreationTest()
        {

        }

        public ObjectCreationTest(int value)
        {
            Value = value;
        }
    }
}