using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;

namespace TerrarianCompendium.UI
{
    internal static class AsyncBuffIconRenderer
    {
        public static void RequestAsync(int buffId)
        {
            if (!TryGetAsset(buffId, out Asset<Texture2D> asset))
                return;

            RequestAsync(asset);
        }

        public static void Draw(SpriteBatch spriteBatch, int buffId, int x, int y, int width, int height)
        {
            if (spriteBatch == null || width <= 0 || height <= 0)
                return;

            if (!TryGetAsset(buffId, out Asset<Texture2D> asset))
                return;

            RequestAsync(asset);

            if (asset.State != AssetState.Loaded)
                return;

            Texture2D texture = asset.Value;

            if (texture == null || texture.Width <= 0 || texture.Height <= 0)
                return;

            float scale = AsyncItemIconRenderer.CalculateScale(texture.Width, texture.Height, width, height);

            if (scale <= 0f)
                return;

            var center = new Vector2(x + width / 2f, y + height / 2f);
            var origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            spriteBatch.Draw(texture, center, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        private static bool TryGetAsset(int buffId, out Asset<Texture2D> asset)
        {
            asset = null;

            Asset<Texture2D>[] textures = TextureAssets.Buff;

            if (textures == null || buffId <= 0 || buffId >= textures.Length)
                return false;

            asset = textures[buffId];
            return asset != null;
        }

        private static void RequestAsync(Asset<Texture2D> asset)
        {
            if (asset == null || asset.State != AssetState.NotLoaded)
                return;

            Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
        }
    }
}