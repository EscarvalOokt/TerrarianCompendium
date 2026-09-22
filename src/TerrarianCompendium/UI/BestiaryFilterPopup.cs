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
        private readonly TextLabelElement _dropsLabel;
        private readonly VanillaIconButton _encounteredButton;
        private readonly TextLabelElement _encounterLabel;
        private readonly Action _filtersChanged;
        private readonly BestiaryFilterState _filterState;
        private readonly UIElement _grid;
        private readonly VanillaTextButton _hasDropsButton;
        private readonly VanillaTextButton _hasMissingDropsButton;
        private readonly VanillaTextButton _hasStockButton;
        private readonly VanillaTextButton _hasUnresearchedDropsButton;
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
            bool lootAwareFiltersAvailable,
            bool unresearchedDropsFilterAvailable,
            CompendiumLocalization localization,
            Action filtersChanged)
        {
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _filtersChanged = filtersChanged;
            SetPadding(0f);

            _scroll = new VanillaScrollRegion();
            Append(_scroll);

            _encounterLabel = new TextLabelElement(string.Empty);
            _scroll.Content.Append(_encounterLabel);

            _unknownButton = CreateEncounterButton(found: false, BestiaryEncounterFilter.Unknown);
            _encounteredButton = CreateEncounterButton(found: true, BestiaryEncounterFilter.Encountered);
            _scroll.Content.Append(_unknownButton);
            _scroll.Content.Append(_encounteredButton);

            if (merchantFilterAvailable)
            {
                _merchantLabel = new TextLabelElement(string.Empty);
                _scroll.Content.Append(_merchantLabel);

                _hasStockButton = new VanillaTextButton(string.Empty, ToggleHasStockOnly)
                {
                    ActiveBorderColor = UIColors.Success,
                    ActiveBorderThickness = 2,
                };
                _scroll.Content.Append(_hasStockButton);
            }

            if (lootAwareFiltersAvailable)
            {
                _dropsLabel = new TextLabelElement(string.Empty);
                _scroll.Content.Append(_dropsLabel);

                _hasDropsButton = CreateDropCriterionButton(BestiaryFilterCriterion.HasDrops);
                _hasMissingDropsButton = CreateDropCriterionButton(BestiaryFilterCriterion.HasMissingDrops);
                _scroll.Content.Append(_hasDropsButton);
                _scroll.Content.Append(_hasMissingDropsButton);

                if (unresearchedDropsFilterAvailable)
                {
                    _hasUnresearchedDropsButton =
                        CreateDropCriterionButton(BestiaryFilterCriterion.HasUnresearchedDrops);
                    _scroll.Content.Append(_hasUnresearchedDropsButton);
                }
            }

            _nativeLabel = new TextLabelElement(string.Empty);
            _scroll.Content.Append(_nativeLabel);

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
            int naturalContentWidth = CalculateNaturalContentWidth();
            int naturalWidth = VanillaScrollRegion.CalculateRequiredWidth(naturalContentWidth);
            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int contentWidth = VanillaScrollRegion.CalculateContentWidth(width);
            int height = CalculateContentHeight(contentWidth);
            return new VanillaPopoverContentSize(width, height);
        }

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            _encounteredButton.IsActive = _filterState.EncounterFilter == BestiaryEncounterFilter.Encountered;
            _unknownButton.IsActive = _filterState.EncounterFilter == BestiaryEncounterFilter.Unknown;

            if (_hasStockButton != null)
                _hasStockButton.IsActive = _filterState.HasStockOnly;

            if (_hasDropsButton != null)
                _hasDropsButton.IsActive = _filterState.DropCriterion == BestiaryFilterCriterion.HasDrops;
            if (_hasMissingDropsButton != null)
            {
                _hasMissingDropsButton.IsActive = _filterState.DropCriterion == BestiaryFilterCriterion.HasMissingDrops;
            }

            if (_hasUnresearchedDropsButton != null)
            {
                _hasUnresearchedDropsButton.IsActive =
                    _filterState.DropCriterion == BestiaryFilterCriterion.HasUnresearchedDrops;
            }

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

            if (_dropsLabel != null)
                _dropsLabel.Text = _localization.Get(CompendiumTextKeys.Bestiary.Drops);

            if (_hasDropsButton != null)
            {
                _hasDropsButton.Text = _localization.Get(CompendiumTextKeys.Bestiary.HasDrops);
                _hasDropsButton.TooltipText = _localization.Get(CompendiumTextKeys.Bestiary.HasDropsTooltip);
                _hasMissingDropsButton.Text = _localization.Get(CompendiumTextKeys.Bestiary.HasMissingDrops);
                _hasMissingDropsButton.TooltipText =
                    _localization.Get(CompendiumTextKeys.Bestiary.HasMissingDropsTooltip);
            }

            if (_hasUnresearchedDropsButton != null)
            {
                _hasUnresearchedDropsButton.Text = _localization.Get(CompendiumTextKeys.Bestiary.HasUnresearchedDrops);
                _hasUnresearchedDropsButton.TooltipText =
                    _localization.Get(CompendiumTextKeys.Bestiary.HasUnresearchedDropsTooltip);
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
            _scroll.Left.Set(0f, 0f);
            _scroll.Top.Set(0f, 0f);
            _scroll.Width.Set(0f, 1f);
            _scroll.Height.Set(0f, 1f);

            base.RecalculateChildren();

            int width = _scroll.ContentWidth;
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

            if (_dropsLabel != null)
            {
                top += FilterPopupLayout.SectionGap;
                LayoutElement(_dropsLabel, 0, top, width, LabelHeight);
                top += LabelHeight + FilterPopupLayout.ContentGap;

                LayoutElement(_hasDropsButton, 0, top, width, ControlHeight);
                top += ControlHeight + Gap;

                LayoutElement(_hasMissingDropsButton, 0, top, width, ControlHeight);
                top += ControlHeight;

                if (_hasUnresearchedDropsButton != null)
                {
                    top += Gap;
                    LayoutElement(_hasUnresearchedDropsButton, 0, top, width, ControlHeight);
                    top += ControlHeight;
                }
            }

            top += FilterPopupLayout.SectionGap;
            LayoutElement(_nativeLabel, 0, top, width, LabelHeight);
            top += LabelHeight + FilterPopupLayout.ContentGap;

            int columns = CalculateGridColumnCount(width);
            int gridHeight = CalculateGridHeight(_nativeButtons.Length, width);
            LayoutElement(_grid, 0, top, width, gridHeight);

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

            top += gridHeight;
            _grid.Height.Set(gridHeight, 0f);
            _scroll.SetContentHeight(top);
        }

        private int CalculateNaturalContentWidth()
        {
            int width = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.Encounter)),
                IconSize * 2 + Gap);

            if (_merchantLabel != null)
            {
                width = Math.Max(
                    width,
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.MerchantFilter)));
                width = Math.Max(
                    width,
                    VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Bestiary.HasStock)));
            }

            if (_dropsLabel != null)
            {
                width = Math.Max(width, TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.Drops)));
                width = Math.Max(
                    width,
                    VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Bestiary.HasDrops)));
                width = Math.Max(
                    width,
                    VanillaTextButton.MeasureNaturalWidth(
                        _localization.Get(CompendiumTextKeys.Bestiary.HasMissingDrops)));

                if (_hasUnresearchedDropsButton != null)
                {
                    width = Math.Max(
                        width,
                        VanillaTextButton.MeasureNaturalWidth(
                            _localization.Get(CompendiumTextKeys.Bestiary.HasUnresearchedDrops)));
                }
            }

            int nativeWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Bestiary.NativeFilters)),
                CalculateGridWidth(Math.Min(MaxGridColumns, _nativeButtons.Length)));
            return Math.Max(width, nativeWidth);
        }

        private int CalculateContentHeight(int width)
        {
            int top = LabelHeight + FilterPopupLayout.ContentGap + IconSize;

            if (_merchantLabel != null)
            {
                top += FilterPopupLayout.SectionGap + LabelHeight + FilterPopupLayout.ContentGap + ControlHeight;
            }

            if (_dropsLabel != null)
            {
                int criterionCount = _hasUnresearchedDropsButton == null ? 2 : 3;
                top += FilterPopupLayout.SectionGap +
                       LabelHeight +
                       FilterPopupLayout.ContentGap +
                       criterionCount * ControlHeight +
                       Math.Max(0, criterionCount - 1) * Gap;
            }

            top += FilterPopupLayout.SectionGap + LabelHeight + FilterPopupLayout.ContentGap;
            return top + CalculateGridHeight(_nativeButtons.Length, width);
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

        private VanillaTextButton CreateDropCriterionButton(BestiaryFilterCriterion criterion)
        {
            return new VanillaTextButton(string.Empty, () => ToggleDropCriterion(criterion))
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
            };
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

        private void ToggleDropCriterion(BestiaryFilterCriterion criterion)
        {
            _filterState.DropCriterion = _filterState.DropCriterion == criterion
                ? BestiaryFilterCriterion.All
                : criterion;
            NotifyFiltersChanged();
        }

        private void ToggleNativeFilter(int filterId)
        {
            var criterion = BestiaryFilterCriterion.ForNative(filterId);
            _filterState.BestiaryCriterion = _filterState.BestiaryCriterion == criterion
                ? BestiaryFilterCriterion.All
                : criterion;
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
            private readonly SelectionOverlayElement _overlay;
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

                _overlay = new SelectionOverlayElement(this)
                {
                    IgnoresMouseInteraction = true
                };
                Append(_overlay);
            }

            public bool IsActive { get; set; }

            public override void RecalculateChildren()
            {
                CalculatedStyle dimensions = GetInnerDimensions();
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                _overlay.Left.Set(0f, 0f);
                _overlay.Top.Set(0f, 0f);
                _overlay.Width.Set(width, 0f);
                _overlay.Height.Set(height, 0f);
                base.RecalculateChildren();
            }

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
                if (IsActive)
                {
                    UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Success, 2);
                }
                else
                {
                    UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);
                }

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

            private sealed class SelectionOverlayElement(NativeFilterButton owner) : UIElement
            {
                private readonly NativeFilterButton _owner = owner ?? throw new ArgumentNullException(nameof(owner));

                protected override void DrawSelf(SpriteBatch spriteBatch)
                {
                    if (!_owner.IsActive)
                        return;

                    CalculatedStyle dimensions = GetDimensions();
                    var x = (int)dimensions.X;
                    var y = (int)dimensions.Y;
                    int width = Math.Max(0, (int)dimensions.Width);
                    int height = Math.Max(0, (int)dimensions.Height);

                    if (width <= 0 || height <= 0)
                        return;

                    UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Success, 2);
                }
            }
        }
    }
}