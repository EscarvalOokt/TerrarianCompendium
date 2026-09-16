using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Crafting
{
    internal static class ItemCraftingAvailability
    {
        public static bool IsCraftableNow(
            int itemId,
            RecipeIndex recipeIndex,
            CraftingAvailabilityState craftingAvailabilityState)
        {
            ValidateArguments(itemId, recipeIndex, craftingAvailabilityState);

            foreach (RecipeCatalogEntry recipe in recipeIndex.GetRecipesProducing(itemId))
            {
                if (craftingAvailabilityState.IsCraftable(recipe.RuntimeIndex))
                    return true;
            }

            return false;
        }

        public static IReadOnlyList<RecipeCatalogEntry> GetCraftableRecipes(
            int itemId,
            RecipeIndex recipeIndex,
            CraftingAvailabilityState craftingAvailabilityState)
        {
            ValidateArguments(itemId, recipeIndex, craftingAvailabilityState);

            var recipes = new List<RecipeCatalogEntry>();

            foreach (RecipeCatalogEntry recipe in recipeIndex.GetRecipesProducing(itemId))
            {
                if (craftingAvailabilityState.IsCraftable(recipe.RuntimeIndex))
                    recipes.Add(recipe);
            }

            return new ReadOnlyCollection<RecipeCatalogEntry>(recipes);
        }

        private static void ValidateArguments(
            int itemId,
            RecipeIndex recipeIndex,
            CraftingAvailabilityState craftingAvailabilityState)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            if (recipeIndex == null)
                throw new ArgumentNullException(nameof(recipeIndex));

            if (craftingAvailabilityState == null)
                throw new ArgumentNullException(nameof(craftingAvailabilityState));
        }
    }
}