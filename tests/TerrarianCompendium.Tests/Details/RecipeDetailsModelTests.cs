using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Details;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class RecipeDetailsModelTests
    {
        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(RecipeDetailsModel).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void Constructor_WithNullRecipeCatalog_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        null,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullItemCatalog_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        null,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullItemTextIndex_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        null,
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullRecipeIndex_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        null,
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullStationDisplayIndex_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        null,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullChecklistState_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        null,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullFavoriteState_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        null,
                        craftingState,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullCraftingAvailabilityState_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        null,
                        new RecipeFilterState(),
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullFilterState_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeDetailsModel(
                        recipeCatalog,
                        itemCatalog,
                        new ItemTextIndex(itemCatalog),
                        RecipeIndex.Create(recipeCatalog),
                        stationIndex,
                        new ChecklistState(itemCatalog),
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState,
                        null,
                        _localization);
                }));
        }

        [Test]
        public void Constructor_WithNullLocalization_Throws()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeStationDisplayIndex stationIndex = CreateStationIndex(itemCatalog, recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = new RecipeDetailsModel(
                    recipeCatalog,
                    itemCatalog,
                    new ItemTextIndex(itemCatalog),
                    RecipeIndex.Create(recipeCatalog),
                    stationIndex,
                    new ChecklistState(itemCatalog),
                    null,
                    CreateFavoriteState(recipeCatalog),
                    craftingState,
                    new RecipeFilterState(),
                    null);
            });
        }

        [Test]
        public void TryGetProjection_UnknownRuntimeIndex_ReturnsFalse()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            bool found = model.TryGetProjection(999, out RecipeDetailsProjection projection);

            Assert.That(found, Is.False);
            Assert.That(projection, Is.Null);
        }

        [Test]
        public void TryGetProjection_RuntimeIndexZero_ProjectsResultIngredientsAndFlags()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "First Ingredient"),
                new ItemCatalogEntry(3, "Second Ingredient"));

            var environment = new RecipeEnvironmentRequirements(
                null,
                requiresWater: true,
                requiresHoney: true,
                requiresLava: true,
                requiresSnowBiome: true,
                requiresGraveyardBiome: true,
                requiresMechdusa: true,
                requiresTorchGodsFavor: true);

            RecipeCatalogEntry recipe = new(
                0,
                1,
                5,
                [
                    CreateItemIngredient(2, 3),
                    CreateItemIngredient(3, 4)
                ],
                environment,
                isAlchemy: true);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            bool found = model.TryGetProjection(0, out RecipeDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.RuntimeIndex, Is.Zero);
            Assert.That(projection.Result.ItemId, Is.EqualTo(1));
            Assert.That(projection.Result.Name, Is.EqualTo("Result"));
            Assert.That(projection.Result.IsCollectionTracked, Is.True);
            Assert.That(projection.Result.IsFound, Is.False);
            Assert.That(projection.Result.IsMissing, Is.True);
            Assert.That(projection.ResultStack, Is.EqualTo(5));
            Assert.That(projection.Ingredients, Has.Count.EqualTo(2));
            Assert.That(projection.Ingredients[0].DisplayItem.ItemId, Is.EqualTo(2));
            Assert.That(projection.Ingredients[0].DisplayItem.IsMissing, Is.True);
            Assert.That(projection.Ingredients[0].Stack, Is.EqualTo(3));
            Assert.That(projection.Ingredients[1].DisplayItem.ItemId, Is.EqualTo(3));
            Assert.That(projection.Ingredients[1].DisplayItem.IsMissing, Is.True);
            Assert.That(projection.Ingredients[1].Stack, Is.EqualTo(4));
            Assert.That(projection.RequiresWater, Is.True);
            Assert.That(projection.RequiresHoney, Is.True);
            Assert.That(projection.RequiresLava, Is.True);
            Assert.That(projection.RequiresSnowBiome, Is.True);
            Assert.That(projection.RequiresGraveyardBiome, Is.True);
            Assert.That(projection.RequiresMechdusa, Is.True);
            Assert.That(projection.RequiresTorchGodsFavor, Is.True);
            Assert.That(projection.IsAlchemy, Is.True);
            Assert.That(projection.IsFavorite, Is.False);
        }

        [Test]
        public void TryGetProjection_RecipeGroup_PreservesRequirementAndConcreteAlternatives()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Displayed Wood"),
                new ItemCatalogEntry(3, "Alternative A"),
                new ItemCatalogEntry(4, "Alternative B"));

            RecipeIngredient groupIngredient = new(2, 7, RecipeIngredientRequirement.ForRecipeGroup(25, [4, 2, 3]));

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, groupIngredient));

            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            model.TryGetProjection(0, out RecipeDetailsProjection projection);
            RecipeDetailsIngredientProjection ingredient = projection.Ingredients[0];

            Assert.That(ingredient.IsRecipeGroup, Is.True);
            Assert.That(ingredient.RequirementKind, Is.EqualTo(RecipeIngredientRequirementKind.RecipeGroup));
            Assert.That(ingredient.DisplayItem.ItemId, Is.EqualTo(2));
            Assert.That(ingredient.Stack, Is.EqualTo(7));
            Assert.That(ingredient.ValidItems, Has.Count.EqualTo(3));
            Assert.That(ingredient.ValidItems[0].ItemId, Is.EqualTo(2));
            Assert.That(ingredient.ValidItems[0].Name, Is.EqualTo("Displayed Wood"));
            Assert.That(ingredient.ValidItems[0].IsMissing, Is.True);
            Assert.That(ingredient.ValidItems[1].ItemId, Is.EqualTo(3));
            Assert.That(ingredient.ValidItems[1].IsMissing, Is.True);
            Assert.That(ingredient.ValidItems[2].ItemId, Is.EqualTo(4));
            Assert.That(ingredient.ValidItems[2].IsMissing, Is.True);
        }

        [Test]
        public void TryGetProjection_ItemOutsideCatalog_UsesNeutralNameFallback()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Known Result"));
            RecipeCatalogEntry recipe = new(
                0,
                99,
                1,
                [CreateItemIngredient(98, 2)],
                CreateEnvironment(),
                isAlchemy: false);

            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            model.TryGetProjection(0, out RecipeDetailsProjection projection);

            Assert.That(projection.Result.Name, Is.EqualTo("Item #99"));
            Assert.That(projection.Result.IsCollectionTracked, Is.False);
            Assert.That(projection.Result.IsFound, Is.False);
            Assert.That(projection.Result.IsMissing, Is.False);
            Assert.That(projection.Ingredients[0].DisplayItem.Name, Is.EqualTo("Item #98"));
            Assert.That(projection.Ingredients[0].DisplayItem.IsCollectionTracked, Is.False);
            Assert.That(projection.Ingredients[0].DisplayItem.IsMissing, Is.False);
            Assert.That(projection.Ingredients[0].ValidItems[0].Name, Is.EqualTo("Item #98"));
            Assert.That(projection.Ingredients[0].ValidItems[0].IsCollectionTracked, Is.False);
            Assert.That(projection.Ingredients[0].ValidItems[0].IsMissing, Is.False);
        }

        [Test]
        public void TryGetProjection_RequiredTile_UsesRepresentativeStationItem()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(10, "Work Bench"));

            RecipeCatalog recipeCatalog =
                CreateRecipeCatalog(CreateRecipeEntry(0, 1, CreateEnvironment(requiredTileId: 18)));

            var stationIndex = RecipeStationDisplayIndex.Create(
                itemCatalog,
                recipeCatalog,
                itemId => itemId == 10 ? 18 : -1);

            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, stationIndex);

            model.TryGetProjection(0, out RecipeDetailsProjection projection);

            Assert.That(projection.RequiresCraftingStation, Is.True);
            Assert.That(projection.CraftingStation, Is.Not.Null);
            Assert.That(projection.CraftingStation.ItemId, Is.EqualTo(10));
            Assert.That(projection.CraftingStation.Name, Is.EqualTo("Work Bench"));
            Assert.That(projection.CraftingStation.IsCollectionTracked, Is.True);
            Assert.That(projection.CraftingStation.IsMissing, Is.True);
        }

        [Test]
        public void TryGetProjection_LocalizationRevisionInvalidatesFallbackNames()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                new RecipeCatalogEntry(
                    0,
                    99,
                    1,
                    [new RecipeIngredient(98, 1, RecipeIngredientRequirement.ForItem(98))],
                    new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                    false));
            var translated = new Dictionary<string, string>
            {
                [CompendiumTextKeys.Common.ItemIdHash] = "Translated item #{0}"
            };
            var localization = CompendiumLocalization.CreateForTesting(_localization.SourceEntries, translated);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, localization: localization);

            model.TryGetProjection(0, out RecipeDetailsProjection before);
            localization.SynchronizeCulture("test");
            model.TryGetProjection(0, out RecipeDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(before.Result.Name, Is.EqualTo("Item #99"));
            Assert.That(after.Result.Name, Is.EqualTo("Translated item #99"));
        }

        [Test]
        public void TryGetProjection_WhenItemTextRevisionChanges_RefreshesCatalogBackedNames()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Displayed Ingredient"),
                new ItemCatalogEntry(3, "Alternative Ingredient"),
                new ItemCatalogEntry(10, "Work Bench"));
            var itemTextIndex = new ItemTextIndex(itemCatalog);
            RecipeIngredient groupIngredient = new(2, 3, RecipeIngredientRequirement.ForRecipeGroup(25, [2, 3]));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                new RecipeCatalogEntry(
                    0,
                    1,
                    1,
                    [groupIngredient],
                    CreateEnvironment(requiredTileId: 18),
                    isAlchemy: false));
            var stationIndex = RecipeStationDisplayIndex.Create(
                itemCatalog,
                recipeCatalog,
                itemId => itemId == 10 ? 18 : -1);
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                stationDisplayIndex: stationIndex,
                itemTextIndex: itemTextIndex);

            Assert.That(model.TryGetProjection(0, out RecipeDetailsProjection before), Is.True);
            Assert.That(before.Result.Name, Is.EqualTo("Result"));
            Assert.That(before.Ingredients[0].DisplayItem.Name, Is.EqualTo("Displayed Ingredient"));
            Assert.That(before.Ingredients[0].ValidItems[1].Name, Is.EqualTo("Alternative Ingredient"));
            Assert.That(before.CraftingStation.Name, Is.EqualTo("Work Bench"));

            ReplaceItemTextNames(
                itemTextIndex,
                itemCatalog,
                new Dictionary<int, string>
                {
                    [1] = "Localized Result",
                    [2] = "Localized Display",
                    [3] = "Localized Alternative",
                    [10] = "Localized Station"
                },
                "fr-FR");

            Assert.That(model.TryGetProjection(0, out RecipeDetailsProjection after), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Result.Name, Is.EqualTo("Localized Result"));
            Assert.That(after.Ingredients[0].DisplayItem.Name, Is.EqualTo("Localized Display"));
            Assert.That(after.Ingredients[0].ValidItems[1].Name, Is.EqualTo("Localized Alternative"));
            Assert.That(after.CraftingStation.Name, Is.EqualTo("Localized Station"));
        }

        [Test]
        public void TryGetProjection_RequiredTileWithoutDisplayMapping_RemainsValidWithoutRawTilePresentation()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog =
                CreateRecipeCatalog(CreateRecipeEntry(0, 1, CreateEnvironment(requiredTileId: 18)));

            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            bool found = model.TryGetProjection(0, out RecipeDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.RequiresCraftingStation, Is.True);
            Assert.That(projection.CraftingStation, Is.Null);
        }

        [Test]
        public void TryGetProjection_CraftabilityRevisionInvalidatesCachedProjection()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                craftingAvailabilityState: craftingState);

            model.TryGetProjection(0, out RecipeDetailsProjection before);

            bool changed = craftingState.ReplaceSnapshot([0]);
            model.TryGetProjection(0, out RecipeDetailsProjection after);

            Assert.That(changed, Is.True);
            Assert.That(before.IsCraftableNow, Is.False);
            Assert.That(after.IsCraftableNow, Is.True);
            Assert.That(after, Is.Not.SameAs(before));
        }

        [Test]
        public void TryGetProjection_ChecklistRevisionInvalidatesCachedProjectionAndRefreshesItemStates()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, CreateItemIngredient(2, 1)));
            var checklistState = new ChecklistState(itemCatalog);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, checklistState: checklistState);

            model.TryGetProjection(0, out RecipeDetailsProjection before);
            Assert.That(before.Result.IsMissing, Is.True);
            Assert.That(before.Ingredients[0].DisplayItem.IsMissing, Is.True);

            Assert.That(checklistState.MarkFound(1), Is.True);
            Assert.That(checklistState.MarkFound(2), Is.True);
            model.TryGetProjection(0, out RecipeDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Result.IsFound, Is.True);
            Assert.That(after.Result.IsMissing, Is.False);
            Assert.That(after.Ingredients[0].DisplayItem.IsFound, Is.True);
            Assert.That(after.Ingredients[0].DisplayItem.IsMissing, Is.False);
        }

        [Test]
        public void TryGetProjection_UnchangedCraftingRevision_ReusesCachedProjection()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            model.TryGetProjection(0, out RecipeDetailsProjection first);
            model.TryGetProjection(0, out RecipeDetailsProjection second);

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void TryGetProjection_FavoriteRevisionInvalidatesCachedProjectionAndRefreshesState()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, favoriteState: favoriteState);

            model.TryGetProjection(0, out RecipeDetailsProjection before);
            Assert.That(before.IsFavorite, Is.False);

            Assert.That(favoriteState.Toggle(0), Is.True);
            model.TryGetProjection(0, out RecipeDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.IsFavorite, Is.True);
        }

        [Test]
        public void TryGetQueryProjection_UnknownResult_ReturnsFalse()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            bool found = model.TryGetQueryProjection(999, out RecipeQueryDetailsProjection projection);

            Assert.That(found, Is.False);
            Assert.That(projection, Is.Null);
        }

        [Test]
        public void TryGetQueryProjection_MultipleRecipes_PreservesRuntimeOrder()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(3, 1), CreateRecipeEntry(7, 1));
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog);

            bool found = model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.Result.ItemId, Is.EqualTo(1));
            Assert.That(projection.TotalRecipeCount, Is.EqualTo(2));
            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([3, 7]));
        }

        [Test]
        public void TryGetQueryProjection_WhenItemTextRevisionChanges_RefreshesResultName()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var itemTextIndex = new ItemTextIndex(itemCatalog);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, itemTextIndex: itemTextIndex);

            Assert.That(model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before), Is.True);
            Assert.That(before.Result.Name, Is.EqualTo("Result"));

            ReplaceItemTextNames(
                itemTextIndex,
                itemCatalog,
                new Dictionary<int, string> { [1] = "Localized Result" },
                "fr-FR");

            Assert.That(model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after), Is.True);
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Result.Name, Is.EqualTo("Localized Result"));
        }

        [Test]
        public void TryGetQueryProjection_RequirementFilterRestrictsMatchingVariantsButPreservesTotalCount()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateEnvironment(requiredTileId: 10)),
                CreateRecipeEntry(1, 1, CreateEnvironment(requiredTileId: 20)));
            var filterState = new RecipeFilterState { RequiredTileId = 10 };
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.TotalRecipeCount, Is.EqualTo(2));
            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_NoRequirementsOnlyRestrictsMatchingVariants()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 1, CreateEnvironment(requiredTileId: 10)));
            var filterState = new RecipeFilterState { NoRequirementsOnly = true };
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_FilterRevisionInvalidatesCachedProjection()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateEnvironment(requiredTileId: 10)),
                CreateRecipeEntry(1, 1, CreateEnvironment(requiredTileId: 20)));
            var filterState = new RecipeFilterState();
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            filterState.RequiredTileId = 20;
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.EqualTo([1]));
        }

        [Test]
        public void TryGetQueryProjection_CompletionFilterCanRemoveAllVariants()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));
            var checklistState = new ChecklistState(itemCatalog);
            checklistState.MarkFound(1);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                checklistState: checklistState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.TotalRecipeCount, Is.EqualTo(2));
            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.Empty);
        }

        [Test]
        public void TryGetQueryProjection_ResearchFilterUsesJourneyState()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            JourneyResearchState researchState = CreateResearchState(1, amountNeeded: 2, progress: 0);
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                journeyResearchState: researchState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_CraftableNowRestrictsVariants()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(3, 1), CreateRecipeEntry(7, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([7]);
            var filterState = new RecipeFilterState { CraftableNowOnly = true };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                craftingAvailabilityState: craftingState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.TotalRecipeCount, Is.EqualTo(2));
            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([7]));
        }

        [Test]
        public void TryGetQueryProjection_RequirementAndCraftabilityMustMatchSameVariant()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(3, 1, CreateEnvironment(requiredTileId: 10)),
                CreateRecipeEntry(7, 1, CreateEnvironment(requiredTileId: 20)));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([7]);
            var filterState = new RecipeFilterState { RequiredTileId = 10, CraftableNowOnly = true };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                craftingAvailabilityState: craftingState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.Empty);
        }

        [Test]
        public void TryGetQueryProjection_FavoritesOnlyRestrictsMatchingVariants()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(3, 1, CreateEnvironment(requiredTileId: 10)),
                CreateRecipeEntry(7, 1, CreateEnvironment(requiredTileId: 20)));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(7);
            var filterState = new RecipeFilterState { FavoritesOnly = true };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                favoriteState: favoriteState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection projection);

            Assert.That(projection.TotalRecipeCount, Is.EqualTo(2));
            Assert.That(projection.MatchingRecipeRuntimeIndices, Is.EqualTo([7]));
        }

        [Test]
        public void TryGetQueryProjection_FavoriteRevisionInvalidatesWhenFavoritesFilterIsActive()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var filterState = new RecipeFilterState { FavoritesOnly = true };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                favoriteState: favoriteState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            favoriteState.Toggle(0);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(before.MatchingRecipeRuntimeIndices, Is.Empty);
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_FavoriteRevisionDoesNotInvalidateWhenFavoritesFilterIsInactive()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, favoriteState: favoriteState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            favoriteState.Toggle(0);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.SameAs(before));
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_ChecklistRevisionInvalidatesWhenCompletionFilterIsActive()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(itemCatalog);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                checklistState: checklistState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            checklistState.MarkFound(1);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.Empty);
        }

        [Test]
        public void TryGetQueryProjection_ChecklistRevisionInvalidatesWithoutCompletionFilter()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(itemCatalog);
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, checklistState: checklistState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            Assert.That(before.Result.IsMissing, Is.True);

            Assert.That(checklistState.MarkFound(1), Is.True);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.Result.IsFound, Is.True);
            Assert.That(after.Result.IsMissing, Is.False);
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetQueryProjection_ResearchRevisionInvalidatesWhenResearchFilterIsActive()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            JourneyResearchState researchState = CreateResearchState(1, amountNeeded: 1, progress: 0);
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                journeyResearchState: researchState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 1)]);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.Empty);
        }

        [Test]
        public void TryGetQueryProjection_CraftingRevisionInvalidatesWhenCraftableFilterIsActive()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var filterState = new RecipeFilterState { CraftableNowOnly = true };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                craftingAvailabilityState: craftingState,
                filterState: filterState);

            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection before);
            craftingState.ReplaceSnapshot([0]);
            model.TryGetQueryProjection(1, out RecipeQueryDetailsProjection after);

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after.MatchingRecipeRuntimeIndices, Is.EqualTo([0]));
        }

        [Test]
        public void TryGetProjection_CollectionFiltersDoNotHideStandaloneExactRecipe()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(itemCatalog);
            checklistState.MarkFound(1);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeDetailsModel model = CreateModel(
                itemCatalog,
                recipeCatalog,
                checklistState: checklistState,
                filterState: filterState);

            bool found = model.TryGetProjection(0, out RecipeDetailsProjection projection);

            Assert.That(found, Is.True);
            Assert.That(projection.RuntimeIndex, Is.Zero);
        }

        [Test]
        public void TryGetQueryProjection_DoesNotMutateFilterState()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var filterState = new RecipeFilterState();
            RecipeDetailsModel model = CreateModel(itemCatalog, recipeCatalog, filterState: filterState);
            long revision = filterState.Revision;

            model.TryGetQueryProjection(1, out _);

            Assert.That(filterState.Revision, Is.EqualTo(revision));
        }

        private static RecipeDetailsModel CreateModel(
            ItemCatalog itemCatalog,
            RecipeCatalog recipeCatalog,
            RecipeStationDisplayIndex stationDisplayIndex = null,
            ChecklistState checklistState = null,
            JourneyResearchState journeyResearchState = null,
            RecipeFavoriteState favoriteState = null,
            CraftingAvailabilityState craftingAvailabilityState = null,
            RecipeFilterState filterState = null,
            ItemTextIndex itemTextIndex = null,
            CompendiumLocalization localization = null)
        {
            stationDisplayIndex ??= CreateStationIndex(itemCatalog, recipeCatalog);
            checklistState ??= new ChecklistState(itemCatalog);
            favoriteState ??= CreateFavoriteState(recipeCatalog);
            craftingAvailabilityState ??= new CraftingAvailabilityState(recipeCatalog);
            filterState ??= new RecipeFilterState();
            itemTextIndex ??= new ItemTextIndex(itemCatalog);
            localization ??= _localization;

            return new RecipeDetailsModel(
                recipeCatalog,
                itemCatalog,
                itemTextIndex,
                RecipeIndex.Create(recipeCatalog),
                stationDisplayIndex,
                checklistState,
                journeyResearchState,
                favoriteState,
                craftingAvailabilityState,
                filterState,
                localization);
        }

        private static RecipeFavoriteState CreateFavoriteState(RecipeCatalog recipeCatalog)
        {
            return new RecipeFavoriteState(RecipePersistentKeyIndex.Create(recipeCatalog));
        }

        private static void ReplaceItemTextNames(
            ItemTextIndex itemTextIndex,
            ItemCatalog catalog,
            IReadOnlyDictionary<int, string> overrides,
            string cultureName)
        {
            var names = new Dictionary<int, string>(catalog.Count);
            var descriptions = new Dictionary<int, string>(catalog.Count);

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                names.Add(
                    entry.Id,
                    overrides != null && overrides.TryGetValue(entry.Id, out string name) ? name : entry.Name);
                descriptions.Add(entry.Id, string.Empty);
            }

            itemTextIndex.ReplaceSnapshot(cultureName, names, descriptions);
        }

        private static JourneyResearchState CreateResearchState(int itemId, int amountNeeded, int progress)
        {
            var state = new JourneyResearchState(
                [new JourneyResearchDefinition(itemId, itemId, amountNeeded, hasSharedResearchIdentity: false)]);

            if (progress > 0)
                state.ReplaceProgressSnapshot([new KeyValuePair<int, int>(itemId, progress)]);

            return state;
        }

        private static RecipeStationDisplayIndex CreateStationIndex(
            ItemCatalog itemCatalog,
            RecipeCatalog recipeCatalog)
        {
            return RecipeStationDisplayIndex.Create(itemCatalog, recipeCatalog, _ => -1);
        }

        private static ItemCatalog CreateItemCatalog(params ItemCatalogEntry[] entries)
        {
            return ItemCatalog.Create(entries);
        }

        private static RecipeCatalog CreateRecipeCatalog(params RecipeCatalogEntry[] entries)
        {
            return RecipeCatalog.Create(entries);
        }

        private static RecipeCatalogEntry CreateRecipeEntry(
            int runtimeIndex,
            int resultItemId,
            params RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                ingredients,
                CreateEnvironment(),
                isAlchemy: false);
        }

        private static RecipeCatalogEntry CreateRecipeEntry(
            int runtimeIndex,
            int resultItemId,
            RecipeEnvironmentRequirements environmentRequirements)
        {
            return new RecipeCatalogEntry(runtimeIndex, resultItemId, 1, [], environmentRequirements, isAlchemy: false);
        }

        private static RecipeIngredient CreateItemIngredient(int itemId, int stack)
        {
            return new RecipeIngredient(itemId, stack, RecipeIngredientRequirement.ForItem(itemId));
        }

        private static RecipeEnvironmentRequirements CreateEnvironment(int? requiredTileId = null)
        {
            return new RecipeEnvironmentRequirements(requiredTileId, false, false, false, false, false, false, false);
        }
    }
}