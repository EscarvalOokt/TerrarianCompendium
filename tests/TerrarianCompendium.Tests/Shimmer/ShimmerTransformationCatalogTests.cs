using System;
using NUnit.Framework;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class ShimmerTransformationCatalogTests
    {
        [Test]
        public void DirectEntry_PreservesStaticResultIdentity()
        {
            var entry = ShimmerTransformationEntry.ForDirect(10, 20);

            Assert.Multiple(() =>
            {
                Assert.That(entry.InputItemId, Is.EqualTo(10));
                Assert.That(entry.Kind, Is.EqualTo(ShimmerTransformationKind.Direct));
                Assert.That(entry.IsDirect, Is.True);
                Assert.That(entry.IsDecraft, Is.False);
                Assert.That(entry.DirectResultItemId, Is.EqualTo(20));
                Assert.That(entry.HasDynamicDirectResult, Is.False);
                Assert.That(entry.BaseRecipeRuntimeIndex, Is.Null);
            });
        }

        [Test]
        public void DynamicDirectEntry_DoesNotPretendToHaveStaticResult()
        {
            var entry = ShimmerTransformationEntry.ForDynamicDirect(10);

            Assert.Multiple(() =>
            {
                Assert.That(entry.IsDirect, Is.True);
                Assert.That(entry.DirectResultItemId, Is.Null);
                Assert.That(entry.HasDynamicDirectResult, Is.True);
            });
        }

        [Test]
        public void DecraftEntry_SelectsWorldSpecificVariantWhenPresent()
        {
            var entry = ShimmerTransformationEntry.ForDecraft(10, 5, 6, 7);

            Assert.Multiple(() =>
            {
                Assert.That(entry.IsDecraft, Is.True);
                Assert.That(entry.SelectDecraftingRecipeRuntimeIndex(worldIsCrimson: true), Is.EqualTo(6));
                Assert.That(entry.SelectDecraftingRecipeRuntimeIndex(worldIsCrimson: false), Is.EqualTo(7));
            });
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DecraftEntry_WithoutWorldSpecificVariant_FallsBackToBase(bool worldIsCrimson)
        {
            var entry = ShimmerTransformationEntry.ForDecraft(10, 5);

            Assert.That(entry.SelectDecraftingRecipeRuntimeIndex(worldIsCrimson), Is.EqualTo(5));
        }

        [Test]
        public void DirectEntry_SelectDecraftingRecipe_Throws()
        {
            var entry = ShimmerTransformationEntry.ForDirect(10, 20);

            Assert.Throws<InvalidOperationException>(() =>
                entry.SelectDecraftingRecipeRuntimeIndex(worldIsCrimson: true));
        }

        [Test]
        public void Create_SortsByInputItemIdAndSupportsLookup()
        {
            var catalog = ShimmerTransformationCatalog.Create(
            [
                ShimmerTransformationEntry.ForDirect(30, 300),
                ShimmerTransformationEntry.ForDecraft(10, 5),
                ShimmerTransformationEntry.ForDirect(20, 200)
            ]);

            Assert.That(catalog.Entries[0].InputItemId, Is.EqualTo(10));
            Assert.That(catalog.Entries[1].InputItemId, Is.EqualTo(20));
            Assert.That(catalog.Entries[2].InputItemId, Is.EqualTo(30));
            Assert.That(catalog.Contains(20), Is.True);
            Assert.That(catalog.TryGet(20, out ShimmerTransformationEntry entry), Is.True);
            Assert.That(entry.InputItemId, Is.EqualTo(20));
        }

        [Test]
        public void Create_WithDuplicateInputIdentity_Throws()
        {
            Assert.Throws<ArgumentException>(() => ShimmerTransformationCatalog.Create(
            [
                ShimmerTransformationEntry.ForDirect(10, 20),
                ShimmerTransformationEntry.ForDecraft(10, 5)
            ]));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DirectEntry_WithNonPositiveInputItemId_Throws(int inputItemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShimmerTransformationEntry.ForDirect(inputItemId, 20));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void DirectEntry_WithNonPositiveResultItemId_Throws(int resultItemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShimmerTransformationEntry.ForDirect(10, resultItemId));
        }

        [Test]
        public void DecraftEntry_WithNegativeBaseRecipeIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ShimmerTransformationEntry.ForDecraft(10, -1));
        }
    }
}