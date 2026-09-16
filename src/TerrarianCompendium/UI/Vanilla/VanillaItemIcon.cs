using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using CoreItemTooltip = TerrariaModder.Core.UI.Widgets.ItemTooltip;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaItemIcon : UIElement
    {
        private int _itemId;

        public VanillaItemIcon()
        {
            IgnoresMouseInteraction = false;
        }

        public int ItemId
        {
            get => _itemId;
            set => _itemId = Math.Max(0, value);
        }

        public bool IsMissing { get; set; }

        public bool ShowTooltip { get; set; } = true;

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_itemId <= 0)
                return;

            CalculatedStyle dimensions = GetDimensions();
            var x = (int)dimensions.X;
            var y = (int)dimensions.Y;
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            AsyncItemIconRenderer.Draw(_itemId, x, y, width, height, IsMissing);

            if (ShowTooltip && IsMouseHovering)
                CoreItemTooltip.Set(_itemId);
        }
    }
}