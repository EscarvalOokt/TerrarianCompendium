using System;
using Microsoft.Xna.Framework;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Shimmer;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ShimmerFilterPopup : UIElement, IVanillaPopoverContent
    {
        private const int LabelHeight = 18;
        private const int ControlHeight = 24;
        private const int ControlGap = 2;
        private const int IconSize = 30;
        private const int IconGap = 4;

        private readonly TextLabelElement _completionLabel;
        private readonly VanillaTextButton _decraftButton;
        private readonly Action _filtersChanged;
        private readonly ShimmerFilterState _filterState;
        private readonly VanillaIconButton _foundButton;
        private readonly TextLabelElement _kindLabel;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaTextButton _lockedButton;
        private readonly VanillaIconButton _missingButton;
        private readonly TextLabelElement _progressionLabel;
        private readonly bool _researchAvailable;
        private readonly VanillaIconButton _researchedButton;
        private readonly TextLabelElement _researchLabel;
        private readonly VanillaTextButton _transformButton;
        private readonly VanillaTextButton _unlockedButton;
        private readonly VanillaIconButton _unresearchedButton;
        private long _localizationRevision = -1;

        public ShimmerFilterPopup(
            ShimmerFilterState filterState,
            CompendiumLocalization localization,
            bool researchAvailable,
            Action filtersChanged)
        {
            _filterState = filterState ?? throw new ArgumentNullException(nameof(filterState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _researchAvailable = researchAvailable;
            _filtersChanged = filtersChanged;
            SetPadding(0f);

            _completionLabel = new TextLabelElement(string.Empty);
            Append(_completionLabel);
            _missingButton = CreateIconFilterButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: false),
                () => ToggleCompletion(ChecklistCompletionFilter.Missing),
                string.Empty);
            _foundButton = CreateIconFilterButton(
                bounds => VanillaPresentationIcons.DrawCompletion(bounds, found: true),
                () => ToggleCompletion(ChecklistCompletionFilter.Found),
                string.Empty);
            Append(_missingButton);
            Append(_foundButton);

            if (_researchAvailable)
            {
                _researchLabel = new TextLabelElement(string.Empty);
                _unresearchedButton = CreateIconFilterButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: false),
                    () => ToggleResearch(ChecklistResearchFilter.Unresearched),
                    string.Empty);
                _researchedButton = CreateIconFilterButton(
                    bounds => VanillaPresentationIcons.DrawJourneyResearch(bounds, researched: true),
                    () => ToggleResearch(ChecklistResearchFilter.Researched),
                    string.Empty);
                Append(_researchLabel);
                Append(_unresearchedButton);
                Append(_researchedButton);
            }

            _kindLabel = new TextLabelElement(string.Empty);
            Append(_kindLabel);
            _transformButton = CreateTextFilterButton(string.Empty, () => ToggleKind(ShimmerTransformationKind.Direct));
            _decraftButton = CreateTextFilterButton(string.Empty, () => ToggleKind(ShimmerTransformationKind.Decraft));
            Append(_transformButton);
            Append(_decraftButton);

            _progressionLabel = new TextLabelElement(string.Empty);
            Append(_progressionLabel);
            _unlockedButton = CreateTextFilterButton(
                string.Empty,
                () => ToggleProgression(ShimmerProgressionFilter.Unlocked));
            _lockedButton = CreateTextFilterButton(
                string.Empty,
                () => ToggleProgression(ShimmerProgressionFilter.Locked));
            Append(_unlockedButton);
            Append(_lockedButton);

            SynchronizeLocalization(force: true);
            SynchronizeState();
        }

        public int ActiveFilterCount => _filterState.ActiveFilterCount;

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int iconPairWidth = IconSize * 2 + IconGap;
            int collectionGroupWidth = Math.Max(
                TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Shimmer.FilterCompletion)),
                iconPairWidth);
            int topSectionWidth = collectionGroupWidth;

            if (_researchAvailable)
            {
                int researchGroupWidth = Math.Max(
                    TextUtil.MeasureWidth(_localization.Get(CompendiumTextKeys.Shimmer.FilterResearch)),
                    iconPairWidth);
                int columnWidth = Math.Max(collectionGroupWidth, researchGroupWidth);
                topSectionWidth = columnWidth * 2 + FilterPopupLayout.SectionGap;
            }

            int naturalWidth = Math.Max(
                topSectionWidth,
                Math.Max(
                    MeasurePairWidth(_transformButton.Text, _decraftButton.Text),
                    MeasurePairWidth(_unlockedButton.Text, _lockedButton.Text)));

            naturalWidth = Math.Max(
                naturalWidth,
                Math.Max(TextUtil.MeasureWidth(_kindLabel.Text), TextUtil.MeasureWidth(_progressionLabel.Text)));

            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int collectionSectionHeight = LabelHeight + FilterPopupLayout.ContentGap + IconSize;
            int textSectionHeight = LabelHeight + FilterPopupLayout.ContentGap + ControlHeight;
            int height = collectionSectionHeight +
                         FilterPopupLayout.SectionGap +
                         textSectionHeight +
                         FilterPopupLayout.SectionGap +
                         textSectionHeight;
            return new VanillaPopoverContentSize(width, height);
        }

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            _missingButton.IsActive = _filterState.CompletionFilter == ChecklistCompletionFilter.Missing;
            _foundButton.IsActive = _filterState.CompletionFilter == ChecklistCompletionFilter.Found;

            if (_researchAvailable)
            {
                _unresearchedButton.IsActive = _filterState.ResearchFilter == ChecklistResearchFilter.Unresearched;
                _researchedButton.IsActive = _filterState.ResearchFilter == ChecklistResearchFilter.Researched;
            }

            _transformButton.IsActive = _filterState.Kind == ShimmerTransformationKind.Direct;
            _decraftButton.IsActive = _filterState.Kind == ShimmerTransformationKind.Decraft;
            _unlockedButton.IsActive = _filterState.Progression == ShimmerProgressionFilter.Unlocked;
            _lockedButton.IsActive = _filterState.Progression == ShimmerProgressionFilter.Locked;
        }

        public void ClearFilters()
        {
            if (!_filterState.IsActive)
                return;

            _filterState.Clear();
            NotifyFiltersChanged();
        }

        public override void RecalculateChildren()
        {
            int width = Math.Max(0, (int)GetInnerDimensions().Width);
            var top = 0;

            if (_researchAvailable)
            {
                int columnGap = FilterPopupLayout.SectionGap;
                int collectionWidth = Math.Max(0, (width - columnGap) / 2);
                int researchLeft = collectionWidth + columnGap;
                int researchWidth = Math.Max(0, width - researchLeft);

                int collectionBottom = LayoutIconGroup(
                    _completionLabel,
                    _missingButton,
                    _foundButton,
                    0,
                    collectionWidth,
                    top);
                int researchBottom = LayoutIconGroup(
                    _researchLabel,
                    _unresearchedButton,
                    _researchedButton,
                    researchLeft,
                    researchWidth,
                    top);
                top = Math.Max(collectionBottom, researchBottom);
            }
            else
            {
                top = LayoutIconGroup(_completionLabel, _missingButton, _foundButton, 0, width, top);
            }

            top += FilterPopupLayout.SectionGap;
            LayoutSection(_kindLabel, _transformButton, _decraftButton, ref top, width, addSectionGap: true);
            LayoutSection(_progressionLabel, _unlockedButton, _lockedButton, ref top, width, addSectionGap: false);

            base.RecalculateChildren();
        }

        private void ToggleKind(ShimmerTransformationKind kind)
        {
            _filterState.Kind = _filterState.Kind == kind ? null : kind;
            NotifyFiltersChanged();
        }

        private void ToggleProgression(ShimmerProgressionFilter progression)
        {
            _filterState.Progression = _filterState.Progression == progression ? null : progression;
            NotifyFiltersChanged();
        }

        private void ToggleCompletion(ChecklistCompletionFilter completion)
        {
            _filterState.CompletionFilter = _filterState.CompletionFilter == completion
                ? ChecklistCompletionFilter.All
                : completion;
            NotifyFiltersChanged();
        }

        private void ToggleResearch(ChecklistResearchFilter research)
        {
            _filterState.ResearchFilter = _filterState.ResearchFilter == research
                ? ChecklistResearchFilter.All
                : research;
            NotifyFiltersChanged();
        }

        private void NotifyFiltersChanged()
        {
            SynchronizeState();
            _filtersChanged?.Invoke();
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _completionLabel.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterCompletion);
            _missingButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.FilterMissing);
            _foundButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.FilterFound);

            if (_researchAvailable)
            {
                _researchLabel.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterResearch);
                _unresearchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.FilterUnresearched);
                _researchedButton.TooltipText = _localization.Get(CompendiumTextKeys.Shimmer.FilterResearched);
            }

            _kindLabel.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterKind);
            _transformButton.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterTransform);
            _decraftButton.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterDecraft);
            _progressionLabel.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterProgression);
            _unlockedButton.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterUnlocked);
            _lockedButton.Text = _localization.Get(CompendiumTextKeys.Shimmer.FilterLocked);

            Recalculate();
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
                TooltipText = tooltipText ?? string.Empty
            };
        }

        private static VanillaTextButton CreateTextFilterButton(string text, Action onClick)
        {
            return new VanillaTextButton(text, onClick)
            {
                ActiveBorderColor = UIColors.Success,
                ActiveBorderThickness = 2
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
            Layout(label, left, top, width, LabelHeight);

            int buttonsTop = top + LabelHeight + FilterPopupLayout.ContentGap;
            int pairWidth = IconSize * 2 + IconGap;
            int pairLeft = left + Math.Max(0, (width - pairWidth) / 2);

            Layout(firstButton, pairLeft, buttonsTop, IconSize, IconSize);
            Layout(secondButton, pairLeft + IconSize + IconGap, buttonsTop, IconSize, IconSize);

            return buttonsTop + IconSize;
        }

        private static int MeasurePairWidth(string first, string second)
        {
            int buttonWidth = Math.Max(
                VanillaTextButton.MeasureNaturalWidth(first),
                VanillaTextButton.MeasureNaturalWidth(second));
            long width = buttonWidth * 2L + ControlGap;
            return width >= int.MaxValue ? int.MaxValue : (int)width;
        }

        private static void LayoutSection(
            UIElement label,
            UIElement first,
            UIElement second,
            ref int top,
            int width,
            bool addSectionGap)
        {
            Layout(label, 0, top, width, LabelHeight);
            top += LabelHeight + FilterPopupLayout.ContentGap;
            LayoutButtonPair(first, second, top, width);
            top += ControlHeight;

            if (addSectionGap)
                top += FilterPopupLayout.SectionGap;
        }

        private static void LayoutButtonPair(UIElement first, UIElement second, int top, int width)
        {
            int usableWidth = Math.Max(0, width - ControlGap);
            int firstWidth = usableWidth / 2;
            Layout(first, 0, top, firstWidth, ControlHeight);
            Layout(second, firstWidth + ControlGap, top, Math.Max(0, width - firstWidth - ControlGap), ControlHeight);
        }

        private static void Layout(UIElement element, int left, int top, int width, int height)
        {
            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }
    }
}