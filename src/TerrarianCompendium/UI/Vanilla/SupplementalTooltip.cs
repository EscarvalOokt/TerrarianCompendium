using System;
using System.Collections.Generic;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class SupplementalTooltip
    {
        private const int CursorOffset = 16;
        private const int EdgePadding = 4;
        private const int LineHeight = 16;
        private const int Padding = 8;
        private const int TooltipGap = 4;

        private static readonly List<Entry> _entries = new();
        private static int _primaryItemTooltipId;

        public static void Clear()
        {
            _entries.Clear();
            _primaryItemTooltipId = 0;
        }

        internal static void RegisterPrimaryItemTooltip(int itemId)
        {
            _primaryItemTooltipId = Math.Max(0, itemId);
        }

        public static void RegisterLocalized(
            string localizationKey,
            Color4 color,
            string prefix = null,
            string suffix = null)
        {
            if (string.IsNullOrWhiteSpace(localizationKey))
                throw new ArgumentException("Localization key must not be empty.", nameof(localizationKey));

            _entries.Add(Entry.FromText(Line.FromLocalization(localizationKey, prefix, suffix, color)));
        }

        public static void RegisterText(string text, Color4 color)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Tooltip text must not be empty.", nameof(text));

            _entries.Add(Entry.FromText(Line.FromText(text, color)));
        }

        public static void RegisterVisualRow(IEnumerable<VisualToken> tokens, int gap)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));
            if (gap < 0)
                throw new ArgumentOutOfRangeException(nameof(gap));

            var snapshot = new List<VisualToken>();

            foreach (VisualToken token in tokens)
                snapshot.Add(token);

            if (snapshot.Count == 0)
                throw new ArgumentException("Visual row must contain at least one token.", nameof(tokens));

            _entries.Add(Entry.FromVisual(snapshot.ToArray(), gap));
        }

        public static void DrawDeferred(CompendiumLocalization localization)
        {
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            if (_entries.Count == 0)
                return;

            try
            {
                int screenWidth = Math.Max(EdgePadding * 2 + 1, WidgetInput.ScreenWidth);
                int screenHeight = Math.Max(EdgePadding * 2 + 1, WidgetInput.ScreenHeight);
                int maxTooltipWidth = Math.Max(1, screenWidth - EdgePadding * 2);
                int maxContentWidth = Math.Max(1, maxTooltipWidth - Padding * 2);
                var resolvedRows = new List<ResolvedRow>(_entries.Count);
                var contentWidth = 0;
                var contentHeight = 0;

                for (var entryIndex = 0; entryIndex < _entries.Count; entryIndex++)
                {
                    Entry entry = _entries[entryIndex];

                    if (entry.Kind == EntryKind.Text)
                    {
                        string text = entry.TextLine.Resolve(localization);

                        if (text.Length == 0)
                            continue;

                        IReadOnlyList<string> wrappedLines = WrapText(text, maxContentWidth, UIRenderer.MeasureText);

                        for (var wrappedIndex = 0; wrappedIndex < wrappedLines.Count; wrappedIndex++)
                        {
                            string wrappedText = wrappedLines[wrappedIndex];
                            int width = UIRenderer.MeasureText(wrappedText);
                            resolvedRows.Add(ResolvedRow.FromText(wrappedText, entry.TextLine.Color, width));
                            contentWidth = Math.Max(contentWidth, width);
                            contentHeight += LineHeight;
                        }

                        continue;
                    }

                    IReadOnlyList<VisualRowLayout> visualRows = CalculateVisualRows(
                        entry.VisualTokens,
                        maxContentWidth,
                        entry.VisualGap);

                    for (var visualIndex = 0; visualIndex < visualRows.Count; visualIndex++)
                    {
                        VisualRowLayout layout = visualRows[visualIndex];
                        resolvedRows.Add(
                            ResolvedRow.FromVisual(entry.VisualTokens, layout, entry.VisualGap, maxContentWidth));
                        contentWidth = Math.Max(contentWidth, layout.Width);
                        contentHeight += layout.Height;
                    }
                }

                if (resolvedRows.Count == 0)
                    return;

                int tooltipWidth = Math.Min(contentWidth + Padding * 2, maxTooltipWidth);
                int tooltipHeight = contentHeight + Padding * 2;
                int mouseX = WidgetInput.MouseX;
                int mouseY = WidgetInput.MouseY;

                tooltipHeight = Math.Min(tooltipHeight, Math.Max(1, screenHeight - EdgePadding * 2));

                SupplementalTooltipPosition position;

                if (_primaryItemTooltipId > 0)
                {
                    if (!ItemTooltipGeometryEstimator.TryEstimateBounds(
                            _primaryItemTooltipId,
                            screenWidth,
                            screenHeight,
                            mouseX,
                            mouseY,
                            out SupplementalTooltipBounds primaryBounds))
                    {
                        primaryBounds = ItemTooltipGeometryEstimator.EstimateConservativeBounds(
                            screenWidth,
                            screenHeight,
                            mouseX,
                            mouseY);
                    }

                    position = SupplementalTooltipPlacement.Resolve(
                        screenWidth,
                        screenHeight,
                        mouseX,
                        mouseY,
                        tooltipWidth,
                        tooltipHeight,
                        primaryBounds,
                        EdgePadding,
                        TooltipGap);
                }
                else
                {
                    position = ResolveWithoutPrimary(
                        screenWidth,
                        screenHeight,
                        mouseX,
                        mouseY,
                        tooltipWidth,
                        tooltipHeight);
                }

                int x = position.X;
                int y = position.Y;

                UIRenderer.DrawRect(x, y, tooltipWidth, tooltipHeight, UIColors.PanelBg);
                UIRenderer.DrawRectOutline(x, y, tooltipWidth, tooltipHeight, UIColors.Border);

                int rowY = y + Padding;
                int contentX = x + Padding;

                for (var index = 0; index < resolvedRows.Count; index++)
                {
                    ResolvedRow row = resolvedRows[index];

                    if (row.Kind == EntryKind.Text)
                    {
                        UIRenderer.DrawText(row.Text, contentX, rowY, row.Color);
                        rowY += LineHeight;
                        continue;
                    }

                    int cursorX = contentX;

                    for (var tokenIndex = 0; tokenIndex < row.VisualLayout.Count; tokenIndex++)
                    {
                        VisualToken token = row.VisualTokens[row.VisualLayout.StartIndex + tokenIndex];
                        int tokenWidth = Math.Min(token.Width, row.MaxContentWidth);
                        int tokenY = rowY + Math.Max(0, (row.VisualLayout.Height - token.Height) / 2);
                        token.Draw(cursorX, tokenY, tokenWidth, token.Height);
                        cursorX += tokenWidth;

                        if (tokenIndex + 1 < row.VisualLayout.Count)
                            cursorX += row.VisualGap;
                    }

                    rowY += row.VisualLayout.Height;
                }
            }
            finally
            {
                _entries.Clear();
                _primaryItemTooltipId = 0;
            }
        }

        private static SupplementalTooltipPosition ResolveWithoutPrimary(
            int screenWidth,
            int screenHeight,
            int mouseX,
            int mouseY,
            int tooltipWidth,
            int tooltipHeight)
        {
            int x = mouseX + CursorOffset;

            if (x + tooltipWidth > screenWidth - EdgePadding)
                x = mouseX - tooltipWidth - EdgePadding;

            int y = mouseY - tooltipHeight - TooltipGap;

            if (y < EdgePadding)
            {
                y = mouseY + CursorOffset;

                if (y + tooltipHeight > screenHeight - EdgePadding)
                    y = screenHeight - tooltipHeight - EdgePadding;
            }

            x = Math.Max(EdgePadding, Math.Min(x, screenWidth - tooltipWidth - EdgePadding));
            y = Math.Max(EdgePadding, Math.Min(y, screenHeight - tooltipHeight - EdgePadding));

            return new SupplementalTooltipPosition(x, y, SupplementalTooltipPlacementKind.MinimumOverlap);
        }

        internal static IReadOnlyList<string> WrapText(string text, int maxWidth, Func<string, int> measureText)
        {
            if (measureText == null)
                throw new ArgumentNullException(nameof(measureText));

            if (maxWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWidth));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<string>();

            string[] words = text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            var result = new List<string>();
            string current = string.Empty;

            for (var index = 0; index < words.Length; index++)
            {
                string word = words[index];

                if (current.Length == 0)
                {
                    current = StartLine(word, maxWidth, measureText, result);
                    continue;
                }

                string candidate = current + " " + word;

                if (measureText(candidate) <= maxWidth)
                {
                    current = candidate;
                    continue;
                }

                result.Add(current);
                current = StartLine(word, maxWidth, measureText, result);
            }

            if (current.Length > 0)
                result.Add(current);

            return result;
        }

        internal static IReadOnlyList<VisualRowLayout> CalculateVisualRows(
            IReadOnlyList<VisualToken> tokens,
            int maxWidth,
            int gap)
        {
            if (tokens == null)
                throw new ArgumentNullException(nameof(tokens));
            if (maxWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxWidth));
            if (gap < 0)
                throw new ArgumentOutOfRangeException(nameof(gap));
            if (tokens.Count == 0)
                return Array.Empty<VisualRowLayout>();

            var rows = new List<VisualRowLayout>();
            var rowStart = 0;
            var rowCount = 0;
            var rowWidth = 0;
            var rowHeight = 0;

            for (var index = 0; index < tokens.Count; index++)
            {
                VisualToken token = tokens[index];
                int tokenWidth = Math.Min(token.Width, maxWidth);
                int requiredWidth = rowCount == 0 ? tokenWidth : rowWidth + gap + tokenWidth;

                if (rowCount > 0 && requiredWidth > maxWidth)
                {
                    rows.Add(new VisualRowLayout(rowStart, rowCount, rowWidth, rowHeight));
                    rowStart = index;
                    rowCount = 0;
                    rowWidth = 0;
                    rowHeight = 0;
                }

                if (rowCount > 0)
                    rowWidth += gap;

                rowWidth += tokenWidth;
                rowHeight = Math.Max(rowHeight, token.Height);
                rowCount++;
            }

            if (rowCount > 0)
                rows.Add(new VisualRowLayout(rowStart, rowCount, rowWidth, rowHeight));

            return rows;
        }

        private static string StartLine(string word, int maxWidth, Func<string, int> measureText, List<string> result)
        {
            if (measureText(word) <= maxWidth)
                return word;

            var offset = 0;
            string remainder = string.Empty;

            while (offset < word.Length)
            {
                int length = FindFittingPrefixLength(word, offset, maxWidth, measureText);
                string fragment = word.Substring(offset, length);
                offset += length;

                if (offset < word.Length)
                    result.Add(fragment);
                else
                    remainder = fragment;
            }

            return remainder;
        }

        private static int FindFittingPrefixLength(
            string value,
            int offset,
            int maxWidth,
            Func<string, int> measureText)
        {
            var low = 1;
            int high = value.Length - offset;
            var best = 1;

            while (low <= high)
            {
                int middle = low + (high - low) / 2;
                string candidate = value.Substring(offset, middle);

                if (measureText(candidate) <= maxWidth)
                {
                    best = middle;
                    low = middle + 1;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return best;
        }

        internal readonly struct VisualToken
        {
            public VisualToken(int width, int height, Action<int, int, int, int> draw)
            {
                if (width <= 0)
                    throw new ArgumentOutOfRangeException(nameof(width));
                if (height <= 0)
                    throw new ArgumentOutOfRangeException(nameof(height));

                Width = width;
                Height = height;
                Draw = draw ?? throw new ArgumentNullException(nameof(draw));
            }

            public int Width { get; }

            public int Height { get; }

            public Action<int, int, int, int> Draw { get; }
        }

        internal readonly struct VisualRowLayout(int startIndex, int count, int width, int height)
        {
            public int StartIndex { get; } = startIndex;

            public int Count { get; } = count;

            public int Width { get; } = width;

            public int Height { get; } = height;
        }

        private enum EntryKind
        {
            Text,
            Visual
        }

        private readonly struct Entry
        {
            private Entry(EntryKind kind, Line textLine, VisualToken[] visualTokens, int visualGap)
            {
                Kind = kind;
                TextLine = textLine;
                VisualTokens = visualTokens;
                VisualGap = visualGap;
            }

            public EntryKind Kind { get; }

            public Line TextLine { get; }

            public VisualToken[] VisualTokens { get; }

            public int VisualGap { get; }

            public static Entry FromText(Line line)
            {
                return new Entry(EntryKind.Text, line, null, 0);
            }

            public static Entry FromVisual(VisualToken[] tokens, int gap)
            {
                return new Entry(EntryKind.Visual, default, tokens, gap);
            }
        }

        private readonly struct Line
        {
            private Line(string localizationKey, string text, string prefix, string suffix, Color4 color)
            {
                LocalizationKey = localizationKey;
                Text = text;
                Prefix = prefix;
                Suffix = suffix;
                Color = color;
            }

            public string LocalizationKey { get; }

            public string Text { get; }

            public string Prefix { get; }

            public string Suffix { get; }

            public Color4 Color { get; }

            public static Line FromLocalization(string localizationKey, string prefix, string suffix, Color4 color)
            {
                return new Line(localizationKey, null, prefix, suffix, color);
            }

            public static Line FromText(string text, Color4 color)
            {
                return new Line(null, text, null, null, color);
            }

            public string Resolve(CompendiumLocalization localization)
            {
                if (Text != null)
                    return Text;

                return (Prefix ?? string.Empty) + localization.Get(LocalizationKey) + (Suffix ?? string.Empty);
            }
        }

        private readonly struct ResolvedRow
        {
            private ResolvedRow(
                EntryKind kind,
                string text,
                Color4 color,
                VisualToken[] visualTokens,
                VisualRowLayout visualLayout,
                int visualGap,
                int maxContentWidth)
            {
                Kind = kind;
                Text = text;
                Color = color;
                VisualTokens = visualTokens;
                VisualLayout = visualLayout;
                VisualGap = visualGap;
                MaxContentWidth = maxContentWidth;
            }

            public EntryKind Kind { get; }

            public string Text { get; }

            public Color4 Color { get; }

            public VisualToken[] VisualTokens { get; }

            public VisualRowLayout VisualLayout { get; }

            public int VisualGap { get; }

            public int MaxContentWidth { get; }

            public static ResolvedRow FromText(string text, Color4 color, int width)
            {
                return new ResolvedRow(
                    EntryKind.Text,
                    text,
                    color,
                    null,
                    new VisualRowLayout(0, 0, width, LineHeight),
                    0,
                    0);
            }

            public static ResolvedRow FromVisual(
                VisualToken[] tokens,
                VisualRowLayout layout,
                int gap,
                int maxContentWidth)
            {
                return new ResolvedRow(EntryKind.Visual, null, default, tokens, layout, gap, maxContentWidth);
            }
        }
    }
}