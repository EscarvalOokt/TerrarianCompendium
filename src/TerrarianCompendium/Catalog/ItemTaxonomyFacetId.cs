using System;

namespace TerrarianCompendium.Catalog
{
    [Flags]
    internal enum ItemTaxonomyFacetId
    {
        None = 0,
        Materials = 1 << 0,
        Consumables = 1 << 1,
        Ammo = 1 << 2,
        OpenableItems = 1 << 3,
        BossSummonItems = 1 << 4,
        MusicBoxes = 1 << 5,
        Dyes = 1 << 6,
        Fishing = 1 << 7
    }
}