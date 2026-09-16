using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class OpenableItemLootIndexTests
    {
        [Test]
        public void Relations_AreIndexedInBothDirectionsWithDeduplicationAndSorting()
        {
            var index = new OpenableItemLootIndex(
            [
                new OpenableItemLootRelation(20, 1),
                new OpenableItemLootRelation(10, 1),
                new OpenableItemLootRelation(10, 1),
                new OpenableItemLootRelation(10, 3),
                new OpenableItemLootRelation(10, 2),
                new OpenableItemLootRelation(0, 1),
                new OpenableItemLootRelation(30, 0)
            ]);

            Assert.That(index.GetSourcesForItem(1), Is.EqualTo([10, 20]));
            Assert.That(index.GetSourcesForItem(2), Is.EqualTo([10]));
            Assert.That(index.GetSourcesForItem(4), Is.Empty);
            Assert.That(index.GetContentsForItem(10), Is.EqualTo([1, 2, 3]));
            Assert.That(index.GetContentsForItem(20), Is.EqualTo([1]));
            Assert.That(index.GetContentsForItem(30), Is.Empty);
            Assert.That(index.GetContentsForItem(0), Is.Empty);
        }
    }
}