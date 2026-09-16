namespace TerrarianCompendium.Catalog
{
    internal enum ItemDetailsDamageType
    {
        None,
        Melee,
        Ranged,
        Magic,
        Summon,
        Generic
    }

    internal readonly struct ItemDetailsStats(
        ItemDetailsDamageType damageType,
        float? knockback,
        int? baseCriticalHitChance,
        int? useTimeTicks,
        int? tagDamage)
    {
        public ItemDetailsDamageType DamageType { get; } = damageType;

        public float? Knockback { get; } = knockback;

        public int? BaseCriticalHitChance { get; } = baseCriticalHitChance;

        public int? UseTimeTicks { get; } = useTimeTicks;

        public int? TagDamage { get; } = tagDamage;
    }

    internal static class ItemDetailsStatsProjector
    {
        private const int DefaultCriticalHitChance = 4;

        public static ItemDetailsStats ProjectBaseItem(
            int damage,
            bool melee,
            bool ranged,
            bool magic,
            bool summon,
            float knockback,
            int crit,
            int useTime,
            int useStyle,
            int tagDamage)
        {
            ItemDetailsDamageType damageType = GetDamageType(damage, melee, ranged, magic, summon);
            float? projectedKnockback = damage > 0 ? knockback : null;
            int? baseCriticalHitChance = IsOrdinaryCriticalHitType(damageType) ? DefaultCriticalHitChance + crit : null;
            int? useTimeTicks = damage > 0 && useStyle != 0 && useTime > 0 ? useTime : null;
            int? projectedTagDamage = tagDamage > 0 ? tagDamage : null;

            return new ItemDetailsStats(
                damageType,
                projectedKnockback,
                baseCriticalHitChance,
                useTimeTicks,
                projectedTagDamage);
        }

        private static ItemDetailsDamageType GetDamageType(int damage, bool melee, bool ranged, bool magic, bool summon)
        {
            if (damage <= 0)
                return ItemDetailsDamageType.None;

            if (melee)
                return ItemDetailsDamageType.Melee;

            if (ranged)
                return ItemDetailsDamageType.Ranged;

            if (magic)
                return ItemDetailsDamageType.Magic;

            return summon ? ItemDetailsDamageType.Summon : ItemDetailsDamageType.Generic;
        }

        private static bool IsOrdinaryCriticalHitType(ItemDetailsDamageType damageType)
        {
            return damageType == ItemDetailsDamageType.Melee ||
                   damageType == ItemDetailsDamageType.Ranged ||
                   damageType == ItemDetailsDamageType.Magic;
        }
    }
}