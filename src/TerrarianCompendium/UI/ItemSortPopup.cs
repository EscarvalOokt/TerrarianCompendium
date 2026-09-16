using System;
using System.Collections.Generic;
using Terraria.UI;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemSortPopup : UIElement, IVanillaPopoverContent
    {
        private const int ControlHeight = 24;
        private const int ControlGap = 2;

        private readonly ChecklistFilterModel _filterModel;
        private readonly CompendiumLocalization _localization;
        private readonly Action _sortChanged;
        private VanillaTextButton[] _buttons = Array.Empty<VanillaTextButton>();
        private long _localizationRevision = -1;
        private ChecklistSortMode[] _modes = Array.Empty<ChecklistSortMode>();

        public ItemSortPopup(ChecklistFilterModel filterModel, CompendiumLocalization localization, Action sortChanged)
        {
            _filterModel = filterModel ?? throw new ArgumentNullException(nameof(filterModel));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sortChanged = sortChanged;
            SetPadding(0f);

            SynchronizeState();
        }

        public string ButtonText =>
            _localization.Format(
                CompendiumTextKeys.Common.SortButton,
                GetSortModeName(_filterModel.SortMode, _localization));

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            var naturalWidth = 0;

            for (var index = 0; index < _modes.Length; index++)
            {
                naturalWidth = Math.Max(
                    naturalWidth,
                    VanillaTextButton.MeasureNaturalWidth(GetSortModeName(_modes[index], _localization)));
            }

            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int height = _buttons.Length == 0
                ? 0
                : _buttons.Length * ControlHeight + (_buttons.Length - 1) * ControlGap;
            return new VanillaPopoverContentSize(width, height);
        }

        public bool SynchronizeState()
        {
            IReadOnlyList<ChecklistSortMode> availableModes = _filterModel.AvailableSortModes;
            bool localizationChanged = _localizationRevision != _localization.Revision;
            bool optionsChanged = !MatchesCurrentOptions(availableModes);

            if (optionsChanged || localizationChanged)
            {
                _localizationRevision = _localization.Revision;
                RebuildOptions(availableModes);
            }

            ChecklistSortMode selectedMode = _filterModel.SortMode;

            for (var index = 0; index < _buttons.Length; index++)
                _buttons[index].IsActive = _modes[index] == selectedMode;

            return optionsChanged;
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            var top = 0;

            for (var index = 0; index < _buttons.Length; index++)
            {
                VanillaTextButton button = _buttons[index];
                button.Left.Set(0f, 0f);
                button.Top.Set(top, 0f);
                button.Width.Set(width, 0f);
                button.Height.Set(ControlHeight, 0f);
                top += ControlHeight + ControlGap;
            }

            base.RecalculateChildren();
        }

        internal static string GetSortModeName(ChecklistSortMode sortMode, CompendiumLocalization localization)
        {
            switch (sortMode)
            {
                case ChecklistSortMode.Native:
                    return localization.Get(CompendiumTextKeys.Items.SortNative);
                case ChecklistSortMode.ItemId:
                    return localization.Get(CompendiumTextKeys.Items.SortItemId);
                case ChecklistSortMode.Name:
                    return localization.Get(CompendiumTextKeys.Items.SortName);
                case ChecklistSortMode.Value:
                    return localization.Get(CompendiumTextKeys.Items.SortValue);
                case ChecklistSortMode.Rarity:
                    return localization.Get(CompendiumTextKeys.Items.SortRarity);
                case ChecklistSortMode.Damage:
                    return localization.Get(CompendiumTextKeys.Items.SortDamage);
                case ChecklistSortMode.Defense:
                    return localization.Get(CompendiumTextKeys.Items.SortDefense);
                case ChecklistSortMode.PickPower:
                    return localization.Get(CompendiumTextKeys.Items.SortPickPower);
                case ChecklistSortMode.AxePower:
                    return localization.Get(CompendiumTextKeys.Items.SortAxePower);
                case ChecklistSortMode.HammerPower:
                    return localization.Get(CompendiumTextKeys.Items.SortHammerPower);
                case ChecklistSortMode.FishingPower:
                    return localization.Get(CompendiumTextKeys.Items.SortFishingPower);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sortMode),
                        sortMode,
                        "Unsupported checklist sort mode.");
            }
        }

        private bool MatchesCurrentOptions(IReadOnlyList<ChecklistSortMode> availableModes)
        {
            if (availableModes == null || availableModes.Count != _modes.Length)
                return false;

            for (var index = 0; index < availableModes.Count; index++)
            {
                if (availableModes[index] != _modes[index])
                    return false;
            }

            return true;
        }

        private void RebuildOptions(IReadOnlyList<ChecklistSortMode> availableModes)
        {
            if (availableModes == null)
                throw new ArgumentNullException(nameof(availableModes));

            int count = availableModes.Count;
            _modes = new ChecklistSortMode[count];
            _buttons = new VanillaTextButton[count];
            RemoveAllChildren();

            for (var index = 0; index < count; index++)
            {
                ChecklistSortMode mode = availableModes[index];
                _modes[index] = mode;

                var button = new VanillaTextButton(GetSortModeName(mode, _localization), () => SelectSortMode(mode))
                {
                    TooltipText = _localization.Format(
                        CompendiumTextKeys.Common.SortBy,
                        GetSortModeName(mode, _localization))
                };

                _buttons[index] = button;
                Append(button);
            }
        }

        private void SelectSortMode(ChecklistSortMode mode)
        {
            if (_filterModel.SortMode != mode)
                _filterModel.SortMode = mode;

            SynchronizeState();
            _sortChanged?.Invoke();
        }
    }
}