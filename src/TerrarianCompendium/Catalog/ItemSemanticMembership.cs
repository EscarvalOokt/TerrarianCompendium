using System;

namespace TerrarianCompendium.Catalog
{
    [Flags]
    internal enum ItemSemanticMembership : ulong
    {
        None = 0,

        Melee = 1UL << 0,
        Yoyos = 1UL << 1,
        Magic = 1UL << 2,
        RangedWeapon = 1UL << 3,
        ArrowFamilyWeapons = 1UL << 4,
        BulletFamilyWeapons = 1UL << 5,
        SpecialistWeapons = 1UL << 6,

        Ammo = 1UL << 7,
        ArrowAmmo = 1UL << 8,
        BulletAmmo = 1UL << 9,
        SpecialistAmmo = 1UL << 10,

        Summon = 1UL << 11,
        Whips = 1UL << 12,
        Sentries = 1UL << 13,

        ArmorHead = 1UL << 14,
        ArmorBody = 1UL << 15,
        ArmorLegs = 1UL << 16,

        VanityHead = 1UL << 17,
        VanityBody = 1UL << 18,
        VanityLegs = 1UL << 19,

        SolidBlocks = 1UL << 20,
        Walls = 1UL << 21,

        Containers = 1UL << 22,
        Statues = 1UL << 23,
        Platforms = 1UL << 24,
        Doors = 1UL << 25,
        Chairs = 1UL << 26,
        Tables = 1UL << 27,
        CraftingStations = 1UL << 28,
        LightSources = 1UL << 29,
        Torches = 1UL << 30,
        Banners = 1UL << 31,
        WallDecorations = 1UL << 32,
        Critters = 1UL << 33,

        Wings = 1UL << 34,

        Pets = 1UL << 35,
        LightPets = 1UL << 36,
        Mounts = 1UL << 37,
        Minecarts = 1UL << 38,
        Hooks = 1UL << 39,

        HealthPotions = 1UL << 40,
        ManaPotions = 1UL << 41,
        BuffPotions = 1UL << 42,
        Flasks = 1UL << 43,
        Food = 1UL << 44,

        Pickaxes = 1UL << 45,
        Axes = 1UL << 46,
        Hammers = 1UL << 47,
        FishingRods = 1UL << 48,

        OpenableItems = 1UL << 49,
        BossSummonItems = 1UL << 50,
        MusicBoxes = 1UL << 51,
        Dyes = 1UL << 52,
        QuestFish = 1UL << 53,
        Fishing = 1UL << 54
    }
}