using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Shimmer
{
    internal sealed class VanillaShimmerTransformationCatalogBuilder(ILogger logger)
    {
        private const int DynamicLunarBrickItemId = 3461;
        private const int RainbowSlimeUnlockItemId = 4986;
        private const int SlimeRainTriggerItemId = 560;

        public ShimmerTransformationCatalog Build(ItemCatalog itemCatalog, RecipeCatalog recipeCatalog)
        {
            if (itemCatalog == null)
                throw new ArgumentNullException(nameof(itemCatalog));

            if (recipeCatalog == null)
                throw new ArgumentNullException(nameof(recipeCatalog));

            var entries = new List<ShimmerTransformationEntry>();

            foreach (ItemCatalogEntry itemEntry in itemCatalog.Items)
            {
                int inputItemId = itemEntry.Id;
                int shimmerEquivalentItemId = ResolveEquivalentItemId(
                    inputItemId,
                    ItemID.Sets.ShimmerCountsAsItem[inputItemId],
                    decraftEquivalentItemId: -1,
                    forDecrafting: false);
                int decraftEquivalentItemId = ResolveEquivalentItemId(
                    inputItemId,
                    ItemID.Sets.ShimmerCountsAsItem[inputItemId],
                    ItemID.Sets.ShimmerCountsAsItemForDecraft[inputItemId],
                    forDecrafting: true);
                bool hasDynamicDirectResult = shimmerEquivalentItemId == DynamicLunarBrickItemId;
                int directResultItemId = hasDynamicDirectResult
                    ? 0
                    : ShimmerTransforms.GetTransformToItem(shimmerEquivalentItemId);
                bool hasDirectTransformation = hasDynamicDirectResult || directResultItemId > 0;
                int baseRecipeRuntimeIndex = ItemID.Sets.IsCrafted[decraftEquivalentItemId];
                int crimsonRecipeRuntimeIndex = ItemID.Sets.IsCraftedCrimson[decraftEquivalentItemId];
                int corruptionRecipeRuntimeIndex = ItemID.Sets.IsCraftedCorruption[decraftEquivalentItemId];
                Item sample = ContentSamples.ItemsByType[inputItemId];

                ShimmerTransformationEntry transformation = CreateEntry(
                    inputItemId,
                    ItemID.Sets.CommonCoin[shimmerEquivalentItemId],
                    hasDirectTransformation,
                    directResultItemId,
                    hasDynamicDirectResult,
                    inputItemId == RainbowSlimeUnlockItemId || inputItemId == SlimeRainTriggerItemId,
                    sample is { makeNPC: > 0 },
                    baseRecipeRuntimeIndex,
                    crimsonRecipeRuntimeIndex,
                    corruptionRecipeRuntimeIndex,
                    recipeCatalog);

                if (transformation != null)
                    entries.Add(transformation);
            }

            var catalog = ShimmerTransformationCatalog.Create(entries);
            logger?.Info($"Vanilla Shimmer transformation catalog built: {catalog.Count} entries.");
            return catalog;
        }

        internal static int ResolveEquivalentItemId(
            int inputItemId,
            int shimmerEquivalentItemId,
            int decraftEquivalentItemId,
            bool forDecrafting)
        {
            if (inputItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputItemId),
                    inputItemId,
                    "Input item ID must be greater than zero.");
            }

            if (forDecrafting && decraftEquivalentItemId >= 0)
                return decraftEquivalentItemId;

            return shimmerEquivalentItemId >= 0 ? shimmerEquivalentItemId : inputItemId;
        }

        internal static ShimmerTransformationEntry CreateEntry(
            int inputItemId,
            bool isCommonCoin,
            bool hasDirectTransformation,
            int directResultItemId,
            bool hasDynamicDirectResult,
            bool hasSpecialItemBehavior,
            bool hasNpcBehavior,
            int baseRecipeRuntimeIndex,
            int crimsonRecipeRuntimeIndex,
            int corruptionRecipeRuntimeIndex,
            RecipeCatalog recipeCatalog)
        {
            if (recipeCatalog == null)
                throw new ArgumentNullException(nameof(recipeCatalog));

            if (inputItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputItemId),
                    inputItemId,
                    "Input item ID must be greater than zero.");
            }

            if (isCommonCoin)
                return null;

            if (hasDirectTransformation)
            {
                if (hasDynamicDirectResult)
                    return ShimmerTransformationEntry.ForDynamicDirect(inputItemId);

                return ShimmerTransformationEntry.ForDirect(inputItemId, directResultItemId);
            }

            if (hasSpecialItemBehavior || hasNpcBehavior || baseRecipeRuntimeIndex < 0)
                return null;

            ValidateRecipeExists(recipeCatalog, baseRecipeRuntimeIndex, "base");
            int? crimson = NormalizeOptionalRecipeRuntimeIndex(crimsonRecipeRuntimeIndex);
            int? corruption = NormalizeOptionalRecipeRuntimeIndex(corruptionRecipeRuntimeIndex);

            if (crimson.HasValue)
                ValidateRecipeExists(recipeCatalog, crimson.Value, "Crimson");

            if (corruption.HasValue)
                ValidateRecipeExists(recipeCatalog, corruption.Value, "Corruption");

            return ShimmerTransformationEntry.ForDecraft(inputItemId, baseRecipeRuntimeIndex, crimson, corruption);
        }

        private static int? NormalizeOptionalRecipeRuntimeIndex(int runtimeIndex)
        {
            return runtimeIndex >= 0 ? runtimeIndex : null;
        }

        private static void ValidateRecipeExists(RecipeCatalog recipeCatalog, int runtimeIndex, string role)
        {
            if (!recipeCatalog.Contains(runtimeIndex))
            {
                throw new InvalidOperationException(
                    $"Shimmer {role} decrafting recipe index {runtimeIndex} is not present in the recipe catalog.");
            }
        }
    }
}