using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeCatalogEntry
    {
        public RecipeCatalogEntry(
            int runtimeIndex,
            int resultItemId,
            int resultStack,
            IEnumerable<RecipeIngredient> ingredients,
            RecipeEnvironmentRequirements environmentRequirements,
            bool isAlchemy)
        {
            if (runtimeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeIndex),
                    runtimeIndex,
                    "Runtime recipe index must not be negative.");
            }

            if (resultItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resultItemId),
                    resultItemId,
                    "Result item ID must be greater than zero.");
            }

            if (resultStack <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resultStack),
                    resultStack,
                    "Result stack must be greater than zero.");
            }

            if (ingredients == null)
                throw new ArgumentNullException(nameof(ingredients));

            var copiedIngredients = new List<RecipeIngredient>();

            foreach (RecipeIngredient ingredient in ingredients)
            {
                if (ingredient == null)
                {
                    throw new ArgumentException(
                        "Recipe ingredients must not contain null values.",
                        nameof(ingredients));
                }

                copiedIngredients.Add(ingredient);
            }

            RuntimeIndex = runtimeIndex;
            ResultItemId = resultItemId;
            ResultStack = resultStack;
            Ingredients = new ReadOnlyCollection<RecipeIngredient>(copiedIngredients);
            EnvironmentRequirements = environmentRequirements ??
                                      throw new ArgumentNullException(nameof(environmentRequirements));
            IsAlchemy = isAlchemy;
        }

        public int RuntimeIndex { get; }

        public int ResultItemId { get; }

        public int ResultStack { get; }

        public IReadOnlyList<RecipeIngredient> Ingredients { get; }

        public RecipeEnvironmentRequirements EnvironmentRequirements { get; }

        public bool IsAlchemy { get; }
    }
}