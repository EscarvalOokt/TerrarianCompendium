using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Details
{
    internal sealed class RecipeDetailsItemReference(int itemId, string name, bool isCollectionTracked, bool isFound)
    {
        public int ItemId { get; } = itemId;

        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

        public bool IsCollectionTracked { get; } = isCollectionTracked;

        public bool IsFound { get; } = isFound;

        public bool IsMissing => IsCollectionTracked && !IsFound;
    }

    internal sealed class RecipeDetailsIngredientProjection
    {
        public RecipeDetailsIngredientProjection(
            RecipeDetailsItemReference displayItem,
            int stack,
            RecipeIngredientRequirementKind requirementKind,
            IEnumerable<RecipeDetailsItemReference> validItems)
        {
            if (stack <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stack),
                    stack,
                    "Ingredient stack must be greater than zero.");
            }

            if (validItems == null)
                throw new ArgumentNullException(nameof(validItems));

            var copiedValidItems = new List<RecipeDetailsItemReference>();

            foreach (RecipeDetailsItemReference validItem in validItems)
            {
                if (validItem == null)
                {
                    throw new ArgumentException(
                        "Valid ingredient items must not contain null values.",
                        nameof(validItems));
                }

                copiedValidItems.Add(validItem);
            }

            DisplayItem = displayItem ?? throw new ArgumentNullException(nameof(displayItem));
            Stack = stack;
            RequirementKind = requirementKind;
            ValidItems = new ReadOnlyCollection<RecipeDetailsItemReference>(copiedValidItems);
        }

        public RecipeDetailsItemReference DisplayItem { get; }

        public int Stack { get; }

        public RecipeIngredientRequirementKind RequirementKind { get; }

        public IReadOnlyList<RecipeDetailsItemReference> ValidItems { get; }

        public bool IsRecipeGroup => RequirementKind == RecipeIngredientRequirementKind.RecipeGroup;
    }


    internal sealed class RecipeQueryDetailsProjection
    {
        public RecipeQueryDetailsProjection(
            RecipeDetailsItemReference result,
            int totalRecipeCount,
            IEnumerable<int> matchingRecipeRuntimeIndices)
        {
            if (totalRecipeCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalRecipeCount),
                    totalRecipeCount,
                    "Total recipe count must be greater than zero.");
            }

            if (matchingRecipeRuntimeIndices == null)
                throw new ArgumentNullException(nameof(matchingRecipeRuntimeIndices));

            var copiedIndices = new List<int>();

            foreach (int runtimeIndex in matchingRecipeRuntimeIndices)
            {
                if (runtimeIndex < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(matchingRecipeRuntimeIndices),
                        runtimeIndex,
                        "Matching runtime recipe indices must not be negative.");
                }

                copiedIndices.Add(runtimeIndex);
            }

            Result = result ?? throw new ArgumentNullException(nameof(result));
            TotalRecipeCount = totalRecipeCount;
            MatchingRecipeRuntimeIndices = new ReadOnlyCollection<int>(copiedIndices);
        }

        public RecipeDetailsItemReference Result { get; }

        public int TotalRecipeCount { get; }

        public IReadOnlyList<int> MatchingRecipeRuntimeIndices { get; }

        public int MatchingRecipeCount => MatchingRecipeRuntimeIndices.Count;
    }

    internal sealed class RecipeDetailsProjection
    {
        public RecipeDetailsProjection(
            int runtimeIndex,
            RecipeDetailsItemReference result,
            int resultStack,
            IEnumerable<RecipeDetailsIngredientProjection> ingredients,
            bool requiresCraftingStation,
            RecipeDetailsItemReference craftingStation,
            bool requiresWater,
            bool requiresHoney,
            bool requiresLava,
            bool requiresSnowBiome,
            bool requiresGraveyardBiome,
            bool requiresMechdusa,
            bool requiresTorchGodsFavor,
            bool isAlchemy,
            bool isCraftableNow,
            bool isFavorite)
        {
            if (runtimeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeIndex),
                    runtimeIndex,
                    "Runtime recipe index must not be negative.");
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

            var copiedIngredients = new List<RecipeDetailsIngredientProjection>();

            foreach (RecipeDetailsIngredientProjection ingredient in ingredients)
            {
                if (ingredient == null)
                {
                    throw new ArgumentException(
                        "Recipe detail ingredients must not contain null values.",
                        nameof(ingredients));
                }

                copiedIngredients.Add(ingredient);
            }

            RuntimeIndex = runtimeIndex;
            Result = result ?? throw new ArgumentNullException(nameof(result));
            ResultStack = resultStack;
            Ingredients = new ReadOnlyCollection<RecipeDetailsIngredientProjection>(copiedIngredients);
            RequiresCraftingStation = requiresCraftingStation;
            CraftingStation = craftingStation;
            RequiresWater = requiresWater;
            RequiresHoney = requiresHoney;
            RequiresLava = requiresLava;
            RequiresSnowBiome = requiresSnowBiome;
            RequiresGraveyardBiome = requiresGraveyardBiome;
            RequiresMechdusa = requiresMechdusa;
            RequiresTorchGodsFavor = requiresTorchGodsFavor;
            IsAlchemy = isAlchemy;
            IsCraftableNow = isCraftableNow;
            IsFavorite = isFavorite;
        }

        public int RuntimeIndex { get; }

        public RecipeDetailsItemReference Result { get; }

        public int ResultStack { get; }

        public IReadOnlyList<RecipeDetailsIngredientProjection> Ingredients { get; }

        public bool RequiresCraftingStation { get; }

        public RecipeDetailsItemReference CraftingStation { get; }

        public bool RequiresWater { get; }

        public bool RequiresHoney { get; }

        public bool RequiresLava { get; }

        public bool RequiresSnowBiome { get; }

        public bool RequiresGraveyardBiome { get; }

        public bool RequiresMechdusa { get; }

        public bool RequiresTorchGodsFavor { get; }

        public bool IsAlchemy { get; }

        public bool IsCraftableNow { get; }

        public bool IsFavorite { get; }
    }
}