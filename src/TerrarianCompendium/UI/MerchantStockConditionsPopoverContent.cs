using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Details;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class MerchantStockConditionsPopoverContent : UIElement, IVanillaPopoverContent
    {
        private const int ItemIconSize = 30;
        private const int ItemTextGap = 5;
        private const int ItemRowHeight = 30;
        private const int ContentGap = 6;
        private const int ConditionTextGap = 6;
        private const int ConditionLineHeight = 18;
        private const int SeparatorHeight = 18;
        private const int RowGap = 4;

        private readonly List<DetailRow> _detailRows = new();
        private readonly VanillaItemIcon _itemIcon;
        private readonly TextLabelElement _itemNameLabel;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaScrollRegion _scroll;
        private NpcDetailsMerchantStockEntry _entry;
        private long _localizationRevision;

        public MerchantStockConditionsPopoverContent(
            CompendiumLocalization localization,
            JourneyResearchState journeyResearchState = null)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            SetPadding(0f);

            _scroll = new VanillaScrollRegion
            {
                Width = StyleDimension.Fill,
                Height = StyleDimension.Fill
            };
            Append(_scroll);

            _itemIcon = new VanillaItemIcon(journeyResearchState)
            {
                ShowTooltip = true
            };
            _itemNameLabel = new TextLabelElement(string.Empty, UIColors.TextTitle)
            {
                IgnoresMouseInteraction = true
            };

            _localizationRevision = _localization.Revision;
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

        public void Bind(NpcDetailsMerchantStockEntry entry)
        {
            _entry = entry;
            RebuildContent();
            _scroll.ResetScroll();
        }

        public override void Update(GameTime gameTime)
        {
            if (_localizationRevision != _localization.Revision)
            {
                _localizationRevision = _localization.Revision;
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

            if (_entry != null)
            {
                Layout(_itemIcon, 0, cursor, ItemIconSize, ItemIconSize);
                int itemTextTop = Math.Max(0, (ItemRowHeight - ConditionLineHeight) / 2);
                Layout(
                    _itemNameLabel,
                    ItemIconSize + ItemTextGap,
                    itemTextTop,
                    Math.Max(0, contentWidth - ItemIconSize - ItemTextGap),
                    ConditionLineHeight);
                cursor += ItemRowHeight + ContentGap;

                for (var index = 0; index < _detailRows.Count; index++)
                {
                    DetailRow row = _detailRows[index];
                    int rowHeight = row.GetHeight(contentWidth);
                    Layout(row.Element, 0, cursor, contentWidth, rowHeight);
                    cursor += rowHeight;

                    if (index < _detailRows.Count - 1)
                        cursor += RowGap;
                }
            }

            _scroll.SetContentHeight(cursor);
            base.RecalculateChildren();
        }

        private void RebuildContent()
        {
            _scroll.Content.RemoveAllChildren();
            _detailRows.Clear();

            if (_entry == null)
                return;

            _itemIcon.ItemId = _entry.Item.ItemId;
            _itemIcon.IsMissing = _entry.Item.IsCollectionTracked && !_entry.Item.IsFound;
            _itemNameLabel.Text = _entry.Item.Name ?? string.Empty;
            _scroll.Content.Append(_itemIcon);
            _scroll.Content.Append(_itemNameLabel);

            if (_entry.Variants.Count == 0)
            {
                AddSeparatorRow(
                    _localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability),
                    UIColors.TextDim);
                return;
            }

            for (var variantIndex = 0; variantIndex < _entry.Variants.Count; variantIndex++)
            {
                if (variantIndex > 0)
                    AddSeparatorRow(_localization.Get(CompendiumTextKeys.Common.Or), UIColors.TextTitle);

                NpcDetailsMerchantVariant variant = _entry.Variants[variantIndex];
                var componentCount = 0;

                for (var conditionIndex = 0; conditionIndex < variant.Conditions.Count; conditionIndex++)
                {
                    if (componentCount > 0)
                        AddSeparatorRow(_localization.Get(CompendiumTextKeys.Common.And), UIColors.TextTitle);

                    NpcDetailsMerchantCondition condition = variant.Conditions[conditionIndex];
                    MerchantConditionVisualDescriptor descriptor =
                        MerchantConditionIconPresentation.Describe(condition.Condition);
                    AddConditionRow(descriptor, condition.Description);
                    componentCount++;
                }

                if (variant.RandomStock)
                {
                    if (componentCount > 0)
                        AddSeparatorRow(_localization.Get(CompendiumTextKeys.Common.And), UIColors.TextTitle);

                    AddConditionRow(
                        MerchantConditionIconPresentation.DescribeRandomStock(),
                        _localization.Get(CompendiumTextKeys.Merchant.RandomStock));
                    componentCount++;
                }

                if (variant.ShopCapacityLimited)
                {
                    if (componentCount > 0)
                        AddSeparatorRow(_localization.Get(CompendiumTextKeys.Common.And), UIColors.TextTitle);

                    AddConditionRow(
                        MerchantConditionIconPresentation.DescribeShopCapacityLimited(),
                        _localization.Get(CompendiumTextKeys.Merchant.ShopCapacityLimited));
                    componentCount++;
                }

                if (componentCount == 0)
                    AddSeparatorRow(
                        _localization.Get(CompendiumTextKeys.Merchant.ConditionalAvailability),
                        UIColors.TextDim);
            }
        }

        private void AddConditionRow(MerchantConditionVisualDescriptor descriptor, string description)
        {
            MerchantConditionIconPresentation.Request(descriptor);
            var element = new ConditionDetailsRowElement(descriptor, description ?? string.Empty);
            _detailRows.Add(new DetailRow(element, element.NaturalWidth, element.CalculateRequiredHeight));
            _scroll.Content.Append(element);
        }

        private void AddSeparatorRow(string text, Color4 color)
        {
            var element = new TextLabelElement(text ?? string.Empty, color)
            {
                IgnoresMouseInteraction = true
            };
            int naturalWidth = UIRenderer.MeasureText(text ?? string.Empty);
            _detailRows.Add(new DetailRow(element, naturalWidth, _ => SeparatorHeight));
            _scroll.Content.Append(element);
        }

        private int CalculateNaturalContentWidth()
        {
            if (_entry == null)
                return 1;

            int width = ItemIconSize + ItemTextGap + UIRenderer.MeasureText(_entry.Item.Name ?? string.Empty);

            foreach (DetailRow row in _detailRows)
                width = Math.Max(width, row.NaturalWidth);

            return Math.Max(1, width);
        }

        private int CalculateContentHeight(int contentWidth)
        {
            if (_entry == null)
                return 0;

            int height = ItemRowHeight + ContentGap;

            for (var index = 0; index < _detailRows.Count; index++)
            {
                height += _detailRows[index].GetHeight(contentWidth);

                if (index < _detailRows.Count - 1)
                    height += RowGap;
            }

            return height;
        }

        private static void Layout(UIElement element, int left, int top, int width, int height)
        {
            element.Left.Set(left, 0f);
            element.Top.Set(top, 0f);
            element.Width.Set(Math.Max(0, width), 0f);
            element.Height.Set(Math.Max(0, height), 0f);
        }

        private sealed class DetailRow(UIElement element, int naturalWidth, Func<int, int> heightProvider)
        {
            private readonly Func<int, int> _heightProvider =
                heightProvider ?? throw new ArgumentNullException(nameof(heightProvider));

            public UIElement Element { get; } = element ?? throw new ArgumentNullException(nameof(element));

            public int NaturalWidth { get; } = Math.Max(0, naturalWidth);

            public int GetHeight(int width)
            {
                return Math.Max(0, _heightProvider(Math.Max(0, width)));
            }
        }

        private sealed class ConditionDetailsRowElement : UIElement
        {
            private readonly string _description;
            private readonly MerchantConditionVisualDescriptor _descriptor;
            private readonly int _visualWidth;

            public ConditionDetailsRowElement(MerchantConditionVisualDescriptor descriptor, string description)
            {
                _descriptor = descriptor;
                _description = description ?? string.Empty;
                _visualWidth = MerchantConditionIconPresentation.MeasureWidth(descriptor);
                SetPadding(0f);
                IgnoresMouseInteraction = true;
            }

            public int NaturalWidth => _visualWidth + ConditionTextGap + UIRenderer.MeasureText(_description);

            public int CalculateRequiredHeight(int width)
            {
                int textWidth = Math.Max(0, width - _visualWidth - ConditionTextGap);

                if (textWidth <= 0 || string.IsNullOrWhiteSpace(_description))
                    return MerchantConditionIconPresentation.IconSize;

                IReadOnlyList<string> lines = SupplementalTooltip.WrapText(
                    _description,
                    textWidth,
                    UIRenderer.MeasureText);
                int textHeight = lines.Count * ConditionLineHeight;
                return Math.Max(MerchantConditionIconPresentation.IconSize, textHeight);
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

                int visualWidth = Math.Min(_visualWidth, width);
                int visualY = y + Math.Max(0, (height - MerchantConditionIconPresentation.IconSize) / 2);
                MerchantConditionIconPresentation.Draw(
                    _descriptor,
                    x,
                    visualY,
                    visualWidth,
                    MerchantConditionIconPresentation.IconSize);

                int textX = x + visualWidth + ConditionTextGap;
                int textWidth = Math.Max(0, width - visualWidth - ConditionTextGap);

                if (textWidth <= 0 || string.IsNullOrWhiteSpace(_description))
                    return;

                IReadOnlyList<string> lines = SupplementalTooltip.WrapText(
                    _description,
                    textWidth,
                    UIRenderer.MeasureText);
                int textY = y;

                for (var index = 0; index < lines.Count; index++)
                {
                    UIRenderer.DrawText(lines[index], textX, textY + 1, UIColors.Text);
                    textY += ConditionLineHeight;
                }
            }
        }
    }
}