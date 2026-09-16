using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class VanillaBestiaryConditionIcon(VanillaBestiaryFilterCatalog filterCatalog) : UIElement
    {
        private readonly VanillaBestiaryFilterCatalog _filterCatalog =
            filterCatalog ?? throw new ArgumentNullException(nameof(filterCatalog));

        private string _displayNameKey;
        private string _tooltipText;

        public bool HasImage { get; private set; }

        public bool ShowTooltip { get; set; } = true;

        public bool Bind(string displayNameKey, string tooltipText)
        {
            bool imageMustChange = !string.Equals(_displayNameKey, displayNameKey, StringComparison.Ordinal);
            _displayNameKey = displayNameKey;
            _tooltipText = tooltipText ?? string.Empty;
            IgnoresMouseInteraction = false;

            if (!imageMustChange)
                return HasImage;

            RemoveAllChildren();
            HasImage = false;

            if (!_filterCatalog.TryGetOptionByDisplayNameKey(displayNameKey, out VanillaBestiaryFilterOption option))
                return false;

            UIElement image = option.CreateImage();

            if (image == null)
                return false;

            image.IgnoresMouseInteraction = true;
            image.HAlign = 0.5f;
            image.VAlign = 0.5f;
            Append(image);
            HasImage = true;
            return true;
        }

        public void Hide()
        {
            _displayNameKey = null;
            _tooltipText = null;
            HasImage = false;
            IgnoresMouseInteraction = true;
            RemoveAllChildren();
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (ShowTooltip && IsMouseHovering && !string.IsNullOrEmpty(_tooltipText))
                Tooltip.Set(_tooltipText);
        }
    }
}