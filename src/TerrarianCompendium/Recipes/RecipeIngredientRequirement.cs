using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeIngredientRequirement
    {
        private RecipeIngredientRequirement(
            RecipeIngredientRequirementKind kind,
            int identityId,
            IReadOnlyList<int> validItemIds)
        {
            Kind = kind;
            IdentityId = identityId;
            ValidItemIds = validItemIds;
        }

        public RecipeIngredientRequirementKind Kind { get; }

        public int IdentityId { get; }

        public IReadOnlyList<int> ValidItemIds { get; }

        public static RecipeIngredientRequirement ForItem(int itemId)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            return new RecipeIngredientRequirement(
                RecipeIngredientRequirementKind.Item,
                itemId,
                new ReadOnlyCollection<int>(new List<int> { itemId }));
        }

        public static RecipeIngredientRequirement ForRecipeGroup(int recipeGroupId, IEnumerable<int> validItemIds)
        {
            if (recipeGroupId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(recipeGroupId),
                    recipeGroupId,
                    "Recipe group ID must not be negative.");
            }

            if (validItemIds == null)
                throw new ArgumentNullException(nameof(validItemIds));

            var uniqueItemIds = new HashSet<int>();

            foreach (int itemId in validItemIds)
            {
                if (itemId <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(validItemIds),
                        itemId,
                        "Recipe group item IDs must be greater than zero.");
                }

                uniqueItemIds.Add(itemId);
            }

            if (uniqueItemIds.Count == 0)
            {
                throw new ArgumentException(
                    "Recipe group must contain at least one valid item ID.",
                    nameof(validItemIds));
            }

            var copiedItemIds = new List<int>(uniqueItemIds);
            copiedItemIds.Sort();

            return new RecipeIngredientRequirement(
                RecipeIngredientRequirementKind.RecipeGroup,
                recipeGroupId,
                new ReadOnlyCollection<int>(copiedItemIds));
        }
    }
}