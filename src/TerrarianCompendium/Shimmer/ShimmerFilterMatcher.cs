using System;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Shimmer
{
    internal static class ShimmerFilterMatcher
    {
        public static bool MatchesResult(
            int resultItemId,
            ShimmerFilterState filterState,
            ChecklistState checklistState,
            JourneyResearchState journeyResearchState)
        {
            if (filterState == null)
                throw new ArgumentNullException(nameof(filterState));
            if (checklistState == null)
                throw new ArgumentNullException(nameof(checklistState));

            switch (filterState.CompletionFilter)
            {
                case ChecklistCompletionFilter.All:
                    break;
                case ChecklistCompletionFilter.Missing:
                    if (checklistState.IsFound(resultItemId))
                        return false;
                    break;
                case ChecklistCompletionFilter.Found:
                    if (!checklistState.IsFound(resultItemId))
                        return false;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported completion filter.");
            }

            switch (filterState.ResearchFilter)
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

        public static bool MatchesVariant(
            ShimmerTransformationVariant variant,
            ShimmerFilterState filterState,
            Func<ShimmerTransformationVariant, bool> isProgressionLocked)
        {
            if (variant == null)
                throw new ArgumentNullException(nameof(variant));
            if (filterState == null)
                throw new ArgumentNullException(nameof(filterState));
            if (isProgressionLocked == null)
                throw new ArgumentNullException(nameof(isProgressionLocked));

            if (filterState.Kind.HasValue && variant.Kind != filterState.Kind.Value)
                return false;

            if (!filterState.Progression.HasValue)
                return true;

            bool locked = isProgressionLocked(variant);
            return filterState.Progression.Value == ShimmerProgressionFilter.Locked ? locked : !locked;
        }
    }
}