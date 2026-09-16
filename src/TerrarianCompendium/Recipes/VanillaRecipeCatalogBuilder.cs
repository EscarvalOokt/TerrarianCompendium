using System;
using System.Collections.Generic;
using Terraria;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Recipes
{
    internal sealed class VanillaRecipeCatalogBuilder(ILogger logger)
    {
        public RecipeCatalog Build()
        {
            Recipe[] recipes = Main.recipe;
            int recipeCount = Recipe.numRecipes;

            if (recipes == null)
                throw new InvalidOperationException("Terraria recipe array is not initialized.");

            if (recipeCount <= 0 || recipeCount > recipes.Length)
            {
                throw new InvalidOperationException(
                    $"Terraria recipe count {recipeCount} is not a finalized recipe universe for array length " +
                    $"{recipes.Length}.");
            }

            var entries = new List<RecipeCatalogEntry>(recipeCount);

            for (var runtimeIndex = 0; runtimeIndex < recipeCount; runtimeIndex++)
            {
                Recipe recipe = recipes[runtimeIndex];

                if (recipe == null)
                {
                    throw new InvalidOperationException(
                        $"Terraria recipe at runtime index {runtimeIndex} is not initialized.");
                }

                entries.Add(CreateEntry(runtimeIndex, recipe));
            }

            var catalog = RecipeCatalog.Create(entries);

            logger?.Info(
                $"Vanilla recipe catalog built: {catalog.Count} entries from Recipe.numRecipes={recipeCount}.");

            return catalog;
        }

        internal static RecipeIngredientRequirement CreateItemRequirement(int itemId)
        {
            return RecipeIngredientRequirement.ForItem(itemId);
        }

        internal static RecipeIngredientRequirement CreateRecipeGroupRequirement(
            int itemIdOrRecipeGroup,
            int fakeItemIdOffset,
            IEnumerable<int> validItemIds)
        {
            int recipeGroupId = NormalizeRecipeGroupId(itemIdOrRecipeGroup, fakeItemIdOffset);

            return RecipeIngredientRequirement.ForRecipeGroup(recipeGroupId, validItemIds);
        }

        internal static int NormalizeRecipeGroupId(int itemIdOrRecipeGroup, int fakeItemIdOffset)
        {
            if (fakeItemIdOffset <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(fakeItemIdOffset),
                    fakeItemIdOffset,
                    "Fake recipe group item ID offset must be greater than zero.");
            }

            if (itemIdOrRecipeGroup < fakeItemIdOffset)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemIdOrRecipeGroup),
                    itemIdOrRecipeGroup,
                    "Encoded recipe group identity must include the fake item ID offset.");
            }

            return itemIdOrRecipeGroup - fakeItemIdOffset;
        }

        internal static RecipeEnvironmentRequirements CreateEnvironmentRequirements(
            int requiredTile,
            bool requiresWater,
            bool requiresHoney,
            bool requiresLava,
            bool requiresSnowBiome,
            bool requiresGraveyardBiome,
            bool requiresMechdusa,
            bool requiresTorchGodsFavor)
        {
            int? requiredTileId = requiredTile >= 0 ? requiredTile : null;

            return new RecipeEnvironmentRequirements(
                requiredTileId,
                requiresWater,
                requiresHoney,
                requiresLava,
                requiresSnowBiome,
                requiresGraveyardBiome,
                requiresMechdusa,
                requiresTorchGodsFavor);
        }

        internal static RecipeCatalogEntry CreateEntry(
            int runtimeIndex,
            int resultItemId,
            int resultStack,
            IEnumerable<RecipeIngredient> ingredients,
            int requiredTile,
            bool requiresWater,
            bool requiresHoney,
            bool requiresLava,
            bool requiresSnowBiome,
            bool requiresGraveyardBiome,
            bool requiresMechdusa,
            bool requiresTorchGodsFavor,
            bool isAlchemy)
        {
            RecipeEnvironmentRequirements environmentRequirements = CreateEnvironmentRequirements(
                requiredTile,
                requiresWater,
                requiresHoney,
                requiresLava,
                requiresSnowBiome,
                requiresGraveyardBiome,
                requiresMechdusa,
                requiresTorchGodsFavor);

            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                resultStack,
                ingredients,
                environmentRequirements,
                isAlchemy);
        }

        private static RecipeCatalogEntry CreateEntry(int runtimeIndex, Recipe recipe)
        {
            Item result = recipe.createItem;

            if (result == null || result.type <= 0 || result.stack <= 0)
            {
                throw new InvalidOperationException(
                    $"Terraria recipe at runtime index {runtimeIndex} has an invalid result item.");
            }

            IReadOnlyList<RecipeIngredient> ingredients = CreateIngredients(runtimeIndex, recipe);

            return CreateEntry(
                runtimeIndex,
                result.type,
                result.stack,
                ingredients,
                recipe.requiredTile,
                recipe.needWater,
                recipe.needHoney,
                recipe.needLava,
                recipe.needSnowBiome,
                recipe.needGraveyardBiome,
                recipe.needMechdusa,
                recipe.needTorchGodsFavor,
                recipe.alchemy);
        }

        private static IReadOnlyList<RecipeIngredient> CreateIngredients(int runtimeIndex, Recipe recipe)
        {
            Recipe.RequiredItemEntry[] normalizedRequirements = recipe.requiredItemQuickLookup;
            Item[] displayRequirements = recipe.requiredItem;

            if (normalizedRequirements == null)
            {
                throw new InvalidOperationException(
                    $"Terraria recipe at runtime index {runtimeIndex} has no normalized ingredient requirements.");
            }

            var ingredients = new List<RecipeIngredient>();

            for (var requirementIndex = 0; requirementIndex < normalizedRequirements.Length; requirementIndex++)
            {
                Recipe.RequiredItemEntry normalizedRequirement = normalizedRequirements[requirementIndex];

                if (normalizedRequirement.itemIdOrRecipeGroup == 0)
                    break;

                Item displayRequirement = displayRequirements != null && requirementIndex < displayRequirements.Length
                    ? displayRequirements[requirementIndex]
                    : null;

                if (displayRequirement == null || displayRequirement.type <= 0)
                {
                    throw new InvalidOperationException(
                        $"Terraria recipe at runtime index {runtimeIndex} has no display ingredient for requirement " +
                        $"slot {requirementIndex}.");
                }

                RecipeIngredientRequirement requirement = normalizedRequirement.IsRecipeGroup
                    ? CreateRecipeGroupRequirement(normalizedRequirement.itemIdOrRecipeGroup)
                    : CreateItemRequirement(normalizedRequirement.itemIdOrRecipeGroup);

                ingredients.Add(
                    new RecipeIngredient(displayRequirement.type, normalizedRequirement.stack, requirement));
            }

            return ingredients;
        }

        private static RecipeIngredientRequirement CreateRecipeGroupRequirement(int itemIdOrRecipeGroup)
        {
            int recipeGroupId = NormalizeRecipeGroupId(itemIdOrRecipeGroup, RecipeGroup.FakeItemIdOffset);

            if (RecipeGroup.recipeGroups == null ||
                !RecipeGroup.recipeGroups.TryGetValue(recipeGroupId, out RecipeGroup recipeGroup) ||
                recipeGroup == null)
            {
                throw new InvalidOperationException(
                    $"Terraria recipe references unknown recipe group ID {recipeGroupId}.");
            }

            if (recipeGroup.RegisteredId != recipeGroupId)
            {
                throw new InvalidOperationException(
                    $"Terraria recipe group lookup returned registered ID {recipeGroup.RegisteredId} " +
                    $"for requested group ID {recipeGroupId}.");
            }

            return RecipeIngredientRequirement.ForRecipeGroup(recipeGroup.RegisteredId, recipeGroup.ValidItems);
        }
    }
}