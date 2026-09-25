using System;
using TerrarianCompendium.Filtering;

namespace TerrarianCompendium.Shimmer
{
    internal enum ShimmerProgressionFilter
    {
        Unlocked,
        Locked
    }

    internal sealed class ShimmerFilterState
    {
        private ChecklistCompletionFilter _completionFilter = ChecklistCompletionFilter.All;
        private ShimmerTransformationKind? _kind;
        private ShimmerProgressionFilter? _progression;
        private ChecklistResearchFilter _researchFilter = ChecklistResearchFilter.All;
        private long _revision;

        public ShimmerTransformationKind? Kind
        {
            get => _kind;
            set
            {
                ValidateKind(value);

                if (_kind == value)
                    return;

                _kind = value;
                _revision++;
            }
        }

        public ShimmerProgressionFilter? Progression
        {
            get => _progression;
            set
            {
                ValidateProgression(value);

                if (_progression == value)
                    return;

                _progression = value;
                _revision++;
            }
        }

        public ChecklistCompletionFilter CompletionFilter
        {
            get => _completionFilter;
            set
            {
                ValidateCompletion(value);

                if (_completionFilter == value)
                    return;

                _completionFilter = value;
                _revision++;
            }
        }

        public ChecklistResearchFilter ResearchFilter
        {
            get => _researchFilter;
            set
            {
                ValidateResearch(value);

                if (_researchFilter == value)
                    return;

                _researchFilter = value;
                _revision++;
            }
        }

        public long Revision => _revision;

        public bool IsActive => ActiveFilterCount > 0;

        public int ActiveFilterCount =>
            (_kind.HasValue ? 1 : 0) +
            (_progression.HasValue ? 1 : 0) +
            (_completionFilter == ChecklistCompletionFilter.All ? 0 : 1) +
            (_researchFilter == ChecklistResearchFilter.All ? 0 : 1);

        public bool UsesChecklistState => _completionFilter != ChecklistCompletionFilter.All;

        public bool UsesJourneyResearchState => _researchFilter != ChecklistResearchFilter.All;

        public void Clear()
        {
            if (!IsActive)
                return;

            _kind = null;
            _progression = null;
            _completionFilter = ChecklistCompletionFilter.All;
            _researchFilter = ChecklistResearchFilter.All;
            _revision++;
        }

        private static void ValidateKind(ShimmerTransformationKind? kind)
        {
            if (!kind.HasValue)
                return;

            if (kind.Value != ShimmerTransformationKind.Direct && kind.Value != ShimmerTransformationKind.Decraft)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Shimmer transformation kind.");
            }
        }

        private static void ValidateProgression(ShimmerProgressionFilter? progression)
        {
            if (!progression.HasValue)
                return;

            if (progression.Value != ShimmerProgressionFilter.Unlocked &&
                progression.Value != ShimmerProgressionFilter.Locked)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(progression),
                    progression,
                    "Unsupported Shimmer progression filter.");
            }
        }

        private static void ValidateCompletion(ChecklistCompletionFilter completion)
        {
            if (completion != ChecklistCompletionFilter.All &&
                completion != ChecklistCompletionFilter.Missing &&
                completion != ChecklistCompletionFilter.Found)
            {
                throw new ArgumentOutOfRangeException(nameof(completion), completion, "Unsupported completion filter.");
            }
        }

        private static void ValidateResearch(ChecklistResearchFilter research)
        {
            if (research != ChecklistResearchFilter.All &&
                research != ChecklistResearchFilter.Researched &&
                research != ChecklistResearchFilter.Unresearched)
            {
                throw new ArgumentOutOfRangeException(nameof(research), research, "Unsupported research filter.");
            }
        }
    }
}