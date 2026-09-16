using System;

namespace TerrarianCompendium.Catalog
{
    [Flags]
    internal enum ItemCategoryMembership
    {
        None = 0,
        Weapons = 1 << 0,
        Armor = 1 << 1,
        Vanity = 1 << 2,
        Blocks = 1 << 3,
        Furniture = 1 << 4,
        Accessories = 1 << 5,
        MiscAccessories = 1 << 6,
        Consumables = 1 << 7,
        Tools = 1 << 8,
        Materials = 1 << 9,
        Misc = 1 << 10
    }
}