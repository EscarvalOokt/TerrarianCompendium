using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Details;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class ArmorSetDetailsModelTests
    {
        [Test]
        public void TryGetProjection_KnownSet_ProjectsExactVariantsAndBonus()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3, 4);
            ItemTextIndex textIndex = CreateTextIndex(
                itemCatalog,
                "en-US",
                new Dictionary<int, string>
                {
                    [1] = "Head A",
                    [2] = "Head B",
                    [3] = "Body",
                    [4] = "Legs"
                });
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(
                    1,
                    "ArmorSetBonus.Test",
                    3,
                    [
                        new ArmorSetVariant(1, 3, 4),
                        new ArmorSetVariant(2, 3, 4)
                    ])
            ]);
            var model = new ArmorSetDetailsModel(
                armorSetCatalog,
                itemCatalog,
                textIndex,
                new ChecklistState(itemCatalog),
                key => key == "ArmorSetBonus.Test" ? "Test bonus" : string.Empty);

            bool found = model.TryGetProjection(1, out ArmorSetDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.ArmorSetId, Is.EqualTo(1));
            Assert.That(projection.RepresentativeItemId, Is.EqualTo(3));
            Assert.That(projection.BonusDescription, Is.EqualTo("Test bonus"));
            Assert.That(projection.Variants, Has.Count.EqualTo(2));
            Assert.That(projection.Variants[0].Head.ItemId, Is.EqualTo(1));
            Assert.That(projection.Variants[1].Head.ItemId, Is.EqualTo(2));
            Assert.That(projection.Variants[0].Body.ItemId, Is.EqualTo(3));
            Assert.That(projection.Variants[0].Legs.ItemId, Is.EqualTo(4));
        }

        [Test]
        public void TryGetProjection_FullCartesianVariants_ExposeCompactRolePresentation()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3, 4, 5, 6);
            ItemTextIndex textIndex = CreateTextIndex(itemCatalog, "en-US", null);
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(
                    1,
                    "ArmorSetBonus.Cartesian",
                    1,
                    [
                        new ArmorSetVariant(1, 3, 5),
                        new ArmorSetVariant(1, 3, 6),
                        new ArmorSetVariant(1, 4, 5),
                        new ArmorSetVariant(1, 4, 6),
                        new ArmorSetVariant(2, 3, 5),
                        new ArmorSetVariant(2, 3, 6),
                        new ArmorSetVariant(2, 4, 5),
                        new ArmorSetVariant(2, 4, 6)
                    ])
            ]);
            var model = new ArmorSetDetailsModel(
                armorSetCatalog,
                itemCatalog,
                textIndex,
                new ChecklistState(itemCatalog),
                _ => "Bonus");

            model.TryGetProjection(1, out ArmorSetDetailsProjection projection);

            Assert.That(projection.UsesRoleVariantPresentation, Is.True);
            Assert.That(projection.HeadVariants, Has.Count.EqualTo(2));
            Assert.That(projection.BodyVariants, Has.Count.EqualTo(2));
            Assert.That(projection.LegVariants, Has.Count.EqualTo(2));
            Assert.That(projection.Variants, Has.Count.EqualTo(8));
        }

        [Test]
        public void TryGetProjection_NonCartesianConnectedVariants_PreserveExactVariantPresentation()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3, 4, 5);
            ItemTextIndex textIndex = CreateTextIndex(itemCatalog, "en-US", null);
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(
                    1,
                    "ArmorSetBonus.Chain",
                    1,
                    [
                        new ArmorSetVariant(1, 3, 5),
                        new ArmorSetVariant(2, 3, 5),
                        new ArmorSetVariant(2, 4, 5)
                    ])
            ]);
            var model = new ArmorSetDetailsModel(
                armorSetCatalog,
                itemCatalog,
                textIndex,
                new ChecklistState(itemCatalog),
                _ => "Bonus");

            model.TryGetProjection(1, out ArmorSetDetailsProjection projection);

            Assert.That(projection.UsesRoleVariantPresentation, Is.False);
            Assert.That(projection.Variants, Has.Count.EqualTo(3));
        }

        [Test]
        public void TryGetProjection_TwoPartVariant_DoesNotProjectPhantomLegsItem()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2);
            ItemTextIndex textIndex = CreateTextIndex(itemCatalog, "en-US", null);
            var armorSetCatalog = ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.Wizard", 1, [new ArmorSetVariant(1, 2, 0)])
            ]);
            var model = new ArmorSetDetailsModel(
                armorSetCatalog,
                itemCatalog,
                textIndex,
                new ChecklistState(itemCatalog),
                _ => "Wizard bonus");

            model.TryGetProjection(1, out ArmorSetDetailsProjection projection);

            Assert.That(projection.Variants[0].Head, Is.Not.Null);
            Assert.That(projection.Variants[0].Body, Is.Not.Null);
            Assert.That(projection.Variants[0].Legs, Is.Null);
            Assert.That(projection.UsesRoleVariantPresentation, Is.True);
            Assert.That(projection.HeadVariants, Has.Count.EqualTo(1));
            Assert.That(projection.BodyVariants, Has.Count.EqualTo(1));
            Assert.That(projection.LegVariants, Is.Empty);
        }

        [Test]
        public void TryGetProjection_WhenItemTextRevisionChanges_RefreshesLocalizedNames()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3);
            ItemTextIndex textIndex = CreateTextIndex(itemCatalog, "en-US", null);
            ArmorSetCatalog armorSetCatalog = CreateSingleSetCatalog();
            var model = new ArmorSetDetailsModel(
                armorSetCatalog,
                itemCatalog,
                textIndex,
                new ChecklistState(itemCatalog),
                _ => "Bonus");

            model.TryGetProjection(1, out ArmorSetDetailsProjection before);
            ReplaceNames(textIndex, itemCatalog, "uk-UA", new Dictionary<int, string> { [1] = "Шолом" });
            model.TryGetProjection(1, out ArmorSetDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Variants[0].Head.Name, Is.EqualTo("Шолом"));
        }

        [Test]
        public void TryGetProjection_WhenChecklistRevisionChanges_RefreshesFoundState()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3);
            ItemTextIndex textIndex = CreateTextIndex(itemCatalog, "en-US", null);
            var checklistState = new ChecklistState(itemCatalog);
            var model = new ArmorSetDetailsModel(
                CreateSingleSetCatalog(),
                itemCatalog,
                textIndex,
                checklistState,
                _ => "Bonus");

            model.TryGetProjection(1, out ArmorSetDetailsProjection before);
            checklistState.MarkFound(1);
            model.TryGetProjection(1, out ArmorSetDetailsProjection after);

            Assert.That(before.Variants[0].Head.IsFound, Is.False);
            Assert.That(after.Variants[0].Head.IsFound, Is.True);
        }

        [Test]
        public void TryGetProjection_UnknownSet_ReturnsFalse()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(1, 2, 3);
            var model = new ArmorSetDetailsModel(
                CreateSingleSetCatalog(),
                itemCatalog,
                CreateTextIndex(itemCatalog, "en-US", null),
                new ChecklistState(itemCatalog),
                _ => "Bonus");

            Assert.That(model.TryGetProjection(999, out ArmorSetDetailsProjection projection), Is.False);
            Assert.That(projection, Is.Null);
        }

        private static ArmorSetCatalog CreateSingleSetCatalog()
        {
            return ArmorSetCatalog.Create(
            [
                new ArmorSetCatalogEntry(1, "ArmorSetBonus.Test", 2, [new ArmorSetVariant(1, 2, 3)])
            ]);
        }

        private static ItemCatalog CreateItemCatalog(params int[] itemIds)
        {
            var entries = new List<ItemCatalogEntry>(itemIds.Length);

            foreach (int itemId in itemIds)
                entries.Add(new ItemCatalogEntry(itemId, $"Item {itemId}"));

            return ItemCatalog.Create(entries);
        }

        private static ItemTextIndex CreateTextIndex(
            ItemCatalog catalog,
            string cultureName,
            IReadOnlyDictionary<int, string> overrides)
        {
            var index = new ItemTextIndex(catalog);
            ReplaceNames(index, catalog, cultureName, overrides);
            return index;
        }

        private static void ReplaceNames(
            ItemTextIndex index,
            ItemCatalog catalog,
            string cultureName,
            IReadOnlyDictionary<int, string> overrides)
        {
            var names = new Dictionary<int, string>();
            var descriptions = new Dictionary<int, string>();

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                names.Add(
                    entry.Id,
                    overrides != null && overrides.TryGetValue(entry.Id, out string name) ? name : entry.Name);
                descriptions.Add(entry.Id, string.Empty);
            }

            index.ReplaceSnapshot(cultureName, names, descriptions);
        }
    }
}