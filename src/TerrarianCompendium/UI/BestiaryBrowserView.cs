using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Navigation;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class BestiaryBrowserView : UIElement
    {
        private const int ControlHeight = 24;
        private const int SearchControlHeight = 30;
        private const int LayoutSpacing = 4;
        private const int TopBarGroupGap = 4;
        private const int TopBarControlGap = 2;
        private const int ClearButtonWidth = 30;
        private const int FilterButtonWidth = 100;
        private const int SortControlWidth = 180;
        private const int ProgressHeight = 24;
        private const int ProgressGap = 6;
        private const int SearchMaxLength = 100;
        private readonly VanillaTextButton _clearFiltersButton;
        private readonly VanillaTextButton _filterButton;
        private readonly VanillaPopover _filterPopover;
        private readonly BestiaryFilterPopup _filterPopup;
        private readonly BestiaryFilterState _filterState;
        private readonly CompendiumLocalization _localization;
        private readonly BestiaryBrowserModel _model;
        private readonly VirtualNpcGrid _npcGrid;
        private readonly VanillaScrollRegion _npcScroll;
        private readonly CompendiumSearchBar _searchInput;
        private readonly VanillaTextButton _sortButton;
        private readonly VanillaTextButton _sortDirectionButton;
        private readonly VanillaPopover _sortPopover;
        private readonly BestiarySortPopup _sortPopup;
        private long _localizationRevision = -1;

        public BestiaryBrowserView(
            BestiaryBrowserModel model,
            BestiaryFilterState filterState,
            VanillaBestiaryFilterCatalog filterCatalog,
            BrowserNavigationState navigationState,
            CompendiumLocalization localization)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            BrowserNavigationState resolvedNavigationState = navigationState ??
                                                             throw new ArgumentNullException(nameof(navigationState));
            VanillaBestiaryFilterCatalog resolvedFilterCatalog = filterCatalog ??
                                                                 throw new ArgumentNullException(nameof(filterCatalog));
            SetPadding(0f);

            _searchInput = new CompendiumSearchBar(() => UserInterface.ActiveInstance.GoBack(), _localization)
            {
                MaxInputLength = SearchMaxLength
            };
            _searchInput.OnSearchContentsChanged += OnSearchContentsChanged;
            Append(_searchInput);

            _clearFiltersButton = new VanillaTextButton("X", ClearFilters);
            Append(_clearFiltersButton);

            _filterButton = new VanillaTextButton(string.Empty, ToggleFilterPopover);
            Append(_filterButton);

            _filterPopup = new BestiaryFilterPopup(
                _filterState,
                resolvedFilterCatalog,
                _model.MerchantStockFilterAvailable,
                _localization,
                OnFiltersChanged);
            _filterPopover = new VanillaPopover(this, _filterButton, _filterPopup);

            _sortPopup = new BestiarySortPopup(_model, _localization, OnSortModeChanged);
            _sortButton = new VanillaTextButton(_sortPopup.ButtonText, ToggleSortPopover);
            Append(_sortButton);
            _sortPopover = new VanillaPopover(this, _sortButton, _sortPopup);

            _sortDirectionButton = new VanillaTextButton("↑", ToggleSortDirection);
            Append(_sortDirectionButton);

            _npcScroll = new VanillaScrollRegion();
            Append(_npcScroll);

            _npcGrid = new VirtualNpcGrid(
                _npcScroll,
                () => _model.VisibleEntries,
                string.Empty,
                npcNetId => BrowserSelectionNavigation.ToggleNpc(resolvedNavigationState, npcNetId),
                npcNetId => BrowserSelectionNavigation.IsNpcSelected(
                    resolvedNavigationState.CurrentDestination,
                    npcNetId));
            _npcScroll.Content.Append(_npcGrid);

            SynchronizeLocalization(force: true);

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
            _model.SynchronizeState();
            _filterPopup.SynchronizeState();
            _sortPopup.SynchronizeState();
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
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
            int searchWidth = Math.Max(0, remainingBeforeFilter - clearWidth - TopBarGroupGap);
            int controlY = Math.Max(0, (SearchControlHeight - ControlHeight) / 2);

            LayoutElement(_searchInput, 0, controlY, searchWidth, ControlHeight);

            int clearX = searchWidth + TopBarGroupGap;
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

            int contentTop = SearchControlHeight + LayoutSpacing;
            int contentHeight = Math.Max(0, height - contentTop - LayoutSpacing - ProgressHeight);
            LayoutElement(_npcScroll, 0, contentTop, width, contentHeight);

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
                _model.OverallEncounteredCount,
                _model.OverallTotalCount,
                _model.OverallEncounteredRatio);
            DrawProgressBar(x, y, progressWidth, ProgressHeight, _model.OverallEncounteredRatio, overallText);

            int filteredX = x + progressWidth + ProgressGap;
            int filteredWidth = Math.Max(0, width - progressWidth - ProgressGap);
            string filteredText = FormatProgressText(
                _localization.Get(CompendiumTextKeys.Common.Filtered),
                _model.ScopeEncounteredCount,
                _model.ScopeTotalCount,
                _model.ScopeEncounteredRatio);
            DrawProgressBar(filteredX, y, filteredWidth, ProgressHeight, _model.ScopeEncounteredRatio, filteredText);
        }

        private void OnSearchContentsChanged(string contents)
        {
            _model.SearchQuery = contents;
            _npcScroll.ResetScroll();
        }

        private void OnFiltersChanged()
        {
            _model.SynchronizeState();
            _npcScroll.ResetScroll();
            UpdateFilterControlsPresentation();
        }

        private void OnSortModeChanged()
        {
            _npcScroll.ResetScroll();
            UpdateSortControlsPresentation();
        }

        private void ClearFilters()
        {
            _filterPopup.ClearFilters();
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

        private void UpdateFilterControlsPresentation()
        {
            int activeCount = _filterState.ActiveFilterCount;
            _clearFiltersButton.IsEnabled = activeCount > 0;
            _filterButton.Text = activeCount == 0
                ? _localization.Get(CompendiumTextKeys.Common.Filters)
                : _localization.Format(CompendiumTextKeys.Common.FiltersCount, activeCount);
            _filterButton.IsActive = _filterPopover.IsOpen || activeCount > 0;
        }

        private void UpdateSortControlsPresentation()
        {
            _sortButton.Text = _sortPopup.ButtonText;
            _sortButton.IsActive = _sortPopover.IsOpen;
            _sortDirectionButton.Text = _model.SortDirection == BestiarySortDirection.Ascending ? "↑" : "↓";
            _sortDirectionButton.TooltipText = GetSortDirectionTooltip(_model.SortMode, _model.SortDirection);
        }

        private void ToggleSortDirection()
        {
            _model.SortDirection = _model.SortDirection == BestiarySortDirection.Ascending
                ? BestiarySortDirection.Descending
                : BestiarySortDirection.Ascending;
            _npcScroll.ResetScroll();
            UpdateSortControlsPresentation();
        }

        private static void LayoutElement(UIElement element, int x, int y, int width, int height)
        {
            element.Left.Set(x, 0f);
            element.Top.Set(y, 0f);
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

        private string GetSortDirectionTooltip(BestiarySortMode sortMode, BestiarySortDirection sortDirection)
        {
            string directionName = _localization.Get(
                sortDirection == BestiarySortDirection.Ascending
                    ? CompendiumTextKeys.Common.Ascending
                    : CompendiumTextKeys.Common.Descending);

            if (sortMode == BestiarySortMode.BestiaryOrder)
            {
                return _localization.Format(
                    sortDirection == BestiarySortDirection.Ascending
                        ? CompendiumTextKeys.Common.SortNativeAscending
                        : CompendiumTextKeys.Common.SortNativeDescending,
                    directionName);
            }

            if (sortMode == BestiarySortMode.Name)
            {
                return _localization.Format(
                    sortDirection == BestiarySortDirection.Ascending
                        ? CompendiumTextKeys.Common.SortNameAscending
                        : CompendiumTextKeys.Common.SortNameDescending,
                    directionName);
            }

            return _localization.Format(
                sortDirection == BestiarySortDirection.Ascending
                    ? CompendiumTextKeys.Common.SortNumericAscending
                    : CompendiumTextKeys.Common.SortNumericDescending,
                directionName);
        }

        private string FormatProgressText(string label, int encounteredCount, int totalCount, double ratio)
        {
            return _localization.Format(
                CompendiumTextKeys.Common.Progress,
                label,
                encounteredCount,
                totalCount,
                ratio * 100.0);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _clearFiltersButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.ClearFilters);
            _filterButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.FiltersTooltip);
            _sortButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.SortTooltip);
            _npcGrid.EmptyStateText = _localization.Get(CompendiumTextKeys.Bestiary.EmptyState);
            UpdateFilterControlsPresentation();
            UpdateSortControlsPresentation();
            Recalculate();
        }
    }
}