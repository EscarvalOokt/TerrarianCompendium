using System;
using System.Collections.Generic;
using System.Globalization;
using Terraria;
using TerrariaModder.Core.UI;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI
{
    internal enum ItemDetailsStatKind
    {
        Rarity,
        Damage,
        Defense,
        Knockback,
        CriticalHitChance,
        UseTime,
        TagDamage,
        PickPower,
        AxePower,
        HammerPower,
        FishingPower
    }

    internal readonly struct ItemDetailsStatPresentation(ItemDetailsStatKind kind, string valueText, string tooltipText)
    {
        public ItemDetailsStatKind Kind { get; } = kind;

        public string ValueText { get; } = valueText ?? string.Empty;

        public string TooltipText { get; } = tooltipText ?? string.Empty;
    }

    internal static class ItemDetailsPresentation
    {
        public static IReadOnlyList<ItemDetailsStatPresentation> BuildStats(
            ItemDetailsProjection projection,
            CompendiumLocalization localization)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            var stats = new List<ItemDetailsStatPresentation>(11)
            {
                new(
                    ItemDetailsStatKind.Rarity,
                    projection.Rarity.ToString(CultureInfo.InvariantCulture),
                    localization.Get(CompendiumTextKeys.ItemStats.Rarity))
            };

            if (projection.Damage > 0)
            {
                string damageTypeText = GetDamageTypeText(projection.DamageType, localization);
                var damageText = projection.Damage.ToString(CultureInfo.InvariantCulture);
                string damageTooltip = localization.Get(CompendiumTextKeys.ItemStats.Damage);

                if (!string.IsNullOrEmpty(damageTypeText))
                {
                    damageText += " " + damageTypeText;
                    damageTooltip = localization.Format(CompendiumTextKeys.ItemStats.DamageTyped, damageTypeText);
                }

                stats.Add(new ItemDetailsStatPresentation(ItemDetailsStatKind.Damage, damageText, damageTooltip));
            }

            if (projection.Defense > 0)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.Defense,
                        projection.Defense.ToString(CultureInfo.InvariantCulture),
                        localization.Get(CompendiumTextKeys.ItemStats.Defense)));
            }

            if (projection.Knockback.HasValue)
            {
                float knockback = projection.Knockback.Value;
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.Knockback,
                        FormatKnockback(knockback, localization),
                        localization.Get(CompendiumTextKeys.ItemStats.Knockback)));
            }

            if (projection.BaseCriticalHitChance.HasValue)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.CriticalHitChance,
                        localization.Format(
                            CompendiumTextKeys.ItemStats.CriticalHit,
                            projection.BaseCriticalHitChance.Value),
                        localization.Get(CompendiumTextKeys.ItemStats.CriticalHitTooltip)));
            }

            if (projection.UseTimeTicks.HasValue)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.UseTime,
                        localization.Format(CompendiumTextKeys.ItemStats.UseTime, projection.UseTimeTicks.Value),
                        localization.Get(CompendiumTextKeys.ItemStats.UseTimeTooltip)));
            }

            if (projection.TagDamage.HasValue)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.TagDamage,
                        projection.TagDamage.Value.ToString(CultureInfo.InvariantCulture),
                        localization.Get(CompendiumTextKeys.ItemStats.TagDamage)));
            }

            if (projection.PickPower > 0)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.PickPower,
                        localization.Format(CompendiumTextKeys.Common.Percentage, projection.PickPower),
                        localization.Get(CompendiumTextKeys.ItemStats.PickPower)));
            }

            if (projection.AxePower > 0)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.AxePower,
                        localization.Format(CompendiumTextKeys.Common.Percentage, projection.AxePower),
                        localization.Get(CompendiumTextKeys.ItemStats.AxePower)));
            }

            if (projection.HammerPower > 0)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.HammerPower,
                        localization.Format(CompendiumTextKeys.Common.Percentage, projection.HammerPower),
                        localization.Get(CompendiumTextKeys.ItemStats.HammerPower)));
            }

            if (projection.FishingPower > 0)
            {
                stats.Add(
                    new ItemDetailsStatPresentation(
                        ItemDetailsStatKind.FishingPower,
                        localization.Format(CompendiumTextKeys.Common.Percentage, projection.FishingPower),
                        localization.Get(CompendiumTextKeys.ItemStats.FishingPower)));
            }

            return stats;
        }

        public static Color4 GetRarityColor(int rarity)
        {
            return GetRarityColor(
                rarity,
                (byte)Main.DiscoR,
                (byte)Main.DiscoG,
                (byte)Main.DiscoB,
                (byte)(Main.masterColor * 200f));
        }

        internal static Color4 GetRarityColor(int rarity, byte discoR, byte discoG, byte discoB, byte masterGreen)
        {
            switch (rarity)
            {
                case -13:
                    return new Color4(255, masterGreen, 0);
                case -12:
                    return new Color4(discoR, discoG, discoB);
                case -11:
                    return new Color4(255, 175, 0);
                case -1:
                    return new Color4(130, 130, 130);
                case 1:
                    return new Color4(150, 150, 255);
                case 2:
                    return new Color4(150, 255, 150);
                case 3:
                    return new Color4(255, 200, 150);
                case 4:
                    return new Color4(255, 150, 150);
                case 5:
                    return new Color4(255, 150, 255);
                case 6:
                    return new Color4(210, 160, 255);
                case 7:
                    return new Color4(150, 255, 10);
                case 8:
                    return new Color4(255, 255, 10);
                case 9:
                    return new Color4(5, 200, 255);
                case 10:
                    return new Color4(255, 40, 100);
                default:
                    return rarity >= 11 ? new Color4(180, 40, 255) : new Color4(255, 255, 255);
            }
        }

        private static string FormatKnockback(float knockback, CompendiumLocalization localization)
        {
            var valueText = knockback.ToString("0.##", CultureInfo.InvariantCulture);
            return valueText + " " + GetKnockbackSuffix(knockback, localization);
        }

        private static string GetKnockbackSuffix(float knockback, CompendiumLocalization localization)
        {
            if (knockback == 0f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackNone);
            if (knockback <= 1.5)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackExtremelyWeak);
            if (knockback <= 3f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackVeryWeak);
            if (knockback <= 4f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackWeak);
            if (knockback <= 6f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackAverage);
            if (knockback <= 7f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackStrong);
            if (knockback <= 9f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackVeryStrong);
            if (knockback <= 11f)
                return localization.Get(CompendiumTextKeys.ItemStats.KnockbackExtremelyStrong);

            return localization.Get(CompendiumTextKeys.ItemStats.KnockbackInsane);
        }

        private static string GetDamageTypeText(ItemDetailsDamageType damageType, CompendiumLocalization localization)
        {
            switch (damageType)
            {
                case ItemDetailsDamageType.Melee:
                    return localization.Get(CompendiumTextKeys.ItemStats.DamageMelee);
                case ItemDetailsDamageType.Ranged:
                    return localization.Get(CompendiumTextKeys.ItemStats.DamageRanged);
                case ItemDetailsDamageType.Magic:
                    return localization.Get(CompendiumTextKeys.ItemStats.DamageMagic);
                case ItemDetailsDamageType.Summon:
                    return localization.Get(CompendiumTextKeys.ItemStats.DamageSummon);
                case ItemDetailsDamageType.Generic:
                    return localization.Get(CompendiumTextKeys.ItemStats.DamageGeneric);
                default:
                    return null;
            }
        }
    }
}