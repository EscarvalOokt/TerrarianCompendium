using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeBrowserModelTests
    {
        [Test]
        public void Constructor_WithNullCatalog_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        null,
                        new ItemTextIndex(catalog),
                        recipeIndex,
                        new RecipeFilterState(),
                        checklistState,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullItemTextIndex_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        null,
                        RecipeIndex.Create(recipeCatalog),
                        new RecipeFilterState(),
                        checklistState,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullRecipeIndex_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        new ItemTextIndex(catalog),
                        null,
                        new RecipeFilterState(),
                        checklistState,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullFilterState_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        new ItemTextIndex(catalog),
                        RecipeIndex.Create(recipeCatalog),
                        null,
                        checklistState,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullChecklistState_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        new ItemTextIndex(catalog),
                        RecipeIndex.Create(recipeCatalog),
                        new RecipeFilterState(),
                        null,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullFavoriteState_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        new ItemTextIndex(catalog),
                        RecipeIndex.Create(recipeCatalog),
                        new RecipeFilterState(),
                        checklistState,
                        null,
                        null,
                        craftingState);
                }));
        }

        [Test]
        public void Constructor_WithNullCraftingAvailabilityState_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);

            Assert.Throws<ArgumentNullException>(
                (Action)(() =>
                {
                    _ = new RecipeBrowserModel(
                        catalog,
                        new ItemTextIndex(catalog),
                        RecipeIndex.Create(recipeCatalog),
                        new RecipeFilterState(),
                        checklistState,
                        null,
                        CreateFavoriteState(recipeCatalog),
                        null);
                }));
        }

        [Test]
        public void Defaults_ReturnOnlyItemsWithProducingRecipes()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Ingredient"),
                new ItemCatalogEntry(3, "Unused"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, CreateItemIngredient(2)));

            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(model.SearchQuery, Is.Empty);
            Assert.That(model.NavigationFilter, Is.EqualTo(ChecklistNavigationFilter.AllItems));
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
            Assert.That(model.IsItemAvailable(1), Is.True);
            Assert.That(model.IsItemAvailable(2), Is.False);
        }

        [Test]
        public void IsContextItemAvailable_ProducingIngredientAndUnrelatedItemsUseStaticRecipeRelations()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Ingredient"),
                new ItemCatalogEntry(3, "Unused"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1, CreateItemIngredient(2)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.Multiple(() =>
            {
                Assert.That(model.IsContextItemAvailable(1), Is.True);
                Assert.That(model.IsContextItemAvailable(2), Is.True);
                Assert.That(model.IsContextItemAvailable(3), Is.False);
                Assert.That(model.IsContextItemAvailable(999), Is.False);
            });
        }

        [Test]
        public void IsContextItemAvailable_RecipeGroupMemberUsesExistingReverseRelation()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Display Ingredient"),
                new ItemCatalogEntry(3, "Alternative Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateRecipeGroupIngredient(2, 10, 2, 3)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(model.IsContextItemAvailable(3), Is.True);
        }

        [Test]
        public void IsContextItemAvailable_IgnoresSearchNavigationAndRecipeFilters()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Weapon Result", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Furniture Ingredient", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(
                    0,
                    1,
                    CreateRequirements(requiredTileId: 20),
                    CreateItemIngredient(2)));
            var filterState = new RecipeFilterState { RequiredTileId = 10 };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState);
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.SearchQuery = "No matching result";

            Assert.Multiple(() =>
            {
                Assert.That(model.MatchingItems, Is.Empty);
                Assert.That(model.IsItemAvailable(1), Is.False);
                Assert.That(model.IsContextItemAvailable(2), Is.True);
            });
        }

        [Test]
        public void ContextItemId_OrdinaryIngredientLimitsProjectionToUsedInResults()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "First Result"),
                new ItemCatalogEntry(2, "Second Result"),
                new ItemCatalogEntry(8, "Other Ingredient"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateItemIngredient(9)),
                CreateRecipeEntry(1, 2, CreateItemIngredient(8)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.ContextItemId = 9;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void ContextItemId_RecipeGroupMemberUsesExistingReverseRelation()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(2, "Display Ingredient"),
                new ItemCatalogEntry(3, "Alternative Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateRecipeGroupIngredient(2, 10, 2, 3)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.ContextItemId = 3;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void ContextItemId_MultipleCandidateRecipesDeduplicateResultsAndPreserveCatalogOrder()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(30, "Third Result"),
                new ItemCatalogEntry(10, "First Result"),
                new ItemCatalogEntry(20, "Second Result"),
                new ItemCatalogEntry(99, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 30, CreateItemIngredient(99)),
                CreateRecipeEntry(1, 10, CreateItemIngredient(99)),
                CreateRecipeEntry(2, 10, CreateItemIngredient(99)),
                CreateRecipeEntry(3, 20, CreateItemIngredient(99)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.ContextItemId = 99;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([10, 20, 30]));
        }

        [Test]
        public void ContextItemId_ChangeInvalidatesProjectionAndNullRestoresGlobalProjection()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "First Result"),
                new ItemCatalogEntry(2, "Second Result"),
                new ItemCatalogEntry(8, "First Context"),
                new ItemCatalogEntry(9, "Second Context"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateItemIngredient(8)),
                CreateRecipeEntry(1, 2, CreateItemIngredient(9)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);
            IReadOnlyList<ItemCatalogEntry> global = model.MatchingItems;

            model.ContextItemId = 8;
            IReadOnlyList<ItemCatalogEntry> firstContext = model.MatchingItems;
            model.ContextItemId = 9;
            IReadOnlyList<ItemCatalogEntry> secondContext = model.MatchingItems;
            model.ContextItemId = null;
            IReadOnlyList<ItemCatalogEntry> restoredGlobal = model.MatchingItems;

            Assert.Multiple(() =>
            {
                Assert.That(GetItemIds(global), Is.EqualTo([1, 2]));
                Assert.That(firstContext, Is.Not.SameAs(global));
                Assert.That(GetItemIds(firstContext), Is.EqualTo([1]));
                Assert.That(secondContext, Is.Not.SameAs(firstContext));
                Assert.That(GetItemIds(secondContext), Is.EqualTo([2]));
                Assert.That(restoredGlobal, Is.Not.SameAs(secondContext));
                Assert.That(GetItemIds(restoredGlobal), Is.EqualTo([1, 2]));
            });
        }

        [Test]
        public void ContextItemId_EmptyUsedInProjectionDoesNotChangeGlobalOrContextAvailability()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.ContextItemId = 1;

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.IsItemAvailable(1), Is.True);
            Assert.That(model.IsContextItemAvailable(1), Is.True);
        }

        [Test]
        public void ContextItemId_NonContextualProducingRecipeCannotSatisfyRequirementFilter()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(8, "Other Ingredient"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(
                    0,
                    1,
                    CreateRequirements(requiredTileId: 20),
                    CreateItemIngredient(9)),
                CreateRecipeEntryWithRequirements(
                    1,
                    1,
                    CreateRequirements(requiredTileId: 10),
                    CreateItemIngredient(8)));
            var filterState = new RecipeFilterState { RequiredTileId = 10 };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState);
            model.ContextItemId = 9;

            Assert.That(model.MatchingItems, Is.Empty);
        }

        [Test]
        public void ContextItemId_RecipeLevelFiltersMustMatchSameContextualCandidate()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(
                    3,
                    1,
                    CreateRequirements(requiredTileId: 10),
                    CreateItemIngredient(9)),
                CreateRecipeEntryWithRequirements(
                    7,
                    1,
                    CreateRequirements(requiredTileId: 20),
                    CreateItemIngredient(9)));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(3);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([7]);
            var filterState = new RecipeFilterState
            {
                RequiredTileId = 10,
                FavoritesOnly = true,
                CraftableNowOnly = true
            };
            RecipeBrowserModel model = CreateModel(
                catalog,
                recipeCatalog,
                filterState,
                favoriteState: favoriteState,
                craftingState: craftingState);
            model.ContextItemId = 9;

            Assert.That(model.MatchingItems, Is.Empty);

            craftingState.ReplaceSnapshot([3, 7]);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void ContextItemId_CompletionFilterAppliesToResultItems()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Missing Result"),
                new ItemCatalogEntry(2, "Found Result"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateItemIngredient(9)),
                CreateRecipeEntry(1, 2, CreateItemIngredient(9)));
            var checklistState = new ChecklistState(catalog);
            checklistState.MarkFound(2);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, checklistState: checklistState);
            model.ContextItemId = 9;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));

            filterState.CompletionFilter = ChecklistCompletionFilter.Found;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void ContextItemId_ResearchFilterAppliesToResultItems()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Researched Result"),
                new ItemCatalogEntry(2, "Unresearched Result"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateItemIngredient(9)),
                CreateRecipeEntry(1, 2, CreateItemIngredient(9)));
            JourneyResearchState researchState = CreateResearchState((1, 1, 1), (2, 2, 0));
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Researched };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, researchState: researchState);
            model.ContextItemId = 9;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));

            filterState.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void ContextItemId_SearchAndCategoryRemainResultItemPredicates()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Target Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Target Chair", ItemCategoryMembership.Furniture),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1, CreateItemIngredient(9)),
                CreateRecipeEntry(1, 2, CreateItemIngredient(9)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);
            model.ContextItemId = 9;
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.SearchQuery = "Target";

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));

            model.SearchQuery = "Chair";

            Assert.That(model.MatchingItems, Is.Empty);
        }

        [Test]
        public void ContextItemId_FilterRevisionRefreshesCachedProjection()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Result"),
                new ItemCatalogEntry(9, "Context Ingredient"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(
                    0,
                    1,
                    CreateRequirements(requiredTileId: 10),
                    CreateItemIngredient(9)));
            var filterState = new RecipeFilterState { RequiredTileId = 10 };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState);
            model.ContextItemId = 9;
            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;

            filterState.RequiredTileId = 20;
            IReadOnlyList<ItemCatalogEntry> after = model.MatchingItems;

            Assert.Multiple(() =>
            {
                Assert.That(after, Is.Not.SameAs(before));
                Assert.That(after, Is.Empty);
            });
        }

        [Test]
        public void MatchingItems_WithMultipleRecipesForSameResult_ContainsResultOnce()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));

            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void HasFavoriteRecipe_WithNoFavoriteProducingRecipe_ReturnsFalse()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(model.HasFavoriteRecipe(1), Is.False);
        }

        [Test]
        public void HasFavoriteRecipe_WithAnyFavoriteProducingRecipe_ReturnsTrue()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(1);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, favoriteState: favoriteState);

            Assert.That(model.HasFavoriteRecipe(1), Is.True);
        }

        [Test]
        public void HasFavoriteRecipe_AfterFavoriteIsRemoved_ReturnsFalse()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(0);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, favoriteState: favoriteState);

            Assert.That(model.HasFavoriteRecipe(1), Is.True);

            favoriteState.Toggle(0);

            Assert.That(model.HasFavoriteRecipe(1), Is.False);
        }

        [Test]
        public void HasFavoriteRecipe_DoesNotDependOnActiveRecipeFilters()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 10)));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(0);
            var filterState = new RecipeFilterState { RequiredTileId = 20 };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, favoriteState: favoriteState);

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.HasFavoriteRecipe(1), Is.True);
        }

        [Test]
        public void NavigationFilter_RootCategory_LimitsResultItems()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void NavigationFilter_NestedCategory_AppliesParentAndSemanticScope()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(
                    2,
                    "Magic Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Magic),
                new ItemCatalogEntry(
                    3,
                    "Melee Furniture",
                    ItemCategoryMembership.Furniture,
                    semanticMemberships: ItemSemanticMembership.Melee));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.WeaponsMelee);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void NavigationFilter_Other_ReturnsOnlyRecipeResultResidual()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(2, "Residual Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.True);

            model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Weapons);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void HasOtherItems_IgnoresResidualItemsWithoutProducingRecipes()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(
                    1,
                    "Melee Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee),
                new ItemCatalogEntry(2, "Residual Without Recipe", ItemCategoryMembership.Weapons));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.False);
            Assert.Throws<InvalidOperationException>(
                (Action)(() =>
                {
                    model.NavigationFilter = ChecklistNavigationFilter.ForOther(ItemNavigationNodeId.Weapons);
                }));
        }

        [Test]
        public void HasOtherItems_IsIndependentOfSearchAndRecipeFilters()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Residual Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(
                    2,
                    "Melee Weapon",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 20)),
                CreateRecipeEntryWithRequirements(1, 2, CreateRequirements(requiredTileId: 10)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            model.SearchQuery = "Melee";
            filterState.RequiredTileId = 10;

            Assert.That(model.HasOtherItems(ItemNavigationNodeId.Weapons), Is.True);
        }

        [Test]
        public void NavigationFilter_CombinesWithSearch()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Copper Sword", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Copper Chair", ItemCategoryMembership.Furniture),
                new ItemCatalogEntry(3, "Wooden Sword", ItemCategoryMembership.Weapons));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.SearchQuery = "Copper";

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void NavigationFilter_CombinesWithRecipeFiltersAtResultItemBoundary()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Matching Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Wrong Recipe Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Matching Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 20)),
                CreateRecipeEntryWithRequirements(1, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(2, 2, CreateRequirements(requiredTileId: 20)),
                CreateRecipeEntryWithRequirements(3, 3, CreateRequirements(requiredTileId: 10)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            filterState.RequiredTileId = 10;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void NavigationFilter_CombinesWithCompletionResearchAndCraftableFilters()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Matching Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Missing Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(3, "Matching Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3));
            var checklistState = new ChecklistState(catalog);
            checklistState.MarkFound(1);
            checklistState.MarkFound(3);
            JourneyResearchState researchState = CreateResearchState((1, 1, 1), (2, 1, 1), (3, 1, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([0, 2]);
            var filterState = new RecipeFilterState
            {
                CompletionFilter = ChecklistCompletionFilter.Found,
                ResearchFilter = ChecklistResearchFilter.Researched,
                CraftableNowOnly = true
            };
            RecipeBrowserModel model = CreateModel(
                catalog,
                recipeCatalog,
                filterState,
                checklistState,
                researchState,
                craftingState: craftingState);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void IsItemAvailable_UsesNavigationAndRecipeFiltersButIgnoresSearch()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(1, 2, CreateRequirements(requiredTileId: 10)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            model.SearchQuery = "No matching result name";
            filterState.RequiredTileId = 10;

            Assert.Multiple(() =>
            {
                Assert.That(model.MatchingItems, Is.Empty);
                Assert.That(model.IsItemAvailable(1), Is.True);
                Assert.That(model.IsItemAvailable(2), Is.False);
            });
        }

        [Test]
        public void NavigationFilterChange_InvalidatesProjectionWithoutChangingRecipeFilterRevision()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Weapon", ItemCategoryMembership.Weapons),
                new ItemCatalogEntry(2, "Furniture", ItemCategoryMembership.Furniture));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);
            long filterRevision = filterState.Revision;
            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;

            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            IReadOnlyList<ItemCatalogEntry> after = model.MatchingItems;

            Assert.Multiple(() =>
            {
                Assert.That(after, Is.Not.SameAs(before));
                Assert.That(GetItemIds(after), Is.EqualTo([1]));
                Assert.That(filterState.Revision, Is.EqualTo(filterRevision));
            });
        }

        [Test]
        public void SearchQuery_IsTrimmedAndMatchesResultNamesCaseInsensitive()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Wooden Sword"),
                new ItemCatalogEntry(2, "Copper Shortsword"),
                new ItemCatalogEntry(3, "Wood Wall"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.SearchQuery = "  WoOd  ";

            Assert.That(model.SearchQuery, Is.EqualTo("WoOd"));
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void SearchQuery_UsesItemTextIndexNameInsteadOfCatalogName()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Copper Sword"),
                new ItemCatalogEntry(2, "Wooden Sword"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            var itemTextIndex = new ItemTextIndex(catalog);
            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string>
                {
                    [1] = "Медный меч",
                    [2] = "Деревянный меч"
                },
                "ru-RU");
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, itemTextIndex: itemTextIndex);

            model.SearchQuery = "Copper";
            Assert.That(model.MatchingItems, Is.Empty);

            model.SearchQuery = "медный";
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void SearchQuery_AfterItemTextRevisionChange_RefreshesWithoutChangingQuery()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            var itemTextIndex = new ItemTextIndex(catalog);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, itemTextIndex: itemTextIndex);
            model.SearchQuery = "target";

            Assert.That(model.MatchingItems, Is.Empty);

            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string> { [2] = "Target Result" },
                "fr-FR");

            Assert.That(model.SearchQuery, Is.EqualTo("target"));
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void SynchronizeFilters_WithEmptySearch_IgnoresItemTextRevision()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var itemTextIndex = new ItemTextIndex(catalog);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, itemTextIndex: itemTextIndex);

            _ = model.MatchingItems;
            ReplaceItemTextNames(
                itemTextIndex,
                catalog,
                new Dictionary<int, string> { [1] = "Localized Result" },
                "fr-FR");

            Assert.That(model.SynchronizeFilters(), Is.False);
        }

        [Test]
        public void SearchQuery_NullBehavesAsEmptyQuery()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "First"),
                new ItemCatalogEntry(2, "Second"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            model.SearchQuery = "First";
            model.SearchQuery = null;

            Assert.That(model.SearchQuery, Is.Empty);
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1, 2]));
        }

        [Test]
        public void MatchingItems_FollowCatalogOrder()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(30, "Third"),
                new ItemCatalogEntry(10, "First"),
                new ItemCatalogEntry(20, "Second"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 30),
                CreateRecipeEntry(1, 10),
                CreateRecipeEntry(2, 20));

            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([10, 20, 30]));
        }

        [Test]
        public void RequirementFilter_KeepsResultWhenAnyProducingRecipeMatches()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(1, 1, CreateRequirements(requiredTileId: 20)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            filterState.RequiredTileId = 10;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
            Assert.That(model.IsItemAvailable(1), Is.True);
        }

        [Test]
        public void RequirementFilter_RemovesResultWhenNoProducingRecipeMatches()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 20)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            filterState.RequiredTileId = 10;

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.IsItemAvailable(1), Is.False);
        }

        [Test]
        public void EnvironmentFilters_UseRecipeFilterStateAndSemantics()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Both"),
                new ItemCatalogEntry(2, "Water Only"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(
                    0,
                    1,
                    CreateRequirements(requiresWater: true, requiresSnowBiome: true)),
                CreateRecipeEntryWithRequirements(1, 2, CreateRequirements(requiresWater: true)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);
            filterState.EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                                  RecipeEnvironmentRequirementFilter.SnowBiome;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void NoRequirementsOnly_ReturnsOnlyResultsWithMatchingProducingRecipe()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "No Requirements"),
                new ItemCatalogEntry(2, "Station"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntryWithRequirements(1, 2, CreateRequirements(requiredTileId: 10)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            filterState.NoRequirementsOnly = true;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void CompletionMissingAndFound_FilterResultItems()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Missing"),
                new ItemCatalogEntry(2, "Found"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1), CreateRecipeEntry(1, 2));
            var checklistState = new ChecklistState(catalog);
            checklistState.MarkFound(2);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, checklistState: checklistState);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));

            filterState.CompletionFilter = ChecklistCompletionFilter.Found;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void ResearchFilters_UseExistingJourneySemantics()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Researched"),
                new ItemCatalogEntry(2, "Unresearched"),
                new ItemCatalogEntry(3, "Not Researchable"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntry(0, 1),
                CreateRecipeEntry(1, 2),
                CreateRecipeEntry(2, 3));
            JourneyResearchState researchState = CreateResearchState((1, 1, 1), (2, 2, 0));
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Researched };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, researchState: researchState);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));

            filterState.ResearchFilter = ChecklistResearchFilter.Unresearched;

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([2]));
        }

        [Test]
        public void CraftableNow_KeepsResultWhenAnyProducingRecipeIsCraftable()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(3, 1), CreateRecipeEntry(7, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([7]);
            var filterState = new RecipeFilterState { CraftableNowOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, craftingState: craftingState);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void RequirementAndCraftableNow_MustMatchSameProducingRecipe()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(3, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(7, 1, CreateRequirements(requiredTileId: 20)));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            craftingState.ReplaceSnapshot([7]);
            var filterState = new RecipeFilterState { RequiredTileId = 10, CraftableNowOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, craftingState: craftingState);

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.IsItemAvailable(1), Is.False);
        }

        [Test]
        public void ChecklistRevision_InvalidatesCachedProjectionWhenCompletionFilterIsActive()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, checklistState: checklistState);

            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;
            checklistState.MarkFound(1);

            Assert.That(model.SynchronizeFilters(), Is.True);
            IReadOnlyList<ItemCatalogEntry> after = model.MatchingItems;
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(after, Is.Empty);
        }

        [Test]
        public void ResearchRevision_InvalidatesCachedProjectionWhenResearchFilterIsActive()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            JourneyResearchState researchState = CreateResearchState((1, 1, 0));
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, researchState: researchState);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
            researchState.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 1)]);

            Assert.That(model.SynchronizeFilters(), Is.True);
            Assert.That(model.MatchingItems, Is.Empty);
        }

        [Test]
        public void CraftingRevision_InvalidatesCachedProjectionWhenCraftableFilterIsActive()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var filterState = new RecipeFilterState { CraftableNowOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, craftingState: craftingState);

            Assert.That(model.MatchingItems, Is.Empty);
            craftingState.ReplaceSnapshot([0]);

            Assert.That(model.SynchronizeFilters(), Is.True);
            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
        }

        [Test]
        public void FavoritesOnly_ReturnsResultWhenAnyProducingRecipeIsFavorite()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(3, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(7, 1, CreateRequirements(requiredTileId: 20)));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(7);
            var filterState = new RecipeFilterState { FavoritesOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, favoriteState: favoriteState);

            Assert.That(GetItemIds(model.MatchingItems), Is.EqualTo([1]));
            Assert.That(model.IsItemAvailable(1), Is.True);
        }

        [Test]
        public void FavoritesOnly_RejectsResultWithoutFavoritedProducingRecipe()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var filterState = new RecipeFilterState { FavoritesOnly = true };

            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState);

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.IsItemAvailable(1), Is.False);
        }

        [Test]
        public void FavoriteAndRequirementFilters_MustMatchSameProducingRecipe()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(3, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(7, 1, CreateRequirements(requiredTileId: 20)));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(7);
            var filterState = new RecipeFilterState { RequiredTileId = 10, FavoritesOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, favoriteState: favoriteState);

            Assert.That(model.MatchingItems, Is.Empty);
            Assert.That(model.IsItemAvailable(1), Is.False);
        }

        [Test]
        public void FavoriteRevision_InvalidatesCachedProjectionWhenFavoritesFilterIsActive()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var filterState = new RecipeFilterState { FavoritesOnly = true };
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, filterState, favoriteState: favoriteState);

            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;
            Assert.That(before, Is.Empty);

            favoriteState.Toggle(0);

            Assert.That(model.SynchronizeFilters(), Is.True);
            IReadOnlyList<ItemCatalogEntry> after = model.MatchingItems;
            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(GetItemIds(after), Is.EqualTo([1]));
        }

        [Test]
        public void FavoriteRevision_DoesNotInvalidateCachedProjectionWhenFavoritesFilterIsInactive()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, favoriteState: favoriteState);

            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;
            favoriteState.Toggle(0);

            Assert.That(model.SynchronizeFilters(), Is.False);
            Assert.That(model.MatchingItems, Is.SameAs(before));
        }

        [Test]
        public void UnusedSessionRevision_DoesNotInvalidateProjection()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            var checklistState = new ChecklistState(catalog);
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, checklistState: checklistState);

            _ = model.MatchingItems;
            checklistState.MarkFound(1);

            Assert.That(model.SynchronizeFilters(), Is.False);
        }

        [Test]
        public void FilterRevision_InvalidatesCachedResultProjection()
        {
            ItemCatalog catalog = CreateItemCatalog(
                new ItemCatalogEntry(1, "Workbench"),
                new ItemCatalogEntry(2, "Furnace"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(
                CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 10)),
                CreateRecipeEntryWithRequirements(1, 2, CreateRequirements(requiredTileId: 20)));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            IReadOnlyList<ItemCatalogEntry> before = model.MatchingItems;
            filterState.RequiredTileId = 10;
            IReadOnlyList<ItemCatalogEntry> after = model.MatchingItems;

            Assert.That(after, Is.Not.SameAs(before));
            Assert.That(GetItemIds(after), Is.EqualTo([1]));
        }

        [Test]
        public void SynchronizeFilters_ReturnsTrueOnlyWhenRelevantStateChanged()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(CreateRecipeEntry(0, 1));
            RecipeBrowserModel model = CreateModel(catalog, recipeCatalog, out RecipeFilterState filterState);

            Assert.That(model.SynchronizeFilters(), Is.False);

            filterState.RequiredTileId = 10;

            Assert.That(model.SynchronizeFilters(), Is.True);
            Assert.That(model.SynchronizeFilters(), Is.False);
        }

        [Test]
        public void Reads_DoNotMutateCatalogRecipeIndexOrOwners()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result", ItemCategoryMembership.Weapons));
            RecipeCatalogEntry recipe = CreateRecipeEntryWithRequirements(0, 1, CreateRequirements(requiredTileId: 10));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var recipeIndex = RecipeIndex.Create(recipeCatalog);
            var filterState = new RecipeFilterState { RequiredTileId = 10 };
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var itemTextIndex = new ItemTextIndex(catalog);
            long filterRevision = filterState.Revision;
            long checklistRevision = checklistState.Revision;
            long craftingRevision = craftingState.Revision;
            long favoriteRevision = favoriteState.Revision;
            long itemTextRevision = itemTextIndex.Revision;
            var model = new RecipeBrowserModel(
                catalog,
                itemTextIndex,
                recipeIndex,
                filterState,
                checklistState,
                null,
                favoriteState,
                craftingState);
            IReadOnlyList<ItemCatalogEntry> itemsBefore = catalog.Items;
            IReadOnlyList<RecipeCatalogEntry> recipesBefore = recipeIndex.GetRecipesProducing(1);

            model.SearchQuery = "Result";
            model.NavigationFilter = ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons);
            _ = model.MatchingItems;
            _ = model.IsItemAvailable(1);

            Assert.That(catalog.Items, Is.EqualTo(itemsBefore));
            Assert.That(recipeIndex.GetRecipesProducing(1), Is.EqualTo(recipesBefore));
            Assert.That(filterState.Revision, Is.EqualTo(filterRevision));
            Assert.That(checklistState.Revision, Is.EqualTo(checklistRevision));
            Assert.That(craftingState.Revision, Is.EqualTo(craftingRevision));
            Assert.That(favoriteState.Revision, Is.EqualTo(favoriteRevision));
            Assert.That(itemTextIndex.Revision, Is.EqualTo(itemTextRevision));
            Assert.That(filterState.RequiredTileId, Is.EqualTo(10));
            Assert.That(
                model.NavigationFilter,
                Is.EqualTo(ChecklistNavigationFilter.ForNode(ItemNavigationNodeId.Weapons)));
        }

        private static RecipeBrowserModel CreateModel(
            ItemCatalog catalog,
            RecipeCatalog recipeCatalog,
            out RecipeFilterState filterState)
        {
            filterState = new RecipeFilterState();
            return CreateModel(catalog, recipeCatalog, filterState);
        }

        private static RecipeBrowserModel CreateModel(
            ItemCatalog catalog,
            RecipeCatalog recipeCatalog,
            RecipeFilterState filterState = null,
            ChecklistState checklistState = null,
            JourneyResearchState researchState = null,
            RecipeFavoriteState favoriteState = null,
            CraftingAvailabilityState craftingState = null,
            ItemTextIndex itemTextIndex = null)
        {
            filterState ??= new RecipeFilterState();
            checklistState ??= new ChecklistState(catalog);
            favoriteState ??= CreateFavoriteState(recipeCatalog);
            craftingState ??= new CraftingAvailabilityState(recipeCatalog);
            itemTextIndex ??= new ItemTextIndex(catalog);

            return new RecipeBrowserModel(
                catalog,
                itemTextIndex,
                RecipeIndex.Create(recipeCatalog),
                filterState,
                checklistState,
                researchState,
                favoriteState,
                craftingState);
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

        private static RecipeFavoriteState CreateFavoriteState(RecipeCatalog recipeCatalog)
        {
            return new RecipeFavoriteState(RecipePersistentKeyIndex.Create(recipeCatalog));
        }

        private static JourneyResearchState CreateResearchState(params (int ItemId, int Needed, int Progress)[] items)
        {
            var definitions = new List<JourneyResearchDefinition>(items.Length);
            var progress = new List<KeyValuePair<int, int>>();

            foreach ((int itemId, int needed, int current) in items)
            {
                definitions.Add(
                    new JourneyResearchDefinition(itemId, itemId, needed, hasSharedResearchIdentity: false));

                if (current > 0)
                    progress.Add(new KeyValuePair<int, int>(itemId, current));
            }

            var state = new JourneyResearchState(definitions);
            state.ReplaceProgressSnapshot(progress);
            return state;
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
                CreateRequirements(),
                isAlchemy: false);
        }

        private static RecipeCatalogEntry CreateRecipeEntryWithRequirements(
            int runtimeIndex,
            int resultItemId,
            RecipeEnvironmentRequirements requirements,
            params RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(runtimeIndex, resultItemId, 1, ingredients, requirements, isAlchemy: false);
        }

        private static RecipeEnvironmentRequirements CreateRequirements(
            int? requiredTileId = null,
            bool requiresWater = false,
            bool requiresHoney = false,
            bool requiresLava = false,
            bool requiresSnowBiome = false,
            bool requiresGraveyardBiome = false,
            bool requiresMechdusa = false,
            bool requiresTorchGodsFavor = false)
        {
            return new RecipeEnvironmentRequirements(
                requiredTileId,
                requiresWater,
                requiresHoney,
                requiresLava,
                requiresSnowBiome,
                requiresGraveyardBiome,
                requiresMechdusa,
                requiresTorchGodsFavor);
        }

        private static RecipeIngredient CreateItemIngredient(int itemId)
        {
            return new RecipeIngredient(itemId, 1, RecipeIngredientRequirement.ForItem(itemId));
        }

        private static RecipeIngredient CreateRecipeGroupIngredient(
            int displayItemId,
            int recipeGroupId,
            params int[] validItemIds)
        {
            return new RecipeIngredient(
                displayItemId,
                1,
                RecipeIngredientRequirement.ForRecipeGroup(recipeGroupId, validItemIds));
        }

        private static int[] GetItemIds(IReadOnlyList<ItemCatalogEntry> items)
        {
            return items.Select(entry => entry.Id).ToArray();
        }
    }
}