using System;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeEnvironmentRequirements
    {
        public RecipeEnvironmentRequirements(
            int? requiredTileId,
            bool requiresWater,
            bool requiresHoney,
            bool requiresLava,
            bool requiresSnowBiome,
            bool requiresGraveyardBiome,
            bool requiresMechdusa,
            bool requiresTorchGodsFavor)
        {
            if (requiredTileId is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredTileId),
                    requiredTileId,
                    "Required tile ID must not be negative when present.");
            }

            RequiredTileId = requiredTileId;
            RequiresWater = requiresWater;
            RequiresHoney = requiresHoney;
            RequiresLava = requiresLava;
            RequiresSnowBiome = requiresSnowBiome;
            RequiresGraveyardBiome = requiresGraveyardBiome;
            RequiresMechdusa = requiresMechdusa;
            RequiresTorchGodsFavor = requiresTorchGodsFavor;
        }

        public int? RequiredTileId { get; }

        public bool RequiresWater { get; }

        public bool RequiresHoney { get; }

        public bool RequiresLava { get; }

        public bool RequiresSnowBiome { get; }

        public bool RequiresGraveyardBiome { get; }

        public bool RequiresMechdusa { get; }

        public bool RequiresTorchGodsFavor { get; }
    }
}