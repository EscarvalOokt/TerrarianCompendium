using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeBrowserModel(
        ItemCatalog catalog,
        ItemTextIndex itemTextIndex,
        RecipeIndex recipeIndex,
        RecipeFilterState filterState,
        ChecklistState checklistState,
        JourneyResearchState journeyResearchState,
        RecipeFavoriteState favoriteState,
        CraftingAvailabilityState craftingAvailabilityState)
    {
        private readonly ItemCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        private readonly CraftingAvailabilityState _craftingAvailabilityState = craftingAvailabilityState ??
            throw new ArgumentNullException(nameof(craftingAvailabilityState));

        private readonly RecipeFavoriteState _favoriteState =
            favoriteState ?? throw new ArgumentNullException(nameof(favoriteState));

        private readonly RecipeFilterState _filterState =
            filterState ?? throw new ArgumentNullException(nameof(filterState));

        private readonly ItemTextIndex _itemTextIndex =
            itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));

        private readonly JourneyResearchState _journeyResearchState = journeyResearchState;

        private readonly HashSet<ItemNavigationNodeId> _nodesWithOtherItems =
            CalculateNodesWithOtherItems(catalog, recipeIndex);

        private readonly RecipeIndex _recipeIndex = recipeIndex ?? throw new ArgumentNullException(nameof(recipeIndex));

        private int? _contextItemId;
        private IReadOnlyList<ItemCatalogEntry> _matchingItems;
        private ChecklistNavigationFilter _navigationFilter = ChecklistNavigationFilter.AllItems;
        private long _observedChecklistRevision = checklistState.Revision;
        private long _observedCraftingRevision = craftingAvailabilityState.Revision;
        private long _observedFavoriteRevision = favoriteState.Revision;
        private long _observedFilterRevision = filterState.Revision;
        private long _observedItemTextRevision = itemTextIndex.Revision;
        private long _observedResearchRevision = journeyResearchState?.Revision ?? -1;
        private string _searchQuery = string.Empty;

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
                SynchronizeFilters();
                _matchingItems ??= BuildMatchingItems();
                return _matchingItems;
            }
        }

        public bool IsItemAvailable(int itemId)
        {
            SynchronizeFilters();

            if (!_catalog.TryGet(itemId, out ItemCatalogEntry entry))
                return false;

            return MatchesNavigation(entry) && HasMatchingProducingRecipe(itemId);
        }

        public bool IsContextItemAvailable(int itemId)
        {
            if (!_catalog.TryGet(itemId, out _))
                return false;

            return _recipeIndex.HasRecipeProducing(itemId) || _recipeIndex.GetRecipesUsing(itemId).Count > 0;
        }

        public bool HasFavoriteRecipe(int itemId)
        {
            IReadOnlyList<RecipeCatalogEntry> recipes = _recipeIndex.GetRecipesProducing(itemId);

            foreach (RecipeCatalogEntry recipe in recipes)
            {
                if (_favoriteState.IsFavorite(recipe.RuntimeIndex))
                    return true;
            }

            return false;
        }

        public bool HasOtherItems(ItemNavigationNodeId nodeId)
        {
            ItemTaxonomyDefinitions.GetNavigationNode(nodeId);
            return _nodesWithOtherItems.Contains(nodeId);
        }

        public bool SynchronizeFilters()
        {
            long filterRevision = _filterState.Revision;
            long checklistRevision = _filterState.UsesChecklistState
                ? _checklistState.Revision
                : _observedChecklistRevision;
            long researchRevision = _filterState.UsesJourneyResearchState
                ? _journeyResearchState?.Revision ?? -1
                : _observedResearchRevision;
            long craftingRevision = _filterState.UsesCraftingAvailabilityState
                ? _craftingAvailabilityState.Revision
                : _observedCraftingRevision;
            long favoriteRevision = _filterState.UsesRecipeFavoriteState
                ? _favoriteState.Revision
                : _observedFavoriteRevision;
            long itemTextRevision = _searchQuery.Length > 0 ? _itemTextIndex.Revision : _observedItemTextRevision;

            if (_observedFilterRevision == filterRevision &&
                _observedChecklistRevision == checklistRevision &&
                _observedResearchRevision == researchRevision &&
                _observedCraftingRevision == craftingRevision &&
                _observedFavoriteRevision == favoriteRevision &&
                _observedItemTextRevision == itemTextRevision)
            {
                return false;
            }

            _observedFilterRevision = filterRevision;
            _observedChecklistRevision = checklistRevision;
            _observedResearchRevision = researchRevision;
            _observedCraftingRevision = craftingRevision;
            _observedFavoriteRevision = favoriteRevision;
            _observedItemTextRevision = itemTextRevision;
            _matchingItems = null;
            return true;
        }

        private IReadOnlyList<ItemCatalogEntry> BuildMatchingItems()
        {
            HashSet<int> contextualResultItemIds = BuildContextualResultItemIds();
            var matches = new List<ItemCatalogEntry>();

            foreach (ItemCatalogEntry entry in _catalog.Items)
            {
                if (contextualResultItemIds != null && !contextualResultItemIds.Contains(entry.Id))
                    continue;

                if (!MatchesNavigation(entry))
                    continue;

                if (_searchQuery.Length > 0 &&
                    _itemTextIndex.GetName(entry.Id).IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!HasMatchingProducingRecipe(entry.Id))
                    continue;

                matches.Add(entry);
            }

            return new ReadOnlyCollection<ItemCatalogEntry>(matches);
        }

        private HashSet<int> BuildContextualResultItemIds()
        {
            if (!_contextItemId.HasValue)
                return null;

            IReadOnlyList<RecipeCatalogEntry> recipes = _recipeIndex.GetRecipesUsing(_contextItemId.Value);
            var resultItemIds = new HashSet<int>();

            foreach (RecipeCatalogEntry recipe in recipes)
            {
                if (_filterState.IsActive &&
                    !RecipeFilterMatcher.Matches(
                        recipe,
                        _filterState,
                        _checklistState,
                        _journeyResearchState,
                        _favoriteState,
                        _craftingAvailabilityState))
                {
                    continue;
                }

                resultItemIds.Add(recipe.ResultItemId);
            }

            return resultItemIds;
        }

        private bool MatchesNavigation(ItemCatalogEntry entry)
        {
            if (_navigationFilter.IsOther)
                return ItemTaxonomyDefinitions.MatchesOther(entry, _navigationFilter.NodeId);

            if (_navigationFilter.IsNode)
                return ItemTaxonomyDefinitions.MatchesNavigationNode(entry, _navigationFilter.NodeId);

            throw new InvalidOperationException("Unsupported navigation filter state.");
        }

        private bool HasMatchingProducingRecipe(int itemId)
        {
            IReadOnlyList<RecipeCatalogEntry> recipes = _recipeIndex.GetRecipesProducing(itemId);

            if (!_filterState.IsActive)
                return recipes.Count > 0;

            foreach (RecipeCatalogEntry recipe in recipes)
            {
                if (RecipeFilterMatcher.Matches(
                        recipe,
                        _filterState,
                        _checklistState,
                        _journeyResearchState,
                        _favoriteState,
                        _craftingAvailabilityState))
                {
                    return true;
                }
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
                    $"Navigation node '{navigationFilter.NodeId}' does not contain an Other residual in the recipe result-item universe.");
            }
        }

        private static HashSet<ItemNavigationNodeId> CalculateNodesWithOtherItems(
            ItemCatalog catalog,
            RecipeIndex recipeIndex)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (recipeIndex == null)
                throw new ArgumentNullException(nameof(recipeIndex));

            var result = new HashSet<ItemNavigationNodeId>();

            foreach (ItemNavigationNodeDefinition node in ItemTaxonomyDefinitions.NavigationNodes)
            {
                if (!ItemTaxonomyDefinitions.HasChildren(node.Id))
                    continue;

                foreach (ItemCatalogEntry entry in catalog.Items)
                {
                    if (!recipeIndex.HasRecipeProducing(entry.Id))
                        continue;

                    if (!ItemTaxonomyDefinitions.MatchesOther(entry, node.Id))
                        continue;

                    result.Add(node.Id);
                    break;
                }
            }

            return result;
        }
    }
}