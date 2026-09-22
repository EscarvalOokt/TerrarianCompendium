using System;
using System.Collections.Generic;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class BestiaryFilterState
    {
        private readonly HashSet<int> _validNativeFilterIds;
        private BestiaryFilterCriterion _bestiaryCriterion;
        private BestiaryFilterCriterion _dropCriterion;
        private BestiaryEncounterFilter _encounterFilter;
        private bool _hasStockOnly;
        private long _revision;

        public BestiaryFilterState(IEnumerable<int> nativeFilterIds)
        {
            if (nativeFilterIds == null)
                throw new ArgumentNullException(nameof(nativeFilterIds));

            _validNativeFilterIds = new HashSet<int>();

            foreach (int filterId in nativeFilterIds)
            {
                if (!_validNativeFilterIds.Add(filterId))
                    throw new ArgumentException($"Duplicate Bestiary filter ID {filterId}.", nameof(nativeFilterIds));
            }
        }

        public BestiaryFilterCriterion BestiaryCriterion
        {
            get => _bestiaryCriterion;
            set
            {
                ValidateBestiaryCriterion(value);

                if (_bestiaryCriterion == value)
                    return;

                _bestiaryCriterion = value;
                _revision++;
            }
        }

        public BestiaryFilterCriterion DropCriterion
        {
            get => _dropCriterion;
            set
            {
                ValidateDropCriterion(value);

                if (_dropCriterion == value)
                    return;

                _dropCriterion = value;
                _revision++;
            }
        }

        public BestiaryEncounterFilter EncounterFilter
        {
            get => _encounterFilter;
            set
            {
                ValidateEncounterFilter(value);

                if (_encounterFilter == value)
                    return;

                _encounterFilter = value;
                _revision++;
            }
        }

        public bool HasStockOnly
        {
            get => _hasStockOnly;
            set
            {
                if (_hasStockOnly == value)
                    return;

                _hasStockOnly = value;
                _revision++;
            }
        }

        public int ActiveFilterCount =>
            (_bestiaryCriterion.IsAll ? 0 : 1) +
            (_dropCriterion.IsAll ? 0 : 1) +
            (_encounterFilter == BestiaryEncounterFilter.All ? 0 : 1) +
            (_hasStockOnly ? 1 : 0);

        public bool UsesChecklistState => _dropCriterion.Kind == BestiaryFilterCriterionKind.HasMissingDrops;

        public bool UsesJourneyResearchState => _dropCriterion.Kind == BestiaryFilterCriterionKind.HasUnresearchedDrops;

        public long Revision => _revision;

        public bool IsNativeFilterActive(int filterId)
        {
            ValidateNativeFilterId(filterId);
            return _bestiaryCriterion == BestiaryFilterCriterion.ForNative(filterId);
        }

        public void Clear()
        {
            if (_bestiaryCriterion.IsAll &&
                _dropCriterion.IsAll &&
                _encounterFilter == BestiaryEncounterFilter.All &&
                !_hasStockOnly)
            {
                return;
            }

            _bestiaryCriterion = BestiaryFilterCriterion.All;
            _dropCriterion = BestiaryFilterCriterion.All;
            _encounterFilter = BestiaryEncounterFilter.All;
            _hasStockOnly = false;
            _revision++;
        }

        private void ValidateBestiaryCriterion(BestiaryFilterCriterion criterion)
        {
            switch (criterion.Kind)
            {
                case BestiaryFilterCriterionKind.All:
                    return;

                case BestiaryFilterCriterionKind.Native:
                    ValidateNativeFilterId(criterion.NativeFilterId);
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(criterion),
                        criterion.Kind,
                        "Bestiary criterion must be All or a native Bestiary filter.");
            }
        }

        private static void ValidateDropCriterion(BestiaryFilterCriterion criterion)
        {
            switch (criterion.Kind)
            {
                case BestiaryFilterCriterionKind.All:
                case BestiaryFilterCriterionKind.HasDrops:
                case BestiaryFilterCriterionKind.HasMissingDrops:
                case BestiaryFilterCriterionKind.HasUnresearchedDrops:
                    return;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(criterion),
                        criterion.Kind,
                        "Drop criterion must be All or a loot-aware Bestiary filter.");
            }
        }

        private void ValidateNativeFilterId(int filterId)
        {
            if (!_validNativeFilterIds.Contains(filterId))
                throw new ArgumentOutOfRangeException(nameof(filterId), filterId, "Unknown Bestiary native filter ID.");
        }

        private static void ValidateEncounterFilter(BestiaryEncounterFilter filter)
        {
            if (filter is BestiaryEncounterFilter.All
                or BestiaryEncounterFilter.Encountered
                or BestiaryEncounterFilter.Unknown)
                return;

            throw new ArgumentOutOfRangeException(nameof(filter), filter, "Unsupported Bestiary encounter filter.");
        }
    }
}