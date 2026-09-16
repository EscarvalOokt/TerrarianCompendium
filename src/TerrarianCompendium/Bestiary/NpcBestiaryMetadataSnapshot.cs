using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcBestiaryStatsSnapshot(
        int damage,
        int lifeMax,
        int defense,
        float knockbackResist,
        float monetaryValue)
    {
        public int Damage { get; } = damage;

        public int LifeMax { get; } = lifeMax;

        public int Defense { get; } = defense;

        public float KnockbackResist { get; } = knockbackResist;

        public float MonetaryValue { get; } = monetaryValue;
    }

    internal sealed class NpcBestiarySpawnCondition(string displayNameKey, string displayName)
    {
        public string DisplayNameKey { get; } =
            displayNameKey ?? throw new ArgumentNullException(nameof(displayNameKey));

        public string DisplayName { get; } = displayName ?? throw new ArgumentNullException(nameof(displayName));
    }

    internal sealed class NpcBestiaryDebuffImmunity
    {
        public NpcBestiaryDebuffImmunity(int buffId, string name)
        {
            if (buffId <= 0)
                throw new ArgumentOutOfRangeException(nameof(buffId), buffId, "Buff ID must be greater than zero.");

            BuffId = buffId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public int BuffId { get; }

        public string Name { get; }
    }

    internal sealed class NpcBestiaryMetadataSnapshot
    {
        public NpcBestiaryMetadataSnapshot(
            bool isEncountered,
            string name,
            NpcBestiaryStatsSnapshot stats,
            int bestiaryRarityStars,
            int? rareSpawnRarityLevel,
            IEnumerable<NpcBestiarySpawnCondition> spawnConditions,
            IEnumerable<NpcBestiaryDebuffImmunity> baseDebuffImmunities)
        {
            if (bestiaryRarityStars < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bestiaryRarityStars),
                    bestiaryRarityStars,
                    "Bestiary rarity stars must not be negative.");
            }

            IsEncountered = isEncountered;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Stats = stats;
            BestiaryRarityStars = bestiaryRarityStars;
            RareSpawnRarityLevel = rareSpawnRarityLevel;
            SpawnConditions = CopySpawnConditions(spawnConditions);
            BaseDebuffImmunities = CopyImmunities(baseDebuffImmunities);
        }

        public bool IsEncountered { get; }

        public string Name { get; }

        public NpcBestiaryStatsSnapshot Stats { get; }

        public int BestiaryRarityStars { get; }

        public int? RareSpawnRarityLevel { get; }

        public IReadOnlyList<NpcBestiarySpawnCondition> SpawnConditions { get; }

        public IReadOnlyList<NpcBestiaryDebuffImmunity> BaseDebuffImmunities { get; }

        private static IReadOnlyList<NpcBestiarySpawnCondition> CopySpawnConditions(
            IEnumerable<NpcBestiarySpawnCondition> values)
        {
            if (values == null)
                return Array.Empty<NpcBestiarySpawnCondition>();

            var result = new List<NpcBestiarySpawnCondition>();

            foreach (NpcBestiarySpawnCondition value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "Spawn-condition collection must not contain null values.",
                        nameof(values));
                }

                result.Add(value);
            }

            return result.Count == 0
                ? Array.Empty<NpcBestiarySpawnCondition>()
                : new ReadOnlyCollection<NpcBestiarySpawnCondition>(result);
        }

        private static IReadOnlyList<NpcBestiaryDebuffImmunity> CopyImmunities(
            IEnumerable<NpcBestiaryDebuffImmunity> values)
        {
            if (values == null)
                return Array.Empty<NpcBestiaryDebuffImmunity>();

            var result = new List<NpcBestiaryDebuffImmunity>();

            foreach (NpcBestiaryDebuffImmunity value in values)
            {
                if (value == null)
                {
                    throw new ArgumentException(
                        "Debuff immunity collection must not contain null values.",
                        nameof(values));
                }

                result.Add(value);
            }

            return result.Count == 0
                ? Array.Empty<NpcBestiaryDebuffImmunity>()
                : new ReadOnlyCollection<NpcBestiaryDebuffImmunity>(result);
        }
    }
}