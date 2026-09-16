using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class VanillaOpenableItemLootIndexBuilderTests
    {
        [Test]
        public void Build_ContainsRepresentativeConfirmedOpenableRelations()
        {
            OpenableItemLootIndex index = new VanillaOpenableItemLootIndexBuilder().Build();

            Assert.That(index.GetSourcesForItem(2430), Does.Contain(3318));
            Assert.That(index.GetSourcesForItem(2002), Does.Contain(4345));
            Assert.That(index.GetSourcesForItem(4411), Does.Contain(4410));
            Assert.That(index.GetSourcesForItem(155), Does.Contain(3085));
        }

        [Test]
        public void Build_ContainsRepresentativeFishingCrateRelations()
        {
            OpenableItemLootIndex index = new VanillaOpenableItemLootIndexBuilder().Build();

            Assert.That(index.GetSourcesForItem(3064), Does.Contain(3979));
            Assert.That(index.GetSourcesForItem(211), Does.Contain(3208));
            Assert.That(index.GetSourcesForItem(211), Does.Not.Contain(3207));
        }

        [Test]
        public void Build_PreHardmodeCrates_DoNotInheritHardmodeOnlyCandidates()
        {
            OpenableItemLootIndex index = new VanillaOpenableItemLootIndexBuilder().Build();

            Assert.That(index.GetSourcesForItem(3064), Does.Contain(3979));
            Assert.That(index.GetSourcesForItem(3064), Does.Contain(3980));
            Assert.That(index.GetSourcesForItem(3064), Does.Contain(3981));
            Assert.That(index.GetSourcesForItem(3064), Does.Not.Contain(2334));
            Assert.That(index.GetSourcesForItem(3064), Does.Not.Contain(2335));
            Assert.That(index.GetSourcesForItem(3064), Does.Not.Contain(2336));

            Assert.That(index.GetSourcesForItem(364), Does.Contain(3980));
            Assert.That(index.GetSourcesForItem(364), Does.Not.Contain(2335));
        }
    }
}