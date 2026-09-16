using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class MerchantStockConditionsPopoverContent : UIElement, IVanillaPopoverContent
    {
        private const int PreferredIconColumns = 8;
        private const int LabelHeight = 18;
        private const int SectionGap = 8;
        private const int ContentGap = 4;
        private const int ItemIconSize = 30;
        private const int ItemIconGap = 2;
        private const int ConditionalEntryGap = 6;
        private const int ConditionalItemTextGap = 5;
        private const int ConditionalIndent = 34;

        private readonly List<VanillaItemIcon> _alwaysAvailableIcons = new();
        private readonly TextLabelElement _alwaysAvailableLabel;
        private readonly List<ConditionalStockEntryElement> _conditionalEntries = new();
        private readonly TextLabelElement _conditionalLabel;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaScrollRegion _scroll;
        private long _localizationRevision = -1;
        private IReadOnlyList<NpcDetailsMerchantStockEntry> _stock = Array.Empty<NpcDetailsMerchantStockEntry>();

        public MerchantStockConditionsPopoverContent(CompendiumLocalization localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            SetPadding(0f);

            _scroll = new VanillaScrollRegion
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill
            };
            Append(_scroll);

            _alwaysAvailableLabel = new TextLabelElement(string.Empty, UIColors.TextTitle)
            {
                IgnoresMouseInteraction = true
            };

            _conditionalLabel = new TextLabelElement(string.Empty, UIColors.TextTitle)
            {
                IgnoresMouseInteraction = true
            };
            SynchronizeLocalization(force: true);
        }

        public VanillaPopoverContentSize MeasurePopoverContent(int availableWidth)
        {
            int naturalContentWidth = CalculateNaturalContentWidth();
            int naturalWidth = VanillaScrollRegion.CalculateRequiredWidth(naturalContentWidth);
            int width = Math.Min(Math.Max(0, availableWidth), naturalWidth);
            int contentWidth = VanillaScrollRegion.CalculateContentWidth(width);
            int height = CalculateContentHeight(contentWidth);
            return new VanillaPopoverContentSize(width, height);
        }

        public void Bind(IReadOnlyList<NpcDetailsMerchantStockEntry> stock)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            RebuildContent();
            _scroll.ResetScroll();
        }


        public override void Update(GameTime gameTime)
        {
            if (_localizationRevision != _localization.Revision)
            {
                SynchronizeLocalization(force: true);
                RebuildContent();
                Recalculate();
            }

            base.Update(gameTime);
        }

        public override void RecalculateChildren()
        {
            int width = Math.Max(0, (int)GetInnerDimensions().Width);
            int height = Math.Max(0, (int)GetInnerDimensions().Height);
            Layout(_scroll, 0, 0, width, height);

            int contentWidth = _scroll.ContentWidth;
            var cursor = 0;
            int alwaysCount = CountAlwaysAvailable();
            int conditionalCount = _stock.Count - alwaysCount;

            if (alwaysCount > 0)
            {
                Layout(_alwaysAvailableLabel, 0, cursor, contentWidth, LabelHeight);
                cursor += LabelHeight + ContentGap;

                int columns = CalculateIconColumns(contentWidth);
                var iconIndex = 0;

                for (var index = 0; index < _stock.Count; index++)
                {
                    NpcDetailsMerchantStockEntry entry = _stock[index];

                    if (!entry.IsAlwaysAvailable)
                        continue;

                    int row = iconIndex / columns;
                    int column = iconIndex % columns;
                    Layout(
                        _alwaysAvailableIcons[iconIndex],
                        column * (ItemIconSize + ItemIconGap),
                        cursor + row * (ItemIconSize + ItemIconGap),
                        ItemIconSize,
                        ItemIconSize);
                    iconIndex++;
                }

                cursor += CalculateIconGridHeight(alwaysCount, contentWidth);
            }

            if (conditionalCount > 0)
            {
                if (alwaysCount > 0)
                    cursor += SectionGap;

                Layout(_conditionalLabel, 0, cursor, contentWidth, LabelHeight);
                cursor += LabelHeight + ContentGap;

                var conditionalIndex = 0;

                for (var index = 0; index < _stock.Count; index++)
                {
                    NpcDetailsMerchantStockEntry entry = _stock[index];

                    if (entry.IsAlwaysAvailable)
                        continue;

                    ConditionalStockEntryElement element = _conditionalEntries[conditionalIndex++];
                    Layout(element, 0, cursor, contentWidth, element.RequiredHeight);
                    cursor += element.RequiredHeight;

                    if (conditionalIndex < conditionalCount)
                        cursor += ConditionalEntryGap;
                }
            }

            _scroll.SetContentHeight(cursor);
            base.RecalculateChildren();
        }

        private void SynchronizeLocalization(bool force = false)
        {
            if (!force && _localizationRevision == _localization.Revision)
                return;

            _localizationRevision = _localization.Revision;
            _alwaysAvailableLabel.Text = _localization.Get(CompendiumTextKeys.Merchant.AlwaysAvailable);
            _conditionalLabel.Text = _localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability);
        }

        private void RebuildContent()
        {
            _scroll.Content.RemoveAllChildren();
            _alwaysAvailableIcons.Clear();
            _conditionalEntries.Clear();

            int alwaysCount = CountAlwaysAvailable();
            int conditionalCount = _stock.Count - alwaysCount;

            if (alwaysCount > 0)
            {
                _scroll.Content.Append(_alwaysAvailableLabel);

                foreach (NpcDetailsMerchantStockEntry entry in _stock)
                {
                    if (!entry.IsAlwaysAvailable)
                        continue;

                    var icon = new VanillaItemIcon
                    {
                        ItemId = entry.Item.ItemId,
                        IsMissing = entry.Item.IsCollectionTracked && !entry.Item.IsFound,
                        ShowTooltip = true
                    };
                    _alwaysAvailableIcons.Add(icon);
                    _scroll.Content.Append(icon);
                }
            }

            if (conditionalCount > 0)
            {
                _scroll.Content.Append(_conditionalLabel);

                foreach (NpcDetailsMerchantStockEntry entry in _stock)
                {
                    if (entry.IsAlwaysAvailable)
                        continue;

                    var element = new ConditionalStockEntryElement(entry, _localization);
                    _conditionalEntries.Add(element);
                    _scroll.Content.Append(element);
                }
            }
        }

        private int CalculateNaturalContentWidth()
        {
            var width = 0;
            int alwaysCount = CountAlwaysAvailable();

            if (alwaysCount > 0)
            {
                width = Math.Max(
                    width,
                    UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.Merchant.AlwaysAvailable)));
                int columns = Math.Min(PreferredIconColumns, alwaysCount);
                width = Math.Max(width, columns * ItemIconSize + Math.Max(0, columns - 1) * ItemIconGap);
            }

            int conditionalCount = _stock.Count - alwaysCount;

            if (conditionalCount > 0)
            {
                width = Math.Max(
                    width,
                    UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability)));

                foreach (NpcDetailsMerchantStockEntry entry in _stock)
                {
                    if (entry.IsAlwaysAvailable)
                        continue;

                    width = Math.Max(
                        width,
                        ItemIconSize +
                        ConditionalItemTextGap +
                        UIRenderer.MeasureText(entry.Item.Name ?? string.Empty));

                    foreach (string description in BuildVariantDescriptions(entry))
                    {
                        width = Math.Max(width, ConditionalIndent + UIRenderer.MeasureText(description));
                    }

                    if (entry.Variants.Count > 1)
                        width = Math.Max(
                            width,
                            ConditionalIndent +
                            UIRenderer.MeasureText(_localization.Get(CompendiumTextKeys.Common.Or)));
                }
            }

            return Math.Max(1, width);
        }

        private IReadOnlyList<string> BuildVariantDescriptions(NpcDetailsMerchantStockEntry entry)
        {
            if (entry.Variants.Count == 0)
                return [_localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability)];

            var result = new List<string>(entry.Variants.Count);

            foreach (NpcDetailsMerchantVariant variant in entry.Variants)
            {
                var parts = new List<string>(variant.ConditionDescriptions.Count + 2);
                parts.AddRange(variant.ConditionDescriptions);

                if (variant.RandomStock)
                    parts.Add(_localization.Get(CompendiumTextKeys.Merchant.RandomStock));

                if (variant.ShopCapacityLimited)
                    parts.Add(_localization.Get(CompendiumTextKeys.Merchant.ShopCapacityLimited));

                result.Add(
                    parts.Count == 0
                        ? _localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability)
                        : string.Join(" " + _localization.Get(CompendiumTextKeys.Common.And) + " ", parts));
            }

            return result;
        }

        private int CalculateContentHeight(int contentWidth)
        {
            int alwaysCount = CountAlwaysAvailable();
            int conditionalCount = _stock.Count - alwaysCount;
            var height = 0;

            if (alwaysCount > 0)
                height += LabelHeight + ContentGap + CalculateIconGridHeight(alwaysCount, contentWidth);

            if (conditionalCount > 0)
            {
                if (alwaysCount > 0)
                    height += SectionGap;

                height += LabelHeight + ContentGap;

                var conditionalIndex = 0;
                foreach (NpcDetailsMerchantStockEntry entry in _stock)
                {
                    if (entry.IsAlwaysAvailable)
                        continue;

                    height += ConditionalStockEntryElement.CalculateRequiredHeight(entry);
                    conditionalIndex++;

                    if (conditionalIndex < conditionalCount)
                        height += ConditionalEntryGap;
                }
            }

            return height;
        }

        private int CountAlwaysAvailable()
        {
            var count = 0;

            foreach (NpcDetailsMerchantStockEntry entry in _stock)
            {
                if (entry.IsAlwaysAvailable)
                    count++;
            }

            return count;
        }

        private static int CalculateIconColumns(int width)
        {
            return Math.Max(1, (Math.Max(0, width) + ItemIconGap) / (ItemIconSize + ItemIconGap));
        }

        private static int CalculateIconGridHeight(int count, int width)
        {
            if (count <= 0)
                return 0;

            int columns = CalculateIconColumns(width);
            int rows = (count + columns - 1) / columns;
            return rows * ItemIconSize + Math.Max(0, rows - 1) * ItemIconGap;
        }

        private static void Layout(UIElement element, int left, int top, int width, int height)
        {
            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private sealed class ConditionalStockEntryElement : UIElement
        {
            private const int ItemRowHeight = 30;
            private const int ConditionRowHeight = 18;
            private readonly NpcDetailsMerchantStockEntry _entry;
            private readonly CompendiumLocalization _localization;
            private readonly IReadOnlyList<string> _variantDescriptions;

            public ConditionalStockEntryElement(NpcDetailsMerchantStockEntry entry, CompendiumLocalization localization)
            {
                _entry = entry ?? throw new ArgumentNullException(nameof(entry));
                _localization = localization ?? throw new ArgumentNullException(nameof(localization));
                _variantDescriptions = BuildVariantDescriptions(entry, _localization);
                SetPadding(0f);

                var icon = new VanillaItemIcon
                {
                    ItemId = entry.Item.ItemId,
                    IsMissing = entry.Item.IsCollectionTracked && !entry.Item.IsFound,
                    ShowTooltip = true,
                    Left = new StyleDimension(0f, 0f),
                    Top = new StyleDimension(0f, 0f),
                    Width = new StyleDimension(ItemIconSize, 0f),
                    Height = new StyleDimension(ItemIconSize, 0f)
                };
                Append(icon);
            }

            public int RequiredHeight => CalculateRequiredHeight(_entry);

            public static int CalculateRequiredHeight(NpcDetailsMerchantStockEntry entry)
            {
                int variantCount = Math.Max(1, entry?.Variants.Count ?? 0);
                return ItemRowHeight +
                       variantCount * ConditionRowHeight +
                       Math.Max(0, variantCount - 1) * ConditionRowHeight;
            }

            private static IReadOnlyList<string> BuildVariantDescriptions(
                NpcDetailsMerchantStockEntry entry,
                CompendiumLocalization localization)
            {
                if (entry.Variants.Count == 0)
                    return [localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability)];

                var result = new List<string>(entry.Variants.Count);
                foreach (NpcDetailsMerchantVariant variant in entry.Variants)
                {
                    var parts = new List<string>(variant.ConditionDescriptions.Count + 2);
                    parts.AddRange(variant.ConditionDescriptions);
                    if (variant.RandomStock)
                        parts.Add(localization.Get(CompendiumTextKeys.Merchant.RandomStock));
                    if (variant.ShopCapacityLimited)
                        parts.Add(localization.Get(CompendiumTextKeys.Merchant.ShopCapacityLimited));
                    result.Add(
                        parts.Count == 0
                            ? localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability)
                            : string.Join(" " + localization.Get(CompendiumTextKeys.Common.And) + " ", parts));
                }

                return result;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int itemTextWidth = Math.Max(0, width - ItemIconSize - ConditionalItemTextGap);
                string itemName = _entry.Item.Name ?? string.Empty;
                string displayName = TruncatedTextPresentation.Truncate(
                    itemName,
                    itemTextWidth,
                    out bool itemNameTruncated);

                if (displayName.Length > 0)
                {
                    int textY = y + Math.Max(0, (ItemRowHeight - 16) / 2);
                    UIRenderer.DrawText(
                        displayName,
                        x + ItemIconSize + ConditionalItemTextGap,
                        textY,
                        UIColors.TextTitle);
                    TruncatedTextPresentation.ShowTooltipIfTruncated(
                        itemName,
                        itemNameTruncated,
                        x + ItemIconSize + ConditionalItemTextGap,
                        y,
                        itemTextWidth,
                        ItemRowHeight,
                        IsMouseHovering);
                }

                int conditionY = y + ItemRowHeight;
                int conditionWidth = Math.Max(0, width - ConditionalIndent);

                for (var index = 0; index < _variantDescriptions.Count; index++)
                {
                    if (index > 0)
                    {
                        DrawConditionLine(
                            _localization.Get(CompendiumTextKeys.Common.Or),
                            x + ConditionalIndent,
                            conditionY,
                            conditionWidth,
                            UIColors.TextTitle);
                        conditionY += ConditionRowHeight;
                    }

                    DrawConditionLine(
                        _variantDescriptions[index],
                        x + ConditionalIndent,
                        conditionY,
                        conditionWidth,
                        UIColors.TextDim);
                    conditionY += ConditionRowHeight;
                }
            }

            private void DrawConditionLine(string text, int x, int y, int width, Color4 color)
            {
                if (width <= 0 || string.IsNullOrEmpty(text))
                    return;

                string display = TruncatedTextPresentation.Truncate(text, width, out bool truncated);

                if (display.Length == 0)
                    return;

                UIRenderer.DrawText(display, x, y + 1, color);
                TruncatedTextPresentation.ShowTooltipIfTruncated(
                    text,
                    truncated,
                    x,
                    y,
                    width,
                    ConditionRowHeight,
                    IsMouseHovering);
            }
        }
    }
}