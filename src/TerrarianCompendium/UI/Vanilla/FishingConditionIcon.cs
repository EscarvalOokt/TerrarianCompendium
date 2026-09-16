using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.UI;
using TerrariaModder.Core.UI;
using TerrariaModder.Core.UI.Widgets;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI.Vanilla
{
    internal sealed class FishingConditionIcon : UIElement
    {
        private readonly ExclusionMarker _exclusionMarker;
        private readonly CompendiumLocalization _localization;
        private readonly VanillaBestiaryConditionIcon _nativeIcon;
        private FishingSourceConditionKind? _condition;
        private bool _isExcluded;
        private bool _isExcludedLavaAndHoney;
        private bool _isGeneralFishing;
        private int _representativeItemId;
        private FishingVisualKind _visualKind;

        public FishingConditionIcon(VanillaBestiaryFilterCatalog filterCatalog, CompendiumLocalization localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            if (filterCatalog != null)
            {
                _nativeIcon = new VanillaBestiaryConditionIcon(filterCatalog)
                {
                    ShowTooltip = false,
                    IgnoresMouseInteraction = true
                };
                Append(_nativeIcon);
            }

            _exclusionMarker = new ExclusionMarker();
            Append(_exclusionMarker);
        }

        public void Bind(FishingSourceConditionKind condition, bool isExcluded = false)
        {
            _condition = condition;
            _isExcluded = isExcluded;
            _isExcludedLavaAndHoney = false;
            _isGeneralFishing = false;
            _exclusionMarker.IsExcluded = isExcluded;
            _representativeItemId = 0;
            _visualKind = FishingVisualKind.None;
            IgnoresMouseInteraction = false;

            if (_nativeIcon != null &&
                TryGetBestiaryDisplayNameKey(condition, out string displayNameKey) &&
                _nativeIcon.Bind(displayNameKey, GetTooltip(condition)))
            {
                _nativeIcon.Width = StyleDimension.Fill;
                _nativeIcon.Height = StyleDimension.Fill;
                _visualKind = FishingVisualKind.NativeBestiary;
                return;
            }

            _nativeIcon?.Hide();

            if (TryGetRepresentativeItemId(condition, out int itemId))
            {
                _representativeItemId = itemId;
                _visualKind = FishingVisualKind.Item;
                AsyncItemIconRenderer.RequestAsync(itemId);
                return;
            }

            _visualKind = condition switch
            {
                FishingSourceConditionKind.HardMode => FishingVisualKind.WallOfFleshHead,
                FishingSourceConditionKind.AnglerQuest => FishingVisualKind.AnglerHead,
                FishingSourceConditionKind.Remix => FishingVisualKind.RemixWorld,
                FishingSourceConditionKind.RemixOcean => FishingVisualKind.RemixWorld,
                _ => FishingVisualKind.None
            };
        }

        public void BindExcludedLavaAndHoney()
        {
            _condition = null;
            _isExcluded = false;
            _isExcludedLavaAndHoney = true;
            _isGeneralFishing = false;
            _exclusionMarker.IsExcluded = true;
            _representativeItemId = 0;
            _visualKind = FishingVisualKind.ExcludedLavaAndHoney;
            IgnoresMouseInteraction = false;
            _nativeIcon?.Hide();
            AsyncItemIconRenderer.RequestAsync(ItemID.LavaBucket);
            AsyncItemIconRenderer.RequestAsync(ItemID.HoneyBucket);
        }

        public void BindGeneralFishing()
        {
            _condition = null;
            _isExcluded = false;
            _isExcludedLavaAndHoney = false;
            _isGeneralFishing = true;
            _exclusionMarker.IsExcluded = false;
            _representativeItemId = ItemID.WoodFishingPole;
            _visualKind = FishingVisualKind.Item;
            IgnoresMouseInteraction = false;
            _nativeIcon?.Hide();
            AsyncItemIconRenderer.RequestAsync(_representativeItemId);
        }

        public void Hide()
        {
            _condition = null;
            _isExcluded = false;
            _isExcludedLavaAndHoney = false;
            _isGeneralFishing = false;
            _exclusionMarker.IsExcluded = false;
            _representativeItemId = 0;
            _visualKind = FishingVisualKind.None;
            IgnoresMouseInteraction = true;
            _nativeIcon?.Hide();
            Width.Set(0f, 0f);
            Height.Set(0f, 0f);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (_visualKind != FishingVisualKind.NativeBestiary)
            {
                CalculatedStyle dimensions = GetDimensions();
                int padding = VirtualGridLayout.IconPadding;
                var bounds = new Rectangle(
                    (int)dimensions.X + padding,
                    (int)dimensions.Y + padding,
                    Math.Max(0, (int)dimensions.Width - padding * 2),
                    Math.Max(0, (int)dimensions.Height - padding * 2));

                switch (_visualKind)
                {
                    case FishingVisualKind.Item:
                        AsyncItemIconRenderer.Draw(
                            _representativeItemId,
                            bounds.X,
                            bounds.Y,
                            bounds.Width,
                            bounds.Height);
                        break;
                    case FishingVisualKind.AnglerHead:
                        VanillaPresentationIcons.DrawAnglerHead(bounds);
                        break;
                    case FishingVisualKind.WallOfFleshHead:
                        VanillaPresentationIcons.DrawWallOfFleshHead(bounds);
                        break;
                    case FishingVisualKind.RemixWorld:
                        VanillaPresentationIcons.DrawRemixWorldIcon(bounds);
                        break;
                    case FishingVisualKind.ExcludedLavaAndHoney:
                        DrawExcludedLavaAndHoney(bounds);
                        break;
                }
            }

            if (!IsMouseHovering)
                return;

            string tooltip = GetCurrentTooltip();
            if (!string.IsNullOrEmpty(tooltip))
                Tooltip.Set(tooltip);
        }

        private static void DrawExcludedLavaAndHoney(Rectangle bounds)
        {
            const int gap = 2;
            int availableWidth = Math.Max(0, bounds.Width - gap);
            int leftWidth = availableWidth / 2;
            int rightWidth = availableWidth - leftWidth;

            AsyncItemIconRenderer.Draw(ItemID.LavaBucket, bounds.X, bounds.Y, leftWidth, bounds.Height);
            AsyncItemIconRenderer.Draw(
                ItemID.HoneyBucket,
                bounds.X + leftWidth + gap,
                bounds.Y,
                rightWidth,
                bounds.Height);
        }

        private static bool TryGetBestiaryDisplayNameKey(
            FishingSourceConditionKind condition,
            out string displayNameKey)
        {
            switch (condition)
            {
                case FishingSourceConditionKind.Dungeon:
                    displayNameKey = "Bestiary_Biomes.TheDungeon";
                    return true;
                case FishingSourceConditionKind.Hallow:
                    displayNameKey = "Bestiary_Biomes.TheHallow";
                    return true;
                case FishingSourceConditionKind.TrueDesert:
                case FishingSourceConditionKind.Desert:
                    displayNameKey = "Bestiary_Biomes.Desert";
                    return true;
                case FishingSourceConditionKind.TrueSnow:
                case FishingSourceConditionKind.Snow:
                    displayNameKey = "Bestiary_Biomes.Snow";
                    return true;
                case FishingSourceConditionKind.Corruption:
                    displayNameKey = "Bestiary_Biomes.TheCorruption";
                    return true;
                case FishingSourceConditionKind.Crimson:
                    displayNameKey = "Bestiary_Biomes.Crimson";
                    return true;
                case FishingSourceConditionKind.Jungle:
                    displayNameKey = "Bestiary_Biomes.Jungle";
                    return true;
                case FishingSourceConditionKind.HallowDesert:
                    displayNameKey = "Bestiary_Biomes.HallowDesert";
                    return true;
                case FishingSourceConditionKind.OriginalOcean:
                case FishingSourceConditionKind.Ocean:
                    displayNameKey = "Bestiary_Biomes.Ocean";
                    return true;
                case FishingSourceConditionKind.Height0:
                    displayNameKey = "Bestiary_Biomes.Sky";
                    return true;
                case FishingSourceConditionKind.Height1:
                case FishingSourceConditionKind.Height1And2:
                case FishingSourceConditionKind.HeightAboveAnd1:
                case FishingSourceConditionKind.HeightUnder2:
                    displayNameKey = "Bestiary_Biomes.Surface";
                    return true;
                case FishingSourceConditionKind.Height2:
                case FishingSourceConditionKind.HeightAbove1:
                    displayNameKey = "Bestiary_Biomes.Underground";
                    return true;
                case FishingSourceConditionKind.Height3:
                case FishingSourceConditionKind.HeightAbove2:
                case FishingSourceConditionKind.UnderRockLayer:
                    displayNameKey = "Bestiary_Biomes.Caverns";
                    return true;
                case FishingSourceConditionKind.BloodMoon:
                    displayNameKey = "Bestiary_Events.BloodMoon";
                    return true;
                default:
                    displayNameKey = null;
                    return false;
            }
        }

        private static bool TryGetRepresentativeItemId(FishingSourceConditionKind condition, out int itemId)
        {
            switch (condition)
            {
                case FishingSourceConditionKind.EarlyMode:
                    itemId = ItemID.Bunny;
                    return true;
                case FishingSourceConditionKind.InLava:
                    itemId = ItemID.LavaBucket;
                    return true;
                case FishingSourceConditionKind.InHoney:
                    itemId = ItemID.HoneyBucket;
                    return true;
                case FishingSourceConditionKind.CanFishInLava:
                    itemId = ItemID.HotlineFishingHook;
                    return true;
                case FishingSourceConditionKind.Beach:
                    itemId = ItemID.Coral;
                    return true;
                case FishingSourceConditionKind.GlowingMushrooms:
                    itemId = ItemID.GlowingMushroom;
                    return true;
                case FishingSourceConditionKind.Junk:
                    itemId = ItemID.OldShoe;
                    return true;
                case FishingSourceConditionKind.Crate:
                    itemId = ItemID.WoodenCrate;
                    return true;
                case FishingSourceConditionKind.Water1000:
                    itemId = ItemID.WaterBucket;
                    return true;
                case FishingSourceConditionKind.DidNotUseCombatBook:
                    itemId = ItemID.CombatBook;
                    return true;
                default:
                    itemId = 0;
                    return false;
            }
        }

        private string GetCurrentTooltip()
        {
            if (_isGeneralFishing)
                return _localization.Get(CompendiumTextKeys.Fishing.General);
            if (_isExcludedLavaAndHoney)
                return _localization.Get(CompendiumTextKeys.Fishing.ExcludedLavaAndHoney);

            return _condition.HasValue ? GetTooltip(_condition.Value) : string.Empty;
        }

        private string GetTooltip(FishingSourceConditionKind condition)
        {
            string conditionText = _localization.Get(CompendiumTextKeys.Fishing.Condition(condition));
            return _isExcluded
                ? _localization.Format(CompendiumTextKeys.Fishing.ExcludedCondition, conditionText)
                : conditionText;
        }

        private sealed class ExclusionMarker : UIElement
        {
            private const int BorderThickness = 2;
            private const int CornerMarkerSize = 6;

            public ExclusionMarker()
            {
                Width = StyleDimension.Fill;
                Height = StyleDimension.Fill;
                IgnoresMouseInteraction = true;
            }

            public bool IsExcluded { get; set; }

            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (!IsExcluded)
                    return;

                CalculatedStyle dimensions = GetDimensions();
                var x = (int)dimensions.X;
                var y = (int)dimensions.Y;
                int width = Math.Max(0, (int)dimensions.Width);
                int height = Math.Max(0, (int)dimensions.Height);

                if (width <= 0 || height <= 0)
                    return;

                UIRenderer.DrawRectOutline(x, y, width, height, UIColors.Error, BorderThickness);

                int markerSize = Math.Min(CornerMarkerSize, Math.Min(width, height));
                UIRenderer.DrawRect(x + width - markerSize, y, markerSize, markerSize, UIColors.Error);
            }
        }

        private enum FishingVisualKind
        {
            None,
            NativeBestiary,
            Item,
            AnglerHead,
            WallOfFleshHead,
            RemixWorld,
            ExcludedLavaAndHoney
        }
    }
}