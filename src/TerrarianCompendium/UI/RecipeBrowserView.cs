using System;
using Microsoft.Xna.Framework;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Recipes;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class RecipeBrowserView : UIElement
    {
        private const int SearchControlHeight = 30;
        private const int LayoutSpacing = 2;
        private const int CategoryContentGap = 4;
        private const int SearchMaxLength = 100;
        private const int ClearButtonWidth = 30;
        private const int FavoritesButtonWidth = 30;
        private const int FilterButtonWidth = 100;
        private const int TopBarGroupGap = 4;
        private const int TopBarControlGap = 2;
        private const int CategoryIconSize = 30;
        private const int SidebarGap = 6;
        private readonly ItemCategoryNavigationView _categoryNavigation;
        private readonly ItemCategoryIconSelector _categorySelector;
        private readonly VanillaTextButton _clearFiltersButton;
        private readonly VanillaTextButton _favoritesButton;
        private readonly VanillaTextButton _filterButton;
        private readonly VanillaPopover _filterPopover;
        private readonly RecipeFilterPopup _filterPopup;
        private readonly RecipeFilterState _filterState;
        private readonly VirtualItemGrid _itemGrid;
        private readonly VanillaScrollRegion _itemScroll;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly CompendiumLocalization _localization;
        private readonly RecipeBrowserModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly CompendiumSearchBar _searchInput;
        private long _localizationRevision = -1;
        private long _observedFilterRevision = -1;
        private long _observedItemTextRevision;
        private long _observedNavigationRevision = -1;

        public RecipeBrowserView(
            ItemCatalog catalog,
            ItemTextIndex itemTextIndex,
            RecipeCatalog recipeCatalog,
            RecipeIndex recipeIndex,
            RecipeStationDisplayIndex stationDisplayIndex,
            ChecklistState checklistState,
            JourneyResearchState journeyResearchState,
            RecipeFavoriteState favoriteState,
            CraftingAvailabilityState craftingAvailabilityState,
            RecipeFilterState filterState,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            ItemCatalog resolvedCatalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _observedItemTextRevision = _itemTextIndex.Revision;
            RecipeCatalog resolvedRecipeCatalog =
                recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));
            RecipeIndex resolvedRecipeIndex = recipeIndex ?? throw new ArgumentNullException(nameof(recipeIndex));
            RecipeStationDisplayIndex resolvedStationDisplayIndex =
                stationDisplayIndex ?? throw new ArgumentNullException(nameof(stationDisplayIndex));
            ChecklistState resolvedChecklistState =
                checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            RecipeFavoriteState resolvedFavoriteState =
                favoriteState ?? throw new ArgumentNullException(nameof(favoriteState));
            CraftingAvailabilityState resolvedCraftingAvailabilityState = craftingAvailabilityState ??
                                                                          throw new ArgumentNullException(
                                                                              nameof(craftingAvailabilityState));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _model = new RecipeBrowserModel(
                resolvedCatalog,
                _itemTextIndex,
                resolvedRecipeIndex,
                _filterState,
                resolvedChecklistState,
                journeyResearchState,
                resolvedFavoriteState,
                resolvedCraftingAvailabilityState);

            SetPadding(0f);

            _searchInput = new CompendiumSearchBar(RestoreFromVirtualKeyboard, _localization)
            {
                MaxInputLength = SearchMaxLength
            };
            _searchInput.OnSearchContentsChanged += OnSearchContentsChanged;
            Append(_searchInput);

            _favoritesButton = new VanillaTextButton("☆", ToggleFavorites)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2
            };
            Append(_favoritesButton);

            _clearFiltersButton = new VanillaTextButton("X", ClearFilters);
            Append(_clearFiltersButton);

            _filterButton = new VanillaTextButton(string.Empty, ToggleFilterPopover);
            Append(_filterButton);

            _filterPopup = new RecipeFilterPopup(
                resolvedCatalog,
                _itemTextIndex,
                resolvedRecipeCatalog,
                resolvedStationDisplayIndex,
                _filterState,
                journeyResearchState != null,
                _localization,
                SynchronizeFilters);
            _filterPopover = new VanillaPopover(this, _filterButton, _filterPopup);

            _categorySelector = new ItemCategoryIconSelector(CategoryIconSize, _localization, OnRootCategorySelected);
            Append(_categorySelector);

            _categoryNavigation = new ItemCategoryNavigationView(
                _localization,
                OnCategoryNavigationNodeSelected,
                OnCategoryOtherSelected);
            Append(_categoryNavigation);

            _itemScroll = new VanillaScrollRegion();
            Append(_itemScroll);

            _itemGrid = new VirtualItemGrid(
                _itemScroll,
                () => _model.MatchingItems,
                itemId => BrowserSelectionNavigation.ToggleRecipeResult(_navigationState, itemId),
                isMissing: itemId => !resolvedChecklistState.IsFound(itemId),
                isSelected: itemId =>
                    BrowserSelectionNavigation.IsRecipeResultSelected(_navigationState.CurrentDestination, itemId),
                cornerBadgeText: itemId => _model.HasFavoriteRecipe(itemId) ? "★" : string.Empty,
                emptyStateText: string.Empty);
            _itemScroll.Content.Append(_itemGrid);

            SynchronizeLocalization(force: true);

            SynchronizeCategoryNavigation();
            SynchronizeFilterControlState();
        }

        public bool IsWritingText => _searchInput.IsWritingText;

        public bool HasOpenTransientSurface => _filterPopover.IsOpen;

        public bool TryCloseTransientSurface()
        {
            if (!_filterPopover.IsOpen)
                return false;

            _filterPopover.Close();
            UpdateFilterControlsPresentation();
            return true;
        }

        public override void OnDeactivate()
        {
            _filterPopover.Close();

            if (_searchInput.IsWritingText)
                _searchInput.ToggleTakingText();

            UpdateFilterControlsPresentation();
            base.OnDeactivate();
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle inner = GetInnerDimensions();
            int width = Math.Max(0, (int)inner.Width);
            int height = Math.Max(0, (int)inner.Height);
            int favoritesWidth = Math.Min(FavoritesButtonWidth, width);
            int remainingAfterFavorites = Math.Max(0, width - favoritesWidth - TopBarGroupGap);
            int clearWidth = Math.Min(ClearButtonWidth, remainingAfterFavorites);
            int remainingAfterClear = Math.Max(0, remainingAfterFavorites - clearWidth - TopBarControlGap);
            int filterWidth = Math.Min(FilterButtonWidth, remainingAfterClear);
            int searchWidth = Math.Max(
                0,
                width - favoritesWidth - clearWidth - filterWidth - TopBarGroupGap - TopBarControlGap * 2);
            float controlTop = (SearchControlHeight - 24f) / 2f;

            _searchInput.Left.Set(0f, 0f);
            _searchInput.Top.Set(controlTop, 0f);
            _searchInput.Width.Set(searchWidth, 0f);
            _searchInput.Height.Set(24f, 0f);

            int favoritesX = searchWidth + TopBarGroupGap;
            _favoritesButton.Left.Set(favoritesX, 0f);
            _favoritesButton.Top.Set(controlTop, 0f);
            _favoritesButton.Width.Set(favoritesWidth, 0f);
            _favoritesButton.Height.Set(24f, 0f);

            int clearX = favoritesX + favoritesWidth + TopBarControlGap;
            _clearFiltersButton.Left.Set(clearX, 0f);
            _clearFiltersButton.Top.Set(controlTop, 0f);
            _clearFiltersButton.Width.Set(clearWidth, 0f);
            _clearFiltersButton.Height.Set(24f, 0f);

            int filterX = clearX + clearWidth + TopBarControlGap;
            _filterButton.Left.Set(filterX, 0f);
            _filterButton.Top.Set(controlTop, 0f);
            _filterButton.Width.Set(filterWidth, 0f);
            _filterButton.Height.Set(24f, 0f);

            int y = SearchControlHeight + LayoutSpacing;

            _categorySelector.Left.Set(0f, 0f);
            _categorySelector.Top.Set(y, 0f);
            _categorySelector.Width.Set(width, 0f);
            _categorySelector.Height.Set(CategoryIconSize, 0f);
            y += CategoryIconSize + CategoryContentGap;

            int contentHeight = Math.Max(0, height - y);
            int sidebarWidth = Math.Min(
                ItemCategoryNavigationView.PreferredWidth,
                Math.Max(0, width - SidebarGap - VirtualItemGrid.SlotSize));
            int gridWidth = Math.Max(0, width - sidebarWidth - SidebarGap);

            _categoryNavigation.Left.Set(0f, 0f);
            _categoryNavigation.Top.Set(y, 0f);
            _categoryNavigation.Width.Set(sidebarWidth, 0f);
            _categoryNavigation.Height.Set(contentHeight, 0f);

            _itemScroll.Left.Set(sidebarWidth + SidebarGap, 0f);
            _itemScroll.Top.Set(y, 0f);
            _itemScroll.Width.Set(gridWidth, 0f);
            _itemScroll.Height.Set(contentHeight, 0f);

            base.RecalculateChildren();
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            SynchronizeFilters();
            SynchronizeNavigation();
            SynchronizeCategoryPresentation();
            UpdateFilterControlsPresentation();
            base.Update(gameTime);
            SynchronizeFilters();
            SynchronizeNavigation();
            SynchronizeCategoryPresentation();
            UpdateFilterControlsPresentation();
        }

        private void OnSearchContentsChanged(string contents)
        {
            _model.SearchQuery = contents;
            _itemScroll.ResetScroll();
        }

        private void ClearFilters()
        {
            if (!_filterState.IsActive)
                return;

            _filterState.Clear();
            _filterPopover.Close();
            SynchronizeFilters();
            UpdateFilterControlsPresentation();
        }

        private void ToggleFavorites()
        {
            _filterState.FavoritesOnly = !_filterState.FavoritesOnly;
            SynchronizeFilters();
            UpdateFilterControlsPresentation();
        }

        private void ToggleFilterPopover()
        {
            _filterPopover.Toggle();
            UpdateFilterControlsPresentation();
        }

        private void SetNavigationFilter(ChecklistNavigationFilter navigationFilter)
        {
            if (_model.NavigationFilter == navigationFilter)
                return;

            _model.NavigationFilter = navigationFilter;
            _itemScroll.ResetScroll();
            SynchronizeCategoryPresentation();
            NormalizeInvalidRecipeQueryDestination();
        }

        private void OnCategoryNavigationNodeSelected(ItemNavigationNodeId nodeId)
        {
            SetNavigationFilter(ChecklistNavigationFilter.ForNode(nodeId));
        }

        private void OnCategoryOtherSelected(ItemNavigationNodeId parentNodeId)
        {
            SetNavigationFilter(ChecklistNavigationFilter.ForOther(parentNodeId));
        }

        private void OnRootCategorySelected(ItemNavigationNodeId nodeId)
        {
            ChecklistNavigationFilter current = _model.NavigationFilter;
            ChecklistNavigationFilter next = current.IsNode && current.NodeId == nodeId
                ? ChecklistNavigationFilter.AllItems
                : ChecklistNavigationFilter.ForNode(nodeId);

            SetNavigationFilter(next);
        }

        private void SynchronizeCategoryPresentation()
        {
            _categorySelector.ActiveRoot = GetActiveRootNode();
            SynchronizeCategoryNavigation();
        }

        private void SynchronizeCategoryNavigation()
        {
            ChecklistNavigationFilter navigation = _model.NavigationFilter;
            ItemNavigationNodeId? parentTarget = GetBackTarget(navigation);
            bool hasOther = navigation.IsNode &&
                            navigation.NodeId != ItemNavigationNodeId.AllItems &&
                            _model.HasOtherItems(navigation.NodeId);

            _categoryNavigation.Synchronize(navigation.NodeId, navigation.IsOther, parentTarget, hasOther);
        }

        private ItemNavigationNodeId? GetActiveRootNode()
        {
            ItemNavigationNodeId nodeId = _model.NavigationFilter.NodeId;

            if (nodeId == ItemNavigationNodeId.AllItems)
                return null;

            while (true)
            {
                ItemNavigationNodeDefinition node = ItemTaxonomyDefinitions.GetNavigationNode(nodeId);

                if (node.ParentId == ItemNavigationNodeId.AllItems)
                    return node.Id;

                if (node.ParentId == ItemNavigationNodeId.None)
                    return null;

                nodeId = node.ParentId;
            }
        }

        private static ItemNavigationNodeId? GetBackTarget(ChecklistNavigationFilter navigation)
        {
            if (navigation.IsOther)
                return navigation.NodeId;

            ItemNavigationNodeDefinition node = ItemTaxonomyDefinitions.GetNavigationNode(navigation.NodeId);
            return node.ParentId == ItemNavigationNodeId.None ? null : node.ParentId;
        }

        private void SynchronizeFilters()
        {
            bool modelChanged = _model.SynchronizeFilters();
            bool filterChanged = _observedFilterRevision != _filterState.Revision;
            bool itemTextChanged = _observedItemTextRevision != _itemTextIndex.Revision;

            if (!modelChanged && !filterChanged && !itemTextChanged)
                return;

            _observedFilterRevision = _filterState.Revision;
            _observedItemTextRevision = _itemTextIndex.Revision;

            if (modelChanged || filterChanged)
                _itemScroll.ResetScroll();

            SynchronizeFilterControlState();

            if (modelChanged || filterChanged)
                NormalizeInvalidRecipeQueryDestination();
        }

        private void SynchronizeFilterControlState()
        {
            _filterPopup.SynchronizeState();
            UpdateFilterControlsPresentation();
        }

        private void UpdateFilterControlsPresentation()
        {
            int popupFilterCount = _filterState.ActiveFilterCount - (_filterState.FavoritesOnly ? 1 : 0);
            _favoritesButton.Text = _filterState.FavoritesOnly ? "★" : "☆";
            _favoritesButton.IsActive = _filterState.FavoritesOnly;
            _favoritesButton.TooltipText = _localization.Get(
                _filterState.FavoritesOnly
                    ? CompendiumTextKeys.Recipes.ShowAll
                    : CompendiumTextKeys.Recipes.ShowFavorites);
            _clearFiltersButton.IsEnabled = _filterState.IsActive;
            _filterButton.Text = popupFilterCount == 0
                ? _localization.Get(CompendiumTextKeys.Common.Filters)
                : _localization.Format(CompendiumTextKeys.Common.FiltersCount, popupFilterCount);
            _filterButton.IsActive = _filterPopover.IsOpen || popupFilterCount > 0;
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _clearFiltersButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.ClearFilters);
            _filterButton.TooltipText = _localization.Get(CompendiumTextKeys.Recipes.FiltersTooltip);
            _itemGrid.EmptyStateText = _localization.Get(CompendiumTextKeys.Recipes.EmptyState);
            SynchronizeCategoryNavigation();
            UpdateFilterControlsPresentation();
            Recalculate();
        }

        private void NormalizeInvalidRecipeQueryDestination()
        {
            BrowserDestination destination = _navigationState.CurrentDestination;

            if (destination.Section != BrowserSection.Recipes ||
                !destination.HasRecipeQuery ||
                _model.IsItemAvailable(destination.RecipeQueryItemId))
            {
                return;
            }

            _navigationState.ReplaceCurrent(BrowserDestination.ForSection(BrowserSection.Recipes));
            _observedNavigationRevision = _navigationState.Revision;
        }

        private void SynchronizeNavigation()
        {
            if (_observedNavigationRevision == _navigationState.Revision)
                return;

            _observedNavigationRevision = _navigationState.Revision;
            BrowserDestination destination = _navigationState.CurrentDestination;

            if (destination.Section != BrowserSection.Recipes || !destination.HasRecipeQuery)
                return;

            if (_model.IsItemAvailable(destination.RecipeQueryItemId))
                return;

            _navigationState.ReplaceCurrent(BrowserDestination.ForSection(BrowserSection.Recipes));
            _observedNavigationRevision = _navigationState.Revision;
        }

        private void RestoreFromVirtualKeyboard()
        {
            UserInterface.ActiveInstance.GoBack();
        }
    }
}