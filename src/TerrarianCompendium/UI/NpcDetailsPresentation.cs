using System;
using System.Collections.Generic;
using System.Globalization;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Details;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI
{
    internal enum NpcDetailsStatKind
    {
        Damage,
        MaxLife,
        Defense,
        KnockbackTaken,
        BestiaryRarity,
        RareCreatureLevel
    }

    internal readonly struct NpcDetailsStatPresentation(NpcDetailsStatKind kind, string valueText, string tooltipText)
    {
        public NpcDetailsStatKind Kind { get; } = kind;

        public string ValueText { get; } = valueText ?? string.Empty;

        public string TooltipText { get; } = tooltipText ?? string.Empty;
    }

    internal static class NpcDetailsPresentation
    {
        public static IReadOnlyList<NpcDetailsStatPresentation> BuildStats(
            NpcDetailsProjection projection,
            CompendiumLocalization localization)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            var presentations = new List<NpcDetailsStatPresentation>(6);

            if (projection.Stats != null)
            {
                NpcBestiaryStatsSnapshot stats = projection.Stats;
                var damage = stats.Damage.ToString(CultureInfo.InvariantCulture);
                var maxLife = stats.LifeMax.ToString(CultureInfo.InvariantCulture);
                var defense = stats.Defense.ToString(CultureInfo.InvariantCulture);
                string knockbackTaken = FormatKnockbackTaken(stats.KnockbackResist);

                presentations.Add(
                    new NpcDetailsStatPresentation(
                        NpcDetailsStatKind.Damage,
                        damage,
                        localization.Format(CompendiumTextKeys.NpcDetails.Damage, stats.Damage)));
                presentations.Add(
                    new NpcDetailsStatPresentation(
                        NpcDetailsStatKind.MaxLife,
                        maxLife,
                        localization.Format(CompendiumTextKeys.NpcDetails.MaxLife, stats.LifeMax)));
                presentations.Add(
                    new NpcDetailsStatPresentation(
                        NpcDetailsStatKind.Defense,
                        defense,
                        localization.Format(CompendiumTextKeys.NpcDetails.Defense, stats.Defense)));
                presentations.Add(
                    new NpcDetailsStatPresentation(
                        NpcDetailsStatKind.KnockbackTaken,
                        knockbackTaken,
                        localization.Format(
                            CompendiumTextKeys.NpcDetails.KnockbackTaken,
                            stats.KnockbackResist * 100f)));
            }

            string rarity = FormatRarity(projection.BestiaryRarityStars, localization);
            presentations.Add(
                new NpcDetailsStatPresentation(
                    NpcDetailsStatKind.BestiaryRarity,
                    rarity,
                    localization.Format(CompendiumTextKeys.NpcDetails.Rarity, rarity)));

            if (projection.RareSpawnRarityLevel.HasValue)
            {
                int rareCreatureLevel = projection.RareSpawnRarityLevel.Value;
                presentations.Add(
                    new NpcDetailsStatPresentation(
                        NpcDetailsStatKind.RareCreatureLevel,
                        rareCreatureLevel.ToString(CultureInfo.InvariantCulture),
                        localization.Format(CompendiumTextKeys.NpcDetails.RareCreature, rareCreatureLevel)));
            }

            return presentations;
        }

        private static string FormatKnockbackTaken(float knockbackResist)
        {
            return (knockbackResist * 100f).ToString("0.#", CultureInfo.InvariantCulture) + "%";
        }

        private static string FormatRarity(int stars, CompendiumLocalization localization)
        {
            return stars <= 0 ? localization.Get(CompendiumTextKeys.Common.None) : new string('★', stars);
        }
    }
}