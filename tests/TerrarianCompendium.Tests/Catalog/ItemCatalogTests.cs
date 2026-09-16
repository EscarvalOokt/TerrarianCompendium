using System;
using NUnit.Framework;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Catalog
{
    [TestFixture]
    public sealed class ItemCatalogTests
    {
        [Test]
        public void Create_WithValidEntries_BuildsCatalog()
        {
            var first = new ItemCatalogEntry(1, "First");
            var second = new ItemCatalogEntry(2, "Second");

            var catalog = ItemCatalog.Create(
            [
                first,
                second
            ]);

            Assert.That(catalog.Count, Is.EqualTo(2));

            Assert.That(catalog.Items[0], Is.SameAs(first));
            Assert.That(catalog.Items[0].Id, Is.EqualTo(1));
            Assert.That(catalog.Items[0].Name, Is.EqualTo("First"));

            Assert.That(catalog.Items[1], Is.SameAs(second));
            Assert.That(catalog.Items[1].Id, Is.EqualTo(2));
            Assert.That(catalog.Items[1].Name, Is.EqualTo("Second"));
        }

        [Test]
        public void Create_WithUnorderedEntries_OrdersById()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(3, "Third"),
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second")
            ]);

            Assert.That(catalog.Items[0].Id, Is.EqualTo(1));
            Assert.That(catalog.Items[1].Id, Is.EqualTo(2));
            Assert.That(catalog.Items[2].Id, Is.EqualTo(3));
        }

        [Test]
        public void Create_WithCatalogMetadata_OrdersByIdAndPreservesMetadata()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(
                    3,
                    "Third",
                    ItemCategoryMembership.Tools | ItemCategoryMembership.Materials,
                    nativeSortGroup: 500,
                    nativeSortOrder: 30,
                    semanticMemberships: ItemSemanticMembership.Pickaxes | ItemSemanticMembership.Axes,
                    sortMetrics: new ItemSortMetrics(300, 4, 20, 3, 100, 25, 0, 0)),
                new ItemCatalogEntry(
                    1,
                    "First",
                    ItemCategoryMembership.Weapons,
                    nativeSortGroup: 100,
                    nativeSortOrder: 10,
                    semanticMemberships: ItemSemanticMembership.Melee,
                    sortMetrics: new ItemSortMetrics(100, 1, 12, 0, 0, 0, 0, 0)),
                new ItemCatalogEntry(
                    2,
                    "Second",
                    ItemCategoryMembership.Armor | ItemCategoryMembership.Vanity,
                    nativeSortGroup: 200,
                    nativeSortOrder: 20,
                    semanticMemberships: ItemSemanticMembership.ArmorHead | ItemSemanticMembership.VanityHead,
                    sortMetrics: new ItemSortMetrics(200, 2, 0, 8, 0, 0, 0, 0))
            ]);

            Assert.That(catalog.Items[0].Id, Is.EqualTo(1));
            Assert.That(catalog.Items[0].CategoryMemberships, Is.EqualTo(ItemCategoryMembership.Weapons));
            Assert.That(catalog.Items[0].NativeSortGroup, Is.EqualTo(100));
            Assert.That(catalog.Items[0].NativeSortOrder, Is.EqualTo(10));
            Assert.That(catalog.Items[0].SemanticMemberships, Is.EqualTo(ItemSemanticMembership.Melee));
            AssertSortMetrics(catalog.Items[0].SortMetrics, 100, 1, 12, 0, 0, 0, 0, 0);

            Assert.That(catalog.Items[1].Id, Is.EqualTo(2));
            Assert.That(
                catalog.Items[1].CategoryMemberships,
                Is.EqualTo(ItemCategoryMembership.Armor | ItemCategoryMembership.Vanity));
            Assert.That(catalog.Items[1].NativeSortGroup, Is.EqualTo(200));
            Assert.That(catalog.Items[1].NativeSortOrder, Is.EqualTo(20));
            Assert.That(
                catalog.Items[1].SemanticMemberships,
                Is.EqualTo(ItemSemanticMembership.ArmorHead | ItemSemanticMembership.VanityHead));
            AssertSortMetrics(catalog.Items[1].SortMetrics, 200, 2, 0, 8, 0, 0, 0, 0);

            Assert.That(catalog.Items[2].Id, Is.EqualTo(3));
            Assert.That(
                catalog.Items[2].CategoryMemberships,
                Is.EqualTo(ItemCategoryMembership.Tools | ItemCategoryMembership.Materials));
            Assert.That(catalog.Items[2].NativeSortGroup, Is.EqualTo(500));
            Assert.That(catalog.Items[2].NativeSortOrder, Is.EqualTo(30));
            Assert.That(
                catalog.Items[2].SemanticMemberships,
                Is.EqualTo(ItemSemanticMembership.Pickaxes | ItemSemanticMembership.Axes));
            AssertSortMetrics(catalog.Items[2].SortMetrics, 300, 4, 20, 3, 100, 25, 0, 0);
        }

        [Test]
        public void Create_WithDuplicateId_Throws()
        {
            ItemCatalogEntry[] entries =
            [
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(1, "Duplicate")
            ];

            Assert.Throws<ArgumentException>((Action)(() => { ItemCatalog.Create(entries); }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ItemCatalogEntry_WithNonPositiveId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { _ = new ItemCatalogEntry(itemId, "Item"); }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ItemCatalogEntry_WithBlankName_Throws(string name)
        {
            Assert.Throws<ArgumentException>((Action)(() => { _ = new ItemCatalogEntry(1, name); }));
        }

        [Test]
        public void ItemCatalogEntry_WithExplicitMemberships_PreservesMemberships()
        {
            const ItemCategoryMembership memberships = ItemCategoryMembership.Weapons |
                                                       ItemCategoryMembership.Tools |
                                                       ItemCategoryMembership.Materials;

            var entry = new ItemCatalogEntry(1, "Item", memberships);

            Assert.That(entry.CategoryMemberships, Is.EqualTo(memberships));
        }

        [Test]
        public void ItemCatalogEntry_WithoutMemberships_UsesNone()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            Assert.That(entry.CategoryMemberships, Is.EqualTo(ItemCategoryMembership.None));
        }

        [Test]
        public void ItemCatalogEntry_WithSemanticMemberships_PreservesMemberships()
        {
            const ItemSemanticMembership memberships = ItemSemanticMembership.Melee |
                                                       ItemSemanticMembership.Yoyos |
                                                       ItemSemanticMembership.Axes;

            var entry = new ItemCatalogEntry(1, "Item", semanticMemberships: memberships);

            Assert.That(entry.SemanticMemberships, Is.EqualTo(memberships));
        }

        [Test]
        public void ItemCatalogEntry_WithoutSemanticMemberships_UsesNone()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            Assert.That(entry.SemanticMemberships, Is.EqualTo(ItemSemanticMembership.None));
        }

        [Test]
        public void ItemCatalogEntry_SemanticMemberships_AreIndependentFromNativeCategoryMemberships()
        {
            var entry = new ItemCatalogEntry(
                1,
                "Cross-context item",
                ItemCategoryMembership.Misc | ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.Ammo);

            Assert.That(
                entry.CategoryMemberships,
                Is.EqualTo(ItemCategoryMembership.Misc | ItemCategoryMembership.Materials));
            Assert.That(entry.SemanticMemberships, Is.EqualTo(ItemSemanticMembership.Ammo));
        }

        [Test]
        public void ItemCatalogEntry_WithoutNativeSortMetadata_UsesZeroes()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            Assert.That(entry.NativeSortGroup, Is.Zero);
            Assert.That(entry.NativeSortOrder, Is.Zero);
        }

        [Test]
        public void ItemCatalogEntry_WithoutSortMetrics_UsesZeroes()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            AssertSortMetrics(entry.SortMetrics, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        [Test]
        public void ItemCatalogEntry_WithoutDetailsStats_UsesEmptyDetailsStats()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            Assert.Multiple(() =>
            {
                Assert.That(entry.DetailsStats.DamageType, Is.EqualTo(ItemDetailsDamageType.None));
                Assert.That(entry.DetailsStats.Knockback, Is.Null);
                Assert.That(entry.DetailsStats.BaseCriticalHitChance, Is.Null);
                Assert.That(entry.DetailsStats.UseTimeTicks, Is.Null);
                Assert.That(entry.DetailsStats.TagDamage, Is.Null);
            });
        }

        [Test]
        public void ItemCatalogEntry_WithDetailsStats_PreservesDetailsStats()
        {
            var detailsStats = new ItemDetailsStats(
                ItemDetailsDamageType.Magic,
                knockback: 4.5f,
                baseCriticalHitChance: 9,
                useTimeTicks: 17,
                tagDamage: 6);
            var entry = new ItemCatalogEntry(1, "Item", detailsStats: detailsStats);

            Assert.Multiple(() =>
            {
                Assert.That(entry.DetailsStats.DamageType, Is.EqualTo(ItemDetailsDamageType.Magic));
                Assert.That(entry.DetailsStats.Knockback, Is.EqualTo(4.5f));
                Assert.That(entry.DetailsStats.BaseCriticalHitChance, Is.EqualTo(9));
                Assert.That(entry.DetailsStats.UseTimeTicks, Is.EqualTo(17));
                Assert.That(entry.DetailsStats.TagDamage, Is.EqualTo(6));
            });
        }

        [Test]
        public void Contains_ReturnsExpectedResult()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second")
            ]);

            Assert.That(catalog.Contains(1), Is.True);
            Assert.That(catalog.Contains(3), Is.False);
        }

        [Test]
        public void TryGet_WithExistingId_ReturnsMatchingEntry()
        {
            var expectedEntry = new ItemCatalogEntry(2, "Second");

            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(1, "First"),
                expectedEntry
            ]);

            bool found = catalog.TryGet(2, out ItemCatalogEntry actualEntry);

            Assert.That(found, Is.True);
            Assert.That(actualEntry, Is.SameAs(expectedEntry));
        }

        [Test]
        public void TryGet_WithMissingId_ReturnsFalse()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(1, "First")
            ]);

            bool found = catalog.TryGet(2, out ItemCatalogEntry entry);

            Assert.That(found, Is.False);
            Assert.That(entry, Is.Null);
        }

        private static void AssertSortMetrics(
            ItemSortMetrics metrics,
            int value,
            int rarity,
            int damage,
            int defense,
            int pickPower,
            int axePower,
            int hammerPower,
            int fishingPower)
        {
            Assert.That(metrics.Value, Is.EqualTo(value));
            Assert.That(metrics.Rarity, Is.EqualTo(rarity));
            Assert.That(metrics.Damage, Is.EqualTo(damage));
            Assert.That(metrics.Defense, Is.EqualTo(defense));
            Assert.That(metrics.PickPower, Is.EqualTo(pickPower));
            Assert.That(metrics.AxePower, Is.EqualTo(axePower));
            Assert.That(metrics.HammerPower, Is.EqualTo(hammerPower));
            Assert.That(metrics.FishingPower, Is.EqualTo(fishingPower));
        }
    }
}