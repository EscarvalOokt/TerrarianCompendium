using System;
using System.Globalization;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Details
{
    internal static class MerchantSourceConditionPresentation
    {
        private const int SilverOreTileId = 9;
        private const int TungstenOreTileId = 168;
        private const int SilverOreItemId = 14;
        private const int TungstenOreItemId = 701;
        private const int CopperCoinItemId = 71;
        private const int SilverCoinItemId = 72;
        private const int GoldCoinItemId = 73;
        private const int PlatinumCoinItemId = 74;
        private const int CopperPerSilver = 100;
        private const int CopperPerGold = CopperPerSilver * 100;
        private const int CopperPerPlatinum = CopperPerGold * 100;

        public static string GetDescription(
            CompendiumLocalization localization,
            MerchantSourceCondition condition,
            Func<int, string> itemNameResolver = null,
            Func<int, string> npcNameResolver = null,
            Func<string, string> nativeTextResolver = null)
        {
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            string key = CompendiumTextKeys.Merchant.Condition(condition.Kind, condition.IsNegated);

            switch (condition.Kind)
            {
                case MerchantSourceConditionKind.WorldSilverOreTier:
                    return localization.Format(key, ResolveSilverTierName(localization, condition, itemNameResolver));

                case MerchantSourceConditionKind.PlayerCoinValueAtLeast:
                    return localization.Format(
                        key,
                        ResolveCoinRequirement(localization, condition.Argument, itemNameResolver));

                case MerchantSourceConditionKind.MoonPhase:
                    return localization.Format(
                        key,
                        ResolveMoonPhaseName(localization, condition.Argument, nativeTextResolver));

                case MerchantSourceConditionKind.PlayerLifeMaxAtLeast:
                case MerchantSourceConditionKind.PlayerManaMaxAtLeast:
                case MerchantSourceConditionKind.GolferScoreAtLeast:
                case MerchantSourceConditionKind.GolferScoreGreaterThan:
                    return localization.Format(key, condition.Argument);

                case MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints:
                    return localization.Format(key, condition.Argument / 100f);

                case MerchantSourceConditionKind.NpcPresent:
                    return localization.Format(
                        key,
                        ResolveName(
                            localization,
                            npcNameResolver,
                            condition.Argument,
                            CompendiumTextKeys.Merchant.UnknownNpc));

                case MerchantSourceConditionKind.PlayerHasItem:
                case MerchantSourceConditionKind.PlayerHasItemInAnyInventory:
                    return localization.Format(
                        key,
                        ResolveName(
                            localization,
                            itemNameResolver,
                            condition.Argument,
                            CompendiumTextKeys.Merchant.UnknownItem));

                case MerchantSourceConditionKind.HardMode:
                case MerchantSourceConditionKind.DayTime:
                case MerchantSourceConditionKind.BloodMoon:
                case MerchantSourceConditionKind.Halloween:
                case MerchantSourceConditionKind.Xmas:
                case MerchantSourceConditionKind.BirthdayParty:
                case MerchantSourceConditionKind.HappyWindyDay:
                case MerchantSourceConditionKind.Eclipse:
                case MerchantSourceConditionKind.LanternNight:
                case MerchantSourceConditionKind.Storm:
                case MerchantSourceConditionKind.WorldCrimson:
                case MerchantSourceConditionKind.WorldRemix:
                case MerchantSourceConditionKind.WorldTenthAnniversary:
                case MerchantSourceConditionKind.WorldNotTheBees:
                case MerchantSourceConditionKind.WorldGetGood:
                case MerchantSourceConditionKind.WorldInfectedSeed:
                case MerchantSourceConditionKind.WorldVampireSeed:
                case MerchantSourceConditionKind.WorldShadowOrbSmashed:
                case MerchantSourceConditionKind.ZoneSnow:
                case MerchantSourceConditionKind.ZoneJungle:
                case MerchantSourceConditionKind.ZoneGraveyard:
                case MerchantSourceConditionKind.ZoneBeach:
                case MerchantSourceConditionKind.ZoneDesert:
                case MerchantSourceConditionKind.ZoneHallow:
                case MerchantSourceConditionKind.ZoneCorrupt:
                case MerchantSourceConditionKind.ZoneCrimson:
                case MerchantSourceConditionKind.ZoneGlowshroom:
                case MerchantSourceConditionKind.ZoneSkyHeight:
                case MerchantSourceConditionKind.ZoneUnderworldHeight:
                case MerchantSourceConditionKind.ShoppingZoneForest:
                case MerchantSourceConditionKind.DownedBoss1:
                case MerchantSourceConditionKind.DownedBoss2:
                case MerchantSourceConditionKind.DownedBoss3:
                case MerchantSourceConditionKind.DownedSlimeKing:
                case MerchantSourceConditionKind.DownedQueenSlime:
                case MerchantSourceConditionKind.DownedQueenBee:
                case MerchantSourceConditionKind.DownedClown:
                case MerchantSourceConditionKind.DownedAncientCultist:
                case MerchantSourceConditionKind.DownedFrost:
                case MerchantSourceConditionKind.DownedPirates:
                case MerchantSourceConditionKind.DownedPlantBoss:
                case MerchantSourceConditionKind.DownedGolemBoss:
                case MerchantSourceConditionKind.DownedMartians:
                case MerchantSourceConditionKind.DownedMoonlord:
                case MerchantSourceConditionKind.DownedMechBossAny:
                case MerchantSourceConditionKind.DownedMechBoss1:
                case MerchantSourceConditionKind.DownedMechBoss2:
                case MerchantSourceConditionKind.DownedMechBoss3:
                case MerchantSourceConditionKind.DownedTowerSolar:
                case MerchantSourceConditionKind.DownedDeerclops:
                case MerchantSourceConditionKind.PlayerTeamNonZero:
                case MerchantSourceConditionKind.MultiplayerClient:
                case MerchantSourceConditionKind.BestiaryFairyTorchUnlocked:
                case MerchantSourceConditionKind.SkeletonMerchantEarlyCycle:
                case MerchantSourceConditionKind.PlayerAteArtisanBread:
                case MerchantSourceConditionKind.PylonNearbyNpcRequirement:
                case MerchantSourceConditionKind.PylonForestLocation:
                case MerchantSourceConditionKind.PylonCavernLocation:
                case MerchantSourceConditionKind.PylonOceanLocation:
                    return localization.Get(key);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(condition),
                        condition.Kind,
                        "Unsupported merchant source condition kind.");
            }
        }

        private static string ResolveSilverTierName(
            CompendiumLocalization localization,
            MerchantSourceCondition condition,
            Func<int, string> itemNameResolver)
        {
            int itemId = condition.Argument switch
            {
                SilverOreTileId => condition.IsNegated ? TungstenOreItemId : SilverOreItemId,
                TungstenOreTileId => condition.IsNegated ? SilverOreItemId : TungstenOreItemId,
                _ => 0
            };

            return itemId > 0
                ? ResolveName(localization, itemNameResolver, itemId, CompendiumTextKeys.Merchant.UnknownItem)
                : localization.Get(CompendiumTextKeys.Merchant.UnknownItem);
        }

        private static string ResolveCoinRequirement(
            CompendiumLocalization localization,
            int value,
            Func<int, string> itemNameResolver)
        {
            int normalized = Math.Max(0, value);
            int itemId;
            int denomination;

            if (normalized >= CopperPerPlatinum && normalized % CopperPerPlatinum == 0)
            {
                itemId = PlatinumCoinItemId;
                denomination = CopperPerPlatinum;
            }
            else if (normalized >= CopperPerGold && normalized % CopperPerGold == 0)
            {
                itemId = GoldCoinItemId;
                denomination = CopperPerGold;
            }
            else if (normalized >= CopperPerSilver && normalized % CopperPerSilver == 0)
            {
                itemId = SilverCoinItemId;
                denomination = CopperPerSilver;
            }
            else
            {
                itemId = CopperCoinItemId;
                denomination = 1;
            }

            int amount = normalized / denomination;
            string itemName = ResolveName(
                localization,
                itemNameResolver,
                itemId,
                CompendiumTextKeys.Merchant.UnknownItem);

            return amount == 1 ? "1 " + itemName : amount.ToString(CultureInfo.InvariantCulture) + " × " + itemName;
        }

        private static string ResolveMoonPhaseName(
            CompendiumLocalization localization,
            int moonPhase,
            Func<string, string> nativeTextResolver)
        {
            string nativeKey = moonPhase switch
            {
                0 => "GameUI.FullMoon",
                1 => "GameUI.WaningGibbous",
                2 => "GameUI.ThirdQuarter",
                3 => "GameUI.WaningCrescent",
                4 => "GameUI.NewMoon",
                5 => "GameUI.WaxingCrescent",
                6 => "GameUI.FirstQuarter",
                7 => "GameUI.WaxingGibbous",
                _ => null
            };

            if (nativeKey == null)
                return localization.Get(CompendiumTextKeys.Merchant.UnknownMoonPhase);

            string name = nativeTextResolver?.Invoke(nativeKey);
            return string.IsNullOrWhiteSpace(name)
                ? localization.Get(CompendiumTextKeys.Merchant.UnknownMoonPhase)
                : name;
        }

        private static string ResolveName(
            CompendiumLocalization localization,
            Func<int, string> resolver,
            int id,
            string fallbackKey)
        {
            string name = resolver?.Invoke(id);
            return string.IsNullOrWhiteSpace(name) ? localization.Get(fallbackKey) : name;
        }
    }
}