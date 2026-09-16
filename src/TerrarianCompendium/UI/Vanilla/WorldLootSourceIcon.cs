using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class WorldLootSourceIcon(CompendiumLocalization localization) : UIElement
    {
        private readonly CompendiumLocalization _localization =
            localization ?? throw new ArgumentNullException(nameof(localization));

        private ItemDetailsWorldSourceReference _source;

        public void Bind(ItemDetailsWorldSourceReference source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));

            if (_source.RepresentativeItemId is > 0)
                AsyncItemIconRenderer.RequestAsync(_source.RepresentativeItemId.Value);

            IgnoresMouseInteraction = false;
        }

        public void Hide()
        {
            _source = null;
            IgnoresMouseInteraction = true;
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_source == null)
                return;

            CalculatedStyle dimensions = GetDimensions();
            int padding = VirtualGridLayout.IconPadding;
            var bounds = new Rectangle(
                (int)dimensions.X + padding,
                (int)dimensions.Y + padding,
                Math.Max(0, (int)dimensions.Width - padding * 2),
                Math.Max(0, (int)dimensions.Height - padding * 2));

            if (_source.RepresentativeItemId is > 0)
            {
                AsyncItemIconRenderer.Draw(
                    _source.RepresentativeItemId.Value,
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    bounds.Height);
            }
            else if (_source.Kind == WorldLootSourceKind.Pot)
            {
                VanillaPresentationIcons.DrawPot(bounds);
            }

            if (IsMouseHovering)
                Tooltip.Set(_localization.Get(_source.DisplayNameKey));
        }
    }
}