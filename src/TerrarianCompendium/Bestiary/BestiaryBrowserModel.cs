using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Bestiary
{
    internal readonly struct BestiaryEntryObservation(bool isEncountered, int stateToken)
        : IEquatable<BestiaryEntryObservation>
    {
        public bool IsEncountered { get; } = isEncountered;

        public int StateToken { get; } = stateToken;

        public bool Equals(BestiaryEntryObservation other)
        {
            return IsEncountered == other.IsEncountered && StateToken == other.StateToken;
        }

        public override bool Equals(object obj)
        {
            return obj is BestiaryEntryObservation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (IsEncountered.GetHashCode() * 397) ^ StateToken;
            }
        }
    }

    internal sealed class BestiaryBrowserModel
    {
        private static readonly IReadOnlyList<NpcCatalogEntry> _emptyEntries =
            new ReadOnlyCollection<NpcCatalogEntry>(new List<NpcCatalogEntry>());

        private readonly NpcCatalog _catalog;
        private readonly ChecklistState _checklistState;
        private readonly Func<NpcCatalogEntry, BestiaryEntryObservation> _entryObservationProvider;
        private readonly BestiaryFilterState _filterState;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly MerchantSourceIndex _merchantSourceIndex;
        private readonly Func<NpcCatalogEntry, int, bool> _metadataFilterMatcher;
        private readonly NpcLootIndex _npcLootIndex;
        private readonly BestiaryEntryObservation[] _observedEntries;
        private readonly Func<NpcCatalogEntry, string, bool> _searchMatcher;
        private readonly Action<List<NpcCatalogEntry>, BestiarySortMode, BestiarySortDirection> _sortEntries;

        private long _observedChecklistRevision;
        private long _observedFilterRevision;
        private long _observedResearchRevision;
        private int _overallEncounteredCount;
        private long _revision;
        private int _scopeEncounteredCount;
        private int _scopeTotalCount;
        private string _searchQuery = string.Empty;
        private BestiarySortDirection _sortDirection = BestiarySortDirection.Ascending;
        private BestiarySortMode _sortMode;
        private IReadOnlyList<NpcCatalogEntry> _visibleEntries = _emptyEntries;

        public BestiaryBrowserModel(
            NpcCatalog catalog,
            BestiaryFilterState filterState,
            VanillaBestiaryFilterCatalog filterCatalog,
            ChecklistState checklistState,
            NpcLootIndex npcLootIndex = null,
            JourneyResearchState journeyResearchState = null,
            MerchantSourceIndex merchantSourceIndex = null) : this(
            catalog,
            filterState,
            VanillaBestiaryNativeBridge.GetEntryObservation,
            VanillaBestiaryNativeBridge.CreateSearchMatcher(),
            CreateMetadataFilterMatcher(filterCatalog),
            VanillaBestiaryNativeBridge.SortEntries,
            checklistState,
            npcLootIndex,
            journeyResearchState,
            merchantSourceIndex)
        {
        }

        internal BestiaryBrowserModel(
            NpcCatalog catalog,
            BestiaryFilterState filterState,
            Func<NpcCatalogEntry, BestiaryEntryObservation> entryObservationProvider,
            Func<NpcCatalogEntry, string, bool> searchMatcher,
            Func<NpcCatalogEntry, int, bool> metadataFilterMatcher,
            Action<List<NpcCatalogEntry>, BestiarySortMode, BestiarySortDirection> sortEntries,
            ChecklistState checklistState,
            NpcLootIndex npcLootIndex = null,
            JourneyResearchState journeyResearchState = null,
            MerchantSourceIndex merchantSourceIndex = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _entryObservationProvider = entryObservationProvider ??
                                        throw new ArgumentNullException(nameof(entryObservationProvider));
            _searchMatcher = searchMatcher ?? throw new ArgumentNullException(nameof(searchMatcher));
            _metadataFilterMatcher = metadataFilterMatcher ??
                                     throw new ArgumentNullException(nameof(metadataFilterMatcher));
            _sortEntries = sortEntries ?? throw new ArgumentNullException(nameof(sortEntries));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _npcLootIndex = npcLootIndex;
            _journeyResearchState = journeyResearchState;
            _merchantSourceIndex = merchantSourceIndex;

            _observedEntries = new BestiaryEntryObservation[_catalog.Count];

            for (var index = 0; index < _catalog.Count; index++)
                _observedEntries[index] = _entryObservationProvider(_catalog.Entries[index]);

            NormalizeUnavailableDropCriterion();
            _observedFilterRevision = _filterState.Revision;
            _observedChecklistRevision = _checklistState.Revision;
            _observedResearchRevision = _journeyResearchState?.Revision ?? -1;
            RebuildProjection();
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                string normalized = NormalizeSearchQuery(value);

                if (string.Equals(_searchQuery, normalized, StringComparison.Ordinal))
                    return;

                _searchQuery = normalized;
                AdvanceRevisionAndRebuild();
            }
        }

        public BestiaryEncounterFilter EncounterFilter
        {
            get => _filterState.EncounterFilter;
            set
            {
                long before = _filterState.Revision;
                _filterState.EncounterFilter = value;

                if (_filterState.Revision != before)
                    SynchronizeState();
            }
        }

        public BestiarySortMode SortMode
        {
            get => _sortMode;
            set
            {
                ValidateSortMode(value);

                if (_sortMode == value)
                    return;

                _sortMode = value;
                AdvanceRevisionAndRebuild();
            }
        }

        public BestiarySortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                ValidateSortDirection(value);

                if (_sortDirection == value)
                    return;

                _sortDirection = value;
                AdvanceRevisionAndRebuild();
            }
        }

        public IReadOnlyList<NpcCatalogEntry> VisibleEntries => _visibleEntries;

        public int OverallEncounteredCount => _overallEncounteredCount;

        public int OverallTotalCount => _catalog.Count;

        public double OverallEncounteredRatio =>
            OverallTotalCount == 0 ? 0.0 : (double)OverallEncounteredCount / OverallTotalCount;

        public int ScopeEncounteredCount => _scopeEncounteredCount;

        public int ScopeTotalCount => _scopeTotalCount;

        public double ScopeEncounteredRatio =>
            ScopeTotalCount == 0 ? 0.0 : (double)ScopeEncounteredCount / ScopeTotalCount;

        public long Revision => _revision;

        public bool MerchantStockFilterAvailable => _merchantSourceIndex != null;

        public bool LootAwareFiltersAvailable => _npcLootIndex != null;

        public bool UnresearchedDropsFilterAvailable => _npcLootIndex != null && _journeyResearchState != null;

        private static Func<NpcCatalogEntry, int, bool> CreateMetadataFilterMatcher(
            VanillaBestiaryFilterCatalog filterCatalog)
        {
            if (filterCatalog == null)
                throw new ArgumentNullException(nameof(filterCatalog));

            return filterCatalog.Matches;
        }

        public bool SynchronizeState()
        {
            NormalizeUnavailableDropCriterion();

            long filterRevision = _filterState.Revision;
            long checklistRevision = _filterState.UsesChecklistState
                ? _checklistState.Revision
                : _observedChecklistRevision;
            long researchRevision = _filterState.UsesJourneyResearchState
                ? _journeyResearchState?.Revision ?? -1
                : _observedResearchRevision;
            bool changed = _observedFilterRevision != filterRevision ||
                           _observedChecklistRevision != checklistRevision ||
                           _observedResearchRevision != researchRevision;

            if (changed)
            {
                _observedFilterRevision = filterRevision;
                _observedChecklistRevision = checklistRevision;
                _observedResearchRevision = researchRevision;
            }

            for (var index = 0; index < _catalog.Count; index++)
            {
                BestiaryEntryObservation current = _entryObservationProvider(_catalog.Entries[index]);

                if (_observedEntries[index].Equals(current))
                    continue;

                _observedEntries[index] = current;
                changed = true;
            }

            if (!changed)
                return false;

            AdvanceRevisionAndRebuild();
            return true;
        }

        private void AdvanceRevisionAndRebuild()
        {
            _revision++;
            RebuildProjection();
        }

        private void RebuildProjection()
        {
            var visibleEntries = new List<NpcCatalogEntry>();
            var overallEncounteredCount = 0;
            var scopeEncounteredCount = 0;
            var scopeTotalCount = 0;
            bool hasSearch = _searchQuery.Length > 0;
            BestiaryFilterCriterion bestiaryCriterion = _filterState.BestiaryCriterion;
            BestiaryFilterCriterion dropCriterion = _filterState.DropCriterion;
            BestiaryEncounterFilter encounterFilter = _filterState.EncounterFilter;
            bool hasStockOnly = _filterState.HasStockOnly;

            for (var index = 0; index < _catalog.Count; index++)
            {
                NpcCatalogEntry entry = _catalog.Entries[index];
                BestiaryEntryObservation observation = _observedEntries[index];
                bool encountered = observation.IsEncountered;

                if (encountered)
                    overallEncounteredCount++;

                if (hasSearch && !_searchMatcher(entry, _searchQuery))
                    continue;

                if (!MatchesBestiaryCriterion(entry, bestiaryCriterion))
                    continue;

                if (!MatchesDropCriterion(entry, dropCriterion))
                    continue;

                scopeTotalCount++;

                if (encountered)
                    scopeEncounteredCount++;

                if (!MatchesEncounterFilter(encountered, encounterFilter))
                    continue;

                if (hasStockOnly &&
                    (_merchantSourceIndex == null || !_merchantSourceIndex.ContainsMerchant(entry.NetId)))
                {
                    continue;
                }

                visibleEntries.Add(entry);
            }

            _sortEntries(visibleEntries, _sortMode, _sortDirection);

            _overallEncounteredCount = overallEncounteredCount;
            _scopeEncounteredCount = scopeEncounteredCount;
            _scopeTotalCount = scopeTotalCount;
            _visibleEntries = visibleEntries.Count == 0
                ? _emptyEntries
                : new ReadOnlyCollection<NpcCatalogEntry>(visibleEntries);
        }

        private bool MatchesBestiaryCriterion(NpcCatalogEntry entry, BestiaryFilterCriterion criterion)
        {
            switch (criterion.Kind)
            {
                case BestiaryFilterCriterionKind.All:
                    return true;

                case BestiaryFilterCriterionKind.Native:
                    return _metadataFilterMatcher(entry, criterion.NativeFilterId);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(criterion),
                        criterion.Kind,
                        "Unsupported Bestiary criterion.");
            }
        }

        private bool MatchesDropCriterion(NpcCatalogEntry entry, BestiaryFilterCriterion criterion)
        {
            switch (criterion.Kind)
            {
                case BestiaryFilterCriterionKind.All:
                    return true;

                case BestiaryFilterCriterionKind.HasDrops:
                    return _npcLootIndex != null && _npcLootIndex.GetDropsForNpc(entry.NetId).Count > 0;

                case BestiaryFilterCriterionKind.HasMissingDrops:
                    return HasMissingDrop(entry.NetId);

                case BestiaryFilterCriterionKind.HasUnresearchedDrops:
                    return HasUnresearchedDrop(entry.NetId);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(criterion),
                        criterion.Kind,
                        "Unsupported drop criterion.");
            }
        }

        private bool HasMissingDrop(int npcNetId)
        {
            if (_npcLootIndex == null)
                return false;

            IReadOnlyList<NpcLootRelation> relations = _npcLootIndex.GetDropsForNpc(npcNetId);

            foreach (NpcLootRelation relation in relations)
            {
                if (!_checklistState.IsFound(relation.ItemId))
                    return true;
            }

            return false;
        }

        private bool HasUnresearchedDrop(int npcNetId)
        {
            if (_npcLootIndex == null || _journeyResearchState == null)
                return false;

            IReadOnlyList<NpcLootRelation> relations = _npcLootIndex.GetDropsForNpc(npcNetId);

            foreach (NpcLootRelation relation in relations)
            {
                if (_journeyResearchState.IsUnresearched(relation.ItemId))
                    return true;
            }

            return false;
        }

        private void NormalizeUnavailableDropCriterion()
        {
            BestiaryFilterCriterion criterion = _filterState.DropCriterion;
            bool unavailable;

            switch (criterion.Kind)
            {
                case BestiaryFilterCriterionKind.HasDrops:
                case BestiaryFilterCriterionKind.HasMissingDrops:
                    unavailable = _npcLootIndex == null;
                    break;

                case BestiaryFilterCriterionKind.HasUnresearchedDrops:
                    unavailable = _npcLootIndex == null || _journeyResearchState == null;
                    break;

                default:
                    unavailable = false;
                    break;
            }

            if (unavailable)
                _filterState.DropCriterion = BestiaryFilterCriterion.All;
        }

        private static string NormalizeSearchQuery(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool MatchesEncounterFilter(bool encountered, BestiaryEncounterFilter filter)
        {
            switch (filter)
            {
                case BestiaryEncounterFilter.All:
                    return true;
                case BestiaryEncounterFilter.Encountered:
                    return encountered;
                case BestiaryEncounterFilter.Unknown:
                    return !encountered;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(filter),
                        filter,
                        "Unsupported Bestiary encounter filter.");
            }
        }

        private static void ValidateSortDirection(BestiarySortDirection sortDirection)
        {
            if (sortDirection is BestiarySortDirection.Ascending or BestiarySortDirection.Descending)
                return;

            throw new ArgumentOutOfRangeException(
                nameof(sortDirection),
                sortDirection,
                "Unsupported Bestiary sort direction.");
        }

        private static void ValidateSortMode(BestiarySortMode sortMode)
        {
            if (sortMode is BestiarySortMode.BestiaryOrder
                or BestiarySortMode.Name
                or BestiarySortMode.Rarity
                or BestiarySortMode.Attack
                or BestiarySortMode.Defense
                or BestiarySortMode.Coins
                or BestiarySortMode.HitPoints
                or BestiarySortMode.NpcId)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(nameof(sortMode), sortMode, "Unsupported Bestiary sort mode.");
        }
    }
}