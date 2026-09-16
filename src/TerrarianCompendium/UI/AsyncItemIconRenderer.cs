using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace TerrarianCompendium.UI
{
    internal static class AsyncItemIconRenderer
    {
        public static void Draw(int itemId, int x, int y, int width, int height, bool silhouette = false)
        {
            if (itemId <= 0 || width <= 0 || height <= 0)
                return;

            Asset<Texture2D> asset = TextureAssets.Item[itemId];

            RequestAsync(asset);

            if (asset.State != AssetState.Loaded)
                return;

            Texture2D texture = asset.Value;
            Rectangle frame = GetFrame(itemId, texture);
            float scale = CalculateScale(frame.Width, frame.Height, width, height);

            if (scale <= 0f)
                return;

            var center = new Vector2(x + width / 2f, y + height / 2f);
            var origin = new Vector2(frame.Width / 2f, frame.Height / 2f);
            Color color = silhouette ? Color.Black : Color.White;

            Main.spriteBatch.Draw(texture, center, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        public static void RequestAsync(int itemId)
        {
            if (itemId <= 0)
                return;

            RequestAsync(TextureAssets.Item[itemId]);
        }

        internal static float CalculateScale(int frameWidth, int frameHeight, int width, int height)
        {
            if (frameWidth <= 0 || frameHeight <= 0 || width <= 0 || height <= 0)
                return 0f;

            int sizeLimit = Math.Min(width, height);

            if (frameWidth <= sizeLimit && frameHeight <= sizeLimit)
                return 1f;

            return frameWidth <= frameHeight ? (float)sizeLimit / frameHeight : (float)sizeLimit / frameWidth;
        }

        private static Rectangle GetFrame(int itemId, Texture2D texture)
        {
            var frame = new Rectangle(0, 0, texture.Width, texture.Height);

            if (Main.itemAnimations == null || itemId >= Main.itemAnimations.Length)
                return frame;

            DrawAnimation animation = Main.itemAnimations[itemId];
            return animation?.GetFrame(texture) ?? frame;
        }

        private static void RequestAsync(Asset<Texture2D> asset)
        {
            if (asset.State != AssetState.NotLoaded)
                return;

            Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
        }
    }
}