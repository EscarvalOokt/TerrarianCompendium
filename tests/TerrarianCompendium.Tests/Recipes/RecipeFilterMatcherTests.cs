using System;
using System.Collections.Generic;
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
    public sealed class RecipeFilterMatcherTests
    {
        [Test]
        public void Matches_ResultMissing_UsesChecklistState()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };

            Assert.That(Matches(recipe, filterState, checklistState, null, recipeCatalog), Is.True);

            checklistState.MarkFound(1);

            Assert.That(Matches(recipe, filterState, checklistState, null, recipeCatalog), Is.False);
        }

        [Test]
        public void Matches_ResultFound_UsesChecklistState()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            checklistState.MarkFound(1);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Found };

            Assert.That(Matches(recipe, filterState, checklistState, null, recipeCatalog), Is.True);
        }

        [Test]
        public void Matches_ResultResearched_UsesJourneyResearchState()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(1, amountNeeded: 5, progress: 5);
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Researched };

            Assert.That(Matches(recipe, filterState, checklistState, researchState, recipeCatalog), Is.True);
        }

        [Test]
        public void Matches_ResultUnresearched_UsesJourneyResearchState()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            JourneyResearchState researchState = CreateResearchState(1, amountNeeded: 5, progress: 2);
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };

            Assert.That(Matches(recipe, filterState, checklistState, researchState, recipeCatalog), Is.True);
        }

        [Test]
        public void Matches_Unresearched_DoesNotTreatNonResearchableItemAsUnresearched()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var researchState = new JourneyResearchState(Array.Empty<JourneyResearchDefinition>());
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };

            Assert.That(Matches(recipe, filterState, checklistState, researchState, recipeCatalog), Is.False);
        }

        [Test]
        public void Matches_ResearchFilterWithoutJourneyState_ReturnsFalse()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var filterState = new RecipeFilterState { ResearchFilter = ChecklistResearchFilter.Researched };

            Assert.That(Matches(recipe, filterState, checklistState, null, recipeCatalog), Is.False);
        }

        [Test]
        public void Matches_CraftableNow_UsesRuntimeRecipeIndex()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(7, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var filterState = new RecipeFilterState { CraftableNowOnly = true };

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.False);

            craftingState.ReplaceSnapshot([7]);

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.True);
        }

        [Test]
        public void Matches_RequirementAndCollectionFiltersUseAndSemantics()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1, CreateRequirements(requiredTileId: 10));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            checklistState.MarkFound(1);
            var filterState = new RecipeFilterState
            {
                RequiredTileId = 10,
                CompletionFilter = ChecklistCompletionFilter.Found,
                CraftableNowOnly = true
            };
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            craftingState.ReplaceSnapshot([0]);

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.True);

            filterState.RequiredTileId = 20;

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.False);
        }

        [Test]
        public void Matches_NoRequirementsCanCombineWithCollectionAndCraftabilityFilters()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var filterState = new RecipeFilterState
            {
                NoRequirementsOnly = true,
                CompletionFilter = ChecklistCompletionFilter.Missing,
                CraftableNowOnly = true
            };
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            craftingState.ReplaceSnapshot([0]);

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.True);
        }

        [Test]
        public void Matches_FavoritesOnly_UsesPersistentFavoriteState()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(7, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var filterState = new RecipeFilterState { FavoritesOnly = true };

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.False);

            favoriteState.Toggle(7);

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.True);
        }

        [Test]
        public void Matches_FavoriteAndRequirementFiltersUseAndSemantics()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1, CreateRequirements(requiredTileId: 10));
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            favoriteState.Toggle(0);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            var filterState = new RecipeFilterState { FavoritesOnly = true, RequiredTileId = 10 };

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.True);

            filterState.RequiredTileId = 20;

            Assert.That(
                RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState),
                Is.False);
        }

        [Test]
        public void Matches_DoesNotMutateOwners()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var filterState = new RecipeFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            long filterRevision = filterState.Revision;
            long checklistRevision = checklistState.Revision;
            long craftingRevision = craftingState.Revision;
            long favoriteRevision = favoriteState.Revision;

            RecipeFilterMatcher.Matches(recipe, filterState, checklistState, null, favoriteState, craftingState);

            Assert.That(filterState.Revision, Is.EqualTo(filterRevision));
            Assert.That(checklistState.Revision, Is.EqualTo(checklistRevision));
            Assert.That(craftingState.Revision, Is.EqualTo(craftingRevision));
            Assert.That(favoriteState.Revision, Is.EqualTo(favoriteRevision));
        }

        [Test]
        public void Matches_WithNullRequiredOwner_Throws()
        {
            ItemCatalog catalog = CreateItemCatalog(new ItemCatalogEntry(1, "Result"));
            RecipeCatalogEntry recipe = CreateRecipe(0, 1);
            RecipeCatalog recipeCatalog = CreateRecipeCatalog(recipe);
            var checklistState = new ChecklistState(catalog);
            var craftingState = new CraftingAvailabilityState(recipeCatalog);
            RecipeFavoriteState favoriteState = CreateFavoriteState(recipeCatalog);
            var filterState = new RecipeFilterState();

            Assert.Throws<ArgumentNullException>(
                (Action)(() => RecipeFilterMatcher.Matches(
                    null,
                    filterState,
                    checklistState,
                    null,
                    favoriteState,
                    craftingState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => RecipeFilterMatcher.Matches(
                    recipe,
                    null,
                    checklistState,
                    null,
                    favoriteState,
                    craftingState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => RecipeFilterMatcher.Matches(
                    recipe,
                    filterState,
                    null,
                    null,
                    favoriteState,
                    craftingState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => RecipeFilterMatcher.Matches(
                    recipe,
                    filterState,
                    checklistState,
                    null,
                    null,
                    craftingState)));
            Assert.Throws<ArgumentNullException>(
                (Action)(() => RecipeFilterMatcher.Matches(
                    recipe,
                    filterState,
                    checklistState,
                    null,
                    favoriteState,
                    null)));
        }

        private static bool Matches(
            RecipeCatalogEntry recipe,
            RecipeFilterState filterState,
            ChecklistState checklistState,
            JourneyResearchState researchState,
            RecipeCatalog recipeCatalog)
        {
            return RecipeFilterMatcher.Matches(
                recipe,
                filterState,
                checklistState,
                researchState,
                CreateFavoriteState(recipeCatalog),
                new CraftingAvailabilityState(recipeCatalog));
        }

        private static RecipeFavoriteState CreateFavoriteState(RecipeCatalog recipeCatalog)
        {
            return new RecipeFavoriteState(RecipePersistentKeyIndex.Create(recipeCatalog));
        }

        private static JourneyResearchState CreateResearchState(int itemId, int amountNeeded, int progress)
        {
            var state = new JourneyResearchState(
                [new JourneyResearchDefinition(itemId, itemId, amountNeeded, hasSharedResearchIdentity: false)]);

            if (progress > 0)
                state.ReplaceProgressSnapshot([new KeyValuePair<int, int>(itemId, progress)]);

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

        private static RecipeCatalogEntry CreateRecipe(
            int runtimeIndex,
            int resultItemId,
            RecipeEnvironmentRequirements requirements = null)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                [],
                requirements ?? CreateRequirements(),
                isAlchemy: false);
        }

        private static RecipeEnvironmentRequirements CreateRequirements(int? requiredTileId = null)
        {
            return new RecipeEnvironmentRequirements(requiredTileId, false, false, false, false, false, false, false);
        }
    }
}