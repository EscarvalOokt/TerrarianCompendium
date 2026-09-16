using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrarianCompendium.ArmorSets;
using ItemTooltip = TerrariaModder.Core.UI.Widgets.ItemTooltip;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VirtualArmorSetGrid : UIElement
    {
        public const int SlotSize = VirtualGridLayout.SlotSize;

        private const int CellSize = VirtualGridLayout.CellSize;
        private const int IconPadding = VirtualGridLayout.IconPadding;
        private const int IconSize = VirtualGridLayout.IconSize;
        private const int TextHeight = 16;

        private readonly Action<int> _armorSetClicked;
        private readonly Func<IReadOnlyList<ArmorSetCatalogEntry>> _entriesProvider;
        private readonly Func<int, bool> _isSelected;
        private readonly VanillaScrollRegion _scroll;
        private readonly List<ArmorSetSlotElement> _slots = new();
        private string _emptyStateText;
        private int _lastColumns = -1;
        private int _lastContentHeight = -1;

        public VirtualArmorSetGrid(
            VanillaScrollRegion scroll,
            Func<IReadOnlyList<ArmorSetCatalogEntry>> entriesProvider,
            Action<int> armorSetClicked,
            Func<int, bool> isSelected = null,
            string emptyStateText = null)
        {
            _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            _entriesProvider = entriesProvider ?? throw new ArgumentNullException(nameof(entriesProvider));
            _armorSetClicked = armorSetClicked;
            _isSelected = isSelected;
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
            IReadOnlyList<ArmorSetCatalogEntry> entries = GetEntries();

            if (entries.Count != 0 || _emptyStateText.Length == 0)
                return;

            CalculatedStyle dimensions = GetDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(_scroll.ViewportHeight, (int)dimensions.Height);
            string display = TruncatedTextPresentation.Truncate(_emptyStateText, width, out _);

            if (display.Length == 0)
                return;

            int textWidth = UIRenderer.MeasureText(display);
            int textX = (int)dimensions.X + Math.Max(0, (width - textWidth) / 2);
            int textY = (int)dimensions.Y + Math.Max(0, (height - TextHeight) / 2);
            UIRenderer.DrawText(display, textX, textY, UIColors.TextDim);
        }

        private void BindVisibleSlots()
        {
            IReadOnlyList<ArmorSetCatalogEntry> entries = GetEntries();
            int columns = VirtualGridLayout.CalculateColumnCount(_scroll.ContentWidth);
            int contentHeight = VirtualGridLayout.CalculateContentHeight(
                entries.Count,
                columns,
                _scroll.ViewportHeight);

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
                ArmorSetSlotElement slot = _slots[slotIndex];
                int entryIndex = startIndex + slotIndex;

                if (slotIndex >= capacity || entryIndex >= entries.Count)
                {
                    slot.Hide();
                    continue;
                }

                int row = entryIndex / columns;
                int column = entryIndex % columns;
                ArmorSetCatalogEntry entry = entries[entryIndex];
                slot.Bind(
                    entry.Id,
                    entry.RepresentativeItemId,
                    _isSelected?.Invoke(entry.Id) == true,
                    column * SlotSize,
                    row * SlotSize);
            }
        }

        private IReadOnlyList<ArmorSetCatalogEntry> GetEntries()
        {
            return _entriesProvider() ?? Array.Empty<ArmorSetCatalogEntry>();
        }

        private void EnsureSlotCapacity(int capacity)
        {
            while (_slots.Count < capacity)
            {
                var slot = new ArmorSetSlotElement(_armorSetClicked);
                _slots.Add(slot);
                Append(slot);
            }
        }

        private sealed class ArmorSetSlotElement : UIElement
        {
            private readonly Action<int> _clicked;
            private readonly VanillaItemIcon _icon;
            private int _armorSetId;
            private int _representativeItemId;
            private bool _selected;

            public ArmorSetSlotElement(Action<int> clicked)
            {
                _clicked = clicked;
                _icon = new VanillaItemIcon
                {
                    ShowTooltip = false,
                    IgnoresMouseInteraction = false
                };
                Append(_icon);
                OnLeftClick += OnClicked;
            }

            public void Bind(int armorSetId, int representativeItemId, bool selected, int left, int top)
            {
                _armorSetId = armorSetId;
                _representativeItemId = representativeItemId;
                _selected = selected;
                IgnoresMouseInteraction = false;
                Left.Set(left, 0f);
                Top.Set(top, 0f);
                Width.Set(CellSize, 0f);
                Height.Set(CellSize, 0f);
                _icon.ItemId = representativeItemId;
                _icon.IsMissing = false;
                _icon.Left.Set(IconPadding, 0f);
                _icon.Top.Set(IconPadding, 0f);
                _icon.Width.Set(IconSize, 0f);
                _icon.Height.Set(IconSize, 0f);
                Recalculate();
            }

            public void Hide()
            {
                _armorSetId = 0;
                _representativeItemId = 0;
                _selected = false;
                IgnoresMouseInteraction = true;
                Width.Set(0f, 0f);
                Height.Set(0f, 0f);
                _icon.ItemId = 0;
                _icon.Width.Set(0f, 0f);
                _icon.Height.Set(0f, 0f);
                Recalculate();
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (_armorSetId <= 0)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                UIRenderer.DrawRect(x, y, width, height, _selected ? UIColors.ItemActiveBg : UIColors.ItemBg);
                UIRenderer.DrawRectOutline(x, y, width, height, _selected ? UIColors.Accent : UIColors.Border);

                if ((IsMouseHovering || _icon.IsMouseHovering) && _representativeItemId > 0)
                    ItemTooltip.Set(_representativeItemId);
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (_armorSetId > 0 && (evt.Target == this || evt.Target == _icon))
                    _clicked?.Invoke(_armorSetId);
            }
        }
    }
}