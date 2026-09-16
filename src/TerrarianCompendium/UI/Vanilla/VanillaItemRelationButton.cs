using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI;
using ItemTooltip = TerrariaModder.Core.UI.Widgets.ItemTooltip;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaItemRelationButton : UIElement
    {
        private const int IconPadding = 3;
        private readonly Action<int> _clicked;
        private readonly VanillaItemIcon _icon;
        private int _clickId;
        private int _itemId;

        public VanillaItemRelationButton(Action<int> clicked)
        {
            _clicked = clicked;
            _icon = new VanillaItemIcon
            {
                ShowTooltip = false,
                IgnoresMouseInteraction = true
            };
            Append(_icon);
            OnLeftClick += OnClicked;
        }

        public void Bind(int itemId, bool isMissing)
        {
            Bind(itemId, isMissing, itemId);
        }

        public void Bind(int itemId, bool isMissing, int clickId)
        {
            _itemId = Math.Max(0, itemId);
            _clickId = _itemId > 0 ? Math.Max(0, clickId) : 0;
            _icon.ItemId = _itemId;
            _icon.IsMissing = _itemId > 0 && isMissing;
            IgnoresMouseInteraction = _itemId <= 0 || _clickId <= 0;
        }

        public override void RecalculateChildren()
        {
            CalculatedStyle dimensions = GetInnerDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);
            int iconSize = Math.Max(0, Math.Min(width, height) - IconPadding * 2);
            _icon.Left.Set(IconPadding, 0f);
            _icon.Top.Set(IconPadding, 0f);
            _icon.Width.Set(iconSize, 0f);
            _icon.Height.Set(iconSize, 0f);
            base.RecalculateChildren();
        }

        public void Hide()
        {
            Bind(0, isMissing: false);
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
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
            UIRenderer.DrawRect(x, y, width, height, IsMouseHovering ? UIColors.ButtonHover : UIColors.ItemBg);
            UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Border);

            if (IsMouseHovering)
                ItemTooltip.Set(_itemId);
        }

        private void OnClicked(UIMouseEvent evt, UIElement listeningElement)
        {
            if (_itemId > 0 && _clickId > 0 && evt.Target == this)
                _clicked?.Invoke(_clickId);
        }
    }
}