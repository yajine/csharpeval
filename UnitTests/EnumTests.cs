using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionEvaluator.UnitTests
{
    [TestClass]
    public class EnumTests
    {
        public enum Drinks : int
        {
            Beer = 1,
            Soda = 2,
            Water = 3,
            Milk = 4
        }

        [Flags]
        public enum Food : int
        {
            Fruit = 0x1,
            Grain = 0x2,
            Meat = 0x4,
            Dairy = 0x8
        }

        [TestMethod]
        public void BinaryOpsWithEnumFlagsAttribute()
        {
            const Food all = Food.Fruit | Food.Grain | Food.Meat | Food.Dairy;
            const Food some = Food.Meat | Food.Dairy;

            var expected = (all & some) == some;

            var registry = new TypeRegistry();

            registry.RegisterSymbol(@"all", all);
            registry.RegisterSymbol(@"some", some);

            var ce = new CompiledExpression<bool>(@"(all & some) == some")
            {
                TypeRegistry = registry
            };

            var actual = ce.Eval();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BinaryOpsWithEnumFlagsAttributeToString()
        {
            const Food all = Food.Fruit | Food.Grain | Food.Meat | Food.Dairy;

            var expected = all.ToString();

            var registry = new TypeRegistry();

            registry.RegisterSymbol(@"all", all);

            var ce = new CompiledExpression<string>(@"all.ToString()")
            {
                TypeRegistry = registry
            };

            var actual = ce.Eval();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BinaryOpsWithEnumFlagsAttributeLiterals()
        {
            var expected = Food.Fruit | Food.Grain | Food.Meat | Food.Dairy;

            var registry = new TypeRegistry();

            registry.RegisterType(@"Food", typeof(Food));

            var ce = new CompiledExpression<Food>(@"Food.Fruit | Food.Grain | Food.Meat | Food.Dairy")
            {
                TypeRegistry = registry
            };

            var actual = ce.Eval();

            Assert.AreEqual(expected, actual);
        }


        [TestMethod]
        public void BinaryOpsWithEnum()
        {
            const Drinks all = Drinks.Beer | Drinks.Soda | Drinks.Water | Drinks.Milk;
            const Drinks some = Drinks.Beer | Drinks.Soda;

            var expected = (all & some) == some;

            var registry = new TypeRegistry();

            registry.RegisterSymbol(@"all", all);
            registry.RegisterSymbol(@"some", some);

            var ce = new CompiledExpression<bool>(@"(all & some) == some")
            {
                TypeRegistry = registry
            };

            var actual = ce.Eval();

            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void BinaryOpsWithEnumLiterals()
        {
            var expected = Drinks.Beer | Drinks.Soda | Drinks.Water | Drinks.Milk;

            var registry = new TypeRegistry();

            registry.RegisterType(@"Drinks", typeof(Drinks));

            var ce = new CompiledExpression<Drinks>(@"Drinks.Beer | Drinks.Soda | Drinks.Water | Drinks.Milk")
            {
                TypeRegistry = registry
            };

            var actual = ce.Eval();

            Assert.AreEqual(expected, actual);
        }

    }
}