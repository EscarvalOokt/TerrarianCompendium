using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class WorldLootSourceIndexTests
    {
        [Test]
        public void GetSourcesForItem_DeduplicatesByKeyAndUsesDeterministicOrder()
        {
            var later = new WorldLootSource("later", "Acquisition.World.Later", WorldLootSourceKind.Chest, 20, 48);
            var earlier = new WorldLootSource(
                "earlier",
                "Acquisition.World.Earlier",
                WorldLootSourceKind.Pot,
                10,
                null);
            var index = new WorldLootSourceIndex(
            [
                new WorldLootRelation(later, 1),
                new WorldLootRelation(earlier, 1),
                new WorldLootRelation(earlier, 1),
                new WorldLootRelation(later, 0)
            ]);

            Assert.That(index.GetSourcesForItem(1), Has.Count.EqualTo(2));
            Assert.That(index.GetSourcesForItem(1)[0].Key, Is.EqualTo("earlier"));
            Assert.That(index.GetSourcesForItem(1)[0].DisplayNameKey, Is.EqualTo("Acquisition.World.Earlier"));
            Assert.That(index.GetSourcesForItem(1)[0].RepresentativeItemId, Is.Null);
            Assert.That(index.GetSourcesForItem(1)[1].Key, Is.EqualTo("later"));
            Assert.That(index.GetSourcesForItem(1)[1].RepresentativeItemId, Is.EqualTo(48));
            Assert.That(index.GetSourcesForItem(2), Is.Empty);
        }

        [Test]
        public void GetSourcesForItem_EqualSortOrder_UsesStableKeyInsteadOfDisplayNameKey()
        {
            var zKey = new WorldLootSource("z-source", "A.Display.First", WorldLootSourceKind.Chest, 10, null);
            var aKey = new WorldLootSource("a-source", "Z.Display.Last", WorldLootSourceKind.Chest, 10, null);
            var index = new WorldLootSourceIndex(
            [
                new WorldLootRelation(zKey, 1),
                new WorldLootRelation(aKey, 1)
            ]);

            Assert.That(index.GetSourcesForItem(1).Select(source => source.Key), Is.EqualTo(["a-source", "z-source"]));
        }
    }
}