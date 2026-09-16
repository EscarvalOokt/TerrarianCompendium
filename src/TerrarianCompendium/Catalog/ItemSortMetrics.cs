namespace TerrarianCompendium.Catalog
{
    internal readonly struct ItemSortMetrics(
        int value,
        int rarity,
        int damage,
        int defense,
        int pickPower,
        int axePower,
        int hammerPower,
        int fishingPower)
    {
        public int Value { get; } = value;

        public int Rarity { get; } = rarity;

        public int Damage { get; } = damage;

        public int Defense { get; } = defense;

        public int PickPower { get; } = pickPower;

        public int AxePower { get; } = axePower;

        public int HammerPower { get; } = hammerPower;

        public int FishingPower { get; } = fishingPower;
    }
}