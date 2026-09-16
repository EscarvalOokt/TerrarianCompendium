using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaIconValueElement : UIElement
    {
        internal const int IconSize = 20;
        internal const int IconTextGap = 4;
        private const int TextHeight = 16;

        private Action<Rectangle> _drawIcon;
        private string _tooltipText = string.Empty;
        private Func<Color4> _valueColorProvider;
        private string _valueText = string.Empty;

        public void Bind(
            Action<Rectangle> drawIcon,
            string valueText,
            string tooltipText,
            Func<Color4> valueColorProvider = null)
        {
            _drawIcon = drawIcon ?? throw new ArgumentNullException(nameof(drawIcon));
            BindPresentation(valueText, tooltipText, valueColorProvider);
        }

        public void Hide()
        {
            _drawIcon = null;
            _valueText = string.Empty;
            _tooltipText = string.Empty;
            _valueColorProvider = null;
            IgnoresMouseInteraction = true;
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_drawIcon == null)
                return;

            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            int iconSize = Math.Min(IconSize, height);
            int iconY = y + Math.Max(0, (height - iconSize) / 2);

            _drawIcon(new Rectangle(x, iconY, iconSize, iconSize));

            int textX = x + iconSize + IconTextGap;
            int textWidth = Math.Max(0, width - iconSize - IconTextGap);

            if (textWidth > 0 && _valueText.Length > 0)
            {
                string displayText = TruncatedTextPresentation.Truncate(_valueText, textWidth, out _);

                if (displayText.Length > 0)
                {
                    Color4 color = _valueColorProvider?.Invoke() ?? UIColors.Text;
                    UIRenderer.DrawText(displayText, textX, y + Math.Max(0, (height - TextHeight) / 2), color);
                }
            }

            if (IsMouseHovering && _tooltipText.Length > 0)
                Tooltip.Set(_tooltipText);
        }

        private void BindPresentation(string valueText, string tooltipText, Func<Color4> valueColorProvider)
        {
            _valueText = valueText ?? string.Empty;
            _tooltipText = tooltipText ?? string.Empty;
            _valueColorProvider = valueColorProvider;
            IgnoresMouseInteraction = false;
        }
    }

    internal readonly struct VanillaIconValueCellBounds(int x, int y, int width, int height)
    {
        public int X { get; } = x;

        public int Y { get; } = y;

        public int Width { get; } = width;

        public int Height { get; } = height;

        public int Right => X + Width;
    }

    internal static class VanillaIconValueLayout
    {
        public const int RowHeight = 24;
        public const int ColumnGap = 8;
        public const int RowGap = 4;
        public const int MaximumColumns = 2;
        public const int MinimumCellWidth = 96;

        public static int CalculateColumnCount(int itemCount, int availableWidth)
        {
            if (itemCount <= 0 || availableWidth <= 0)
                return 0;

            if (itemCount > 1 && availableWidth >= MinimumCellWidth * 2 + ColumnGap)
                return MaximumColumns;

            return 1;
        }

        public static int CalculateHeight(int itemCount, int availableWidth)
        {
            int columns = CalculateColumnCount(itemCount, availableWidth);

            if (columns <= 0)
                return 0;

            int rows = (itemCount + columns - 1) / columns;
            return rows * RowHeight + Math.Max(0, rows - 1) * RowGap;
        }

        public static VanillaIconValueCellBounds CalculateCellBounds(int index, int itemCount, int availableWidth)
        {
            int columns = CalculateColumnCount(itemCount, availableWidth);

            if (index < 0 || index >= itemCount || columns <= 0)
                return default;

            int row = index / columns;
            int column = index % columns;
            int totalGapWidth = (columns - 1) * ColumnGap;
            int usableWidth = Math.Max(0, availableWidth - totalGapWidth);
            int baseCellWidth = usableWidth / columns;
            int remainder = usableWidth % columns;
            var x = 0;

            for (var currentColumn = 0; currentColumn < column; currentColumn++)
                x += baseCellWidth + (currentColumn < remainder ? 1 : 0) + ColumnGap;

            int width = baseCellWidth + (column < remainder ? 1 : 0);
            int y = row * (RowHeight + RowGap);

            return new VanillaIconValueCellBounds(x, y, Math.Max(0, width), RowHeight);
        }
    }
}