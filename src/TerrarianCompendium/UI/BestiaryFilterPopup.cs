using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class BestiaryFilterPopup : UIElement, IVanillaPopoverContent
    {
        private const int LabelHeight = 18;
        private const int ControlHeight = 24;
        private const int IconSize = 30;
        private const int Gap = 4;
        private const int MaxGridColumns = 7;

        private readonly VanillaBestiaryFilterCatalog _catalog;
        private readonly VanillaIconButton _encounteredButton;
        private readonly TextLabelElement _encounterLabel;
        private readonly Action _filtersChanged;
        private readonly BestiaryFilterState _filterState;
        private readonly UIElement _grid;
        private readonly bool _hasMerchantFilter;
        private readonly VanillaTextButton _hasStockButton;
        private readonly CompendiumLocalization _localization;
        private readonly TextLabelElement _merchantLabel;
        private readonly NativeFilterButton[] _nativeButtons;
        private readonly TextLabelElement _nativeLabel;
        private readonly VanillaScrollRegion _scroll;
        private readonly VanillaIconButton _unknownButton;
        private long _localizationRevision = -1;

        public BestiaryFilterPopup(
            BestiaryFilterState filterState,
            VanillaBestiaryFilterCatalog catalog,
            bool merchantFilterAvailable,
            CompendiumLocalization localization,
            Action filtersChanged)
        {
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _hasMerchantFilter = merchantFilterAvailable;
            _filtersChanged = filtersChanged;
            SetPadding(0f);

            _encounterLabel = new TextLabelElement(string.Empty);
            Append(_encounterLabel);

            _unknownButton = CreateEncounterButton(found: false, BestiaryEncounterFilter.Unknown);
            _encounteredButton = CreateEncounterButton(found: true, BestiaryEncounterFilter.Encountered);
            Append(_unknownButton);
            Append(_encounteredButton);

            if (_hasMerchantFilter)
            {
                _merchantLabel = new TextLabelElement(string.Empty);
                Append(_merchantLabel);

                _hasStockButton = new VanillaTextButton(string.Empty, ToggleHasStockOnly)
                {
                    ActiveBorderColor = UIColors.Success,
                    ActiveBorderThickness = 2,
                };
                Append(_hasStockButton);
            }

            _nativeLabel = new TextLabelElement(string.Empty);
            Append(_nativeLabel);

            _scroll = new VanillaScrollRegion();
            Append(_scroll);

            _grid = new UIElement
            {
                Width = StyleDimension.Fill
            };
            _grid.SetPadding(0f);
            _scroll.Content.Append(_grid);

            _nativeButtons = new NativeFilterButton[_catalog.Options.Count];

            for (var index = 0; index < _catalog.Options.Count; index++)
            {
                VanillaBestiaryFilterOption option = _catalog.Options[index];
                var button = new NativeFilterButton(
                    option.CreateImage(),
                    () => ToggleNativeFilter(option.Id),
                    () => option.GetDisplayName(_localization));
                _nativeButtons[index] = button;
                _grid.Append(button);
            }

            SynchronizeLocalization(force: true);
            SynchronizeState();
        }

        public int ActiveFilterCount => _filterState.ActiveFilterCount;

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int encounterWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.Encounter)),
                IconSize * 2 + Gap);
            int nativeGridWidth = CalculateGridWidth(Math.Min(MaxGridColumns, _nativeButtons.Length));
            int nativeWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.NativeFilters)),
                nativeGridWidth);
            int naturalWidth = Math.Max(encounterWidth, VanillaScrollRegion.CalculateRequiredWidth(nativeWidth));

            if (_hasMerchantFilter)
            {
                naturalWidth = Math.Max(
                    naturalWidth,
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.MerchantFilter)));
                naturalWidth = Math.Max(
                    naturalWidth,
                    VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Bestiary.HasStock)));
            }

            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int contentWidth = VanillaScrollRegion.CalculateContentWidth(width);
            int nativeHeight = CalculateGridHeight(_nativeButtons.Length, contentWidth);
            int height = LabelHeight + FilterPopupLayout.ContentGap + IconSize;

            if (_hasMerchantFilter)
            {
                height += FilterPopupLayout.SectionGap + LabelHeight + FilterPopupLayout.ContentGap + ControlHeight;
            }

            height += FilterPopupLayout.SectionGap + LabelHeight + FilterPopupLayout.ContentGap + nativeHeight;
            return new VanillaPopoverContentSize(width, height);
        }

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            _encounteredButton.IsActive = _filterState.EncounterFilter == BestiaryEncounterFilter.Encountered;
            _unknownButton.IsActive = _filterState.EncounterFilter == BestiaryEncounterFilter.Unknown;

            if (_hasStockButton != null)
                _hasStockButton.IsActive = _filterState.HasStockOnly;

            for (var index = 0; index < _nativeButtons.Length; index++)
                _nativeButtons[index].IsActive = _filterState.IsNativeFilterActive(_catalog.Options[index].Id);
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _encounterLabel.Text = _localization.Get(CompendiumTextKeys.Bestiary.Encounter);
            _unknownButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.Unknown);
            _encounteredButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.Encountered);
            if (_merchantLabel != null)
                _merchantLabel.Text = _localization.Get(CompendiumTextKeys.Bestiary.MerchantFilter);
            if (_hasStockButton != null)
            {
                _hasStockButton.Text = _localization.Get(CompendiumTextKeys.Bestiary.HasStock);
                _hasStockButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.HasStockTooltip);
            }

            _nativeLabel.Text = _localization.Get(CompendiumTextKeys.Bestiary.NativeFilters);
            Recalculate();
        }

        public void ClearFilters()
        {
            if (_filterState.ActiveFilterCount == 0)
                return;

            _filterState.Clear();
            NotifyFiltersChanged();
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            var top = 0;

            LayoutElement(_encounterLabel, 0, top, width, LabelHeight);
            top += LabelHeight + FilterPopupLayout.ContentGap;

            int pairWidth = IconSize * 2 + Gap;
            int pairLeft = Math.Max(0, (width - pairWidth) / 2);
            LayoutElement(_unknownButton, pairLeft, top, IconSize, IconSize);
            LayoutElement(_encounteredButton, pairLeft + IconSize + Gap, top, IconSize, IconSize);
            top += IconSize;

            if (_merchantLabel != null)
            {
                top += FilterPopupLayout.SectionGap;
                LayoutElement(_merchantLabel, 0, top, width, LabelHeight);
                top += LabelHeight + FilterPopupLayout.ContentGap;
                LayoutElement(_hasStockButton, 0, top, width, ControlHeight);
                top += ControlHeight;
            }

            top += FilterPopupLayout.SectionGap;
            LayoutElement(_nativeLabel, 0, top, width, LabelHeight);
            top += LabelHeight + FilterPopupLayout.ContentGap;

            LayoutElement(_scroll, 0, top, width, Math.Max(0, height - top));

            int contentWidth = _scroll.ContentWidth;
            int columns = CalculateGridColumnCount(contentWidth);
            int contentHeight = CalculateGridHeight(_nativeButtons.Length, contentWidth);

            for (var index = 0; index < _nativeButtons.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                LayoutElement(
                    _nativeButtons[index],
                    column * (IconSize + Gap),
                    row * (IconSize + Gap),
                    IconSize,
                    IconSize);
            }

            _grid.Height.Set(contentHeight, 0f);
            _scroll.SetContentHeight(contentHeight);
            base.RecalculateChildren();
        }

        private static int CalculateGridColumnCount(int width)
        {
            int availableColumns = Math.Max(1, (Math.Max(0, width) + Gap) / (IconSize + Gap));
            return Math.Min(MaxGridColumns, availableColumns);
        }

        private static int CalculateGridHeight(int itemCount, int width)
        {
            if (itemCount <= 0)
                return 0;

            int columns = CalculateGridColumnCount(width);
            int rows = (itemCount + columns - 1) / columns;
            return rows * IconSize + Math.Max(0, rows - 1) * Gap;
        }

        private static int CalculateGridWidth(int columns)
        {
            if (columns <= 0)
                return 0;

            return columns * IconSize + (columns - 1) * Gap;
        }

        private VanillaIconButton CreateEncounterButton(bool found, BestiaryEncounterFilter filter)
        {
            return new VanillaIconButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found),
                () => ToggleEncounterFilter(filter))
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
            };
        }

        private void ToggleEncounterFilter(BestiaryEncounterFilter filter)
        {
            _filterState.EncounterFilter = _filterState.EncounterFilter == filter
                ? BestiaryEncounterFilter.All
                : filter;
            NotifyFiltersChanged();
        }

        private void ToggleHasStockOnly()
        {
            _filterState.HasStockOnly = !_filterState.HasStockOnly;
            NotifyFiltersChanged();
        }

        private void ToggleNativeFilter(int filterId)
        {
            _filterState.ToggleNativeFilter(filterId);
            NotifyFiltersChanged();
        }

        private void NotifyFiltersChanged()
        {
            SynchronizeState();
            _filtersChanged?.Invoke();
        }

        private static void LayoutElement(UIElement element, int x, int y, int width, int height)
        {
            element.Left.Set(x, 0f);
            element.Top.Set(y, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private sealed class NativeFilterButton : UIElement
        {
            private readonly Action _clicked;
            private readonly Func<string> _tooltipProvider;

            public NativeFilterButton(UIElement image, Action clicked, Func<string> tooltipProvider)
            {
                _clicked = clicked;
                _tooltipProvider = tooltipProvider;
                OnLeftClick += OnClicked;

                if (image != null)
                {
                    image.IgnoresMouseInteraction = true;
                    Append(image);
                }
            }

            public bool IsActive { get; set; }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                if (width <= 0 || height <= 0)
                    return;

                UIRenderer.DrawRect(
                    x,
                    y,
                    width,
                    height,
                    IsActive ? UIColors.ItemActiveBg : IsMouseHovering ? UIColors.ButtonHover : UIColors.Button);
                UIRenderer.DrawRectOutline(x, y, width, height, IsActive ? UIColors.Accent : UIColors.Border);

                if (!IsMouseHovering)
                    return;

                string tooltip = _tooltipProvider?.Invoke();

                if (!string.IsNullOrWhiteSpace(tooltip))
                    Tooltip.Set(tooltip);
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (evt.Target == this)
                    _clicked?.Invoke();
            }
        }
    }
}