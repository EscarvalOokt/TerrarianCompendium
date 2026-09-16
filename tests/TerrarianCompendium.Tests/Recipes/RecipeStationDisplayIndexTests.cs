using System;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeStationDisplayIndexTests
    {
        [Test]
        public void Create_WithNullCatalog_Throws()
        {
            RecipeCatalog recipeCatalog = CreateRecipeCatalog();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { RecipeStationDisplayIndex.Create(null, recipeCatalog, _ => -1); }));
        }

        [Test]
        public void Create_WithNullRecipeCatalog_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Item"));

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { RecipeStationDisplayIndex.Create(catalog, null, _ => -1); }));
        }

        [Test]
        public void Create_WithNullResolver_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Item"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => { RecipeStationDisplayIndex.Create(catalog, recipeCatalog, null); }));
        }

        [Test]
        public void Create_PlaceableItemWithoutRecipeRequirement_IsNotIndexed()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Placeable"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 2, requiredTileId: null));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.That(index.TryGetRepresentativeItemId(18, out _), Is.False);
            Assert.That(index.TryGetRequiredTileIdForItem(1, out _), Is.False);
        }

        [Test]
        public void Create_RequiredTileWithRepresentativeItem_IndexesBothDirections()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Non-placeable"),
                new ItemCatalogEntry(2, "Station"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 3, requiredTileId: 18));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, itemId => itemId == 2 ? 18 : -1);

            Assert.That(index.TryGetRepresentativeItemId(18, out int stationItemId), Is.True);
            Assert.That(stationItemId, Is.EqualTo(2));
            Assert.That(index.TryGetRequiredTileIdForItem(2, out int requiredTileId), Is.True);
            Assert.That(requiredTileId, Is.EqualTo(18));
            Assert.That(index.TryGetRequiredTileIdForItem(1, out _), Is.False);
        }

        [Test]
        public void Create_WhenSeveralItemsPlaceSameRequiredTile_UsesLowestCatalogItemIdAsRepresentative()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(30, "Third Variant"),
                new ItemCatalogEntry(10, "First Variant"),
                new ItemCatalogEntry(20, "Second Variant"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 1, requiredTileId: 18));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.That(index.TryGetRepresentativeItemId(18, out int itemId), Is.True);
            Assert.That(itemId, Is.EqualTo(10));
        }

        [Test]
        public void Create_WhenSeveralItemsPlaceSameRequiredTile_MapsEveryVariantBackToTile()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(10, "First Variant"),
                new ItemCatalogEntry(20, "Second Variant"),
                new ItemCatalogEntry(30, "Third Variant"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 1, requiredTileId: 18));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.That(index.TryGetRequiredTileIdForItem(10, out int firstTileId), Is.True);
            Assert.That(index.TryGetRequiredTileIdForItem(20, out int secondTileId), Is.True);
            Assert.That(index.TryGetRequiredTileIdForItem(30, out int thirdTileId), Is.True);
            Assert.That(firstTileId, Is.EqualTo(18));
            Assert.That(secondTileId, Is.EqualTo(18));
            Assert.That(thirdTileId, Is.EqualTo(18));
        }

        [Test]
        public void Create_ItemPlacingDifferentTile_IsNotMappedToRequiredStation()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Station"),
                new ItemCatalogEntry(2, "Other Placeable"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 3, requiredTileId: 18));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, itemId => itemId == 1 ? 18 : 20);

            Assert.That(index.TryGetRequiredTileIdForItem(1, out int tileId), Is.True);
            Assert.That(tileId, Is.EqualTo(18));
            Assert.That(index.TryGetRequiredTileIdForItem(2, out _), Is.False);
            Assert.That(index.TryGetRepresentativeItemId(20, out _), Is.False);
        }

        [Test]
        public void Create_DuplicateRecipeRequirements_DoNotChangeMapping()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(10, "Station"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipe(0, 1, requiredTileId: 18),
                CreateRecipe(1, 2, requiredTileId: 18));

            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.That(index.TryGetRepresentativeItemId(18, out int itemId), Is.True);
            Assert.That(itemId, Is.EqualTo(10));
            Assert.That(index.TryGetRequiredTileIdForItem(10, out int tileId), Is.True);
            Assert.That(tileId, Is.EqualTo(18));
        }

        [Test]
        public void TryGetRepresentativeItemId_WithNegativeTileId_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Station"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 2, requiredTileId: 18));
            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { index.TryGetRepresentativeItemId(-1, out _); }));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void TryGetRequiredTileIdForItem_WithInvalidItemId_Throws(int itemId)
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Station"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipe(0, 2, requiredTileId: 18));
            var index = RecipeStationDisplayIndex.Create(catalog, recipeCatalog, _ => 18);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { index.TryGetRequiredTileIdForItem(itemId, out _); }));
        }

        private static ItemCatalog CreateItemCatalog(params ItemCatalogEntry[] entries)
        {
            return ItemCatalog.Create(entries);
        }

        private static RecipeCatalog CreateRecipeCatalog(params RecipeCatalogEntry[] entries)
        {
            return RecipeCatalog.Create(entries);
        }

        private static RecipeCatalogEntry CreateRecipe(int runtimeIndex, int resultItemId, int? requiredTileId)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                [],
                new RecipeEnvironmentRequirements(
                    requiredTileId,
                    requiresWater: false,
                    requiresHoney: false,
                    requiresLava: false,
                    requiresSnowBiome: false,
                    requiresGraveyardBiome: false,
                    requiresMechdusa: false,
                    requiresTorchGodsFavor: false),
                isAlchemy: false);
        }
    }
}