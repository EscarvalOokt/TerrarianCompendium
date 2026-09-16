using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace TerrarianCompendium.UI
{
    internal static class AsyncNpcIconRenderer
    {
        private const uint AnimationTicksPerFrame = 8u;

        public static void RequestAsync(int npcNetId)
        {
            if (!TryGetTextureAsset(npcNetId, out Asset<Texture2D> asset))
                return;

            RequestAsync(asset);
        }

        public static void Draw(
            SpriteBatch spriteBatch,
            int npcNetId,
            int x,
            int y,
            int width,
            int height,
            bool silhouette = false)
        {
            if (spriteBatch == null || width <= 0 || height <= 0)
                return;

            if (!TryResolveNpcType(npcNetId, out int npcType))
                return;

            if (!TryGetTextureAsset(npcType, isResolvedType: true, out Asset<Texture2D> asset))
                return;

            RequestAsync(asset);

            if (asset.State != AssetState.Loaded)
                return;

            Texture2D texture = asset.Value;

            if (texture == null || texture.Width <= 0 || texture.Height <= 0)
                return;

            Rectangle frame = GetFrame(npcType, texture);
            float scale = AsyncItemIconRenderer.CalculateScale(frame.Width, frame.Height, width, height);

            if (scale <= 0f)
                return;

            var center = new Vector2(x + width / 2f, y + height / 2f);

            var origin = new Vector2(frame.Width / 2f, frame.Height / 2f);

            Color color = silhouette ? Color.Black : Color.White;

            spriteBatch.Draw(texture, center, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }

        private static bool TryGetTextureAsset(int npcNetId, out Asset<Texture2D> asset)
        {
            asset = null;

            if (!TryResolveNpcType(npcNetId, out int npcType))
                return false;

            return TryGetTextureAsset(npcType, isResolvedType: true, out asset);
        }

        private static bool TryGetTextureAsset(int npcType, bool isResolvedType, out Asset<Texture2D> asset)
        {
            asset = null;

            if (!isResolvedType || npcType < 0)
                return false;

            Asset<Texture2D>[] npcTextures = TextureAssets.Npc;

            if (npcTextures == null || npcType >= npcTextures.Length)
                return false;

            asset = npcTextures[npcType];
            return asset != null;
        }

        private static bool TryResolveNpcType(int npcNetId, out int npcType)
        {
            npcType = -1;

            if (ContentSamples.NpcsByNetId == null ||
                !ContentSamples.NpcsByNetId.TryGetValue(npcNetId, out NPC sample) ||
                sample == null)
            {
                return false;
            }

            npcType = sample.type;
            return npcType >= 0;
        }

        private static Rectangle GetFrame(int npcType, Texture2D texture)
        {
            int frameCount = GetFrameCount(npcType);

            if (frameCount <= 1)
                return new Rectangle(0, 0, texture.Width, texture.Height);

            int frameHeight = texture.Height / frameCount;

            if (frameHeight <= 0)
                return new Rectangle(0, 0, texture.Width, texture.Height);

            var frameIndex = (int)(Main.GameUpdateCount / AnimationTicksPerFrame % (uint)frameCount);

            int frameY = frameIndex * frameHeight;

            if (frameY < 0 || frameY + frameHeight > texture.Height)
                frameY = 0;

            return new Rectangle(0, frameY, texture.Width, frameHeight);
        }

        private static int GetFrameCount(int npcType)
        {
            int[] frameCounts = Main.npcFrameCount;

            if (frameCounts == null || npcType < 0 || npcType >= frameCounts.Length || frameCounts[npcType] <= 0)
            {
                return 1;
            }

            return frameCounts[npcType];
        }

        private static void RequestAsync(Asset<Texture2D> asset)
        {
            if (asset == null || asset.State != AssetState.NotLoaded)
                return;

            Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);
        }
    }
}