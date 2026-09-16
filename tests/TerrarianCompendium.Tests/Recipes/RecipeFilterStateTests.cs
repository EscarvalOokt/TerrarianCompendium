using System;
using NUnit.Framework;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeFilterStateTests
    {
        [Test]
        public void NewState_HasNoActiveFiltersAndMatchesAnyRecipe()
        {
            var state = new RecipeFilterState();
            RecipeCatalogEntry recipe = CreateRecipe(0, CreateRequirements(requiredTileId: 10, requiresWater: true));

            Assert.That(state.RequiredTileId, Is.Null);
            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.None));
            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.All));
            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
            Assert.That(state.CraftableNowOnly, Is.False);
            Assert.That(state.FavoritesOnly, Is.False);
            Assert.That(state.IsActive, Is.False);
            Assert.That(state.Revision, Is.Zero);
            Assert.That(state.MatchesRequirements(recipe), Is.True);
        }

        [Test]
        public void RequiredTileId_MatchesExactStationAndAllowsAdditionalEnvironmentRequirements()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10
            };

            RecipeCatalogEntry matching = CreateRecipe(
                0,
                CreateRequirements(requiredTileId: 10, requiresWater: true, requiresHoney: true));
            RecipeCatalogEntry otherStation = CreateRecipe(1, CreateRequirements(requiredTileId: 20));
            RecipeCatalogEntry noStation = CreateRecipe(2, CreateRequirements());

            Assert.That(state.MatchesRequirements(matching), Is.True);
            Assert.That(state.MatchesRequirements(otherStation), Is.False);
            Assert.That(state.MatchesRequirements(noStation), Is.False);
        }

        [Test]
        public void SingleEnvironmentRequirement_MatchesRecipesContainingRequirement()
        {
            var state = new RecipeFilterState();
            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: true);

            RecipeCatalogEntry matching = CreateRecipe(0, CreateRequirements(requiresWater: true, requiresHoney: true));
            RecipeCatalogEntry missing = CreateRecipe(1, CreateRequirements(requiresHoney: true));

            Assert.That(state.MatchesRequirements(matching), Is.True);
            Assert.That(state.MatchesRequirements(missing), Is.False);
        }

        [TestCase((int)RecipeEnvironmentRequirementFilter.Water)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.Honey)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.Lava)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.SnowBiome)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.GraveyardBiome)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.Mechdusa)]
        [TestCase((int)RecipeEnvironmentRequirementFilter.TorchGodsFavor)]
        public void EachEnvironmentRequirement_MatchesItsCorrespondingRecipeField(int requirementValue)
        {
            var requirement = (RecipeEnvironmentRequirementFilter)requirementValue;
            var state = new RecipeFilterState();
            state.SetEnvironmentRequirement(requirement, enabled: true);

            RecipeCatalogEntry matching = CreateRecipe(0, CreateRequirementsForFilter(requirement));
            RecipeCatalogEntry missing = CreateRecipe(1, CreateRequirements());

            Assert.That(state.MatchesRequirements(matching), Is.True);
            Assert.That(state.MatchesRequirements(missing), Is.False);
        }

        [Test]
        public void MultipleEnvironmentRequirements_UseAndSemantics()
        {
            var state = new RecipeFilterState();
            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: true);
            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.SnowBiome, enabled: true);

            RecipeCatalogEntry both = CreateRecipe(0, CreateRequirements(requiresWater: true, requiresSnowBiome: true));
            RecipeCatalogEntry waterOnly = CreateRecipe(1, CreateRequirements(requiresWater: true));
            RecipeCatalogEntry snowOnly = CreateRecipe(2, CreateRequirements(requiresSnowBiome: true));

            Assert.That(state.MatchesRequirements(both), Is.True);
            Assert.That(state.MatchesRequirements(waterOnly), Is.False);
            Assert.That(state.MatchesRequirements(snowOnly), Is.False);
        }

        [Test]
        public void StationAndEnvironmentRequirements_UseAndSemantics()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10
            };
            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: true);

            RecipeCatalogEntry both = CreateRecipe(0, CreateRequirements(requiredTileId: 10, requiresWater: true));
            RecipeCatalogEntry stationOnly = CreateRecipe(1, CreateRequirements(requiredTileId: 10));
            RecipeCatalogEntry waterAtOtherStation = CreateRecipe(
                2,
                CreateRequirements(requiredTileId: 20, requiresWater: true));

            Assert.That(state.MatchesRequirements(both), Is.True);
            Assert.That(state.MatchesRequirements(stationOnly), Is.False);
            Assert.That(state.MatchesRequirements(waterAtOtherStation), Is.False);
        }

        [Test]
        public void EnvironmentRequirements_PropertySupportsMultipleSelectedFlags()
        {
            var state = new RecipeFilterState
            {
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Honey |
                                          RecipeEnvironmentRequirementFilter.Lava
            };

            Assert.That(state.IsEnvironmentRequirementEnabled(RecipeEnvironmentRequirementFilter.Honey), Is.True);
            Assert.That(state.IsEnvironmentRequirementEnabled(RecipeEnvironmentRequirementFilter.Lava), Is.True);
            Assert.That(state.IsEnvironmentRequirementEnabled(RecipeEnvironmentRequirementFilter.Water), Is.False);
        }

        [Test]
        public void SetEnvironmentRequirement_DisablingSelectedRequirementRemovesOnlyThatFlag()
        {
            var state = new RecipeFilterState
            {
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                          RecipeEnvironmentRequirementFilter.Honey
            };

            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: false);

            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.Honey));
        }

        [Test]
        public void ActualChanges_IncrementRevisionExactlyOnceEach()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10
            };

            Assert.That(state.Revision, Is.EqualTo(1));

            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: true);
            Assert.That(state.Revision, Is.EqualTo(2));

            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: false);
            Assert.That(state.Revision, Is.EqualTo(3));

            state.RequiredTileId = null;
            Assert.That(state.Revision, Is.EqualTo(4));
        }

        [Test]
        public void ReassigningSameValues_DoesNotIncrementRevision()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10,
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water
            };
            long revision = state.Revision;

            state.RequiredTileId = 10;
            state.EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water;
            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.Water, enabled: true);

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void NoRequirementsOnly_MatchesOnlyRecipesWithoutStationOrEnvironmentRequirements()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };

            RecipeCatalogEntry noRequirements = CreateRecipe(0, CreateRequirements());
            RecipeCatalogEntry station = CreateRecipe(1, CreateRequirements(requiredTileId: 10));
            RecipeCatalogEntry environment = CreateRecipe(
                2,
                CreateRequirements(
                    requiresWater: true,
                    requiresHoney: true,
                    requiresLava: true,
                    requiresSnowBiome: true,
                    requiresGraveyardBiome: true,
                    requiresMechdusa: true,
                    requiresTorchGodsFavor: true));

            Assert.That(state.IsActive, Is.True);
            Assert.That(state.MatchesRequirements(noRequirements), Is.True);
            Assert.That(state.MatchesRequirements(station), Is.False);
            Assert.That(state.MatchesRequirements(environment), Is.False);
        }

        [Test]
        public void EnablingNoRequirementsOnly_ClearsOtherRequirementFiltersWithOneRevision()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10,
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                          RecipeEnvironmentRequirementFilter.Honey
            };
            long revision = state.Revision;

            state.NoRequirementsOnly = true;

            Assert.That(state.NoRequirementsOnly, Is.True);
            Assert.That(state.RequiredTileId, Is.Null);
            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.None));
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void SelectingStation_DisablesNoRequirementsOnlyWithOneRevision()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };
            long revision = state.Revision;

            state.RequiredTileId = 10;

            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.RequiredTileId, Is.EqualTo(10));
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void EnablingEnvironmentRequirement_DisablesNoRequirementsOnlyWithOneRevision()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };
            long revision = state.Revision;

            state.SetEnvironmentRequirement(RecipeEnvironmentRequirementFilter.SnowBiome, enabled: true);

            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.SnowBiome));
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void DisablingNoRequirementsOnly_LeavesOrdinaryUnfilteredState()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };

            state.NoRequirementsOnly = false;

            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.RequiredTileId, Is.Null);
            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.None));
            Assert.That(state.IsActive, Is.False);
        }

        [Test]
        public void ReassigningNoRequirementsOnly_DoesNotIncrementRevision()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };
            long revision = state.Revision;

            state.NoRequirementsOnly = true;

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void RequiredTileId_WithNegativeValue_Throws()
        {
            var state = new RecipeFilterState();

            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { state.RequiredTileId = -1; }));
        }

        [Test]
        public void EnvironmentRequirements_WithUnsupportedFlag_Throws()
        {
            var state = new RecipeFilterState();

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { state.EnvironmentRequirements = (RecipeEnvironmentRequirementFilter)(1 << 20); }));
        }

        [Test]
        public void SetEnvironmentRequirement_WithMultipleFlags_Throws()
        {
            var state = new RecipeFilterState();
            RecipeEnvironmentRequirementFilter multiple = RecipeEnvironmentRequirementFilter.Water |
                                                          RecipeEnvironmentRequirementFilter.Honey;

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { state.SetEnvironmentRequirement(multiple, enabled: true); }));
        }

        [Test]
        public void IsEnvironmentRequirementEnabled_WithNone_Throws()
        {
            var state = new RecipeFilterState();

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => state.IsEnvironmentRequirementEnabled(RecipeEnvironmentRequirementFilter.None)));
        }

        [Test]
        public void Matches_WithNullRecipe_Throws()
        {
            var state = new RecipeFilterState();

            Assert.Throws<ArgumentNullException>((Action)(() => state.MatchesRequirements(null)));
        }

        [Test]
        public void AllSupportedEnvironmentRequirements_AreMatchedIndependently()
        {
            var state = new RecipeFilterState
            {
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                          RecipeEnvironmentRequirementFilter.Honey |
                                          RecipeEnvironmentRequirementFilter.Lava |
                                          RecipeEnvironmentRequirementFilter.SnowBiome |
                                          RecipeEnvironmentRequirementFilter.GraveyardBiome |
                                          RecipeEnvironmentRequirementFilter.Mechdusa |
                                          RecipeEnvironmentRequirementFilter.TorchGodsFavor
            };

            RecipeCatalogEntry matching = CreateRecipe(
                0,
                CreateRequirements(
                    requiresWater: true,
                    requiresHoney: true,
                    requiresLava: true,
                    requiresSnowBiome: true,
                    requiresGraveyardBiome: true,
                    requiresMechdusa: true,
                    requiresTorchGodsFavor: true));

            Assert.That(state.MatchesRequirements(matching), Is.True);
        }

        [Test]
        public void ActiveFilterCount_CountsStationAndEnvironmentFilters()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10,
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                          RecipeEnvironmentRequirementFilter.Honey
            };

            Assert.That(state.ActiveFilterCount, Is.EqualTo(3));
        }

        [Test]
        public void ActiveFilterCount_WithNoRequirementsOnly_IsOne()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true
            };

            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_RemovesAllFiltersAndIncrementsRevisionOnce()
        {
            var state = new RecipeFilterState
            {
                RequiredTileId = 10,
                EnvironmentRequirements = RecipeEnvironmentRequirementFilter.Water |
                                          RecipeEnvironmentRequirementFilter.SnowBiome,
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched,
                CraftableNowOnly = true,
                FavoritesOnly = true
            };
            long revision = state.Revision;

            state.Clear();

            Assert.That(state.RequiredTileId, Is.Null);
            Assert.That(state.EnvironmentRequirements, Is.EqualTo(RecipeEnvironmentRequirementFilter.None));
            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.All));
            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
            Assert.That(state.CraftableNowOnly, Is.False);
            Assert.That(state.FavoritesOnly, Is.False);
            Assert.That(state.IsActive, Is.False);
            Assert.That(state.ActiveFilterCount, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void Clear_OnAlreadyEmptyState_DoesNotIncrementRevision()
        {
            var state = new RecipeFilterState();

            state.Clear();

            Assert.That(state.Revision, Is.Zero);
            Assert.That(state.ActiveFilterCount, Is.Zero);
        }


        [Test]
        public void NewState_CollectionFiltersDefaultToAllAndCraftableNowIsDisabled()
        {
            var state = new RecipeFilterState();

            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.All));
            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
            Assert.That(state.CraftableNowOnly, Is.False);
            Assert.That(state.FavoritesOnly, Is.False);
            Assert.That(state.UsesChecklistState, Is.False);
            Assert.That(state.UsesJourneyResearchState, Is.False);
            Assert.That(state.UsesCraftingAvailabilityState, Is.False);
            Assert.That(state.UsesRecipeFavoriteState, Is.False);
        }

        [Test]
        public void CompletionFilter_ChangesRevisionAndChecklistDependency()
        {
            var state = new RecipeFilterState
            {
                CompletionFilter = ChecklistCompletionFilter.Missing
            };

            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.Missing));
            Assert.That(state.UsesChecklistState, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));

            state.CompletionFilter = ChecklistCompletionFilter.Found;

            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.Found));
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void ResearchFilter_ChangesRevisionAndResearchDependency()
        {
            var state = new RecipeFilterState
            {
                ResearchFilter = ChecklistResearchFilter.Unresearched
            };

            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.Unresearched));
            Assert.That(state.UsesJourneyResearchState, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void CraftableNowOnly_ChangesRevisionAndCraftingDependency()
        {
            var state = new RecipeFilterState
            {
                CraftableNowOnly = true
            };

            Assert.That(state.UsesCraftingAvailabilityState, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void FavoritesOnly_ChangesRevisionAndFavoriteDependency()
        {
            var state = new RecipeFilterState
            {
                FavoritesOnly = true
            };

            Assert.That(state.FavoritesOnly, Is.True);
            Assert.That(state.UsesRecipeFavoriteState, Is.True);
            Assert.That(state.IsActive, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));

            state.FavoritesOnly = false;

            Assert.That(state.UsesRecipeFavoriteState, Is.False);
            Assert.That(state.IsActive, Is.False);
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void NoRequirementsOnly_CoexistsWithCollectionAndCraftabilityFilters()
        {
            var state = new RecipeFilterState
            {
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched,
                CraftableNowOnly = true,
                NoRequirementsOnly = true
            };

            Assert.That(state.NoRequirementsOnly, Is.True);
            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.Missing));
            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.Unresearched));
            Assert.That(state.CraftableNowOnly, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(4));
        }

        [Test]
        public void SelectingRequirementFilter_DoesNotClearCollectionOrCraftabilityFilters()
        {
            var state = new RecipeFilterState
            {
                NoRequirementsOnly = true,
                CompletionFilter = ChecklistCompletionFilter.Found,
                ResearchFilter = ChecklistResearchFilter.Researched,
                CraftableNowOnly = true,
                RequiredTileId = 10
            };

            Assert.That(state.NoRequirementsOnly, Is.False);
            Assert.That(state.RequiredTileId, Is.EqualTo(10));
            Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.Found));
            Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.Researched));
            Assert.That(state.CraftableNowOnly, Is.True);
            Assert.That(state.ActiveFilterCount, Is.EqualTo(4));
        }

        [Test]
        public void CollectionFilter_WithUnsupportedValue_Throws()
        {
            var state = new RecipeFilterState();

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { state.CompletionFilter = (ChecklistCompletionFilter)999; }));
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { state.ResearchFilter = (ChecklistResearchFilter)999; }));
        }

        [Test]
        public void ReassigningCollectionAndCraftabilityFilters_DoesNotIncrementRevision()
        {
            var state = new RecipeFilterState
            {
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Researched,
                CraftableNowOnly = true,
                FavoritesOnly = true
            };
            long revision = state.Revision;

            state.CompletionFilter = ChecklistCompletionFilter.Missing;
            state.ResearchFilter = ChecklistResearchFilter.Researched;
            state.CraftableNowOnly = true;
            state.FavoritesOnly = true;

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        private static RecipeCatalogEntry CreateRecipe(int runtimeIndex, RecipeEnvironmentRequirements requirements)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                runtimeIndex + 1,
                1,
                Array.Empty<RecipeIngredient>(),
                requirements,
                isAlchemy: false);
        }

        private static RecipeEnvironmentRequirements CreateRequirementsForFilter(
            RecipeEnvironmentRequirementFilter requirement)
        {
            switch (requirement)
            {
                case RecipeEnvironmentRequirementFilter.Water:
                    return CreateRequirements(requiresWater: true);

                case RecipeEnvironmentRequirementFilter.Honey:
                    return CreateRequirements(requiresHoney: true);

                case RecipeEnvironmentRequirementFilter.Lava:
                    return CreateRequirements(requiresLava: true);

                case RecipeEnvironmentRequirementFilter.SnowBiome:
                    return CreateRequirements(requiresSnowBiome: true);

                case RecipeEnvironmentRequirementFilter.GraveyardBiome:
                    return CreateRequirements(requiresGraveyardBiome: true);

                case RecipeEnvironmentRequirementFilter.Mechdusa:
                    return CreateRequirements(requiresMechdusa: true);

                case RecipeEnvironmentRequirementFilter.TorchGodsFavor:
                    return CreateRequirements(requiresTorchGodsFavor: true);

                default:
                    throw new ArgumentOutOfRangeException(nameof(requirement), requirement, "Unsupported requirement.");
            }
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
    }
}