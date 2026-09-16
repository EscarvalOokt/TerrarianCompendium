using NUnit.Framework;
using TerrarianCompendium.ArmorSets;

namespace TerrarianCompendium.Tests.ArmorSets
{
    [TestFixture]
    public sealed class ArmorSetIndexTests
    {
        [Test]
        public void Create_IndexesEveryNonZeroMemberItem()
        {
            var catalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.Test", 2, [new ArmorSetVariant(1, 2, 3)])
            ]);
            var index = ArmorSetIndex.Create(catalog);

            Assert.That(index.GetArmorSetsForItem(1), Has.Count.EqualTo(1));
            Assert.That(index.GetArmorSetsForItem(2), Has.Count.EqualTo(1));
            Assert.That(index.GetArmorSetsForItem(3), Has.Count.EqualTo(1));
            Assert.That(index.GetArmorSetsForItem(0), Is.Empty);
        }

        [Test]
        public void Create_SharedItemAcrossVariants_ReturnsLogicalEntryOnce()
        {
            var catalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(
                    1,
                    "ArmorSetBonus.Test",
                    10,
                    [
                        new ArmorSetVariant(1, 10, 20),
                        new ArmorSetVariant(2, 10, 20)
                    ])
            ]);
            var index = ArmorSetIndex.Create(catalog);

            Assert.That(index.GetArmorSetsForItem(10), Has.Count.EqualTo(1));
            Assert.That(index.GetArmorSetsForItem(10)[0].Id, Is.EqualTo(1));
        }

        [Test]
        public void Create_ItemInMultipleLogicalEntries_ReturnsCatalogOrder()
        {
            var catalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(2, "ArmorSetBonus.Second", 20, [new ArmorSetVariant(2, 10, 20)]),
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.First", 10, [new ArmorSetVariant(1, 10, 11)])
            ]);
            var index = ArmorSetIndex.Create(catalog);

            Assert.That(index.GetArmorSetsForItem(10), Has.Count.EqualTo(2));
            Assert.That(index.GetArmorSetsForItem(10)[0].Id, Is.EqualTo(1));
            Assert.That(index.GetArmorSetsForItem(10)[1].Id, Is.EqualTo(2));
        }

        [Test]
        public void GetArmorSetsForItem_UnknownItem_ReturnsEmptyList()
        {
            var index = ArmorSetIndex.Create(ArmorSetCatalog.Create([]));

            Assert.That(index.GetArmorSetsForItem(999), Is.Empty);
        }
    }
}