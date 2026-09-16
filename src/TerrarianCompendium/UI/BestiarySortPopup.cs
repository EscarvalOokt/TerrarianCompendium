using System;
using Terraria.UI;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class BestiarySortPopup : UIElement, IVanillaPopoverContent
    {
        private const int ControlHeight = 24;
        private const int ControlGap = 2;

        private static readonly BestiarySortMode[] _modes =
        [
            BestiarySortMode.BestiaryOrder,
            BestiarySortMode.Name,
            BestiarySortMode.Rarity,
            BestiarySortMode.Attack,
            BestiarySortMode.Defense,
            BestiarySortMode.Coins,
            BestiarySortMode.HitPoints,
            BestiarySortMode.NpcId
        ];

        private readonly VanillaTextButton[] _buttons;
        private readonly CompendiumLocalization _localization;
        private readonly BestiaryBrowserModel _model;
        private readonly Action _sortChanged;
        private long _localizationRevision = -1;

        public BestiarySortPopup(BestiaryBrowserModel model, CompendiumLocalization localization, Action sortChanged)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _sortChanged = sortChanged;
            SetPadding(0f);

            _buttons = new VanillaTextButton[_modes.Length];

            for (var index = 0; index < _modes.Length; index++)
            {
                BestiarySortMode mode = _modes[index];
                var button = new VanillaTextButton(string.Empty, () => SelectSortMode(mode));

                _buttons[index] = button;
                Append(button);
            }

            SynchronizeState();
        }

        public string ButtonText =>
            _localization.Format(CompendiumTextKeys.Common.SortButton, GetSortModeName(_model.SortMode, _localization));

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

        public void SynchronizeState()
        {
            SynchronizeLocalization();
            for (var index = 0; index < _buttons.Length; index++)
                _buttons[index].IsActive = _modes[index] == _model.SortMode;
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

        internal static string GetSortModeName(BestiarySortMode sortMode, CompendiumLocalization localization)
        {
            switch (sortMode)
            {
                case BestiarySortMode.BestiaryOrder:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortBestiary);
                case BestiarySortMode.Name:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortName);
                case BestiarySortMode.Rarity:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortRarity);
                case BestiarySortMode.Attack:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortAttack);
                case BestiarySortMode.Defense:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortDefense);
                case BestiarySortMode.Coins:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortCoins);
                case BestiarySortMode.HitPoints:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortHp);
                case BestiarySortMode.NpcId:
                    return localization.Get(CompendiumTextKeys.Bestiary.SortNpcId);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(sortMode),
                        sortMode,
                        "Unsupported Bestiary sort mode.");
            }
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            for (var index = 0; index < _buttons.Length; index++)
            {
                string name = GetSortModeName(_modes[index], _localization);
                _buttons[index].Text = name;
                _buttons[index].TooltipText = _localization.Format(CompendiumTextKeys.Common.SortBy, name);
            }

            Recalculate();
        }

        private void SelectSortMode(BestiarySortMode mode)
        {
            if (_model.SortMode != mode)
                _model.SortMode = mode;

            SynchronizeState();
            _sortChanged?.Invoke();
        }
    }
}