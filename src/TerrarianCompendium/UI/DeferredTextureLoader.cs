using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace TerrarianCompendium.UI
{
    internal static class DeferredTextureLoader
    {
        private const int MaxLoadsPerUpdate = 4;
        private const int LoadBudgetMilliseconds = 4;

        private static readonly DeferredLoadQueue<Asset<Texture2D>> _pending = new(
            asset => !asset.IsDisposed && asset.State == AssetState.NotLoaded,
            asset => Main.Assets.Request<Texture2D>(asset.Name),
            Stopwatch.GetTimestamp,
            MaxLoadsPerUpdate,
            Math.Max(1L, Stopwatch.Frequency * LoadBudgetMilliseconds / 1000));

        public static Asset<Texture2D> Request(string assetName, Asset<Texture2D> cachedAsset = null)
        {
            return Request(cachedAsset ?? Main.Assets.Request<Texture2D>(assetName, AssetRequestMode.DoNotLoad));
        }

        public static Asset<Texture2D> Request(Asset<Texture2D> asset)
        {
            _pending.Enqueue(asset);
            return asset;
        }

        public static void ProcessPending()
        {
            _pending.ProcessPending();
        }

        public static void Clear()
        {
            _pending.Clear();
        }
    }
}