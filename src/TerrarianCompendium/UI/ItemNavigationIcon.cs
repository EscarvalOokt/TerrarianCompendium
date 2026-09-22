using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.UI
{
    internal sealed class ItemNavigationIcon : UIElement
    {
        private const string JourneyAtlasPath = "Images/UI/Creative/Infinite_Icons";
        private const int JourneyAtlasFramesPerRow = 11;
        private const int JourneyFrameWidthOffset = -2;
        private const int IconPadding = 3;

        private static Asset<Texture2D> _journeyAtlas;
        private readonly VanillaItemIcon _itemIcon;

        private readonly int? _journeyFrameIndex;

        public ItemNavigationIcon(ItemNavigationNodeId nodeId)
        {
            SetPadding(0f);
            IgnoresMouseInteraction = true;

            if (ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(nodeId, out int frameIndex))
            {
                _journeyFrameIndex = frameIndex;
                RequestJourneyAtlasAsync();
                return;
            }

            if (!ItemTaxonomyIconDefinitions.TryGetNavigationItemId(nodeId, out int itemId))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nodeId),
                    nodeId,
                    "No category icon is defined for this navigation node.");
            }

            _itemIcon = new VanillaItemIcon
            {
                ItemId = itemId,
                ShowTooltip = false,
                IgnoresMouseInteraction = true
            };
            Append(_itemIcon);
        }

        public override void RecalculateChildren()
        {
            if (_itemIcon != null)
            {
                CalculatedStyle dimensions = GetInnerDimensions();
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);
                int iconWidth = Math.Max(0, width - IconPadding * 2);
                int iconHeight = Math.Max(0, height - IconPadding * 2);

                _itemIcon.Left.Set(IconPadding, 0f);
                _itemIcon.Top.Set(IconPadding, 0f);
                _itemIcon.Width.Set(iconWidth, 0f);
                _itemIcon.Height.Set(iconHeight, 0f);
            }

            base.RecalculateChildren();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (!_journeyFrameIndex.HasValue)
                return;

            RequestJourneyAtlasAsync();

            if (_journeyAtlas == null || _journeyAtlas.State != AssetState.Loaded)
                return;

            CalculatedStyle dimensions = GetDimensions();
            int width = Math.Max(0, (int)dimensions.Width);
            int height = Math.Max(0, (int)dimensions.Height);

            if (width <= 0 || height <= 0)
                return;

            Texture2D texture = _journeyAtlas.Value;
            int frameWidth = texture.Width / JourneyAtlasFramesPerRow;
            int sourceWidth = frameWidth + JourneyFrameWidthOffset;

            if (frameWidth <= 0 || sourceWidth <= 0 || texture.Height <= 0)
                return;

            var source = new Rectangle(_journeyFrameIndex.Value * frameWidth, 0, sourceWidth, texture.Height);
            int availableWidth = Math.Max(1, width - IconPadding * 2);
            int availableHeight = Math.Max(1, height - IconPadding * 2);
            float scale = Math.Min((float)availableWidth / source.Width, (float)availableHeight / source.Height);
            int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
            int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
            int drawX = (int)dimensions.X + (width - drawWidth) / 2;
            int drawY = (int)dimensions.Y + (height - drawHeight) / 2;
            var destination = new Rectangle(drawX, drawY, drawWidth, drawHeight);

            spriteBatch.Draw(texture, destination, source, Color.White);
        }

        private static void RequestJourneyAtlasAsync()
        {
            _journeyAtlas = DeferredTextureLoader.Request(JourneyAtlasPath, _journeyAtlas);
        }
    }
}