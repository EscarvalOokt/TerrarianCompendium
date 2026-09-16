using System;

namespace TerrarianCompendium.Recipes
{
    [Flags]
    internal enum RecipeEnvironmentRequirementFilter
    {
        None = 0,
        Water = 1 << 0,
        Honey = 1 << 1,
        Lava = 1 << 2,
        SnowBiome = 1 << 3,
        GraveyardBiome = 1 << 4,
        Mechdusa = 1 << 5,
        TorchGodsFavor = 1 << 6
    }
}