using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.Localization;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class CompendiumSearchBar : UIElement
    {
        private const float SearchTextScale = 0.8f;
        private const int SearchTextHeight = 16;
        private const int HorizontalPadding = 6;
        private const int ClearButtonSize = 20;
        private const int ClearButtonInset = 2;
        private const int ClearButtonGap = 2;

        private readonly VanillaTextButton _clearButton;
        private readonly Action _goBackFromVirtualKeyboard;
        private readonly CompendiumLocalization _localization;
        private readonly UISearchBar _searchBar;
        private long _localizationRevision = -1;
        private string _placeholderText = string.Empty;
        private string _searchString = string.Empty;

        private bool _showClearButton;

        public CompendiumSearchBar(Action goBackFromVirtualKeyboard, CompendiumLocalization localization)
        {
            _goBackFromVirtualKeyboard = goBackFromVirtualKeyboard ??
                                         throw new ArgumentNullException(nameof(goBackFromVirtualKeyboard));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            SetPadding(0f);
            OnLeftClick += OnSearchAreaClicked;
            OnRightClick += OnSearchAreaRightClicked;

            _searchBar = new UISearchBar(LocalizedText.Empty, SearchTextScale)
            {
                IgnoresMouseInteraction = true
            };
            _searchBar.OnContentsChanged += OnContentsChanged;
            _searchBar.OnNeedingVirtualKeyboard += OpenVirtualKeyboardWhenNeeded;
            Append(_searchBar);

            _clearButton = new VanillaTextButton("×", ClearSearch)
            {
                IgnoresMouseInteraction = true
            };
            Append(_clearButton);

            SynchronizeLocalization(force: true);
            SetContents(null, true);
        }

        public bool HasContents => _searchBar.HasContents;

        public bool IsWritingText => _searchBar.IsWritingText;

        public int MaxInputLength
        {
            get => _searchBar.MaxInputLength;
            set => _searchBar.MaxInputLength = value;
        }

        public event Action<string> OnSearchContentsChanged;

        public void SetContents(string contents, bool forced = false)
        {
            _searchBar.SetContents(contents, forced);
        }

        public void ToggleTakingText()
        {
            _searchBar.ToggleTakingText();
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            int clearSize = _showClearButton
                ? Math.Max(0, Math.Min(ClearButtonSize, height - ClearButtonInset * 2))
                : 0;
            int searchRight = clearSize > 0
                ? Math.Max(HorizontalPadding, width - ClearButtonInset - clearSize - ClearButtonGap)
                : Math.Max(HorizontalPadding, width - HorizontalPadding);
            int searchWidth = Math.Max(0, searchRight - HorizontalPadding);

            _searchBar.Left.Set(HorizontalPadding, 0f);
            _searchBar.Top.Set(0f, 0f);
            _searchBar.Width.Set(searchWidth, 0f);
            _searchBar.Height.Set(height, 0f);

            _clearButton.Left.Set(Math.Max(0, width - ClearButtonInset - clearSize), 0f);
            _clearButton.Top.Set(Math.Max(0, (height - clearSize) / 2), 0f);
            _clearButton.Width.Set(clearSize, 0f);
            _clearButton.Height.Set(clearSize, 0f);

            base.RecalculateChildren();
        }

        public override void Update(GameTime gameTime)
        {
            if (_localizationRevision != _localization.Revision)
                SynchronizeLocalization(force: true);

            base.Update(gameTime);

            if (IsWritingText && (Main.mouseLeft || Main.mouseRight) && !IsMouseHovering)
                ToggleTakingText();
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

            UIRenderer.DrawRect(x, y, width, height, UIColors.SectionBg);
            UIRenderer.DrawRectOutline(x, y, width, height, IsWritingText ? UIColors.Accent : UIColors.Border);

            if (HasContents || IsWritingText || _placeholderText.Length == 0)
                return;

            int availableTextWidth = Math.Max(0, width - HorizontalPadding * 2);
            string display = TextUtil.Truncate(_placeholderText, availableTextWidth);

            if (display.Length == 0)
                return;

            int textY = y + Math.Max(0, (height - SearchTextHeight) / 2);
            UIRenderer.DrawText(display, x + HorizontalPadding, textY, UIColors.TextDim);
        }

        private void OnContentsChanged(string contents)
        {
            _searchString = contents ?? string.Empty;
            SynchronizeClearButton();
            OnSearchContentsChanged?.Invoke(_searchString);
        }

        private void SynchronizeClearButton()
        {
            bool shouldShow = HasContents;

            if (_showClearButton == shouldShow)
                return;

            _showClearButton = shouldShow;
            _clearButton.IgnoresMouseInteraction = !shouldShow;
            Recalculate();
        }

        private void ClearSearch()
        {
            SetContents(null, true);
        }

        private void OnSearchAreaClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (ReferenceEquals(evt.Target, _clearButton))
                return;

            ToggleTakingText();
        }

        private void OnSearchAreaRightClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            SetContents(null, true);

            if (!IsWritingText)
                ToggleTakingText();
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _placeholderText = _localization.Get(CompendiumTextKeys.Common.SearchPlaceholder);
        }

        private void OpenVirtualKeyboardWhenNeeded()
        {
            var keyboard = new UIVirtualKeyboard(
                _placeholderText,
                _searchString,
                SubmitVirtualText,
                GoBackFromVirtualKeyboard,
                0,
                true,
                MaxInputLength);

            UserInterface.ActiveInstance.SetState(keyboard);
        }

        private void SubmitVirtualText(string text)
        {
            SetContents(text.Trim());
            GoBackFromVirtualKeyboard();
        }

        private void GoBackFromVirtualKeyboard()
        {
            if (_searchBar.IsWritingText)
                _searchBar.ToggleTakingText();

            _goBackFromVirtualKeyboard();
        }
    }
}