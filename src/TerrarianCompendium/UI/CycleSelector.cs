using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal static class CycleSelectorIndex
    {
        internal static int GetPreviousIndex(int activeIndex, int optionCount)
        {
            ValidateIndex(activeIndex, optionCount);
            return activeIndex == 0 ? optionCount - 1 : activeIndex - 1;
        }

        internal static int GetNextIndex(int activeIndex, int optionCount)
        {
            ValidateIndex(activeIndex, optionCount);
            return activeIndex == optionCount - 1 ? 0 : activeIndex + 1;
        }

        internal static void ValidateIndex(int activeIndex, int optionCount)
        {
            if (optionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(optionCount),
                    optionCount,
                    "Option count must be positive.");
            }

            if (activeIndex < 0 || activeIndex >= optionCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(activeIndex),
                    activeIndex,
                    "Active option index is outside the available range.");
            }
        }
    }

    internal sealed class CycleSelector : UIElement
    {
        private const int Gap = 2;
        private const int HorizontalPadding = 6;

        private readonly VanillaTextButton _nextButton;
        private readonly VanillaTextButton _previousButton;
        private int _activeIndex;
        private string[] _options;

        public CycleSelector(string[] options, int activeIndex, Action<int> selectionChanged = null)
        {
            Validate(options, activeIndex);
            _options = CopyOptions(options);
            _activeIndex = activeIndex;
            SelectionChanged = selectionChanged;
            SetPadding(0f);

            _previousButton = new VanillaTextButton("<", SelectPrevious);
            Append(_previousButton);

            _nextButton = new VanillaTextButton(">", SelectNext);
            Append(_nextButton);
        }

        public Action<int> SelectionChanged { get; set; }

        public void SetOptions(string[] options, int activeIndex)
        {
            Validate(options, activeIndex);
            _options = CopyOptions(options);
            _activeIndex = activeIndex;
            Recalculate();
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            int arrowWidth = Math.Min(height, Math.Max(18, (width - Gap * 2) / 4));
            int valueWidth = Math.Max(0, width - arrowWidth * 2 - Gap * 2);

            _previousButton.Left.Set(0f, 0f);
            _previousButton.Top.Set(0f, 0f);
            _previousButton.Width.Set(arrowWidth, 0f);
            _previousButton.Height.Set(height, 0f);

            _nextButton.Left.Set(arrowWidth + Gap + valueWidth + Gap, 0f);
            _nextButton.Top.Set(0f, 0f);
            _nextButton.Width.Set(arrowWidth, 0f);
            _nextButton.Height.Set(height, 0f);

            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            int arrowWidth = Math.Min(height, Math.Max(18, (width - Gap * 2) / 4));
            int valueX = x + arrowWidth + Gap;
            int valueWidth = width - arrowWidth * 2 - Gap * 2;

            if (valueWidth <= 0)
                return;

            UIRenderer.DrawRect(valueX, y, valueWidth, height, UIColors.SectionBg);
            UIRenderer.DrawRectOutline(valueX, y, valueWidth, height, UIColors.Border);

            int availableWidth = Math.Max(0, valueWidth - HorizontalPadding * 2);
            string optionText = _options[_activeIndex];
            string display = TruncatedTextPresentation.Truncate(optionText, availableWidth, out bool wasTruncated);
            int textWidth = TextUtil.MeasureWidth(display);
            int textX = valueX + Math.Max(HorizontalPadding, (valueWidth - textWidth) / 2);
            int textY = y + Math.Max(0, (height - 16) / 2);

            UIRenderer.DrawText(display, textX, textY, UIColors.Text);
            TruncatedTextPresentation.ShowTooltipIfTruncated(
                optionText,
                wasTruncated,
                valueX,
                y,
                valueWidth,
                height,
                IsMouseHovering);
        }

        private void SelectPrevious()
        {
            SetSelectedIndex(CycleSelectorIndex.GetPreviousIndex(_activeIndex, _options.Length));
        }

        private void SelectNext()
        {
            SetSelectedIndex(CycleSelectorIndex.GetNextIndex(_activeIndex, _options.Length));
        }

        private void SetSelectedIndex(int selectedIndex)
        {
            if (_activeIndex == selectedIndex)
                return;

            _activeIndex = selectedIndex;
            SelectionChanged?.Invoke(selectedIndex);
        }

        private static string[] CopyOptions(string[] options)
        {
            var copy = new string[options.Length];
            Array.Copy(options, copy, options.Length);
            return copy;
        }

        private static void Validate(string[] options, int activeIndex)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            CycleSelectorIndex.ValidateIndex(activeIndex, options.Length);
        }
    }
}