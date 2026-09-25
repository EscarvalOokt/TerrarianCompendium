using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Shimmer
{
    internal sealed class ShimmerBrowserModel
    {
        private readonly ItemCatalog _catalog;
        private readonly ChecklistState _checklistState;
        private readonly ShimmerFilterState _filterState;
        private readonly Func<ShimmerTransformationVariant, bool> _isProgressionLocked;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly HashSet<ItemNavigationNodeId> _nodesWithOtherItems;
        private readonly Func<ShimmerRuntimeContextKey> _runtimeContextProvider;
        private readonly ShimmerTransformationIndex _shimmerIndex;

        private int? _contextItemId;
        private IReadOnlyList<ItemCatalogEntry> _matchingItems;
        private ChecklistNavigationFilter _navigationFilter = ChecklistNavigationFilter.AllItems;
        private long _observedChecklistRevision;
        private long _observedFilterRevision;
        private long _observedItemTextRevision;
        private long _observedResearchRevision;
        private ShimmerRuntimeContextKey _observedRuntimeContext;
        private string _searchQuery = string.Empty;

        public ShimmerBrowserModel(
            ItemCatalog catalog,
            ItemTextIndex itemTextIndex,
            ShimmerTransformationIndex shimmerIndex,
            ShimmerFilterState filterState,
            ChecklistState checklistState,
            JourneyResearchState journeyResearchState,
            Func<ShimmerTransformationVariant, bool> isProgressionLocked,
            Func<ShimmerRuntimeContextKey> runtimeContextProvider)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _shimmerIndex = shimmerIndex ?? throw new ArgumentNullException(nameof(shimmerIndex));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _journeyResearchState = journeyResearchState;
            _isProgressionLocked = isProgressionLocked ?? throw new ArgumentNullException(nameof(isProgressionLocked));
            _runtimeContextProvider = runtimeContextProvider ??
                                      throw new ArgumentNullException(nameof(runtimeContextProvider));
            _nodesWithOtherItems = CalculateNodesWithOtherItems(_catalog, _shimmerIndex);
            _observedItemTextRevision = _itemTextIndex.Revision;
            _observedFilterRevision = _filterState.Revision;
            _observedChecklistRevision = _checklistState.Revision;
            _observedResearchRevision = _journeyResearchState?.Revision ?? -1;
            _observedRuntimeContext = _runtimeContextProvider();
        }

        public int? ContextItemId
        {
            get => _contextItemId;
            set
            {
                if (value is <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value.Value,
                        "Context item ID must be greater than zero.");
                }

                if (_contextItemId == value)
                    return;

                _contextItemId = value;
                _matchingItems = null;
            }
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                string normalized = value?.Trim() ?? string.Empty;

                if (string.Equals(_searchQuery, normalized, StringComparison.Ordinal))
                    return;

                _searchQuery = normalized;
                _matchingItems = null;
            }
        }

        public ChecklistNavigationFilter NavigationFilter
        {
            get => _navigationFilter;
            set
            {
                ValidateNavigationFilter(value);

                if (_navigationFilter == value)
                    return;

                _navigationFilter = value;
                _matchingItems = null;
            }
        }

        public IReadOnlyList<ItemCatalogEntry> MatchingItems
        {
            get
            {
                Synchronize();
                _matchingItems ??= BuildMatchingItems();
                return _matchingItems;
            }
        }

        public bool IsItemAvailable(int itemId)
        {
            Synchronize();

            if (!_catalog.TryGet(itemId, out ItemCatalogEntry entry))
                return false;

            return MatchesNavigation(entry) && HasMatchingProducingVariant(itemId);
        }

        public bool IsContextItemAvailable(int itemId)
        {
            return _catalog.Contains(itemId) && _shimmerIndex.HasRelation(itemId);
        }

        public bool HasOtherItems(ItemNavigationNodeId nodeId)
        {
            ItemTaxonomyDefinitions.GetNavigationNode(nodeId);
            return _nodesWithOtherItems.Contains(nodeId);
        }

        public bool Synchronize()
        {
            long filterRevision = _filterState.Revision;
            long checklistRevision = _filterState.UsesChecklistState
                ? _checklistState.Revision
                : _observedChecklistRevision;
            long researchRevision = _filterState.UsesJourneyResearchState
                ? _journeyResearchState?.Revision ?? -1
                : _observedResearchRevision;
            long itemTextRevision = _searchQuery.Length > 0 ? _itemTextIndex.Revision : _observedItemTextRevision;
            ShimmerRuntimeContextKey runtimeContext = _runtimeContextProvider();
            bool runtimeChanged = _filterState.Progression.HasValue && _observedRuntimeContext != runtimeContext;

            bool changed = _observedFilterRevision != filterRevision ||
                           _observedChecklistRevision != checklistRevision ||
                           _observedResearchRevision != researchRevision ||
                           _observedItemTextRevision != itemTextRevision ||
                           runtimeChanged;

            _observedRuntimeContext = runtimeContext;

            if (!changed)
                return false;

            _observedFilterRevision = filterRevision;
            _observedChecklistRevision = checklistRevision;
            _observedResearchRevision = researchRevision;
            _observedItemTextRevision = itemTextRevision;
            _matchingItems = null;
            return true;
        }

        private IReadOnlyList<ItemCatalogEntry> BuildMatchingItems()
        {
            var matches = new List<ItemCatalogEntry>();

            foreach (ItemCatalogEntry entry in _catalog.Items)
            {
                if (!MatchesNavigation(entry))
                    continue;

                if (_searchQuery.Length > 0 &&
                    _itemTextIndex.GetName(entry.Id).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                bool hasMatch = _contextItemId.HasValue
                    ? HasMatchingContextVariant(_contextItemId.Value, entry.Id)
                    : HasMatchingProducingVariant(entry.Id);

                if (hasMatch)
                    matches.Add(entry);
            }

            return new ReadOnlyCollection<ItemCatalogEntry>(matches);
        }

        private bool HasMatchingProducingVariant(int resultItemId)
        {
            if (!ShimmerFilterMatcher.MatchesResult(resultItemId, _filterState, _checklistState, _journeyResearchState))
            {
                return false;
            }

            foreach (ShimmerTransformationVariant variant in _shimmerIndex.GetProducing(resultItemId))
            {
                if (ShimmerFilterMatcher.MatchesVariant(variant, _filterState, _isProgressionLocked))
                    return true;
            }

            return false;
        }

        private bool HasMatchingContextVariant(int contextItemId, int resultItemId)
        {
            if (!ShimmerFilterMatcher.MatchesResult(resultItemId, _filterState, _checklistState, _journeyResearchState))
            {
                return false;
            }

            foreach (ShimmerTransformationVariant variant in _shimmerIndex.GetUsing(contextItemId))
            {
                if (!variant.Produces(resultItemId))
                    continue;

                if (ShimmerFilterMatcher.MatchesVariant(variant, _filterState, _isProgressionLocked))
                    return true;
            }

            return false;
        }

        private bool MatchesNavigation(ItemCatalogEntry entry)
        {
            if (_navigationFilter.IsOther)
                return ItemTaxonomyDefinitions.MatchesOther(entry, _navigationFilter.NodeId);

            if (_navigationFilter.IsNode)
                return ItemTaxonomyDefinitions.MatchesNavigationNode(entry, _navigationFilter.NodeId);

            throw new InvalidOperationException("Unsupported navigation filter state.");
        }

        private void ValidateNavigationFilter(ChecklistNavigationFilter navigationFilter)
        {
            ItemTaxonomyDefinitions.GetNavigationNode(navigationFilter.NodeId);

            if (navigationFilter.IsNode)
                return;

            if (!navigationFilter.IsOther)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(navigationFilter),
                    navigationFilter,
                    "Unsupported navigation filter state.");
            }

            if (!ItemTaxonomyDefinitions.HasChildren(navigationFilter.NodeId))
            {
                throw new InvalidOperationException(
                    $"Navigation node '{navigationFilter.NodeId}' does not define child refinements.");
            }

            if (!_nodesWithOtherItems.Contains(navigationFilter.NodeId))
            {
                throw new InvalidOperationException(
                    $"Navigation node '{navigationFilter.NodeId}' does not contain an Other residual in the Shimmer result-item universe.");
            }
        }

        private static HashSet<ItemNavigationNodeId> CalculateNodesWithOtherItems(
            ItemCatalog catalog,
            ShimmerTransformationIndex shimmerIndex)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (shimmerIndex == null)
                throw new ArgumentNullException(nameof(shimmerIndex));

            var result = new HashSet<ItemNavigationNodeId>();

            foreach (ItemNavigationNodeDefinition node in ItemTaxonomyDefinitions.NavigationNodes)
            {
                if (!ItemTaxonomyDefinitions.HasChildren(node.Id))
                    continue;

                foreach (ItemCatalogEntry entry in catalog.Items)
                {
                    if (!shimmerIndex.HasProducing(entry.Id))
                        continue;

                    if (ItemTaxonomyDefinitions.MatchesOther(entry, node.Id))
                    {
                        result.Add(node.Id);
                        break;
                    }
                }
            }

            return result;
        }
    }
}