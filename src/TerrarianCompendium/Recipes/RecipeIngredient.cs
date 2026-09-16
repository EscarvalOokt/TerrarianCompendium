using System;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeIngredient
    {
        public RecipeIngredient(int displayItemId, int stack, RecipeIngredientRequirement requirement)
        {
            if (displayItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(displayItemId),
                    displayItemId,
                    "Display item ID must be greater than zero.");
            }

            if (stack <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stack),
                    stack,
                    "Ingredient stack must be greater than zero.");
            }

            Requirement = requirement ?? throw new ArgumentNullException(nameof(requirement));
            DisplayItemId = displayItemId;
            Stack = stack;
        }

        public int DisplayItemId { get; }

        public int Stack { get; }

        public RecipeIngredientRequirement Requirement { get; }
    }
}