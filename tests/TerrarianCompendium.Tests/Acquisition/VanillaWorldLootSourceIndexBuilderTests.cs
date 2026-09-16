using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class VanillaWorldLootSourceIndexBuilderTests
    {
        [Test]
        public void Build_ContainsRepresentativeConfirmedWorldSources()
        {
            WorldLootSourceIndex index = new VanillaWorldLootSourceIndexBuilder().Build();

            AssertSource(index, 49, "generated-chests", 48);
            AssertSource(index, 211, "jungle-shrine-chests", 626);
            AssertSource(index, 863, "water-chests", 1298);
            AssertSource(index, 159, "skyware-chests", 838);
            AssertSource(index, 848, "pyramid-chests", 306);
            AssertSource(index, 5007, "dead-mans-chests", 3988);
            AssertSource(index, 155, "dungeon-chests", 306);
            AssertSource(index, 1156, "dungeon-biome-chests", 1528);
            AssertSource(index, 1293, "temple-chests", 1142);
        }

        [Test]
        public void Build_TreeShakingUsesArchetypeSpecificSourcesAndRepresentatives()
        {
            WorldLootSourceIndex index = new VanillaWorldLootSourceIndexBuilder().Build();

            AssertTreeGroup(index, "tree-shaking-forest", 9, 4009, 4293, 4282, 4290, 4291);
            AssertTreeGroup(index, "tree-shaking-snow", 2503, 4295, 4286);
            AssertTreeGroup(index, "tree-shaking-jungle", 620, 4292, 4294);
            AssertTreeGroup(index, "tree-shaking-palm", 2504, 4287, 4283);
            AssertTreeGroup(index, "tree-shaking-corruption", 619, 4289, 4284);
            AssertTreeGroup(index, "tree-shaking-hallow", 621, 4288, 4297);
            AssertTreeGroup(index, "tree-shaking-crimson", 911, 4285, 4296);
            AssertTreeGroup(index, "tree-shaking-ash", 5215, 5278, 5277);
        }

        [Test]
        public void Build_PreservesPotAndTreeShakingOverlap()
        {
            WorldLootSourceIndex index = new VanillaWorldLootSourceIndexBuilder().Build();

            AssertSource(index, 4286, "pots", null);
            AssertSource(index, 4286, "tree-shaking-snow", 2503);
        }

        [Test]
        public void Build_TreeShakingIsLimitedToConfirmedFruitCandidates()
        {
            WorldLootSourceIndex index = new VanillaWorldLootSourceIndexBuilder().Build();

            Assert.That(HasTreeShakingSource(index, 4009), Is.True);
            Assert.That(HasTreeShakingSource(index, 965), Is.False);
            Assert.That(HasTreeShakingSource(index, 211), Is.False);
        }

        [Test]
        public void Build_DoesNotInventTransitiveOpenableRelations()
        {
            WorldLootSourceIndex index = new VanillaWorldLootSourceIndexBuilder().Build();

            Assert.That(index.GetSourcesForItem(4411).Any(source => source.Key == "pots"), Is.False);
        }

        private static void AssertTreeGroup(
            WorldLootSourceIndex index,
            string expectedKey,
            int expectedRepresentativeItemId,
            params int[] itemIds)
        {
            foreach (int itemId in itemIds)
                AssertSource(index, itemId, expectedKey, expectedRepresentativeItemId);
        }

        private static void AssertSource(
            WorldLootSourceIndex index,
            int itemId,
            string expectedKey,
            int? expectedRepresentativeItemId)
        {
            WorldLootSource source = index.GetSourcesForItem(itemId).Single(candidate => candidate.Key == expectedKey);
            Assert.That(source.RepresentativeItemId, Is.EqualTo(expectedRepresentativeItemId));
        }

        private static bool HasTreeShakingSource(WorldLootSourceIndex index, int itemId)
        {
            IReadOnlyList<WorldLootSource> sources = index.GetSourcesForItem(itemId);
            return sources.Any(source => source.Kind == WorldLootSourceKind.TreeShaking);
        }
    }
}