using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.UI.Vanilla
{
    internal static class VanillaPresentationIcons
    {
        private const string JourneyToggleTexturePath = "Images/UI/Creative/Journey_Toggle";
        private const string ClassicDifficultyTexturePath = "Images/UI/WorldCreation/IconDifficultyNormal";
        private const string ExpertDifficultyTexturePath = "Images/UI/WorldCreation/IconDifficultyExpert";
        private const string MasterDifficultyTexturePath = "Images/UI/WorldCreation/IconDifficultyMaster";
        private const string BestiaryTagAtlasPath = "Images/UI/Bestiary/Icon_Tags_Shadow";
        private const string BestiaryRankLightTexturePath = "Images/UI/Bestiary/Icon_Rank_Light";
        private const string RemixWorldTexturePath = "Images/UI/IconHallowCorruptionRemix";
        private const int AnglerHeadIndex = 22;
        private const int WallOfFleshHeadIndex = 22;
        private const int BestiaryTagColumns = 16;
        private const int BestiaryTagRows = 5;
        private const int CompletionBestiaryFrame = 62;
        private const int DefenseCounterTextureIndex = 58;
        private const int DefenseCounterColumns = 3;
        private const int DefenseCounterRows = 2;

        private static Asset<Texture2D> _bestiaryRankLightTexture;
        private static Asset<Texture2D> _bestiaryTagAtlas;
        private static Asset<Texture2D> _classicDifficultyTexture;
        private static Asset<Texture2D> _expertDifficultyTexture;
        private static Asset<Texture2D> _journeyToggleTexture;
        private static Asset<Texture2D> _masterDifficultyTexture;
        private static Asset<Texture2D> _remixWorldTexture;

        public static void DrawCompletion(Rectangle bounds, bool found)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _bestiaryTagAtlas ??= Main.Assets.Request<Texture2D>(BestiaryTagAtlasPath, AssetRequestMode.AsyncLoad);

            if (_bestiaryTagAtlas.State != AssetState.Loaded)
                return;

            int frameX = CompletionBestiaryFrame % BestiaryTagColumns;
            int frameY = CompletionBestiaryFrame / BestiaryTagColumns;
            DrawAtlasFrame(
                _bestiaryTagAtlas.Value,
                BestiaryTagColumns,
                BestiaryTagRows,
                frameX,
                frameY,
                bounds,
                found ? Color.White : Color.Black);
        }

        public static void DrawNpcDifficulty(Rectangle bounds, NpcDifficultyMode difficulty)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Asset<Texture2D> asset;

            switch (difficulty)
            {
                case NpcDifficultyMode.Classic:
                    _classicDifficultyTexture ??= Main.Assets.Request<Texture2D>(
                        ClassicDifficultyTexturePath,
                        AssetRequestMode.AsyncLoad);
                    asset = _classicDifficultyTexture;
                    break;
                case NpcDifficultyMode.Expert:
                    _expertDifficultyTexture ??= Main.Assets.Request<Texture2D>(
                        ExpertDifficultyTexturePath,
                        AssetRequestMode.AsyncLoad);
                    asset = _expertDifficultyTexture;
                    break;
                case NpcDifficultyMode.Master:
                    _masterDifficultyTexture ??= Main.Assets.Request<Texture2D>(
                        MasterDifficultyTexturePath,
                        AssetRequestMode.AsyncLoad);
                    asset = _masterDifficultyTexture;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(difficulty),
                        difficulty,
                        "Unsupported NPC difficulty mode.");
            }

            if (asset.State != AssetState.Loaded)
                return;

            DrawTexture(asset.Value, bounds, Color.White);
        }

        public static void DrawJourneyResearch(Rectangle bounds, bool researched)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _journeyToggleTexture ??= Main.Assets.Request<Texture2D>(
                JourneyToggleTexturePath,
                AssetRequestMode.AsyncLoad);

            if (_journeyToggleTexture.State != AssetState.Loaded)
                return;

            DrawTexture(_journeyToggleTexture.Value, bounds, researched ? Color.White : Color.Black);
        }

        public static void DrawCraftToggle(Rectangle bounds, int normalIndex, int hoverIndex, bool hovered)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || TextureAssets.CraftToggle == null)
                return;

            int index = hovered ? hoverIndex : normalIndex;

            if (index < 0 || index >= TextureAssets.CraftToggle.Length)
                return;

            Asset<Texture2D> asset = TextureAssets.CraftToggle[index];

            if (asset == null)
                return;

            if (asset.State == AssetState.NotLoaded)
                asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            if (asset.State != AssetState.Loaded)
                return;

            DrawTexture(asset.Value, bounds, Color.White);
        }

        public static void DrawDefenseCounterIcon(Rectangle bounds)
        {
            if (bounds.Width <= 0 ||
                bounds.Height <= 0 ||
                TextureAssets.Extra == null ||
                DefenseCounterTextureIndex >= TextureAssets.Extra.Length)
            {
                return;
            }

            Asset<Texture2D> asset = TextureAssets.Extra[DefenseCounterTextureIndex];

            if (asset == null)
                return;

            if (asset.State == AssetState.NotLoaded)
                asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            if (asset.State != AssetState.Loaded)
                return;

            DrawAtlasFrame(asset.Value, DefenseCounterColumns, DefenseCounterRows, 0, 0, bounds, Color.White);
        }

        public static void DrawBestiaryRankLight(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _bestiaryRankLightTexture ??= Main.Assets.Request<Texture2D>(
                BestiaryRankLightTexturePath,
                AssetRequestMode.AsyncLoad);

            if (_bestiaryRankLightTexture.State == AssetState.Loaded)
                DrawTexture(_bestiaryRankLightTexture.Value, bounds, Color.White);
        }

        public static void DrawPot(Rectangle bounds)
        {
            if (bounds.Width <= 0 ||
                bounds.Height <= 0 ||
                TextureAssets.Tile == null ||
                TileID.Pots >= TextureAssets.Tile.Length)
                return;

            Asset<Texture2D> asset = TextureAssets.Tile[TileID.Pots];

            if (asset == null)
                return;

            if (asset.State == AssetState.NotLoaded)
                asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            if (asset.State != AssetState.Loaded)
                return;

            const int cellSize = 16;
            const int atlasStep = 18;
            const int logicalSize = cellSize * 2;

            float scale = Math.Min(1f, Math.Min((float)bounds.Width / logicalSize, (float)bounds.Height / logicalSize));
            if (scale <= 0f)
                return;

            float drawSize = logicalSize * scale;
            var topLeft = new Vector2(
                bounds.X + (bounds.Width - drawSize) / 2f,
                bounds.Y + (bounds.Height - drawSize) / 2f);

            Texture2D texture = asset.Value;
            for (var row = 0; row < 2; row++)
            {
                for (var column = 0; column < 2; column++)
                {
                    var source = new Rectangle(column * atlasStep, row * atlasStep, cellSize, cellSize);
                    Vector2 position = topLeft + new Vector2(column * cellSize * scale, row * cellSize * scale);
                    Main.spriteBatch.Draw(
                        texture,
                        position,
                        source,
                        Color.White,
                        0f,
                        Vector2.Zero,
                        scale,
                        SpriteEffects.None,
                        0f);
                }
            }
        }

        public static void DrawAnglerHead(Rectangle bounds)
        {
            DrawTextureAsset(TextureAssets.NpcHead, AnglerHeadIndex, bounds);
        }

        public static void DrawWallOfFleshHead(Rectangle bounds)
        {
            DrawTextureAsset(TextureAssets.NpcHeadBoss, WallOfFleshHeadIndex, bounds);
        }

        public static void DrawRemixWorldIcon(Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            _remixWorldTexture ??= Main.Assets.Request<Texture2D>(RemixWorldTexturePath, AssetRequestMode.AsyncLoad);

            if (_remixWorldTexture.State != AssetState.Loaded)
                return;

            DrawTexture(_remixWorldTexture.Value, bounds, Color.White);
        }

        private static void DrawTextureAsset(Asset<Texture2D>[] assets, int index, Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0 || assets == null || index < 0 || index >= assets.Length)
                return;

            Asset<Texture2D> asset = assets[index];

            if (asset == null)
                return;

            if (asset.State == AssetState.NotLoaded)
                asset = Main.Assets.Request<Texture2D>(asset.Name, AssetRequestMode.AsyncLoad);

            if (asset.State != AssetState.Loaded)
                return;

            DrawTexture(asset.Value, bounds, Color.White);
        }

        private static void DrawAtlasFrame(
            Texture2D texture,
            int columns,
            int rows,
            int frameX,
            int frameY,
            Rectangle bounds,
            Color color)
        {
            if (texture == null || columns <= 0 || rows <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            int frameWidth = texture.Width / columns;
            int frameHeight = texture.Height / rows;

            if (frameWidth <= 0 || frameHeight <= 0)
                return;

            var source = new Rectangle(frameX * frameWidth, frameY * frameHeight, frameWidth, frameHeight);
            DrawTexture(texture, source, bounds, color);
        }

        private static void DrawTexture(Texture2D texture, Rectangle bounds, Color color)
        {
            if (texture == null || texture.Width <= 0 || texture.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            DrawTexture(texture, new Rectangle(0, 0, texture.Width, texture.Height), bounds, color);
        }

        private static void DrawTexture(Texture2D texture, Rectangle source, Rectangle bounds, Color color)
        {
            if (texture == null || source.Width <= 0 || source.Height <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            float scale = Math.Min((float)bounds.Width / source.Width, (float)bounds.Height / source.Height);
            int drawWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
            int drawHeight = Math.Max(1, (int)Math.Round(source.Height * scale));
            var destination = new Rectangle(
                bounds.X + (bounds.Width - drawWidth) / 2,
                bounds.Y + (bounds.Height - drawHeight) / 2,
                drawWidth,
                drawHeight);

            Main.spriteBatch.Draw(texture, destination, source, color);
        }
    }
}