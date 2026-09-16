using System;
using System.Collections.Generic;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Filtering
{
    internal sealed class ChecklistFilterModel
    {
        private const int QuestRarity = -11;
        private const int ExpertRarity = -12;
        private const int MasterRarity = -13;

        private static readonly IReadOnlyList<ChecklistSortMode> _commonSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _weaponSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.Damage
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _armorSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.Defense
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _toolsSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.PickPower,
            ChecklistSortMode.AxePower,
            ChecklistSortMode.HammerPower,
            ChecklistSortMode.FishingPower
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _pickaxeSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.PickPower
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _axeSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.AxePower
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _hammerSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.HammerPower
        ];

        private static readonly IReadOnlyList<ChecklistSortMode> _fishingSortModes =
        [
            ChecklistSortMode.Native,
            ChecklistSortMode.ItemId,
            ChecklistSortMode.Name,
            ChecklistSortMode.Value,
            ChecklistSortMode.Rarity,
            ChecklistSortMode.FishingPower
        ];

        private readonly ItemCatalog _catalog;
        private readonly ChecklistState _checklistState;
        private readonly CraftingAvailabilityState _craftingAvailabilityState;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly MerchantSourceIndex _merchantSourceIndex;
        private readonly HashSet<ItemNavigationNodeId> _nodesWithOtherItems;
        private readonly NpcLootIndex _npcLootIndex;
        private readonly RecipeIndex _recipeIndex;
        private ChecklistCompletionFilter _completionFilter = ChecklistCompletionFilter.All;
        private ChecklistCraftingFilter _craftingFilter = ChecklistCraftingFilter.All;
        private bool _criteriaChanged = true;
        private long _filteredCraftingRevision = -1;
        private IReadOnlyList<ItemCatalogEntry> _filteredItems = [];
        private long _filteredItemTextRevision = -1;
        private long _filteredResearchRevision = -1;
        private long _filteredStateRevision = -1;
        private ChecklistNavigationFilter _navigationFilter = ChecklistNavigationFilter.AllItems;
        private bool _npcDropsOnly;
        private bool _purchasableOnly;
        private ChecklistResearchFilter _researchFilter = ChecklistResearchFilter.All;
        private int _scopeFoundCount;
        private int _scopeTotalCount;
        private bool _searchDescriptions;
        private string _searchQuery = string.Empty;
        private ChecklistSortDirection _sortDirection = ChecklistSortDirection.Ascending;
        private ChecklistSortMode _sortMode = ChecklistSortMode.Native;
        private ChecklistTaxonomyFacetSelection _taxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None;

        public ChecklistFilterModel(
            ItemCatalog catalog,
            ChecklistState checklistState,
            ItemTextIndex itemTextIndex,
            JourneyResearchState journeyResearchState = null,
            RecipeIndex recipeIndex = null,
            CraftingAvailabilityState craftingAvailabilityState = null,
            MerchantSourceIndex merchantSourceIndex = null,
            NpcLootIndex npcLootIndex = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _journeyResearchState = journeyResearchState;
            _recipeIndex = recipeIndex;
            _craftingAvailabilityState = craftingAvailabilityState;
            _merchantSourceIndex = merchantSourceIndex;
            _npcLootIndex = npcLootIndex;
            _nodesWithOtherItems = CalculateNodesWithOtherItems(_catalog);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                string normalizedQuery = NormalizeSearchQuery(value);

                if (string.Equals(_searchQuery, normalizedQuery, StringComparison.Ordinal))
                    return;

                _searchQuery = normalizedQuery;
                _criteriaChanged = true;
            }
        }

        public bool SearchDescriptions
        {
            get => _searchDescriptions;
            set
            {
                if (_searchDescriptions == value)
                    return;

                _searchDescriptions = value;
                _criteriaChanged = true;
            }
        }

        public ChecklistCompletionFilter CompletionFilter
        {
            get => _completionFilter;
            set
            {
                if (!IsValidCompletionFilter(value))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported completion filter.");
                }

                if (_completionFilter == value)
                    return;

                _completionFilter = value;
                _criteriaChanged = true;
            }
        }

        public ChecklistResearchFilter ResearchFilter
        {
            get => _researchFilter;
            set
            {
                if (!IsValidResearchFilter(value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported research filter.");

                if (value != ChecklistResearchFilter.All && _journeyResearchState == null)
                {
                    throw new InvalidOperationException(
                        "A Journey research filter requires an available Journey research state.");
                }

                if (_researchFilter == value)
                    return;

                _researchFilter = value;
                _criteriaChanged = true;
            }
        }

        public ChecklistCraftingFilter CraftingFilter
        {
            get => _craftingFilter;
            set
            {
                if (!IsValidCraftingFilter(value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported crafting filter.");

                if (value != ChecklistCraftingFilter.All &&
                    (_recipeIndex == null || _craftingAvailabilityState == null))
                {
                    throw new InvalidOperationException(
                        "A crafting filter requires an available recipe index and crafting availability state.");
                }

                if (_craftingFilter == value)
                    return;

                _craftingFilter = value;
                _criteriaChanged = true;
            }
        }

        public bool NpcDropsOnly
        {
            get => _npcDropsOnly;
            set
            {
                if (value && _npcLootIndex == null)
                {
                    throw new InvalidOperationException("The NPC drops filter requires an available NPC loot index.");
                }

                if (_npcDropsOnly == value)
                    return;

                _npcDropsOnly = value;
                _criteriaChanged = true;
            }
        }

        public bool PurchasableOnly
        {
            get => _purchasableOnly;
            set
            {
                if (value && _merchantSourceIndex == null)
                {
                    throw new InvalidOperationException(
                        "The Purchasable filter requires an available merchant source index.");
                }

                if (_purchasableOnly == value)
                    return;

                _purchasableOnly = value;
                _criteriaChanged = true;
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

                if (!IsSortModeAvailable(_sortMode, _navigationFilter))
                    _sortMode = ChecklistSortMode.Native;

                _criteriaChanged = true;
            }
        }

        public ChecklistTaxonomyFacetSelection TaxonomyFacetSelection
        {
            get => _taxonomyFacetSelection;
            set
            {
                if (_taxonomyFacetSelection == value)
                    return;

                _taxonomyFacetSelection = value;
                _criteriaChanged = true;
            }
        }

        public IReadOnlyList<ChecklistSortMode> AvailableSortModes => GetAvailableSortModes(_navigationFilter);

        public ChecklistSortMode SortMode
        {
            get => _sortMode;
            set
            {
                if (!IsValidSortMode(value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Unsupported checklist sort mode.");

                if (!IsSortModeAvailable(value, _navigationFilter))
                {
                    throw new InvalidOperationException(
                        $"Sort mode '{value}' is not available for navigation scope '{_navigationFilter}'.");
                }

                if (_sortMode == value)
                    return;

                _sortMode = value;
                _criteriaChanged = true;
            }
        }

        public ChecklistSortDirection SortDirection
        {
            get => _sortDirection;
            set
            {
                if (!IsValidSortDirection(value))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        "Unsupported checklist sort direction.");
                }

                if (_sortDirection == value)
                    return;

                _sortDirection = value;
                _criteriaChanged = true;
            }
        }

        public int ScopeFoundCount
        {
            get
            {
                RefreshIfNeeded();

                return _scopeFoundCount;
            }
        }

        public int ScopeTotalCount
        {
            get
            {
                RefreshIfNeeded();

                return _scopeTotalCount;
            }
        }

        public double ScopeCompletionRatio
        {
            get
            {
                RefreshIfNeeded();

                return _scopeTotalCount == 0 ? 0.0 : (double)_scopeFoundCount / _scopeTotalCount;
            }
        }

        public IReadOnlyList<ItemCatalogEntry> Items
        {
            get
            {
                RefreshIfNeeded();

                return _filteredItems;
            }
        }

        public bool HasOtherItems(ItemNavigationNodeId nodeId)
        {
            ItemTaxonomyDefinitions.GetNavigationNode(nodeId);

            return _nodesWithOtherItems.Contains(nodeId);
        }

        private void RefreshIfNeeded()
        {
            long stateRevision = _checklistState.Revision;
            long researchRevision = _journeyResearchState?.Revision ?? -1;
            long craftingRevision = _craftingFilter == ChecklistCraftingFilter.CraftableNow
                ? _craftingAvailabilityState?.Revision ?? -1
                : -1;
            long itemTextRevision = _itemTextIndex.Revision;

            if (!_criteriaChanged &&
                _filteredStateRevision == stateRevision &&
                _filteredResearchRevision == researchRevision &&
                _filteredCraftingRevision == craftingRevision &&
                _filteredItemTextRevision == itemTextRevision)
            {
                return;
            }

            var filteredItems = new List<ItemCatalogEntry>(_catalog.Count);

            var scopeFoundCount = 0;
            var scopeTotalCount = 0;

            foreach (ItemCatalogEntry entry in _catalog.Items)
            {
                if (!MatchesSearch(entry))
                    continue;

                if (!MatchesNavigation(entry))
                    continue;

                if (!MatchesTaxonomyFacets(entry))
                    continue;

                scopeTotalCount++;

                bool isFound = _checklistState.IsFound(entry.Id);

                if (isFound)
                    scopeFoundCount++;

                if (!MatchesCompletion(isFound))
                    continue;

                if (!MatchesResearch(entry))
                    continue;

                if (!MatchesCrafting(entry))
                    continue;

                if (!MatchesNpcDrops(entry))
                    continue;

                if (!MatchesPurchasable(entry))
                    continue;

                filteredItems.Add(entry);
            }

            ApplySorting(filteredItems);

            _filteredItems = filteredItems.AsReadOnly();
            _scopeFoundCount = scopeFoundCount;
            _scopeTotalCount = scopeTotalCount;
            _filteredStateRevision = stateRevision;
            _filteredResearchRevision = researchRevision;
            _filteredCraftingRevision = craftingRevision;
            _filteredItemTextRevision = itemTextRevision;
            _criteriaChanged = false;
        }

        private bool MatchesSearch(ItemCatalogEntry entry)
        {
            if (_searchQuery.Length == 0)
                return true;

            if (_itemTextIndex.GetName(entry.Id).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return _searchDescriptions && _itemTextIndex.MatchesDescription(entry.Id, _searchQuery);
        }

        private bool MatchesNavigation(ItemCatalogEntry entry)
        {
            if (_navigationFilter.IsOther)
                return ItemTaxonomyDefinitions.MatchesOther(entry, _navigationFilter.NodeId);

            if (_navigationFilter.IsNode)
                return ItemTaxonomyDefinitions.MatchesNavigationNode(entry, _navigationFilter.NodeId);

            throw new InvalidOperationException("Unsupported navigation filter state.");
        }

        private bool MatchesTaxonomyFacets(ItemCatalogEntry entry)
        {
            return ItemTaxonomyDefinitions.MatchesFacets(
                entry,
                _taxonomyFacetSelection.IncludedFacets,
                _taxonomyFacetSelection.ExcludedFacets);
        }

        private bool MatchesCompletion(bool isFound)
        {
            switch (_completionFilter)
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

        private bool MatchesResearch(ItemCatalogEntry entry)
        {
            switch (_researchFilter)
            {
                case ChecklistResearchFilter.All:
                    return true;

                case ChecklistResearchFilter.Researched:
                    return _journeyResearchState != null && _journeyResearchState.IsFullyResearched(entry.Id);

                case ChecklistResearchFilter.Unresearched:
                    return _journeyResearchState != null && _journeyResearchState.IsUnresearched(entry.Id);

                default:
                    throw new InvalidOperationException("Unsupported research filter.");
            }
        }

        private bool MatchesCrafting(ItemCatalogEntry entry)
        {
            switch (_craftingFilter)
            {
                case ChecklistCraftingFilter.All:
                    return true;

                case ChecklistCraftingFilter.HasRecipe:
                    return _recipeIndex.HasRecipeProducing(entry.Id);

                case ChecklistCraftingFilter.CraftableNow:
                    return ItemCraftingAvailability.IsCraftableNow(entry.Id, _recipeIndex, _craftingAvailabilityState);

                default:
                    throw new InvalidOperationException("Unsupported crafting filter.");
            }
        }

        private bool MatchesNpcDrops(ItemCatalogEntry entry)
        {
            return !_npcDropsOnly || (_npcLootIndex != null && _npcLootIndex.HasNpcSourceForItem(entry.Id));
        }

        private bool MatchesPurchasable(ItemCatalogEntry entry)
        {
            return !_purchasableOnly || (_merchantSourceIndex != null && _merchantSourceIndex.ContainsItem(entry.Id));
        }

        private void ApplySorting(List<ItemCatalogEntry> items)
        {
            switch (_sortMode)
            {
                case ChecklistSortMode.Native:
                    items.Sort(CompareByNativeOrderWithDirection);

                    return;

                case ChecklistSortMode.ItemId:
                    items.Sort(CompareByItemIdWithDirection);

                    return;

                case ChecklistSortMode.Name:
                    items.Sort(CompareByNameWithDirection);

                    return;

                case ChecklistSortMode.Value:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.Value,
                        right.SortMetrics.Value,
                        left,
                        right));

                    return;

                case ChecklistSortMode.Rarity:
                    items.Sort((left, right) => CompareMetric(
                        GetRarityRank(left.SortMetrics.Rarity),
                        GetRarityRank(right.SortMetrics.Rarity),
                        left,
                        right));

                    return;

                case ChecklistSortMode.Damage:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.Damage,
                        right.SortMetrics.Damage,
                        left,
                        right));

                    return;

                case ChecklistSortMode.Defense:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.Defense,
                        right.SortMetrics.Defense,
                        left,
                        right));

                    return;

                case ChecklistSortMode.PickPower:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.PickPower,
                        right.SortMetrics.PickPower,
                        left,
                        right));

                    return;

                case ChecklistSortMode.AxePower:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.AxePower,
                        right.SortMetrics.AxePower,
                        left,
                        right));

                    return;

                case ChecklistSortMode.HammerPower:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.HammerPower,
                        right.SortMetrics.HammerPower,
                        left,
                        right));

                    return;

                case ChecklistSortMode.FishingPower:
                    items.Sort((left, right) => CompareMetric(
                        left.SortMetrics.FishingPower,
                        right.SortMetrics.FishingPower,
                        left,
                        right));

                    return;

                default:
                    throw new InvalidOperationException("Unsupported checklist sort mode.");
            }
        }

        private static IReadOnlyList<ChecklistSortMode> GetAvailableSortModes(
            ChecklistNavigationFilter navigationFilter)
        {
            ItemNavigationNodeId nodeId = navigationFilter.NodeId;

            if (IsNavigationNodeWithin(nodeId, ItemNavigationNodeId.Weapons))
                return _weaponSortModes;

            if (IsNavigationNodeWithin(nodeId, ItemNavigationNodeId.Armor))
                return _armorSortModes;

            if (nodeId == ItemNavigationNodeId.Tools)
                return navigationFilter.IsOther ? _commonSortModes : _toolsSortModes;

            switch (nodeId)
            {
                case ItemNavigationNodeId.ToolsPickaxes:
                    return _pickaxeSortModes;

                case ItemNavigationNodeId.ToolsAxes:
                    return _axeSortModes;

                case ItemNavigationNodeId.ToolsHammers:
                    return _hammerSortModes;

                case ItemNavigationNodeId.ToolsFishingRods:
                    return _fishingSortModes;

                default:
                    return _commonSortModes;
            }
        }

        private static bool IsSortModeAvailable(ChecklistSortMode sortMode, ChecklistNavigationFilter navigationFilter)
        {
            IReadOnlyList<ChecklistSortMode> availableSortModes = GetAvailableSortModes(navigationFilter);

            foreach (ChecklistSortMode availableSortMode in availableSortModes)
            {
                if (availableSortMode == sortMode)
                    return true;
            }

            return false;
        }

        private static bool IsNavigationNodeWithin(ItemNavigationNodeId nodeId, ItemNavigationNodeId ancestorNodeId)
        {
            while (nodeId != ItemNavigationNodeId.None)
            {
                if (nodeId == ancestorNodeId)
                    return true;

                nodeId = ItemTaxonomyDefinitions.GetNavigationNode(nodeId).ParentId;
            }

            return false;
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
                    $"Navigation node '{navigationFilter.NodeId}' does not contain an Other residual in this catalog.");
            }
        }

        private static HashSet<ItemNavigationNodeId> CalculateNodesWithOtherItems(ItemCatalog catalog)
        {
            var result = new HashSet<ItemNavigationNodeId>();

            foreach (ItemNavigationNodeDefinition node in ItemTaxonomyDefinitions.NavigationNodes)
            {
                if (!ItemTaxonomyDefinitions.HasChildren(node.Id))
                    continue;

                if (ItemTaxonomyDefinitions.HasOtherItems(catalog, node.Id))
                    result.Add(node.Id);
            }

            return result;
        }

        private int CompareByNativeOrderWithDirection(ItemCatalogEntry left, ItemCatalogEntry right)
        {
            return _sortDirection == ChecklistSortDirection.Ascending
                ? CompareByNativeOrder(left, right)
                : CompareByNativeOrder(right, left);
        }

        private int CompareByItemIdWithDirection(ItemCatalogEntry left, ItemCatalogEntry right)
        {
            return _sortDirection == ChecklistSortDirection.Ascending
                ? left.Id.CompareTo(right.Id)
                : right.Id.CompareTo(left.Id);
        }

        private int CompareByNameWithDirection(ItemCatalogEntry left, ItemCatalogEntry right)
        {
            int nameComparison = _sortDirection == ChecklistSortDirection.Ascending
                ? StringComparer.OrdinalIgnoreCase.Compare(
                    _itemTextIndex.GetName(left.Id),
                    _itemTextIndex.GetName(right.Id))
                : StringComparer.OrdinalIgnoreCase.Compare(
                    _itemTextIndex.GetName(right.Id),
                    _itemTextIndex.GetName(left.Id));

            return nameComparison != 0 ? nameComparison : left.Id.CompareTo(right.Id);
        }

        private int CompareMetric(int leftMetric, int rightMetric, ItemCatalogEntry left, ItemCatalogEntry right)
        {
            int metricComparison = _sortDirection == ChecklistSortDirection.Ascending
                ? leftMetric.CompareTo(rightMetric)
                : rightMetric.CompareTo(leftMetric);

            return metricComparison != 0 ? metricComparison : CompareByNativeOrder(left, right);
        }

        private int CompareByNativeOrder(ItemCatalogEntry left, ItemCatalogEntry right)
        {
            int groupComparison = left.NativeSortGroup.CompareTo(right.NativeSortGroup);

            if (groupComparison != 0)
                return groupComparison;

            int orderComparison = left.NativeSortOrder.CompareTo(right.NativeSortOrder);

            if (orderComparison != 0)
                return orderComparison;

            int nameComparison = string.Compare(
                _itemTextIndex.GetName(left.Id),
                _itemTextIndex.GetName(right.Id),
                StringComparison.Ordinal);

            return nameComparison != 0 ? nameComparison : left.Id.CompareTo(right.Id);
        }

        private static int GetRarityRank(int rarity)
        {
            switch (rarity)
            {
                case MasterRarity:
                    return int.MaxValue;

                case ExpertRarity:
                    return int.MaxValue - 1;

                case QuestRarity:
                    return int.MaxValue - 2;

                default:
                    return rarity;
            }
        }

        private static string NormalizeSearchQuery(string searchQuery)
        {
            return searchQuery?.Trim() ?? string.Empty;
        }

        private static bool IsValidCompletionFilter(ChecklistCompletionFilter completionFilter)
        {
            return completionFilter == ChecklistCompletionFilter.All ||
                   completionFilter == ChecklistCompletionFilter.Missing ||
                   completionFilter == ChecklistCompletionFilter.Found;
        }

        private static bool IsValidResearchFilter(ChecklistResearchFilter researchFilter)
        {
            return researchFilter == ChecklistResearchFilter.All ||
                   researchFilter == ChecklistResearchFilter.Researched ||
                   researchFilter == ChecklistResearchFilter.Unresearched;
        }

        private static bool IsValidCraftingFilter(ChecklistCraftingFilter craftingFilter)
        {
            return craftingFilter == ChecklistCraftingFilter.All ||
                   craftingFilter == ChecklistCraftingFilter.HasRecipe ||
                   craftingFilter == ChecklistCraftingFilter.CraftableNow;
        }

        private static bool IsValidSortMode(ChecklistSortMode sortMode)
        {
            return sortMode == ChecklistSortMode.Native ||
                   sortMode == ChecklistSortMode.ItemId ||
                   sortMode == ChecklistSortMode.Name ||
                   sortMode == ChecklistSortMode.Value ||
                   sortMode == ChecklistSortMode.Rarity ||
                   sortMode == ChecklistSortMode.Damage ||
                   sortMode == ChecklistSortMode.Defense ||
                   sortMode == ChecklistSortMode.PickPower ||
                   sortMode == ChecklistSortMode.AxePower ||
                   sortMode == ChecklistSortMode.HammerPower ||
                   sortMode == ChecklistSortMode.FishingPower;
        }

        private static bool IsValidSortDirection(ChecklistSortDirection sortDirection)
        {
            return sortDirection == ChecklistSortDirection.Ascending ||
                   sortDirection == ChecklistSortDirection.Descending;
        }
    }
}