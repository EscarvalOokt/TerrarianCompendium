using System;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class EmptyStateTextLayout
    {
        internal const int HorizontalPadding = 12;
        internal const int LineHeight = 18;
        private const int TextHeight = 16;

        public static int CalculateContentWidth(int width)
        {
            if (width <= 0)
                return 0;

            return Math.Max(1, width - HorizontalPadding * 2);
        }

        public static int CalculateBlockTop(int height, int lineCount)
        {
            if (height <= 0 || lineCount <= 0)
                return 0;

            int blockHeight = lineCount * LineHeight;
            return Math.Max(0, (height - blockHeight) / 2);
        }

        public static int CalculateLineLeft(int width, int lineWidth)
        {
            return Math.Max(0, (Math.Max(0, width) - Math.Max(0, lineWidth)) / 2);
        }

        public static int CalculateLineTop(int blockTop, int lineIndex)
        {
            if (lineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(lineIndex));

            return Math.Max(0, blockTop) + lineIndex * LineHeight + Math.Max(0, (LineHeight - TextHeight) / 2);
        }
    }
}