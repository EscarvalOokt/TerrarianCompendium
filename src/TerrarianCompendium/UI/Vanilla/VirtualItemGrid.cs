using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Journey;
using ItemTooltip = TerrariaModder.Core.UI.Widgets.ItemTooltip;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VirtualItemGrid : UIElement
    {
        public const int CellSize = VirtualGridLayout.CellSize;
        public const int CellGap = VirtualGridLayout.CellGap;
        public const int SlotSize = VirtualGridLayout.SlotSize;

        private const int IconPadding = VirtualGridLayout.IconPadding;
        private const int IconSize = VirtualGridLayout.IconSize;
        private const int TextHeight = 16;

        private readonly Func<int, string> _cornerBadgeText;
        private readonly Func<int, bool> _isMissing;
        private readonly Func<int, bool> _isSelected;
        private readonly Action<int> _itemClicked;
        private readonly Func<IReadOnlyList<ItemCatalogEntry>> _itemsProvider;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly VanillaScrollRegion _scroll;
        private readonly List<ItemSlotElement> _slots = new();
        private string _emptyStateText;
        private int _lastColumns = -1;
        private int _lastContentHeight = -1;

        public VirtualItemGrid(
            VanillaScrollRegion scroll,
            Func<IReadOnlyList<ItemCatalogEntry>> itemsProvider,
            Action<int> itemClicked,
            Func<int, bool> isMissing = null,
            Func<int, bool> isSelected = null,
            Func<int, string> cornerBadgeText = null,
            JourneyResearchState journeyResearchState = null,
            string emptyStateText = null)
        {
            _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            _itemsProvider = itemsProvider ?? throw new ArgumentNullException(nameof(itemsProvider));
            _itemClicked = itemClicked;
            _isMissing = isMissing;
            _isSelected = isSelected;
            _cornerBadgeText = cornerBadgeText;
            _journeyResearchState = journeyResearchState;
            _emptyStateText = emptyStateText ?? string.Empty;
            Width = StyleDimension.Fill;
            SetPadding(0f);
        }

        public string EmptyStateText
        {
            get => _emptyStateText;
            set => _emptyStateText = value ?? string.Empty;
        }

        public override void Update(GameTime gameTime)
        {
            BindVisibleSlots();
            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            IReadOnlyList<ItemCatalogEntry> items = GetItems();

            if (items.Count != 0 || _emptyStateText.Length == 0)
                return;

            CalculatedStyle dimensions = GetDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(_scroll.ViewportHeight, (int)dimensions.Height);
            string display = TextUtil.Truncate(_emptyStateText, width);

            if (display.Length == 0)
                return;

            int textWidth = UIRenderer.MeasureText(display);
            int textX = (int)dimensions.X + Math.Max(0, (width - textWidth) / 2);
            int textY = (int)dimensions.Y + Math.Max(0, (height - TextHeight) / 2);
            UIRenderer.DrawText(display, textX, textY, UIColors.TextDim);
        }

        private void BindVisibleSlots()
        {
            IReadOnlyList<ItemCatalogEntry> items = GetItems();
            int columns = VirtualGridLayout.CalculateColumnCount(_scroll.ContentWidth);
            int contentHeight = VirtualGridLayout.CalculateContentHeight(items.Count, columns, _scroll.ViewportHeight);

            if (_lastContentHeight != contentHeight || _lastColumns != columns)
            {
                _lastContentHeight = contentHeight;
                _lastColumns = columns;
                Height.Set(contentHeight, 0f);
                _scroll.SetContentHeight(contentHeight);
                Recalculate();
            }

            int startRow = VirtualGridLayout.CalculateStartRow(_scroll.ScrollOffset);
            int visibleRows = VirtualGridLayout.CalculateVisibleRowCount(_scroll.ViewportHeight);
            int capacity = visibleRows * columns;
            EnsureSlotCapacity(capacity);
            int startIndex = startRow * columns;

            for (var slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
            {
                ItemSlotElement slot = _slots[slotIndex];
                int itemIndex = startIndex + slotIndex;

                if (slotIndex >= capacity || itemIndex >= items.Count)
                {
                    slot.Hide();
                    continue;
                }

                int row = itemIndex / columns;
                int column = itemIndex % columns;
                ItemCatalogEntry entry = items[itemIndex];
                slot.Bind(
                    entry.Id,
                    _isMissing?.Invoke(entry.Id) == true,
                    _isSelected?.Invoke(entry.Id) == true,
                    _cornerBadgeText?.Invoke(entry.Id),
                    column * SlotSize,
                    row * SlotSize);
            }
        }

        private IReadOnlyList<ItemCatalogEntry> GetItems()
        {
            return _itemsProvider() ?? Array.Empty<ItemCatalogEntry>();
        }

        private void EnsureSlotCapacity(int capacity)
        {
            while (_slots.Count < capacity)
            {
                var slot = new ItemSlotElement(_itemClicked, _journeyResearchState);
                _slots.Add(slot);
                Append(slot);
            }
        }

        private sealed class ItemSlotElement : UIElement
        {
            private readonly Action<int> _clicked;
            private readonly CornerBadgeElement _cornerBadge;
            private readonly VanillaItemIcon _icon;
            private int _itemId;
            private bool _selected;

            public ItemSlotElement(Action<int> clicked, JourneyResearchState journeyResearchState)
            {
                _clicked = clicked;
                _icon = new VanillaItemIcon(journeyResearchState)
                {
                    ShowTooltip = false,
                    IgnoresMouseInteraction = false
                };
                Append(_icon);

                _cornerBadge = new CornerBadgeElement();
                Append(_cornerBadge);
                OnLeftMouseDown += HandleLeftMouseDown;
                OnRightMouseDown += HandleRightMouseDown;
                OnLeftClick += OnClicked;
            }

            public void Bind(int itemId, bool missing, bool selected, string cornerBadgeText, int left, int top)
            {
                _itemId = itemId;
                _selected = selected;
                IgnoresMouseInteraction = false;
                Left.Set(left, 0f);
                Top.Set(top, 0f);
                Width.Set(CellSize, 0f);
                Height.Set(CellSize, 0f);
                _icon.ItemId = itemId;
                _icon.IsMissing = missing;
                _icon.Left.Set(IconPadding, 0f);
                _icon.Top.Set(IconPadding, 0f);
                _icon.Width.Set(IconSize, 0f);
                _icon.Height.Set(IconSize, 0f);
                _cornerBadge.SetText(cornerBadgeText);
                _cornerBadge.Left.Set(0f, 0f);
                _cornerBadge.Top.Set(0f, 0f);
                _cornerBadge.Width.Set(0f, 1f);
                _cornerBadge.Height.Set(TextHeight, 0f);
                Recalculate();
            }

            public void Hide()
            {
                _itemId = 0;
                _selected = false;
                IgnoresMouseInteraction = true;
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
                _icon.ItemId = 0;
                _icon.Width.Set(0f, 0f);
                _icon.Height.Set(0f, 0f);
                _cornerBadge.SetText(string.Empty);
                Recalculate();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_itemId <= 0)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                UIRenderer.DrawRect(x, y, width, height, _selected ? UIColors.ItemActiveBg : UIColors.ItemBg);
                UIRenderer.DrawRectOutline(x, y, width, height, _selected ? UIColors.Accent : UIColors.Border);

                if (IsMouseHovering || _icon.IsMouseHovering)
                {
                    ItemTooltip.Set(_itemId);
                    _icon.RegisterResearchSupplementalTooltip();
                }
            }

            private void HandleLeftMouseDown(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_itemId > 0 && (evt.Target == this || evt.Target == _icon))
                    _icon.TryHandleJourneyDuplicationFromOwnerMouseDown(evt, this, rightClick: false);
            }

            private void HandleRightMouseDown(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_itemId > 0 && (evt.Target == this || evt.Target == _icon))
                    _icon.TryHandleJourneyDuplicationFromOwnerMouseDown(evt, this, rightClick: true);
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_icon.ConsumeJourneyDuplicationClick())
                    return;

                if (_itemId > 0 && (evt.Target == this || evt.Target == _icon))
                    _clicked?.Invoke(_itemId);
            }
        }

        private sealed class CornerBadgeElement : UIElement
        {
            private const int RightPadding = 1;
            private const float TextScale = 0.75f;

            private string _text = string.Empty;

            public CornerBadgeElement()
            {
                IgnoresMouseInteraction = true;
            }

            public void SetText(string text)
            {
                _text = text ?? string.Empty;
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_text.Length == 0)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                int width = Math.Max(0, (int)dimensions.Width);
                var textWidth = (int)Math.Ceiling(UIRenderer.MeasureText(_text) * TextScale);
                int x = (int)dimensions.X + Math.Max(0, width - textWidth - RightPadding);
                var y = (int)dimensions.Y;

                UIRenderer.DrawTextScaled(
                    _text,
                    x,
                    y,
                    UIColors.Success.R,
                    UIColors.Success.G,
                    UIColors.Success.B,
                    UIColors.Success.A,
                    TextScale);
            }
        }
    }
}