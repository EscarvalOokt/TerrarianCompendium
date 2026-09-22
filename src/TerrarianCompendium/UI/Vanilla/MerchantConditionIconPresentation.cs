using System;
using System.Globalization;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.UI.Vanilla
{
    internal enum MerchantConditionVisualKind
    {
        Generic,
        BestiaryTag,
        Item,
        Npc,
        BossHead,
        RemixWorld,
        NotTheBeesWorld,
        BestiaryCompletion
    }

    internal readonly struct MerchantConditionVisualPart(MerchantConditionVisualKind kind, int value = 0)
    {
        public MerchantConditionVisualKind Kind { get; } = kind;

        public int Value { get; } = value;
    }

    internal readonly struct MerchantConditionVisualDescriptor
    {
        private readonly MerchantConditionVisualPart _first;
        private readonly MerchantConditionVisualPart _second;
        private readonly MerchantConditionVisualPart _third;
        private readonly MerchantConditionVisualPart _fourth;

        private MerchantConditionVisualDescriptor(
            MerchantConditionVisualPart first,
            MerchantConditionVisualPart second,
            MerchantConditionVisualPart third,
            MerchantConditionVisualPart fourth,
            int partCount,
            string argumentText,
            bool isNegated)
        {
            _first = first;
            _second = second;
            _third = third;
            _fourth = fourth;
            PartCount = partCount;
            ArgumentText = argumentText ?? string.Empty;
            IsNegated = isNegated;
        }

        public int PartCount { get; }

        public string ArgumentText { get; }

        public bool IsNegated { get; }

        public MerchantConditionVisualPart GetPart(int index)
        {
            return index switch
            {
                0 when PartCount > 0 => _first,
                1 when PartCount > 1 => _second,
                2 when PartCount > 2 => _third,
                3 when PartCount > 3 => _fourth,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        public static MerchantConditionVisualDescriptor Single(
            MerchantConditionVisualPart part,
            string argumentText = null,
            bool isNegated = false)
        {
            return new MerchantConditionVisualDescriptor(part, default, default, default, 1, argumentText, isNegated);
        }

        public static MerchantConditionVisualDescriptor Pair(
            MerchantConditionVisualPart first,
            MerchantConditionVisualPart second,
            string argumentText = null,
            bool isNegated = false)
        {
            return new MerchantConditionVisualDescriptor(first, second, default, default, 2, argumentText, isNegated);
        }

        public static MerchantConditionVisualDescriptor Quad(
            MerchantConditionVisualPart first,
            MerchantConditionVisualPart second,
            MerchantConditionVisualPart third,
            MerchantConditionVisualPart fourth,
            string argumentText = null,
            bool isNegated = false)
        {
            return new MerchantConditionVisualDescriptor(first, second, third, fourth, 4, argumentText, isNegated);
        }
    }

    internal static class MerchantConditionIconPresentation
    {
        public const int IconSize = 24;
        public const int ArgumentGap = 3;
        public const int TokenGap = 4;

        private const int TextHeight = 16;

        private const int BestiaryCavernsFrame = 2;
        private const int BestiaryDesertFrame = 3;
        private const int BestiarySnowFrame = 5;
        private const int BestiaryCorruptionFrame = 7;
        private const int BestiaryCrimsonFrame = 12;
        private const int BestiaryHallowFrame = 17;
        private const int BestiaryJungleFrame = 22;
        private const int BestiarySkyFrame = 26;
        private const int BestiaryOceanFrame = 28;
        private const int BestiaryUnderworldFrame = 33;
        private const int BestiaryGraveyardFrame = 35;
        private const int BestiaryDayTimeFrame = 36;
        private const int BestiaryNightTimeFrame = 37;
        private const int BestiaryBloodMoonFrame = 38;
        private const int BestiaryWindyDayFrame = 41;
        private const int BestiaryFrostLegionFrame = 54;

        private const int CopperPerSilver = 100;
        private const int CopperPerGold = CopperPerSilver * 100;
        private const int CopperPerPlatinum = CopperPerGold * 100;

        public static MerchantConditionVisualDescriptor Describe(MerchantSourceCondition condition)
        {
            bool negated = condition.IsNegated;

            switch (condition.Kind)
            {
                case MerchantSourceConditionKind.HardMode:
                    return Boss(NPCID.WallofFlesh, negated);
                case MerchantSourceConditionKind.DayTime:
                    return Bestiary(negated ? BestiaryNightTimeFrame : BestiaryDayTimeFrame, isNegated: false);
                case MerchantSourceConditionKind.BloodMoon:
                    return Bestiary(BestiaryBloodMoonFrame, negated);
                case MerchantSourceConditionKind.Halloween:
                    return Item(ItemID.Pumpkin, negated);
                case MerchantSourceConditionKind.Xmas:
                    return Item(ItemID.Present, negated);
                case MerchantSourceConditionKind.BirthdayParty:
                    return Item(ItemID.PartyHat, negated);
                case MerchantSourceConditionKind.HappyWindyDay:
                    return Bestiary(BestiaryWindyDayFrame, negated);
                case MerchantSourceConditionKind.Eclipse:
                    return Item(ItemID.SolarTablet, negated);
                case MerchantSourceConditionKind.LanternNight:
                    return Item(ItemID.ChineseLantern, negated);
                case MerchantSourceConditionKind.Storm:
                    return Item(ItemID.Umbrella, negated);

                case MerchantSourceConditionKind.WorldCrimson:
                    return Bestiary(BestiaryCrimsonFrame, negated);
                case MerchantSourceConditionKind.WorldRemix:
                    return Single(MerchantConditionVisualKind.RemixWorld, isNegated: negated);
                case MerchantSourceConditionKind.WorldTenthAnniversary:
                    return Item(ItemID.PartyHat, negated);
                case MerchantSourceConditionKind.WorldNotTheBees:
                    return Single(MerchantConditionVisualKind.NotTheBeesWorld, isNegated: negated);
                case MerchantSourceConditionKind.WorldGetGood:
                    return Item(ItemID.Boulder, negated);
                case MerchantSourceConditionKind.WorldInfectedSeed:
                    return Item(ItemID.Clentaminator, negated);
                case MerchantSourceConditionKind.WorldVampireSeed:
                    return Item(ItemID.VampireKnives, negated);
                case MerchantSourceConditionKind.WorldShadowOrbSmashed:
                    return MerchantConditionVisualDescriptor.Pair(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.Item, ItemID.ShadowOrb),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.Item, ItemID.CrimsonHeart),
                        isNegated: negated);
                case MerchantSourceConditionKind.WorldSilverOreTier:
                    return DescribeSilverTier(condition);

                case MerchantSourceConditionKind.ZoneSnow:
                    return Bestiary(BestiarySnowFrame, negated);
                case MerchantSourceConditionKind.ZoneJungle:
                    return Bestiary(BestiaryJungleFrame, negated);
                case MerchantSourceConditionKind.ZoneGraveyard:
                    return Bestiary(BestiaryGraveyardFrame, negated);
                case MerchantSourceConditionKind.ZoneBeach:
                    return Item(ItemID.Coral, negated);
                case MerchantSourceConditionKind.ZoneDesert:
                    return Bestiary(BestiaryDesertFrame, negated);
                case MerchantSourceConditionKind.ZoneHallow:
                    return Bestiary(BestiaryHallowFrame, negated);
                case MerchantSourceConditionKind.ZoneCorrupt:
                    return Bestiary(BestiaryCorruptionFrame, negated);
                case MerchantSourceConditionKind.ZoneCrimson:
                    return Bestiary(BestiaryCrimsonFrame, negated);
                case MerchantSourceConditionKind.ZoneGlowshroom:
                    return Item(ItemID.GlowingMushroom, negated);
                case MerchantSourceConditionKind.ZoneSkyHeight:
                    return Bestiary(BestiarySkyFrame, negated);
                case MerchantSourceConditionKind.ZoneUnderworldHeight:
                    return Bestiary(BestiaryUnderworldFrame, negated);
                case MerchantSourceConditionKind.ShoppingZoneForest:
                    return Item(ItemID.Acorn, negated);

                case MerchantSourceConditionKind.DownedBoss1:
                    return Boss(NPCID.EyeofCthulhu, negated);
                case MerchantSourceConditionKind.DownedBoss2:
                    return MerchantConditionVisualDescriptor.Pair(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.EaterofWorldsHead),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.BrainofCthulhu),
                        isNegated: negated);
                case MerchantSourceConditionKind.DownedBoss3:
                    return Boss(NPCID.SkeletronHead, negated);
                case MerchantSourceConditionKind.DownedSlimeKing:
                    return Boss(NPCID.KingSlime, negated);
                case MerchantSourceConditionKind.DownedQueenSlime:
                    return Boss(NPCID.QueenSlimeBoss, negated);
                case MerchantSourceConditionKind.DownedQueenBee:
                    return Boss(NPCID.QueenBee, negated);
                case MerchantSourceConditionKind.DownedClown:
                    return Npc(NPCID.Clown, negated);
                case MerchantSourceConditionKind.DownedAncientCultist:
                    return Boss(NPCID.CultistBoss, negated);
                case MerchantSourceConditionKind.DownedFrost:
                    return Bestiary(BestiaryFrostLegionFrame, negated);
                case MerchantSourceConditionKind.DownedPirates:
                    return Item(ItemID.PirateMap, negated);
                case MerchantSourceConditionKind.DownedPlantBoss:
                    return Boss(NPCID.Plantera, negated);
                case MerchantSourceConditionKind.DownedGolemBoss:
                    return Boss(NPCID.GolemHead, negated);
                case MerchantSourceConditionKind.DownedMartians:
                    return Npc(NPCID.MartianProbe, negated);
                case MerchantSourceConditionKind.DownedMoonlord:
                    return Boss(NPCID.MoonLordHead, negated);
                case MerchantSourceConditionKind.DownedMechBossAny:
                    return MerchantConditionVisualDescriptor.Quad(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.TheDestroyer),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.Retinazer),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.Spazmatism),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.SkeletronPrime),
                        isNegated: negated);
                case MerchantSourceConditionKind.DownedMechBoss1:
                    return Boss(NPCID.TheDestroyer, negated);
                case MerchantSourceConditionKind.DownedMechBoss2:
                    return MerchantConditionVisualDescriptor.Pair(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.Retinazer),
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, NPCID.Spazmatism),
                        isNegated: negated);
                case MerchantSourceConditionKind.DownedMechBoss3:
                    return Boss(NPCID.SkeletronPrime, negated);
                case MerchantSourceConditionKind.DownedTowerSolar:
                    return Boss(NPCID.LunarTowerSolar, negated);
                case MerchantSourceConditionKind.DownedDeerclops:
                    return Boss(NPCID.Deerclops, negated);

                case MerchantSourceConditionKind.NpcPresent:
                    return condition.Argument > 0 ? Npc(condition.Argument, negated) : Generic(negated);
                case MerchantSourceConditionKind.PlayerHasItem:
                case MerchantSourceConditionKind.PlayerHasItemInAnyInventory:
                    return condition.Argument > 0 ? Item(condition.Argument, negated) : Generic(negated);
                case MerchantSourceConditionKind.PlayerLifeMaxAtLeast:
                    return ItemWithComparison(ItemID.LifeCrystal, condition.Argument, ">=", "<", negated);
                case MerchantSourceConditionKind.PlayerManaMaxAtLeast:
                    return ItemWithComparison(ItemID.ManaCrystal, condition.Argument, ">=", "<", negated);
                case MerchantSourceConditionKind.PlayerCoinValueAtLeast:
                    return DescribeCoinRequirement(condition.Argument, negated);
                case MerchantSourceConditionKind.PlayerTeamNonZero:
                    return Item(ItemID.TeamDye, negated);
                case MerchantSourceConditionKind.MultiplayerClient:
                    return Item(ItemID.WormholePotion, negated);
                case MerchantSourceConditionKind.GolferScoreAtLeast:
                    return ItemWithComparison(ItemID.GolfBall, condition.Argument, ">=", "<", negated);
                case MerchantSourceConditionKind.GolferScoreGreaterThan:
                    return ItemWithComparison(ItemID.GolfBall, condition.Argument, ">", "<=", negated);
                case MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints:
                    return MerchantConditionVisualDescriptor.Single(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BestiaryCompletion),
                        (negated ? "<" : ">=") + FormatBasisPoints(condition.Argument),
                        isNegated: false);
                case MerchantSourceConditionKind.BestiaryFairyTorchUnlocked:
                    return Item(ItemID.FairyGlowstick, negated);
                case MerchantSourceConditionKind.MoonPhase:
                    return Item(ItemID.Sextant, negated);
                case MerchantSourceConditionKind.SkeletonMerchantEarlyCycle:
                    return Npc(NPCID.SkeletonMerchant, negated);
                case MerchantSourceConditionKind.PlayerAteArtisanBread:
                    return Item(ItemID.ArtisanLoaf, negated);

                case MerchantSourceConditionKind.PylonNearbyNpcRequirement:
                    return Item(ItemID.TeleportationPylonPurity, negated);
                case MerchantSourceConditionKind.PylonForestLocation:
                    return Item(ItemID.TeleportationPylonPurity, negated);
                case MerchantSourceConditionKind.PylonCavernLocation:
                    return Bestiary(BestiaryCavernsFrame, negated);
                case MerchantSourceConditionKind.PylonOceanLocation:
                    return Bestiary(BestiaryOceanFrame, negated);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(condition),
                        condition.Kind,
                        "Unsupported merchant source condition kind.");
            }
        }

        public static MerchantConditionVisualDescriptor DescribeRandomStock()
        {
            return Npc(NPCID.TravellingMerchant, isNegated: false);
        }

        public static MerchantConditionVisualDescriptor DescribeShopCapacityLimited()
        {
            return Item(ItemID.Chest, isNegated: false);
        }

        public static int MeasureWidth(MerchantConditionVisualDescriptor descriptor)
        {
            int width = IconSize;

            if (descriptor.ArgumentText.Length > 0)
                width += ArgumentGap + UIRenderer.MeasureText(descriptor.ArgumentText);

            return width;
        }

        public static void Request(MerchantConditionVisualDescriptor descriptor)
        {
            for (var index = 0; index < descriptor.PartCount; index++)
            {
                MerchantConditionVisualPart part = descriptor.GetPart(index);
                MerchantConditionVisualKind? requestKind = GetAsyncRequestKind(part);

                if (requestKind == MerchantConditionVisualKind.Item)
                    AsyncItemIconRenderer.RequestAsync(part.Value);
                else if (requestKind == MerchantConditionVisualKind.Npc)
                    AsyncNpcIconRenderer.RequestAsync(part.Value);
            }
        }

        internal static MerchantConditionVisualKind? GetAsyncRequestKind(MerchantConditionVisualPart part)
        {
            if (part.Value <= 0)
                return null;

            return part.Kind switch
            {
                MerchantConditionVisualKind.Item => MerchantConditionVisualKind.Item,
                MerchantConditionVisualKind.Npc => MerchantConditionVisualKind.Npc,
                _ => null
            };
        }

        public static void Draw(MerchantConditionVisualDescriptor descriptor, int x, int y, int width, int height)
        {
            if (width <= 0 || height <= 0 || descriptor.PartCount <= 0)
                return;

            int iconSize = Math.Min(IconSize, Math.Min(width, height));
            var iconBounds = new Rectangle(x, y + Math.Max(0, (height - iconSize) / 2), iconSize, iconSize);

            DrawParts(descriptor, iconBounds);

            if (descriptor.IsNegated)
                DrawNegationMarker(iconBounds);

            if (descriptor.ArgumentText.Length == 0)
                return;

            int textX = iconBounds.Right + ArgumentGap;
            int availableTextWidth = Math.Max(0, x + width - textX);
            string displayText = TruncatedTextPresentation.Truncate(descriptor.ArgumentText, availableTextWidth, out _);

            if (displayText.Length == 0)
                return;

            int textY = y + Math.Max(0, (height - TextHeight) / 2);
            UIRenderer.DrawText(displayText, textX, textY, UIColors.Text);
        }

        private static MerchantConditionVisualDescriptor DescribeSilverTier(MerchantSourceCondition condition)
        {
            int itemId = condition.Argument switch
            {
                9 => condition.IsNegated ? ItemID.TungstenOre : ItemID.SilverOre,
                168 => condition.IsNegated ? ItemID.SilverOre : ItemID.TungstenOre,
                _ => 0
            };

            return itemId > 0 ? Item(itemId, isNegated: false) : Generic(condition.IsNegated);
        }

        private static MerchantConditionVisualDescriptor DescribeCoinRequirement(int value, bool isNegated)
        {
            int normalized = Math.Max(0, value);
            int itemId;
            int denomination;

            if (normalized >= CopperPerPlatinum && normalized % CopperPerPlatinum == 0)
            {
                itemId = ItemID.PlatinumCoin;
                denomination = CopperPerPlatinum;
            }
            else if (normalized >= CopperPerGold && normalized % CopperPerGold == 0)
            {
                itemId = ItemID.GoldCoin;
                denomination = CopperPerGold;
            }
            else if (normalized >= CopperPerSilver && normalized % CopperPerSilver == 0)
            {
                itemId = ItemID.SilverCoin;
                denomination = CopperPerSilver;
            }
            else
            {
                itemId = ItemID.CopperCoin;
                denomination = 1;
            }

            var text = (normalized / denomination).ToString(CultureInfo.InvariantCulture);

            return MerchantConditionVisualDescriptor.Single(
                new MerchantConditionVisualPart(MerchantConditionVisualKind.Item, itemId),
                (isNegated ? "<" : ">=") + text,
                isNegated: false);
        }

        private static MerchantConditionVisualDescriptor ItemWithComparison(
            int itemId,
            int argument,
            string positiveComparison,
            string negativeComparison,
            bool isNegated)
        {
            string comparison = isNegated ? negativeComparison : positiveComparison;
            return MerchantConditionVisualDescriptor.Single(
                new MerchantConditionVisualPart(MerchantConditionVisualKind.Item, itemId),
                (comparison ?? string.Empty) + argument.ToString(CultureInfo.InvariantCulture),
                isNegated: false);
        }

        private static string FormatBasisPoints(int basisPoints)
        {
            int normalized = Math.Max(0, basisPoints);
            int whole = normalized / 100;
            int fraction = normalized % 100;

            if (fraction == 0)
                return whole.ToString(CultureInfo.InvariantCulture) + "%";

            if (fraction % 10 == 0)
            {
                return whole.ToString(CultureInfo.InvariantCulture) +
                       "." +
                       (fraction / 10).ToString(CultureInfo.InvariantCulture) +
                       "%";
            }

            return whole.ToString(CultureInfo.InvariantCulture) +
                   "." +
                   fraction.ToString("00", CultureInfo.InvariantCulture) +
                   "%";
        }

        private static MerchantConditionVisualDescriptor Single(
            MerchantConditionVisualKind kind,
            int value = 0,
            string argumentText = null,
            bool isNegated = false)
        {
            return MerchantConditionVisualDescriptor.Single(
                new MerchantConditionVisualPart(kind, value),
                argumentText,
                isNegated);
        }

        private static MerchantConditionVisualDescriptor Generic(bool isNegated)
        {
            return Single(MerchantConditionVisualKind.Generic, isNegated: isNegated);
        }

        private static MerchantConditionVisualDescriptor Bestiary(int frame, bool isNegated)
        {
            return Single(MerchantConditionVisualKind.BestiaryTag, frame, isNegated: isNegated);
        }

        private static MerchantConditionVisualDescriptor Item(int itemId, bool isNegated)
        {
            return Single(MerchantConditionVisualKind.Item, itemId, isNegated: isNegated);
        }

        private static MerchantConditionVisualDescriptor Npc(int npcId, bool isNegated)
        {
            return Single(MerchantConditionVisualKind.Npc, npcId, isNegated: isNegated);
        }

        private static MerchantConditionVisualDescriptor Boss(int npcId, bool isNegated)
        {
            return Single(MerchantConditionVisualKind.BossHead, npcId, isNegated: isNegated);
        }

        private static void DrawParts(MerchantConditionVisualDescriptor descriptor, Rectangle bounds)
        {
            switch (descriptor.PartCount)
            {
                case 1:
                    DrawPart(descriptor.GetPart(0), bounds);
                    break;

                case 2:
                    {
                        const int gap = 1;
                        int availableWidth = Math.Max(0, bounds.Width - gap);
                        int leftWidth = availableWidth / 2;
                        int rightWidth = availableWidth - leftWidth;
                        DrawPart(descriptor.GetPart(0), new Rectangle(bounds.X, bounds.Y, leftWidth, bounds.Height));
                        DrawPart(
                            descriptor.GetPart(1),
                            new Rectangle(bounds.X + leftWidth + gap, bounds.Y, rightWidth, bounds.Height));
                        break;
                    }

                default:
                    {
                        const int gap = 1;
                        int availableWidth = Math.Max(0, bounds.Width - gap);
                        int availableHeight = Math.Max(0, bounds.Height - gap);
                        int leftWidth = availableWidth / 2;
                        int rightWidth = availableWidth - leftWidth;
                        int topHeight = availableHeight / 2;
                        int bottomHeight = availableHeight - topHeight;

                        DrawPart(descriptor.GetPart(0), new Rectangle(bounds.X, bounds.Y, leftWidth, topHeight));
                        DrawPart(
                            descriptor.GetPart(1),
                            new Rectangle(bounds.X + leftWidth + gap, bounds.Y, rightWidth, topHeight));

                        if (descriptor.PartCount > 2)
                        {
                            DrawPart(
                                descriptor.GetPart(2),
                                new Rectangle(bounds.X, bounds.Y + topHeight + gap, leftWidth, bottomHeight));
                        }

                        if (descriptor.PartCount > 3)
                        {
                            DrawPart(
                                descriptor.GetPart(3),
                                new Rectangle(
                                    bounds.X + leftWidth + gap,
                                    bounds.Y + topHeight + gap,
                                    rightWidth,
                                    bottomHeight));
                        }

                        break;
                    }
            }
        }

        private static void DrawPart(MerchantConditionVisualPart part, Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            switch (part.Kind)
            {
                case MerchantConditionVisualKind.Generic:
                    DrawGeneric(bounds);
                    break;
                case MerchantConditionVisualKind.BestiaryTag:
                    VanillaPresentationIcons.DrawBestiaryTag(bounds, part.Value);
                    break;
                case MerchantConditionVisualKind.Item:
                    AsyncItemIconRenderer.Draw(part.Value, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    break;
                case MerchantConditionVisualKind.Npc:
                    AsyncNpcIconRenderer.Draw(
                        Main.spriteBatch,
                        part.Value,
                        bounds.X,
                        bounds.Y,
                        bounds.Width,
                        bounds.Height);
                    break;
                case MerchantConditionVisualKind.BossHead:
                    VanillaPresentationIcons.DrawBossHead(bounds, part.Value);
                    break;
                case MerchantConditionVisualKind.RemixWorld:
                    VanillaPresentationIcons.DrawRemixWorldIcon(bounds);
                    break;
                case MerchantConditionVisualKind.NotTheBeesWorld:
                    VanillaPresentationIcons.DrawNotTheBeesWorldIcon(bounds);
                    break;
                case MerchantConditionVisualKind.BestiaryCompletion:
                    VanillaPresentationIcons.DrawCompletion(bounds, found: true);
                    break;
                default:
                    DrawGeneric(bounds);
                    break;
            }
        }

        private static void DrawNegationMarker(Rectangle bounds)
        {
            const int borderThickness = 2;
            const int markerSize = 5;

            UIRenderer.DrawRectOutline(
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                UIColors.Error,
                borderThickness);

            int size = Math.Min(markerSize, Math.Min(bounds.Width, bounds.Height));
            if (size > 0)
                UIRenderer.DrawRect(bounds.Right - size, bounds.Y, size, size, UIColors.Error);
        }

        private static void DrawGeneric(Rectangle bounds)
        {
            UIRenderer.DrawRectOutline(bounds.X, bounds.Y, bounds.Width, bounds.Height, UIColors.Border);

            const string marker = "?";
            int textWidth = UIRenderer.MeasureText(marker);
            int textX = bounds.X + Math.Max(0, (bounds.Width - textWidth) / 2);
            int textY = bounds.Y + Math.Max(0, (bounds.Height - TextHeight) / 2);
            UIRenderer.DrawText(marker, textX, textY, UIColors.TextDim);
        }
    }
}