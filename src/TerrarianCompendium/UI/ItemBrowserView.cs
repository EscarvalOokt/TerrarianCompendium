using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
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
    internal sealed class ItemBrowserView : UIElement
    {
        private const int ProgressHeight = 24;
        private const int ControlHeight = 24;
        private const int SearchControlHeight = 30;
        private const int SearchDescriptionToggleSize = ControlHeight;
        private const int SearchDescriptionToggleActiveBorderThickness = 2;
        private const int CategoryIconSize = 30;
        private const int LayoutSpacing = 2;
        private const int CategoryContentGap = 4;
        private const int ProgressGap = 6;
        private const int TopBarGroupGap = 4;
        private const int TopBarControlGap = 2;
        private const int ClearButtonWidth = 30;
        private const int FilterButtonWidth = 100;
        private const int SortControlWidth = 180;
        private const int SidebarGap = 6;
        private const int SearchMaxLength = 100;
        private readonly ItemCategoryNavigationView _categoryNavigation;

        private readonly ItemCategoryIconSelector _categorySelector;
        private readonly ChecklistState _checklistState;
        private readonly VanillaTextButton _clearFiltersButton;
        private readonly VanillaTextButton _filterButton;
        private readonly ChecklistFilterModel _filterModel;
        private readonly VanillaPopover _filterPopover;
        private readonly ItemFilterPopup _filterPopup;
        private readonly VirtualItemGrid _itemGrid;
        private readonly VanillaScrollRegion _itemScroll;
        private readonly CompendiumLocalization _localization;
        private readonly BrowserNavigationState _navigationState;
        private readonly SearchDescriptionToggleElement _searchDescriptionToggle;
        private readonly CompendiumSearchBar _searchInput;
        private readonly VanillaTextButton _sortButton;
        private readonly VanillaTextButton _sortDirectionButton;
        private readonly VanillaPopover _sortPopover;
        private readonly ItemSortPopup _sortPopup;
        private long _localizationRevision = -1;


        public ItemBrowserView(
            ItemCatalog catalog,
            ChecklistState checklistState,
            ItemTextIndex itemTextIndex,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization,
            JourneyResearchState journeyResearchState = null,
            RecipeIndex recipeIndex = null,
            CraftingAvailabilityState craftingAvailabilityState = null,
            MerchantSourceIndex merchantSourceIndex = null,
            NpcLootIndex npcLootIndex = null)
        {
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _navigationState = navigationState ?? throw new ArgumentNullException(nameof(navigationState));
            _filterModel = new ChecklistFilterModel(
                catalog ?? throw new ArgumentNullException(nameof(catalog)),
                checklistState,
                itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex)),
                journeyResearchState,
                recipeIndex,
                craftingAvailabilityState,
                merchantSourceIndex,
                npcLootIndex);
            bool hasJourneyResearch = journeyResearchState != null;
            bool hasCraftingFilters = recipeIndex != null && craftingAvailabilityState != null;
            bool hasNpcDropFilters = npcLootIndex != null;
            bool hasMerchantFilters = merchantSourceIndex != null;
            SetPadding(0f);

            _searchInput = new CompendiumSearchBar(() => UserInterface.ActiveInstance.GoBack(), _localization)
            {
                MaxInputLength = SearchMaxLength
            };
            _searchInput.OnSearchContentsChanged += OnSearchContentsChanged;
            Append(_searchInput);

            _searchDescriptionToggle = new SearchDescriptionToggleElement(
                _localization,
                () => _filterModel.SearchDescriptions,
                ToggleSearchDescriptions);
            Append(_searchDescriptionToggle);

            _clearFiltersButton = new VanillaTextButton("X", ClearFilters);
            Append(_clearFiltersButton);

            _filterButton = new VanillaTextButton(string.Empty, ToggleFilterPopover);
            Append(_filterButton);

            _filterPopup = new ItemFilterPopup(
                _filterModel,
                hasJourneyResearch,
                hasCraftingFilters,
                hasNpcDropFilters,
                hasMerchantFilters,
                _localization,
                OnFiltersChanged);
            _filterPopover = new VanillaPopover(this, _filterButton, _filterPopup);

            _sortPopup = new ItemSortPopup(_filterModel, _localization, OnSortModeChanged);
            _sortButton = new VanillaTextButton(_sortPopup.ButtonText, ToggleSortPopover);
            Append(_sortButton);
            _sortPopover = new VanillaPopover(this, _sortButton, _sortPopup);

            _sortDirectionButton = new VanillaTextButton("↑", ToggleSortDirection);
            Append(_sortDirectionButton);

            _categorySelector = new ItemCategoryIconSelector(CategoryIconSize, _localization, OnRootCategorySelected);
            Append(_categorySelector);

            _categoryNavigation = new ItemCategoryNavigationView(
                _localization,
                OnCategoryNavigationNodeSelected,
                OnCategoryOtherSelected,
                rootSelected: NavigateToItemsRoot);
            Append(_categoryNavigation);

            _itemScroll = new VanillaScrollRegion();
            Append(_itemScroll);

            _itemGrid = new VirtualItemGrid(
                _itemScroll,
                () => _filterModel.Items,
                itemId => BrowserSelectionNavigation.ToggleItem(_navigationState, itemId),
                isMissing: itemId => !_checklistState.IsFound(itemId),
                isSelected: itemId => BrowserSelectionNavigation.IsItemSelected(
                    _navigationState.CurrentDestination,
                    itemId),
                journeyResearchState: journeyResearchState,
                emptyStateText: string.Empty);
            _itemScroll.Content.Append(_itemGrid);
            SynchronizeLocalization(force: true);
            SynchronizeCategoryNavigation();
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
        }

        public bool IsWritingText => _searchInput.IsWritingText;

        public bool HasOpenTransientSurface => _filterPopover.IsOpen || _sortPopover.IsOpen;

        public bool TryCloseTransientSurface()
        {
            if (_filterPopover.IsOpen)
            {
                _filterPopover.Close();
                UpdateFilterControlsPresentation();
                return true;
            }

            if (_sortPopover.IsOpen)
            {
                _sortPopover.Close();
                UpdateSortControlsPresentation();
                return true;
            }

            return false;
        }

        public override void OnDeactivate()
        {
            _filterPopover.Close();
            _sortPopover.Close();

            if (_searchInput.IsWritingText)
                _searchInput.ToggleTakingText();

            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
            base.OnDeactivate();
        }

        public override void Update(GameTime gameTime)
        {
            SynchronizeLocalization();
            _filterPopup.SynchronizeState();
            bool sortOptionsChanged = _sortPopup.SynchronizeState();

            if (sortOptionsChanged && _sortPopover.IsOpen)
                _sortPopover.Recalculate();

            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
            _categorySelector.ActiveRoot = GetActiveRootNode();
            _sortDirectionButton.Text = _filterModel.SortDirection == ChecklistSortDirection.Ascending ? "↑" : "↓";
            _sortDirectionButton.TooltipText = GetSortDirectionTooltip(
                _filterModel.SortMode,
                _filterModel.SortDirection);

            SynchronizeCategoryNavigation();

            base.Update(gameTime);
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            int sortWidth = Math.Min(SortControlWidth, Math.Max(ControlHeight * 3, width / 2));
            int remainingBeforeSort = Math.Max(0, width - sortWidth - TopBarGroupGap);
            int filterWidth = Math.Min(FilterButtonWidth, remainingBeforeSort);
            int remainingBeforeFilter = Math.Max(0, remainingBeforeSort - filterWidth - TopBarControlGap);
            int clearWidth = Math.Min(ClearButtonWidth, remainingBeforeFilter);
            int searchGroupWidth = Math.Max(0, remainingBeforeFilter - clearWidth - TopBarGroupGap);
            int controlY = (SearchControlHeight - ControlHeight) / 2;
            int searchToggleWidth = Math.Min(SearchDescriptionToggleSize, searchGroupWidth);
            int searchToggleGap = searchToggleWidth > 0 ? TopBarControlGap : 0;
            int searchInputWidth = Math.Max(0, searchGroupWidth - searchToggleWidth - searchToggleGap);

            LayoutElement(_searchInput, 0, controlY, searchInputWidth, ControlHeight);
            LayoutElement(
                _searchDescriptionToggle,
                searchInputWidth + searchToggleGap,
                controlY,
                searchToggleWidth,
                ControlHeight);

            int clearX = searchGroupWidth + TopBarGroupGap;
            LayoutElement(_clearFiltersButton, clearX, controlY, clearWidth, ControlHeight);

            int filterX = clearX + clearWidth + TopBarControlGap;
            LayoutElement(_filterButton, filterX, controlY, filterWidth, ControlHeight);

            int directionWidth = ControlHeight;
            int sortButtonWidth = Math.Max(0, sortWidth - directionWidth - TopBarControlGap);
            int sortX = filterX + filterWidth + TopBarGroupGap;
            LayoutElement(_sortButton, sortX, controlY, sortButtonWidth, ControlHeight);
            LayoutElement(
                _sortDirectionButton,
                sortX + sortButtonWidth + TopBarControlGap,
                controlY,
                directionWidth,
                ControlHeight);
            int y = SearchControlHeight + LayoutSpacing;

            LayoutElement(_categorySelector, 0, y, width, CategoryIconSize);
            y += CategoryIconSize + CategoryContentGap;

            int contentHeight = Math.Max(0, height - y - LayoutSpacing - ProgressHeight);
            int sidebarWidth = Math.Min(
                ItemCategoryNavigationView.PreferredWidth,
                Math.Max(0, width - SidebarGap - VirtualItemGrid.SlotSize));
            int gridWidth = Math.Max(0, width - sidebarWidth - SidebarGap);

            LayoutElement(_categoryNavigation, 0, y, sidebarWidth, contentHeight);
            LayoutElement(_itemScroll, sidebarWidth + SidebarGap, y, gridWidth, contentHeight);

            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            int height = Math.Max(0, (int)dimensions.Height);
            int y = (int)dimensions.Y + Math.Max(0, height - ProgressHeight);
            int width = Math.Max(0, (int)dimensions.Width);
            int progressWidth = Math.Max(0, (width - ProgressGap) / 2);

            string overallText = FormatProgressText(
                _localization.Get(CompendiumTextKeys.Common.Overall),
                _checklistState.FoundCount,
                _checklistState.TotalCount,
                _checklistState.CompletionRatio);
            DrawProgressBar(x, y, progressWidth, ProgressHeight, _checklistState.CompletionRatio, overallText);

            int filteredX = x + progressWidth + ProgressGap;
            int filteredWidth = Math.Max(0, width - progressWidth - ProgressGap);
            string filteredText = FormatProgressText(
                _localization.Get(CompendiumTextKeys.Common.Filtered),
                _filterModel.ScopeFoundCount,
                _filterModel.ScopeTotalCount,
                _filterModel.ScopeCompletionRatio);
            DrawProgressBar(
                filteredX,
                y,
                filteredWidth,
                ProgressHeight,
                _filterModel.ScopeCompletionRatio,
                filteredText);
        }

        private void SetNavigationFilter(ChecklistNavigationFilter navigationFilter)
        {
            if (_filterModel.NavigationFilter == navigationFilter)
                return;

            _filterModel.NavigationFilter = navigationFilter;
            _itemScroll.ResetScroll();
            SynchronizeCategoryNavigation();
        }

        private void SynchronizeCategoryNavigation()
        {
            ChecklistNavigationFilter navigation = _filterModel.NavigationFilter;
            ItemNavigationNodeId? parentTarget = GetBackTarget(navigation);
            bool hasOther = navigation.IsNode &&
                            navigation.NodeId != ItemNavigationNodeId.AllItems &&
                            _filterModel.HasOtherItems(navigation.NodeId);

            _categoryNavigation.Synchronize(
                navigation.NodeId,
                navigation.IsOther,
                parentTarget,
                hasOther,
                rootActionText: _localization.Get(
                    CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.AllItems)));
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
            ChecklistNavigationFilter current = _filterModel.NavigationFilter;
            ChecklistNavigationFilter next = current.IsNode && current.NodeId == nodeId
                ? ChecklistNavigationFilter.AllItems
                : ChecklistNavigationFilter.ForNode(nodeId);

            SetNavigationFilter(next);
        }

        private void NavigateToItemsRoot()
        {
            SetNavigationFilter(ChecklistNavigationFilter.AllItems);
            _navigationState.Navigate(BrowserDestination.ForSection(BrowserSection.Items));
        }

        private void OnSearchContentsChanged(string contents)
        {
            _filterModel.SearchQuery = contents ?? string.Empty;
            _itemScroll.ResetScroll();
        }

        private void ToggleSearchDescriptions()
        {
            _filterModel.SearchDescriptions = !_filterModel.SearchDescriptions;
            _itemScroll.ResetScroll();
        }

        private void ClearFilters()
        {
            if (_filterPopup.ActiveFilterCount == 0)
                return;

            _filterPopup.ClearFilters();
            _filterPopover.Close();
            UpdateFilterControlsPresentation();
        }

        private void ToggleFilterPopover()
        {
            if (_sortPopover.IsOpen)
                _sortPopover.Close();

            _filterPopup.SynchronizeState();
            _filterPopover.Toggle();
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
        }

        private void ToggleSortPopover()
        {
            if (_filterPopover.IsOpen)
                _filterPopover.Close();

            _sortPopup.SynchronizeState();
            _sortPopover.Toggle();
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
        }

        private void OnFiltersChanged()
        {
            _itemScroll.ResetScroll();
            UpdateFilterControlsPresentation();
        }

        private void UpdateFilterControlsPresentation()
        {
            int activeFilterCount = _filterPopup.ActiveFilterCount;
            _clearFiltersButton.IsEnabled = activeFilterCount > 0;
            _filterButton.Text = activeFilterCount == 0
                ? _localization.Get(CompendiumTextKeys.Common.Filters)
                : _localization.Format(CompendiumTextKeys.Common.FiltersCount, activeFilterCount);
            _filterButton.IsActive = _filterPopover.IsOpen || activeFilterCount > 0;
        }

        private void OnSortModeChanged()
        {
            _itemScroll.ResetScroll();
            _sortPopover.Close();
            UpdateSortControlsPresentation();
        }

        private void UpdateSortControlsPresentation()
        {
            _sortButton.Text = _sortPopup.ButtonText;
            _sortButton.IsActive = _sortPopover.IsOpen;
        }

        private void ToggleSortDirection()
        {
            _filterModel.SortDirection = _filterModel.SortDirection == ChecklistSortDirection.Ascending
                ? ChecklistSortDirection.Descending
                : ChecklistSortDirection.Ascending;
            _itemScroll.ResetScroll();
        }

        private ItemNavigationNodeId? GetActiveRootNode()
        {
            ItemNavigationNodeId nodeId = _filterModel.NavigationFilter.NodeId;

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

        private static void LayoutElement(UIElement element, int left, int top, int width, int height)
        {
            if (element == null)
                return;

            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private static void DrawProgressBar(int x, int y, int width, int height, double ratio, string text)
        {
            if (width <= 0 || height <= 0)
                return;

            double clampedRatio = Math.Max(0.0, Math.Min(1.0, ratio));
            var fillWidth = (int)Math.Round(width * clampedRatio);

            UIRenderer.DrawRect(x, y, width, height, UIColors.SectionBg);

            if (fillWidth > 0)
                UIRenderer.DrawRect(x, y, fillWidth, height, UIColors.Accent);

            UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);
            UIRenderer.DrawText(text, x + 6, y + (height - 16) / 2, UIColors.Text);
        }

        private string FormatProgressText(string label, int foundCount, int totalCount, double ratio)
        {
            return _localization.Format(
                CompendiumTextKeys.Common.Progress,
                label,
                foundCount,
                totalCount,
                ratio * 100.0);
        }

        private string GetSortDirectionTooltip(ChecklistSortMode sortMode, ChecklistSortDirection sortDirection)
        {
            string directionName = _localization.Get(
                sortDirection == ChecklistSortDirection.Ascending
                    ? CompendiumTextKeys.Common.Ascending
                    : CompendiumTextKeys.Common.Descending);

            switch (sortMode)
            {
                case ChecklistSortMode.Native:
                    return _localization.Format(
                        sortDirection == ChecklistSortDirection.Ascending
                            ? CompendiumTextKeys.Common.SortNativeAscending
                            : CompendiumTextKeys.Common.SortNativeDescending,
                        directionName);

                case ChecklistSortMode.Name:
                    return _localization.Format(
                        sortDirection == ChecklistSortDirection.Ascending
                            ? CompendiumTextKeys.Common.SortNameAscending
                            : CompendiumTextKeys.Common.SortNameDescending,
                        directionName);

                default:
                    return _localization.Format(
                        sortDirection == ChecklistSortDirection.Ascending
                            ? CompendiumTextKeys.Common.SortNumericAscending
                            : CompendiumTextKeys.Common.SortNumericDescending,
                        directionName);
            }
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _clearFiltersButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.ClearFilters);
            _filterButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.FiltersTooltip);
            _sortButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.SortTooltip);
            _itemGrid.EmptyStateText = _localization.Get(CompendiumTextKeys.Items.EmptyState);
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
            SynchronizeCategoryNavigation();
            Recalculate();
        }

        private sealed class SearchDescriptionToggleElement : UIElement
        {
            private readonly Action _clicked;
            private readonly Func<bool> _isEnabled;
            private readonly CompendiumLocalization _localization;

            public SearchDescriptionToggleElement(
                CompendiumLocalization localization,
                Func<bool> isEnabled,
                Action clicked)
            {
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _isEnabled = isEnabled ?? throw new ArgumentNullException(nameof(isEnabled));
                _clicked = clicked;
                OnLeftClick += OnClicked;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int size = Math.Max(0, Math.Min((int)dimensions.Width, (int)dimensions.Height));

                if (size <= 0)
                    return;

                bool enabled = _isEnabled();
                UIRenderer.DrawRect(x, y, size, size, IsMouseHovering ? UIColors.ButtonHover : UIColors.Button);
                UIRenderer.DrawRectOutline(
                    x,
                    y,
                    size,
                    size,
                    enabled ? UIColors.Success : UIColors.Border,
                    enabled ? SearchDescriptionToggleActiveBorderThickness : 1);

                int textWidth = UIRenderer.MeasureText("i");
                UIRenderer.DrawText(
                    "i",
                    x + Math.Max(0, (size - textWidth) / 2),
                    y + Math.Max(0, (size - 16) / 2),
                    UIColors.Text);

                if (IsMouseHovering)
                    Tooltip.Set(
                        _localization.Format(
                            CompendiumTextKeys.Items.SearchDescriptions,
                            _localization.Get(enabled ? CompendiumTextKeys.Common.On : CompendiumTextKeys.Common.Off)));
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (evt.Target == this)
                    _clicked?.Invoke();
            }
        }
    }
}