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
            if (TryGetTextureAsset(npcNetId, out _, out Asset<Texture2D> asset))
                DeferredTextureLoader.Request(asset);
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

            if (!TryGetTextureAsset(npcNetId, out NPC sample, out Asset<Texture2D> asset))
                return;

            DeferredTextureLoader.Request(asset);

            if (asset.State != AssetState.Loaded)
                return;

            Texture2D texture = asset.Value;

            if (texture == null || texture.Width <= 0 || texture.Height <= 0)
                return;

            Rectangle frame = GetFrame(sample.type, texture);
            float scale = AsyncItemIconRenderer.CalculateScale(frame.Width, frame.Height, width, height);

            if (scale <= 0f)
                return;

            var center = new Vector2(x + width / 2f, y + height / 2f);

            var origin = new Vector2(frame.Width / 2f, frame.Height / 2f);

            bool tinted = !silhouette && sample.color != default;
            Color color = silhouette ? Color.Black : Color.White;
            if (tinted)
                color = sample.GetAlpha(Color.White);
            spriteBatch.Draw(texture, center, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);

            // Vanilla draws the variant's color as a second pass over the shared NPC texture.
            if (tinted)
            {
                Color variantColor = sample.GetColor(Color.White);
                spriteBatch.Draw(texture, center, frame, variantColor, 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        private static bool TryGetTextureAsset(int npcNetId, out NPC sample, out Asset<Texture2D> asset)
        {
            sample = null;
            asset = null;

            if (ContentSamples.NpcsByNetId == null ||
                !ContentSamples.NpcsByNetId.TryGetValue(npcNetId, out sample) ||
                sample == null)
                return false;

            Asset<Texture2D>[] npcTextures = TextureAssets.Npc;

            if (npcTextures == null || sample.type < 0 || sample.type >= npcTextures.Length)
                return false;

            asset = npcTextures[sample.type];
            return asset != null;
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
    }
}