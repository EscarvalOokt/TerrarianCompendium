using System;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Details
{
    internal static class MerchantSourceConditionPresentation
    {
        public static string GetDescription(
            CompendiumLocalization localization,
            MerchantSourceCondition condition,
            Func<int, string> itemNameResolver = null,
            Func<int, string> npcNameResolver = null)
        {
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            string key = CompendiumTextKeys.Merchant.Condition(condition.Kind, condition.IsNegated);

            switch (condition.Kind)
            {
                case MerchantSourceConditionKind.WorldSilverOreTier:
                case MerchantSourceConditionKind.PlayerLifeMaxAtLeast:
                case MerchantSourceConditionKind.PlayerManaMaxAtLeast:
                case MerchantSourceConditionKind.PlayerCoinValueAtLeast:
                case MerchantSourceConditionKind.GolferScoreAtLeast:
                case MerchantSourceConditionKind.GolferScoreGreaterThan:
                case MerchantSourceConditionKind.MoonPhase:
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
                            CompendiumTextKeys.Common.NpcIdInline));

                case MerchantSourceConditionKind.PlayerHasItem:
                case MerchantSourceConditionKind.PlayerHasItemInAnyInventory:
                    return localization.Format(
                        key,
                        ResolveName(
                            localization,
                            itemNameResolver,
                            condition.Argument,
                            CompendiumTextKeys.Common.ItemIdInline));

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

        private static string ResolveName(
            CompendiumLocalization localization,
            Func<int, string> resolver,
            int id,
            string fallbackKey)
        {
            string name = resolver?.Invoke(id);
            return string.IsNullOrWhiteSpace(name) ? localization.Format(fallbackKey, id) : name;
        }
    }
}