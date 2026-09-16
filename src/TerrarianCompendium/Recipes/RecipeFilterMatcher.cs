using System;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Recipes
{
    internal static class RecipeFilterMatcher
    {
        public static bool Matches(
            RecipeCatalogEntry recipe,
            RecipeFilterState filterState,
            ChecklistState checklistState,
            JourneyResearchState journeyResearchState,
            RecipeFavoriteState favoriteState,
            CraftingAvailabilityState craftingAvailabilityState)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            if (filterState == null)
                throw new ArgumentNullException(nameof(filterState));

            if (checklistState == null)
                throw new ArgumentNullException(nameof(checklistState));

            if (favoriteState == null)
                throw new ArgumentNullException(nameof(favoriteState));

            if (craftingAvailabilityState == null)
                throw new ArgumentNullException(nameof(craftingAvailabilityState));

            if (!filterState.MatchesRequirements(recipe))
                return false;

            if (!MatchesCompletion(recipe.ResultItemId, filterState.CompletionFilter, checklistState))
                return false;

            if (!MatchesResearch(recipe.ResultItemId, filterState.ResearchFilter, journeyResearchState))
                return false;

            if (filterState.FavoritesOnly && !favoriteState.IsFavorite(recipe.RuntimeIndex))
                return false;

            return !filterState.CraftableNowOnly || craftingAvailabilityState.IsCraftable(recipe.RuntimeIndex);
        }

        private static bool MatchesCompletion(
            int resultItemId,
            ChecklistCompletionFilter filter,
            ChecklistState checklistState)
        {
            bool isFound = checklistState.IsFound(resultItemId);

            switch (filter)
            {
                case ChecklistCompletionFilter.All:
                    return true;

                case ChecklistCompletionFilter.Missing:
                    return !isFound;

                case ChecklistCompletionFilter.Found:
                    return isFound;

                default:
                    throw new InvalidOperationException("Unsupported completion filter.");
            }
        }

        private static bool MatchesResearch(
            int resultItemId,
            ChecklistResearchFilter filter,
            JourneyResearchState journeyResearchState)
        {
            switch (filter)
            {
                case ChecklistResearchFilter.All:
                    return true;

                case ChecklistResearchFilter.Researched:
                    return journeyResearchState != null && journeyResearchState.IsFullyResearched(resultItemId);

                case ChecklistResearchFilter.Unresearched:
                    return journeyResearchState != null && journeyResearchState.IsUnresearched(resultItemId);

                default:
                    throw new InvalidOperationException("Unsupported research filter.");
            }
        }
    }
}