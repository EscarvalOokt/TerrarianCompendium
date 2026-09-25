using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Shimmer
{
    internal readonly struct ShimmerWorldRecipeVariant(int runtimeIndex, ShimmerWorldCondition worldCondition)
    {
        public int RuntimeIndex { get; } = runtimeIndex;

        public ShimmerWorldCondition WorldCondition { get; } = worldCondition;
    }

    internal sealed class VanillaShimmerTransformationIndexBuilder(ILogger logger)
    {
        private static readonly IReadOnlyList<KeyValuePair<int, int>> _lunarBrickResults =
            new ReadOnlyCollection<KeyValuePair<int, int>>(
                new List<KeyValuePair<int, int>>
                {
                    new(0, 5408),
                    new(1, 5401),
                    new(2, 5403),
                    new(3, 5402),
                    new(4, 5406),
                    new(5, 5407),
                    new(6, 5405),
                    new(7, 5404)
                });

        public ShimmerTransformationIndex Build(
            ShimmerTransformationCatalog transformationCatalog,
            ItemCatalog itemCatalog,
            RecipeCatalog recipeCatalog)
        {
            if (transformationCatalog == null)
                throw new ArgumentNullException(nameof(transformationCatalog));
            if (itemCatalog == null)
                throw new ArgumentNullException(nameof(itemCatalog));
            if (recipeCatalog == null)
                throw new ArgumentNullException(nameof(recipeCatalog));

            var variants = new List<ShimmerTransformationVariant>();

            foreach (ShimmerTransformationEntry entry in transformationCatalog.Entries)
            {
                if (!itemCatalog.Contains(entry.InputItemId))
                {
                    throw new InvalidOperationException(
                        $"Shimmer transformation references catalog-missing input item ID {entry.InputItemId}.");
                }

                if (entry.IsDirect)
                {
                    AddDirectVariants(variants, entry, itemCatalog);
                }
                else
                {
                    AddDecraftVariants(variants, entry, itemCatalog, recipeCatalog);
                }
            }

            var index = ShimmerTransformationIndex.Create(variants);
            logger?.Info($"Vanilla Shimmer transformation index built: {index.Variants.Count} variants.");
            return index;
        }

        internal static IReadOnlyList<KeyValuePair<int, int>> GetLunarBrickResults()
        {
            return _lunarBrickResults;
        }

        internal static int GetLunarBrickResultItemId(int moonPhase)
        {
            if (moonPhase < 0 || moonPhase > 7)
                throw new ArgumentOutOfRangeException(
                    nameof(moonPhase),
                    moonPhase,
                    "Moon phase must be between 0 and 7.");

            return _lunarBrickResults[moonPhase].Value;
        }

        internal static IReadOnlyList<ShimmerWorldRecipeVariant> ResolveWorldRecipeVariants(
            int baseRuntimeIndex,
            int? crimsonRuntimeIndex,
            int? corruptionRuntimeIndex)
        {
            if (baseRuntimeIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(baseRuntimeIndex));
            if (crimsonRuntimeIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(crimsonRuntimeIndex));
            if (corruptionRuntimeIndex is < 0)
                throw new ArgumentOutOfRangeException(nameof(corruptionRuntimeIndex));

            int crimsonEffective = crimsonRuntimeIndex ?? baseRuntimeIndex;
            int corruptionEffective = corruptionRuntimeIndex ?? baseRuntimeIndex;

            if (crimsonEffective == corruptionEffective)
            {
                return new ReadOnlyCollection<ShimmerWorldRecipeVariant>(
                    new List<ShimmerWorldRecipeVariant>
                    {
                        new(crimsonEffective, ShimmerWorldCondition.Any)
                    });
            }

            return new ReadOnlyCollection<ShimmerWorldRecipeVariant>(
                new List<ShimmerWorldRecipeVariant>
                {
                    new(crimsonEffective, ShimmerWorldCondition.Crimson),
                    new(corruptionEffective, ShimmerWorldCondition.Corruption)
                });
        }

        private static void AddDirectVariants(
            List<ShimmerTransformationVariant> variants,
            ShimmerTransformationEntry entry,
            ItemCatalog itemCatalog)
        {
            int equivalentItemId = ResolveDirectEquivalentItemId(entry.InputItemId);
            ShimmerProgressionRequirement progressionRequirement =
                GetSetValue(ItemID.Sets.ShimmerPostMoonlord, equivalentItemId)
                    ? ShimmerProgressionRequirement.PostMoonLord
                    : ShimmerProgressionRequirement.None;

            if (entry.HasDynamicDirectResult)
            {
                foreach (KeyValuePair<int, int> phaseResult in _lunarBrickResults)
                {
                    ValidateOutputItem(itemCatalog, phaseResult.Value);
                    variants.Add(
                        new ShimmerTransformationVariant(
                            entry.InputItemId,
                            ShimmerTransformationKind.Direct,
                            inputStack: 1,
                            outputs: [new ShimmerTransformationOutput(phaseResult.Value, 1)],
                            progressionRequirement: progressionRequirement,
                            worldCondition: ShimmerWorldCondition.Any,
                            moonPhase: phaseResult.Key));
                }

                return;
            }

            int resultItemId = entry.DirectResultItemId ?? 0;
            if (resultItemId <= 0)
                throw new InvalidOperationException(
                    "Static direct Shimmer transformation is missing its result item ID.");

            ValidateOutputItem(itemCatalog, resultItemId);
            variants.Add(
                new ShimmerTransformationVariant(
                    entry.InputItemId,
                    ShimmerTransformationKind.Direct,
                    inputStack: 1,
                    outputs: [new ShimmerTransformationOutput(resultItemId, 1)],
                    progressionRequirement: progressionRequirement));
        }

        private static void AddDecraftVariants(
            List<ShimmerTransformationVariant> variants,
            ShimmerTransformationEntry entry,
            ItemCatalog itemCatalog,
            RecipeCatalog recipeCatalog)
        {
            if (!entry.BaseRecipeRuntimeIndex.HasValue)
                throw new InvalidOperationException(
                    "Decrafting Shimmer transformation is missing its base recipe index.");

            IReadOnlyList<ShimmerWorldRecipeVariant> worldVariants = ResolveWorldRecipeVariants(
                entry.BaseRecipeRuntimeIndex.Value,
                entry.CrimsonRecipeRuntimeIndex,
                entry.CorruptionRecipeRuntimeIndex);

            foreach (ShimmerWorldRecipeVariant worldVariant in worldVariants)
            {
                if (!recipeCatalog.Contains(worldVariant.RuntimeIndex))
                {
                    throw new InvalidOperationException(
                        $"Shimmer decrafting variant references catalog-missing recipe runtime index {worldVariant.RuntimeIndex}.");
                }

                ShimmerTransformationVariant variant = BuildDecraftVariant(
                    entry.InputItemId,
                    worldVariant.RuntimeIndex,
                    worldVariant.WorldCondition);

                foreach (ShimmerTransformationOutput output in variant.Outputs)
                    ValidateOutputItem(itemCatalog, output.ItemId);

                variants.Add(variant);
            }
        }

        private static ShimmerTransformationVariant BuildDecraftVariant(
            int inputItemId,
            int runtimeIndex,
            ShimmerWorldCondition worldCondition)
        {
            if (runtimeIndex < 0 || runtimeIndex >= Recipe.numRecipes)
            {
                throw new InvalidOperationException(
                    $"Shimmer decrafting variant references invalid recipe runtime index {runtimeIndex}.");
            }

            Recipe recipe = Main.recipe[runtimeIndex];
            if (recipe == null)
                throw new InvalidOperationException($"Shimmer decrafting recipe {runtimeIndex} is unavailable.");

            List<ShimmerTransformationOutput> outputs = BuildDecraftOutputs(recipe);
            ShimmerProgressionRequirement progressionRequirement = ShimmerProgressionRequirement.None;

            if (GetSetValue(ShimmerTransforms.RecipeSets.PostSkeletron, runtimeIndex))
                progressionRequirement |= ShimmerProgressionRequirement.PostSkeletron;
            if (GetSetValue(ShimmerTransforms.RecipeSets.PostGolem, runtimeIndex))
                progressionRequirement |= ShimmerProgressionRequirement.PostGolem;

            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Decraft,
                recipe.createItem.stack,
                outputs,
                progressionRequirement,
                worldCondition,
                moonPhase: null,
                decraftingRecipeRuntimeIndex: runtimeIndex,
                isAlchemy: recipe.alchemy);
        }

        private static List<ShimmerTransformationOutput> BuildDecraftOutputs(Recipe recipe)
        {
            if (recipe == null)
                throw new ArgumentNullException(nameof(recipe));

            var outputs = new List<ShimmerTransformationOutput>();

            if (recipe.customShimmerResults != null)
            {
                foreach (Item item in recipe.customShimmerResults)
                {
                    if (item == null || item.type <= 0 || item.stack <= 0)
                        continue;

                    outputs.Add(new ShimmerTransformationOutput(item.type, item.stack));
                }
            }
            else
            {
                Recipe.RequiredItemEntry[] requirements = recipe.requiredItemQuickLookup;

                if (requirements != null)
                {
                    foreach (Recipe.RequiredItemEntry requirement in requirements)
                    {
                        if (requirement.itemIdOrRecipeGroup <= 0)
                            break;

                        int itemId = requirement.IsRecipeGroup
                            ? requirement.RecipeGroup.DecraftItemId
                            : requirement.itemIdOrRecipeGroup;

                        if (itemId > 0 && requirement.stack > 0)
                            outputs.Add(new ShimmerTransformationOutput(itemId, requirement.stack));
                    }
                }
            }

            if (outputs.Count == 0)
                throw new InvalidOperationException("Shimmer decrafting recipe does not define any return items.");

            return outputs;
        }

        private static int ResolveDirectEquivalentItemId(int inputItemId)
        {
            return VanillaShimmerTransformationCatalogBuilder.ResolveEquivalentItemId(
                inputItemId,
                ItemID.Sets.ShimmerCountsAsItem[inputItemId],
                decraftEquivalentItemId: -1,
                forDecrafting: false);
        }

        private static bool GetSetValue(bool[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length && values[index];
        }

        private static void ValidateOutputItem(ItemCatalog itemCatalog, int itemId)
        {
            if (!itemCatalog.Contains(itemId))
            {
                throw new InvalidOperationException(
                    $"Shimmer transformation references catalog-missing output item ID {itemId}.");
            }
        }
    }
}