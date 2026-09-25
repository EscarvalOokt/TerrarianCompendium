using NUnit.Framework;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class ShimmerTransformationIndexTests
    {
        [Test]
        public void Create_MultipleInputsProducingSameResult_AggregatesProducingVariants()
        {
            ShimmerTransformationVariant first = Direct(10, 100);
            ShimmerTransformationVariant second = Direct(20, 100);

            var index = ShimmerTransformationIndex.Create([first, second]);

            Assert.That(index.GetProducing(100), Is.EqualTo([first, second]));
            Assert.That(index.HasProducing(100), Is.True);
        }

        [Test]
        public void Create_MultiOutputVariant_IndexesEveryDistinctResultOnce()
        {
            var variant = new ShimmerTransformationVariant(
                10,
                ShimmerTransformationKind.Decraft,
                2,
                [
                    new ShimmerTransformationOutput(100, 1),
                    new ShimmerTransformationOutput(200, 3),
                    new ShimmerTransformationOutput(100, 2)
                ],
                decraftingRecipeRuntimeIndex: 7);

            var index = ShimmerTransformationIndex.Create([variant]);

            Assert.Multiple(() =>
            {
                Assert.That(index.GetProducing(100), Is.EqualTo([variant]));
                Assert.That(index.GetProducing(200), Is.EqualTo([variant]));
                Assert.That(index.GetProducing(300), Is.Empty);
            });
        }

        [Test]
        public void Create_InputLookup_ReturnsAllVariantsUsingInput()
        {
            ShimmerTransformationVariant first = Direct(10, 100);
            ShimmerTransformationVariant second = new(
                10,
                ShimmerTransformationKind.Decraft,
                2,
                [new ShimmerTransformationOutput(200, 1)],
                decraftingRecipeRuntimeIndex: 4);

            var index = ShimmerTransformationIndex.Create([first, second]);

            Assert.That(index.GetUsing(10), Is.EqualTo([first, second]));
        }

        [Test]
        public void HasRelation_IsTrueForProducingOrUsingItemOnly()
        {
            var index = ShimmerTransformationIndex.Create([Direct(10, 100)]);

            Assert.Multiple(() =>
            {
                Assert.That(index.HasRelation(10), Is.True);
                Assert.That(index.HasRelation(100), Is.True);
                Assert.That(index.HasRelation(999), Is.False);
            });
        }

        private static ShimmerTransformationVariant Direct(int inputItemId, int resultItemId)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Direct,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)]);
        }
    }
}