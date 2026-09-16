using System;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class VirtualGridLayout
    {
        public const int CellSize = 30;
        public const int CellGap = 2;
        public const int SlotSize = CellSize + CellGap;
        public const int IconPadding = 3;
        public const int IconSize = CellSize - IconPadding * 2;

        public static int CalculateColumnCount(int contentWidth)
        {
            return Math.Max(1, Math.Max(1, contentWidth) / SlotSize);
        }

        public static int CalculateRowCount(int entryCount, int columns)
        {
            if (entryCount <= 0)
                return 0;

            return (entryCount + Math.Max(1, columns) - 1) / Math.Max(1, columns);
        }

        public static int CalculateContentHeight(int entryCount, int columns, int viewportHeight)
        {
            int rowCount = CalculateRowCount(entryCount, columns);
            return Math.Max(Math.Max(0, viewportHeight), rowCount * SlotSize);
        }

        public static int CalculateStartRow(int scrollOffset)
        {
            return Math.Max(0, scrollOffset) / SlotSize;
        }

        public static int CalculateVisibleRowCount(int viewportHeight)
        {
            return Math.Max(1, Math.Max(0, viewportHeight) / SlotSize + 2);
        }
    }
}