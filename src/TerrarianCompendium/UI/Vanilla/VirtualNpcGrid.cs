using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.Bestiary;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VirtualNpcGrid : UIElement
    {
        private const int TextHeight = 16;
        private readonly Dictionary<int, BestiaryEntry> _entriesByNetId = new();
        private readonly Func<int, bool> _isSelected;

        private readonly Action<int> _npcClicked;
        private readonly Func<IReadOnlyList<NpcCatalogEntry>> _npcsProvider;
        private readonly VanillaScrollRegion _scroll;
        private readonly List<NpcSlotElement> _slots = new();
        private string _emptyStateText;

        private int _lastColumns = -1;
        private int _lastContentHeight = -1;

        public VirtualNpcGrid(
            VanillaScrollRegion scroll,
            Func<IReadOnlyList<NpcCatalogEntry>> npcsProvider,
            string emptyStateText = null,
            Action<int> npcClicked = null,
            Func<int, bool> isSelected = null)
        {
            _scroll = scroll ?? throw new ArgumentNullException(nameof(scroll));
            _npcsProvider = npcsProvider ?? throw new ArgumentNullException(nameof(npcsProvider));
            _emptyStateText = emptyStateText ?? string.Empty;
            _npcClicked = npcClicked;
            _isSelected = isSelected;

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
            IReadOnlyList<NpcCatalogEntry> entries = GetEntries();

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
            IReadOnlyList<NpcCatalogEntry> entries = GetEntries();
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
                NpcSlotElement slot = _slots[slotIndex];
                int entryIndex = startIndex + slotIndex;

                if (slotIndex >= capacity || entryIndex >= entries.Count)
                {
                    slot.Hide();
                    continue;
                }

                int row = entryIndex / columns;
                int column = entryIndex % columns;

                NpcCatalogEntry catalogEntry = entries[entryIndex];
                BestiaryEntry bestiaryEntry = GetOrCreateBestiaryEntry(catalogEntry);

                slot.Bind(
                    catalogEntry.NetId,
                    bestiaryEntry,
                    VanillaBestiaryNativeBridge.GetDisplayName(catalogEntry),
                    column * VirtualGridLayout.SlotSize,
                    row * VirtualGridLayout.SlotSize,
                    _isSelected?.Invoke(catalogEntry.NetId) == true);
            }
        }

        private IReadOnlyList<NpcCatalogEntry> GetEntries()
        {
            return _npcsProvider() ?? Array.Empty<NpcCatalogEntry>();
        }

        private BestiaryEntry GetOrCreateBestiaryEntry(NpcCatalogEntry catalogEntry)
        {
            if (_entriesByNetId.TryGetValue(catalogEntry.NetId, out BestiaryEntry existing))
                return existing;

            BestiaryEntry entry = VanillaBestiaryNativeBridge.GetBestiaryEntry(catalogEntry);

            if (entry.Icon == null)
            {
                throw new InvalidOperationException(
                    $"Terraria Bestiary entry for NPC net ID {catalogEntry.NetId} has no icon metadata.");
            }

            _entriesByNetId.Add(catalogEntry.NetId, entry);

            return entry;
        }

        private void EnsureSlotCapacity(int capacity)
        {
            while (_slots.Count < capacity)
            {
                var slot = new NpcSlotElement(_npcClicked);

                _slots.Add(slot);
                Append(slot);
            }
        }

        private sealed class NpcSlotElement : UIElement
        {
            private readonly Action<int> _clicked;
            private BestiaryUICollectionInfo _collectionInfo;
            private BestiaryEntry _entry;
            private string _hoverText;
            private bool _isBound;
            private bool _isSelected;
            private int _npcNetId;

            public NpcSlotElement(Action<int> clicked)
            {
                _clicked = clicked;
                IgnoresMouseInteraction = false;
                OnLeftClick += OnClicked;
            }

            public void Bind(int npcNetId, BestiaryEntry entry, string hoverText, int left, int top, bool isSelected)
            {
                if (entry == null)
                    throw new ArgumentNullException(nameof(entry));

                if (entry.UIInfoProvider == null)
                    throw new InvalidOperationException("Terraria Bestiary entry has no UI info provider.");

                bool entryChanged = !_isBound || _npcNetId != npcNetId || !ReferenceEquals(_entry, entry);

                _isBound = true;
                _npcNetId = npcNetId;
                _entry = entry;
                _hoverText = hoverText ?? string.Empty;
                _isSelected = isSelected;
                _collectionInfo = _entry.UIInfoProvider.GetEntryUICollectionInfo();

                IgnoresMouseInteraction = false;

                Left.Set(left, 0f);
                Top.Set(top, 0f);
                Width.Set(VirtualGridLayout.CellSize, 0f);
                Height.Set(VirtualGridLayout.CellSize, 0f);

                if (entryChanged)
                    AsyncNpcIconRenderer.RequestAsync(_npcNetId);

                Recalculate();
            }

            public void Hide()
            {
                _isBound = false;
                _isSelected = false;
                _npcNetId = 0;
                _entry = null;
                _hoverText = string.Empty;
                _collectionInfo = default;

                IgnoresMouseInteraction = true;

                Width.Set(0f, 0f);
                Height.Set(0f, 0f);

                Recalculate();
            }

            public override void Update(GameTime gameTime)
            {
                if (_isBound && _entry != null)
                    _collectionInfo = _entry.UIInfoProvider.GetEntryUICollectionInfo();

                base.Update(gameTime);
            }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (!_isBound || _entry == null)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                if (width <= 0 || height <= 0)
                    return;

                Color4 backgroundColor = _isSelected ? UIColors.ItemActiveBg : UIColors.ItemBg;
                Color4 borderColor = _isSelected ? UIColors.Accent : UIColors.Border;

                UIRenderer.DrawRect(x, y, width, height, backgroundColor);
                UIRenderer.DrawRectOutline(x, y, width, height, borderColor);

                int iconX = x + VirtualGridLayout.IconPadding;
                int iconY = y + VirtualGridLayout.IconPadding;
                int iconWidth = Math.Max(0, width - VirtualGridLayout.IconPadding * 2);
                int iconHeight = Math.Max(0, height - VirtualGridLayout.IconPadding * 2);
                bool silhouette = _collectionInfo.UnlockState == BestiaryEntryUnlockState.NotKnownAtAll_0;

                AsyncNpcIconRenderer.Draw(spriteBatch, _npcNetId, iconX, iconY, iconWidth, iconHeight, silhouette);

                if (!IsMouseHovering)
                    return;

                if (!string.IsNullOrEmpty(_hoverText))
                    Tooltip.Set(_hoverText);
            }

            private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
            {
                if (!_isBound || evt.Target != this)
                    return;

                _clicked?.Invoke(_npcNetId);
            }
        }
    }
}