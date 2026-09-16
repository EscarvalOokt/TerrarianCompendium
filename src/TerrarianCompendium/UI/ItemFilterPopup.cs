using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemFilterPopup : UIElement, IVanillaPopoverContent
    {
        private const int ControlHeight = 24;
        private const int SectionLabelHeight = 18;
        private const int IconSize = 30;
        private const int IconGap = 4;
        private const int ControlGap = 4;
        private const int TagColumns = 4;

        private readonly TextLabelElement _acquisitionLabel;
        private readonly TextLabelElement _completionLabel;
        private readonly VanillaTextButton _craftableNowButton;
        private readonly TextLabelElement _craftingLabel;
        private readonly FacetFilterControl[] _facetControls;
        private readonly ChecklistFilterModel _filterModel;
        private readonly Action _filtersChanged;
        private readonly VanillaIconButton _foundButton;
        private readonly bool _hasCraftingFilters;
        private readonly bool _hasJourneyResearch;
        private readonly bool _hasMerchantFilters;
        private readonly bool _hasNpcDropFilters;
        private readonly VanillaTextButton _hasRecipeButton;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaIconButton _missingButton;
        private readonly VanillaTextButton _npcDropsButton;
        private readonly VanillaTextButton _purchasableButton;
        private readonly VanillaIconButton _researchedButton;
        private readonly TextLabelElement _researchLabel;
        private readonly TextLabelElement _tagsLabel;
        private readonly VanillaIconButton _unresearchedButton;
        private long _localizationRevision = -1;

        public ItemFilterPopup(
            ChecklistFilterModel filterModel,
            bool journeyResearchAvailable,
            bool craftingFiltersAvailable,
            bool npcDropFiltersAvailable,
            bool merchantFiltersAvailable,
            CompendiumLocalization localization,
            Action filtersChanged)
        {
            _filterModel = filterModel ?? throw new ArgumentNullException(nameof(filterModel));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hasJourneyResearch = journeyResearchAvailable;
            _hasCraftingFilters = craftingFiltersAvailable;
            _hasNpcDropFilters = npcDropFiltersAvailable;
            _hasMerchantFilters = merchantFiltersAvailable;
            _filtersChanged = filtersChanged;
            SetPadding(0f);

            _completionLabel = new TextLabelElement(string.Empty);
            Append(_completionLabel);

            _missingButton = CreateIconFilterButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: false),
                () => ToggleCompletionFilter(ChecklistCompletionFilter.Missing),
                string.Empty);
            Append(_missingButton);

            _foundButton = CreateIconFilterButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: true),
                () => ToggleCompletionFilter(ChecklistCompletionFilter.Found),
                string.Empty);
            Append(_foundButton);

            if (_hasJourneyResearch)
            {
                _researchLabel = new TextLabelElement(string.Empty);
                Append(_researchLabel);

                _unresearchedButton = CreateIconFilterButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: false),
                    () => ToggleResearchFilter(ChecklistResearchFilter.Unresearched),
                    string.Empty);
                Append(_unresearchedButton);

                _researchedButton = CreateIconFilterButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: true),
                    () => ToggleResearchFilter(ChecklistResearchFilter.Researched),
                    string.Empty);
                Append(_researchedButton);
            }

            if (_hasCraftingFilters)
            {
                _craftingLabel = new TextLabelElement(string.Empty);
                Append(_craftingLabel);

                _hasRecipeButton = new VanillaTextButton(
                    string.Empty,
                    () => ToggleCraftingFilter(ChecklistCraftingFilter.HasRecipe))
                {
                    ActiveBorderColor = UIColors.Success,
                    ActiveBorderThickness = 2,
                };
                Append(_hasRecipeButton);

                _craftableNowButton = new VanillaTextButton(
                    string.Empty,
                    () => ToggleCraftingFilter(ChecklistCraftingFilter.CraftableNow))
                {
                    ActiveBorderColor = UIColors.Success,
                    ActiveBorderThickness = 2,
                };
                Append(_craftableNowButton);
            }

            if (_hasNpcDropFilters || _hasMerchantFilters)
            {
                _acquisitionLabel = new TextLabelElement(string.Empty);
                Append(_acquisitionLabel);

                if (_hasNpcDropFilters)
                {
                    _npcDropsButton = new VanillaTextButton(string.Empty, ToggleNpcDropsOnly)
                    {
                        ActiveBorderColor = UIColors.Success,
                        ActiveBorderThickness = 2,
                    };
                    Append(_npcDropsButton);
                }

                if (_hasMerchantFilters)
                {
                    _purchasableButton = new VanillaTextButton(string.Empty, TogglePurchasableOnly)
                    {
                        ActiveBorderColor = UIColors.Success,
                        ActiveBorderThickness = 2,
                    };
                    Append(_purchasableButton);
                }
            }

            _tagsLabel = new TextLabelElement(string.Empty);
            Append(_tagsLabel);

            IReadOnlyList<ItemTaxonomyFacetDefinition> facets = ItemTaxonomyDefinitions.Facets;
            _facetControls = new FacetFilterControl[facets.Count];

            for (var index = 0; index < facets.Count; index++)
            {
                ItemTaxonomyFacetDefinition facet = facets[index];
                ItemTaxonomyFacetId facetId = facet.Id;
                var button = TaxonomyRefinementIconButton.CreateFacet(
                    ItemTaxonomyIconDefinitions.GetFacetItemId(facetId),
                    _localization.Get(facet.DisplayNameKey),
                    _filterModel.TaxonomyFacetSelection.GetState(facetId),
                    () => CycleFacet(facetId),
                    _localization);

                _facetControls[index] = new FacetFilterControl(button, facetId);
                Append(button);
            }

            SynchronizeLocalization(force: true);
            SynchronizeState();
        }

        public int ActiveFilterCount =>
            ItemFilterPresentation.CalculateActiveFilterCount(
                _filterModel.CompletionFilter,
                _hasJourneyResearch,
                _filterModel.ResearchFilter,
                _hasCraftingFilters,
                _filterModel.CraftingFilter,
                _hasNpcDropFilters,
                _filterModel.NpcDropsOnly,
                _hasMerchantFilters,
                _filterModel.PurchasableOnly,
                _filterModel.TaxonomyFacetSelection);

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int iconPairWidth = IconSize * 2 + ControlGap;
            int completionGroupWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Items.Completion)),
                iconPairWidth);
            int topSectionWidth = completionGroupWidth;

            if (_hasJourneyResearch)
            {
                int researchGroupWidth = Math.Max(
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Items.Research)),
                    iconPairWidth);
                int columnWidth = Math.Max(completionGroupWidth, researchGroupWidth);
                topSectionWidth = columnWidth * 2 + FilterPopupLayout.SectionGap;
            }

            int naturalWidth = Math.Max(
                topSectionWidth,
                Math.Max(
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Items.Tags)),
                    CalculateGridWidth(Math.Min(TagColumns, _facetControls.Length))));

            if (_hasCraftingFilters)
            {
                naturalWidth = Math.Max(
                    naturalWidth,
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Items.Crafting)));
                naturalWidth = Math.Max(
                    naturalWidth,
                    VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Items.HasRecipe)));
                naturalWidth = Math.Max(
                    naturalWidth,
                    VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Items.CraftableNow)));
            }

            if (_hasNpcDropFilters || _hasMerchantFilters)
            {
                naturalWidth = Math.Max(
                    naturalWidth,
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Items.AcquisitionSection)));

                if (_hasNpcDropFilters)
                {
                    naturalWidth = Math.Max(
                        naturalWidth,
                        VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Items.NpcDrops)));
                }

                if (_hasMerchantFilters)
                {
                    naturalWidth = Math.Max(
                        naturalWidth,
                        VanillaTextButton.MeasureNaturalWidth(_localization.Get(CompendiumTextKeys.Items.Purchasable)));
                }
            }

            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int height = CalculateContentHeight(width);
            return new VanillaPopoverContentSize(width, height);
        }

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            _missingButton.IsActive = _filterModel.CompletionFilter == ChecklistCompletionFilter.Missing;
            _foundButton.IsActive = _filterModel.CompletionFilter == ChecklistCompletionFilter.Found;

            if (_unresearchedButton != null)
            {
                _unresearchedButton.IsActive = _filterModel.ResearchFilter == ChecklistResearchFilter.Unresearched;
            }

            if (_researchedButton != null)
                _researchedButton.IsActive = _filterModel.ResearchFilter == ChecklistResearchFilter.Researched;

            if (_hasRecipeButton != null)
                _hasRecipeButton.IsActive = _filterModel.CraftingFilter == ChecklistCraftingFilter.HasRecipe;

            if (_craftableNowButton != null)
                _craftableNowButton.IsActive = _filterModel.CraftingFilter == ChecklistCraftingFilter.CraftableNow;

            if (_npcDropsButton != null)
                _npcDropsButton.IsActive = _filterModel.NpcDropsOnly;

            if (_purchasableButton != null)
                _purchasableButton.IsActive = _filterModel.PurchasableOnly;

            for (var index = 0; index < _facetControls.Length; index++)
            {
                FacetFilterControl control = _facetControls[index];
                control.Button.State = _filterModel.TaxonomyFacetSelection.GetState(control.FacetId);
            }
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            var top = 0;

            if (_researchLabel != null)
            {
                int columnGap = FilterPopupLayout.SectionGap;
                int completionWidth = Math.Max(0, (width - columnGap) / 2);
                int researchLeft = completionWidth + columnGap;
                int researchWidth = Math.Max(0, width - researchLeft);

                int completionBottom = LayoutIconGroup(
                    _completionLabel,
                    _missingButton,
                    _foundButton,
                    0,
                    completionWidth,
                    top);
                int researchBottom = LayoutIconGroup(
                    _researchLabel,
                    _unresearchedButton,
                    _researchedButton,
                    researchLeft,
                    researchWidth,
                    top);
                top = Math.Max(completionBottom, researchBottom);
            }
            else
            {
                top = LayoutIconGroup(_completionLabel, _missingButton, _foundButton, 0, width, top);
            }

            if (_craftingLabel != null)
            {
                top += FilterPopupLayout.SectionGap;
                LayoutElement(_craftingLabel, 0, top, width, SectionLabelHeight);
                top += SectionLabelHeight + FilterPopupLayout.ContentGap;

                LayoutElement(_hasRecipeButton, 0, top, width, ControlHeight);
                top += ControlHeight + ControlGap;
                LayoutElement(_craftableNowButton, 0, top, width, ControlHeight);
                top += ControlHeight;
            }

            if (_acquisitionLabel != null)
            {
                top += FilterPopupLayout.SectionGap;
                LayoutElement(_acquisitionLabel, 0, top, width, SectionLabelHeight);
                top += SectionLabelHeight + FilterPopupLayout.ContentGap;

                if (_npcDropsButton != null)
                {
                    LayoutElement(_npcDropsButton, 0, top, width, ControlHeight);
                    top += ControlHeight;
                }

                if (_purchasableButton != null)
                {
                    if (_npcDropsButton != null)
                        top += ControlGap;

                    LayoutElement(_purchasableButton, 0, top, width, ControlHeight);
                    top += ControlHeight;
                }
            }

            top += FilterPopupLayout.SectionGap;
            LayoutElement(_tagsLabel, 0, top, width, SectionLabelHeight);
            top += SectionLabelHeight + FilterPopupLayout.ContentGap;

            int columns = CalculateFacetColumnCount(width);

            for (var index = 0; index < _facetControls.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                int left = CalculateIconX(width, _facetControls.Length, columns, row, column);
                int iconTop = top + row * (IconSize + IconGap);
                LayoutElement(_facetControls[index].Button, left, iconTop, IconSize, IconSize);
            }

            base.RecalculateChildren();
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _completionLabel.Text = _localization.Get(CompendiumTextKeys.Items.Completion);
            _missingButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.Missing);
            _foundButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.Found);

            if (_researchLabel != null)
                _researchLabel.Text = _localization.Get(CompendiumTextKeys.Items.Research);
            if (_unresearchedButton != null)
                _unresearchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.Unresearched);
            if (_researchedButton != null)
                _researchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.Researched);

            if (_craftingLabel != null)
                _craftingLabel.Text = _localization.Get(CompendiumTextKeys.Items.Crafting);
            if (_hasRecipeButton != null)
            {
                _hasRecipeButton.Text = _localization.Get(CompendiumTextKeys.Items.HasRecipe);
                _hasRecipeButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.HasRecipeTooltip);
            }

            if (_craftableNowButton != null)
            {
                _craftableNowButton.Text = _localization.Get(CompendiumTextKeys.Items.CraftableNow);
                _craftableNowButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.CraftableNowTooltip);
            }

            if (_acquisitionLabel != null)
                _acquisitionLabel.Text = _localization.Get(CompendiumTextKeys.Items.AcquisitionSection);
            if (_npcDropsButton != null)
            {
                _npcDropsButton.Text = _localization.Get(CompendiumTextKeys.Items.NpcDrops);
                _npcDropsButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.NpcDropsTooltip);
            }

            if (_purchasableButton != null)
            {
                _purchasableButton.Text = _localization.Get(CompendiumTextKeys.Items.Purchasable);
                _purchasableButton.TooltipText = _localization.Get(CompendiumTextKeys.Items.PurchasableTooltip);
            }

            _tagsLabel.Text = _localization.Get(CompendiumTextKeys.Items.Tags);
            IReadOnlyList<ItemTaxonomyFacetDefinition> facets = ItemTaxonomyDefinitions.Facets;

            for (var index = 0; index < _facetControls.Length; index++)
            {
                _facetControls[index].Button.TooltipText = _localization.Get(facets[index].DisplayNameKey);
            }

            Recalculate();
        }

        public void ClearFilters()
        {
            if (ActiveFilterCount == 0)
                return;

            _filterModel.CompletionFilter = ChecklistCompletionFilter.All;
            _filterModel.ResearchFilter = ChecklistResearchFilter.All;
            _filterModel.CraftingFilter = ChecklistCraftingFilter.All;
            _filterModel.NpcDropsOnly = false;
            _filterModel.PurchasableOnly = false;
            _filterModel.TaxonomyFacetSelection = ChecklistTaxonomyFacetSelection.None;
            NotifyFiltersChanged();
        }

        private void ToggleCompletionFilter(ChecklistCompletionFilter filter)
        {
            _filterModel.CompletionFilter = _filterModel.CompletionFilter == filter
                ? ChecklistCompletionFilter.All
                : filter;
            NotifyFiltersChanged();
        }

        private void ToggleResearchFilter(ChecklistResearchFilter filter)
        {
            _filterModel.ResearchFilter = _filterModel.ResearchFilter == filter ? ChecklistResearchFilter.All : filter;
            NotifyFiltersChanged();
        }

        private void ToggleCraftingFilter(ChecklistCraftingFilter filter)
        {
            _filterModel.CraftingFilter = _filterModel.CraftingFilter == filter ? ChecklistCraftingFilter.All : filter;
            NotifyFiltersChanged();
        }

        private void ToggleNpcDropsOnly()
        {
            _filterModel.NpcDropsOnly = !_filterModel.NpcDropsOnly;
            NotifyFiltersChanged();
        }

        private void TogglePurchasableOnly()
        {
            _filterModel.PurchasableOnly = !_filterModel.PurchasableOnly;
            NotifyFiltersChanged();
        }

        private void CycleFacet(ItemTaxonomyFacetId facetId)
        {
            _filterModel.TaxonomyFacetSelection = _filterModel.TaxonomyFacetSelection.Cycle(facetId);
            NotifyFiltersChanged();
        }

        private void NotifyFiltersChanged()
        {
            SynchronizeState();
            _filtersChanged?.Invoke();
        }

        private static VanillaIconButton CreateIconFilterButton(
            Action<Rectangle> drawIcon,
            Action onClick,
            string tooltipText)
        {
            return new VanillaIconButton(drawIcon, onClick)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2,
                TooltipText = tooltipText
            };
        }

        private static int LayoutIconGroup(
            TextLabelElement label,
            VanillaIconButton firstButton,
            VanillaIconButton secondButton,
            int left,
            int width,
            int top)
        {
            LayoutElement(label, left, top, width, SectionLabelHeight);

            int buttonsTop = top + SectionLabelHeight + FilterPopupLayout.ContentGap;
            int pairWidth = IconSize * 2 + ControlGap;
            int pairLeft = left + Math.Max(0, (width - pairWidth) / 2);

            LayoutElement(firstButton, pairLeft, buttonsTop, IconSize, IconSize);
            LayoutElement(secondButton, pairLeft + IconSize + ControlGap, buttonsTop, IconSize, IconSize);

            return buttonsTop + IconSize;
        }

        private int CalculateContentHeight(int width)
        {
            int height = SectionLabelHeight + FilterPopupLayout.ContentGap + IconSize;

            if (_hasCraftingFilters)
            {
                height += FilterPopupLayout.SectionGap + SectionLabelHeight + FilterPopupLayout.ContentGap;
                height += ControlHeight * 2 + ControlGap;
            }

            if (_hasNpcDropFilters || _hasMerchantFilters)
            {
                height += FilterPopupLayout.SectionGap + SectionLabelHeight + FilterPopupLayout.ContentGap;

                var acquisitionControlCount = 0;
                if (_hasNpcDropFilters)
                    acquisitionControlCount++;
                if (_hasMerchantFilters)
                    acquisitionControlCount++;

                height += acquisitionControlCount * ControlHeight;
                height += Math.Max(0, acquisitionControlCount - 1) * ControlGap;
            }

            height += FilterPopupLayout.SectionGap + SectionLabelHeight + FilterPopupLayout.ContentGap;

            if (_facetControls.Length > 0)
            {
                int columns = CalculateFacetColumnCount(width);
                int rows = (_facetControls.Length + columns - 1) / columns;
                height += rows * IconSize + Math.Max(0, rows - 1) * IconGap;
            }

            return height;
        }

        private int CalculateFacetColumnCount(int width)
        {
            if (_facetControls.Length == 0)
                return 1;

            int availableColumns = Math.Max(1, (Math.Max(0, width) + IconGap) / (IconSize + IconGap));
            return Math.Min(Math.Min(TagColumns, _facetControls.Length), availableColumns);
        }

        private static int CalculateGridWidth(int columns)
        {
            if (columns <= 0)
                return 0;

            return columns * IconSize + (columns - 1) * IconGap;
        }

        private static int CalculateIconX(int width, int itemCount, int columns, int row, int column)
        {
            int rowStartIndex = row * columns;
            int rowItemCount = Math.Min(columns, itemCount - rowStartIndex);
            int rowWidth = CalculateGridWidth(rowItemCount);
            int rowStartX = Math.Max(0, (width - rowWidth) / 2);
            return rowStartX + column * (IconSize + IconGap);
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

        private readonly struct FacetFilterControl(TaxonomyRefinementIconButton button, ItemTaxonomyFacetId facetId)
        {
            public TaxonomyRefinementIconButton Button { get; } = button;

            public ItemTaxonomyFacetId FacetId { get; } = facetId;
        }
    }

    internal static class ItemFilterPresentation
    {
        internal static int CalculateActiveFilterCount(
            ChecklistCompletionFilter completionFilter,
            bool journeyResearchAvailable,
            ChecklistResearchFilter researchFilter,
            bool craftingFiltersAvailable,
            ChecklistCraftingFilter craftingFilter,
            bool npcDropFiltersAvailable,
            bool npcDropsOnly,
            bool merchantFiltersAvailable,
            bool purchasableOnly,
            ChecklistTaxonomyFacetSelection facetSelection)
        {
            var count = 0;

            if (completionFilter != ChecklistCompletionFilter.All)
                count++;

            if (journeyResearchAvailable && researchFilter != ChecklistResearchFilter.All)
                count++;

            if (craftingFiltersAvailable && craftingFilter != ChecklistCraftingFilter.All)
                count++;

            if (npcDropFiltersAvailable && npcDropsOnly)
                count++;

            if (merchantFiltersAvailable && purchasableOnly)
                count++;

            IReadOnlyList<ItemTaxonomyFacetDefinition> facets = ItemTaxonomyDefinitions.Facets;

            for (var index = 0; index < facets.Count; index++)
            {
                if (facetSelection.GetState(facets[index].Id) != ChecklistTaxonomyFacetState.Off)
                    count++;
            }

            return count;
        }
    }
}