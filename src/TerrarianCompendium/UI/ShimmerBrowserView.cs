using System;
using Microsoft.Xna.Framework;
using Terraria.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.Shimmer;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ShimmerBrowserView : UIElement
    {
        private const int SearchControlHeight = 30;
        private const int LayoutSpacing = 2;
        private const int CategoryContentGap = 4;
        private const int SearchMaxLength = 100;
        private const int ClearButtonWidth = 30;
        private const int FilterButtonWidth = 100;
        private const int TopBarControlGap = 2;
        private const int CategoryIconSize = 30;
        private const int SidebarGap = 6;

        private readonly ItemCategoryNavigationView _categoryNavigation;
        private readonly ItemCategoryIconSelector _categorySelector;
        private readonly ChecklistState _checklistState;
        private readonly VanillaTextButton _clearFiltersButton;
        private readonly VanillaTextButton _filterButton;
        private readonly VanillaPopover _filterPopover;
        private readonly ShimmerFilterPopup _filterPopup;
        private readonly ShimmerFilterState _filterState;
        private readonly VirtualItemGrid _itemGrid;
        private readonly VanillaScrollRegion _itemScroll;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly CompendiumLocalization _localization;
        private readonly ShimmerBrowserModel _model;
        private readonly BrowserNavigationState _navigationState;
        private readonly CompendiumSearchBar _searchInput;
        private long _localizationRevision = -1;
        private long _observedFilterRevision;
        private long _observedItemTextRevision;
        private long _observedNavigationRevision = -1;

        public ShimmerBrowserView(
            ShimmerBrowserModel model,
            ShimmerFilterState filterState,
            ItemTextIndex itemTextIndex,
            ChecklistState checklistState,
            JourneyResearchState journeyResearchState,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _observedItemTextRevision = _itemTextIndex.Revision;
            _observedFilterRevision = _filterState.Revision;

            SetPadding(0f);

            _searchInput = new CompendiumSearchBar(RestoreFromVirtualKeyboard, _localization)
            {
                MaxInputLength = SearchMaxLength
            };
            _searchInput.OnSearchContentsChanged += OnSearchContentsChanged;
            Append(_searchInput);

            _clearFiltersButton = new VanillaTextButton("X", ClearFilters);
            Append(_clearFiltersButton);

            _filterButton = new VanillaTextButton(string.Empty, ToggleFilterPopover);
            Append(_filterButton);

            _filterPopup = new ShimmerFilterPopup(
                _filterState,
                _localization,
                researchAvailable: journeyResearchState != null,
                filtersChanged: OnFiltersChanged);
            _filterPopover = new VanillaPopover(this, _filterButton, _filterPopup);

            _categorySelector = new ItemCategoryIconSelector(CategoryIconSize, _localization, OnRootCategorySelected);
            Append(_categorySelector);

            _categoryNavigation = new ItemCategoryNavigationView(
                _localization,
                OnCategoryNavigationNodeSelected,
                OnCategoryOtherSelected,
                journeyResearchState,
                NavigateToShimmerRoot);
            Append(_categoryNavigation);

            _itemScroll = new VanillaScrollRegion();
            Append(_itemScroll);

            _itemGrid = new VirtualItemGrid(
                _itemScroll,
                () => _model.MatchingItems,
                itemId => BrowserSelectionNavigation.ToggleShimmerResult(_navigationState, itemId),
                isMissing: itemId => !_checklistState.IsFound(itemId),
                isSelected: itemId => BrowserSelectionNavigation.IsShimmerResultSelected(
                    _navigationState.CurrentDestination,
                    itemId),
                journeyResearchState: journeyResearchState,
                emptyStateText: string.Empty);
            _itemScroll.Content.Append(_itemGrid);

            SynchronizeLocalization(force: true);
            SynchronizeNavigation();
            SynchronizeCategoryPresentation();
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
            int clearWidth = Math.Min(ClearButtonWidth, width);
            int remainingAfterClear = Math.Max(0, width - clearWidth - TopBarControlGap);
            int filterWidth = Math.Min(FilterButtonWidth, remainingAfterClear);
            int searchWidth = Math.Max(0, width - clearWidth - filterWidth - TopBarControlGap * 2);
            float controlTop = (SearchControlHeight - 24f) / 2f;

            _searchInput.Left.Set(0f, 0f);
            _searchInput.Top.Set(controlTop, 0f);
            _searchInput.Width.Set(searchWidth, 0f);
            _searchInput.Height.Set(24f, 0f);

            int clearX = searchWidth + TopBarControlGap;
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
            SynchronizeModel();
            SynchronizeNavigation();
            SynchronizeCategoryPresentation();
            SynchronizeFilterControlState();
            base.Update(gameTime);
            SynchronizeModel();
            SynchronizeNavigation();
            SynchronizeCategoryPresentation();
            SynchronizeFilterControlState();
            SynchronizeEmptyStatePresentation();
        }

        private void OnSearchContentsChanged(string contents)
        {
            _model.SearchQuery = contents;
            _itemScroll.ResetScroll();
        }

        private void OnFiltersChanged()
        {
            SynchronizeModel();
            _itemScroll.ResetScroll();
            SynchronizeFilterControlState();
        }

        private void ClearFilters()
        {
            if (!_filterState.IsActive)
                return;

            _filterPopup.ClearFilters();
            _filterPopover.Close();
            SynchronizeModel();
            UpdateFilterControlsPresentation();
        }

        private void ToggleFilterPopover()
        {
            _filterPopup.SynchronizeState();
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

        private void NavigateToShimmerRoot()
        {
            SetNavigationFilter(ChecklistNavigationFilter.AllItems);
            _navigationState.Navigate(BrowserDestination.ForSection(BrowserSection.Shimmer));
        }

        private void SynchronizeCategoryPresentation()
        {
            _categorySelector.ActiveRoot = GetActiveRootNode();
            ChecklistNavigationFilter navigation = _model.NavigationFilter;
            ItemNavigationNodeId? parentTarget = GetBackTarget(navigation);
            bool hasOther = navigation.IsNode &&
                            navigation.NodeId != ItemNavigationNodeId.AllItems &&
                            _model.HasOtherItems(navigation.NodeId);
            BrowserDestination destination = _navigationState.CurrentDestination;
            int? contextItemId = destination is { Section: BrowserSection.Shimmer, IsShimmerQuery: true }
                ? destination.ShimmerQueryItemId
                : null;
            bool contextItemIsMissing = contextItemId.HasValue && !_checklistState.IsFound(contextItemId.Value);

            _categoryNavigation.Synchronize(
                navigation.NodeId,
                navigation.IsOther,
                parentTarget,
                hasOther,
                contextItemId,
                contextItemIsMissing,
                _localization.Get(CompendiumTextKeys.Shimmer.ShowAll));
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

        private void SynchronizeModel()
        {
            bool modelChanged = _model.Synchronize();
            bool itemTextChanged = _observedItemTextRevision != _itemTextIndex.Revision;
            bool filterChanged = _observedFilterRevision != _filterState.Revision;

            if (!modelChanged && !itemTextChanged && !filterChanged)
                return;

            _observedItemTextRevision = _itemTextIndex.Revision;
            _observedFilterRevision = _filterState.Revision;

            if (modelChanged || filterChanged)
                _itemScroll.ResetScroll();
        }

        private void SynchronizeFilterControlState()
        {
            _filterPopup.SynchronizeState();
            UpdateFilterControlsPresentation();
        }

        private void UpdateFilterControlsPresentation()
        {
            int filterCount = _filterState.ActiveFilterCount;
            _clearFiltersButton.IsEnabled = _filterState.IsActive;
            _filterButton.Text = filterCount == 0
                ? _localization.Get(CompendiumTextKeys.Common.Filters)
                : _localization.Format(CompendiumTextKeys.Common.FiltersCount, filterCount);
            _filterButton.IsActive = _filterPopover.IsOpen || filterCount > 0;
        }

        private void SynchronizeEmptyStatePresentation()
        {
            var presentation = BrowserEmptyStatePresentation.Create(
                BrowserEmptyStateDomain.Shimmer,
                _model.SearchQuery.Length > 0,
                _model.NavigationFilter != ChecklistNavigationFilter.AllItems,
                _filterState.IsActive,
                _model.ContextItemId.HasValue);
            _itemGrid.EmptyStateText = presentation.Resolve(_localization);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _clearFiltersButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.ClearFilters);
            _filterButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.FiltersTooltip);
            SynchronizeEmptyStatePresentation();
            SynchronizeCategoryPresentation();
            UpdateFilterControlsPresentation();
            Recalculate();
        }

        private void SynchronizeNavigation()
        {
            if (_observedNavigationRevision == _navigationState.Revision)
                return;

            _observedNavigationRevision = _navigationState.Revision;
            BrowserDestination destination = _navigationState.CurrentDestination;

            if (destination.Section != BrowserSection.Shimmer || !destination.IsShimmerQuery)
            {
                SynchronizeContext(destination);
                return;
            }

            if (_model.IsContextItemAvailable(destination.ShimmerQueryItemId))
            {
                SynchronizeContext(destination);
                return;
            }

            _navigationState.ReplaceCurrent(BrowserDestination.ForSection(BrowserSection.Shimmer));
            SynchronizeContext(_navigationState.CurrentDestination);
            _observedNavigationRevision = _navigationState.Revision;
        }

        private void SynchronizeContext(BrowserDestination destination)
        {
            int? contextItemId = destination is { Section: BrowserSection.Shimmer, IsShimmerQuery: true }
                ? destination.ShimmerQueryItemId
                : null;

            if (_model.ContextItemId == contextItemId)
                return;

            _model.ContextItemId = contextItemId;
            _itemScroll.ResetScroll();
        }

        private void RestoreFromVirtualKeyboard()
        {
            UserInterface.ActiveInstance.GoBack();
        }
    }
}