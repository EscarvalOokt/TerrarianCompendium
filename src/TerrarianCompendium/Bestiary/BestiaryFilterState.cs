using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class BestiaryFilterState
    {
        private readonly HashSet<int> _activeNativeFilterIds = new();
        private readonly HashSet<int> _validNativeFilterIds;
        private IReadOnlyCollection<int> _activeNativeFilterIdsSnapshot = Array.Empty<int>();
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

        public IReadOnlyCollection<int> ActiveNativeFilterIds => _activeNativeFilterIdsSnapshot;

        public int ActiveNativeFilterCount => _activeNativeFilterIds.Count;

        public int ActiveFilterCount =>
            ActiveNativeFilterCount +
            (_encounterFilter == BestiaryEncounterFilter.All ? 0 : 1) +
            (_hasStockOnly ? 1 : 0);

        public long Revision => _revision;

        public bool IsNativeFilterActive(int filterId)
        {
            ValidateNativeFilterId(filterId);
            return _activeNativeFilterIds.Contains(filterId);
        }

        public void ToggleNativeFilter(int filterId)
        {
            ValidateNativeFilterId(filterId);

            if (!_activeNativeFilterIds.Remove(filterId))
                _activeNativeFilterIds.Add(filterId);

            RefreshNativeFilterSnapshot();
            _revision++;
        }

        public void Clear()
        {
            if (_encounterFilter == BestiaryEncounterFilter.All && !_hasStockOnly && _activeNativeFilterIds.Count == 0)
            {
                return;
            }

            _encounterFilter = BestiaryEncounterFilter.All;
            _hasStockOnly = false;
            _activeNativeFilterIds.Clear();
            RefreshNativeFilterSnapshot();
            _revision++;
        }

        private void RefreshNativeFilterSnapshot()
        {
            if (_activeNativeFilterIds.Count == 0)
            {
                _activeNativeFilterIdsSnapshot = Array.Empty<int>();
                return;
            }

            var sorted = new List<int>(_activeNativeFilterIds);
            sorted.Sort();
            _activeNativeFilterIdsSnapshot = new ReadOnlyCollection<int>(sorted);
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