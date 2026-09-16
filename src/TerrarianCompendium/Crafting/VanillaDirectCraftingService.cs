using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Crafting
{
    internal sealed class VanillaDirectCraftingService(
        RecipeCatalog recipeCatalog,
        RecipeIndex recipeIndex,
        CraftingAvailabilityState craftingAvailabilityState,
        ILogger logger)
    {
        private readonly CraftingAvailabilityState _craftingAvailabilityState = craftingAvailabilityState ??
            throw new ArgumentNullException(nameof(craftingAvailabilityState));

        private readonly RecipeCatalog _recipeCatalog =
            recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));

        private readonly RecipeIndex _recipeIndex = recipeIndex ?? throw new ArgumentNullException(nameof(recipeIndex));

        private bool _failureReported;

        public long Revision => _craftingAvailabilityState.Revision;

        public IReadOnlyList<RecipeCatalogEntry> GetCraftableRecipesForItem(int itemId)
        {
            return ItemCraftingAvailability.GetCraftableRecipes(itemId, _recipeIndex, _craftingAvailabilityState);
        }

        public bool TryCraft(int runtimeRecipeIndex, int quantity)
        {
            if (quantity <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(quantity),
                    quantity,
                    "Craft quantity must be greater than zero.");

            if (!_recipeCatalog.TryGet(runtimeRecipeIndex, out RecipeCatalogEntry catalogEntry))
                return false;

            Recipe[] recipes = Main.recipe;

            if (recipes == null || runtimeRecipeIndex < 0 || runtimeRecipeIndex >= recipes.Length)
            {
                ReportValidationFailure(
                    $"Recipe runtime index {runtimeRecipeIndex} is outside the initialized Terraria recipe array.");
                return false;
            }

            Recipe recipe = recipes[runtimeRecipeIndex];

            if (recipe?.createItem == null || recipe.createItem.type <= 0)
            {
                ReportValidationFailure($"Terraria recipe at runtime index {runtimeRecipeIndex} is not initialized.");
                return false;
            }

            if (recipe.createItem.type != catalogEntry.ResultItemId ||
                recipe.createItem.stack != catalogEntry.ResultStack)
            {
                ReportValidationFailure(
                    $"Terraria recipe at runtime index {runtimeRecipeIndex} no longer matches catalog result " +
                    $"{catalogEntry.ResultItemId}x{catalogEntry.ResultStack}.");
                return false;
            }

            Player player = Main.LocalPlayer;

            if (player == null ||
                player.dead ||
                player.ghost ||
                player.UsingOrReusingItem ||
                player.IsLockedFromCrafting())
            {
                return false;
            }

            var crafted = false;
            VanillaCraftingAvailabilityNativeExecutionResult execution =
                VanillaCraftingAvailabilityNativeBridge.Execute(
                    player,
                    () =>
                    {
                        if (!recipe.PlayerMeetsEnvironmentConditions(player) ||
                            !Recipe.CollectedEnoughItemsToCraft(recipe) ||
                            !Main.CursorHasSpaceToCraftRecipe(recipe))
                        {
                            return;
                        }

                        ItemSlot.RefreshStackSplitCooldown();
                        CraftingRequests.CraftItem(recipe, quantity, quickCraft: false);
                        crafted = true;
                    });

            if (!execution.Succeeded)
            {
                ReportExecutionFailure(execution);
                return false;
            }

            _failureReported = false;
            return crafted;
        }

        private void ReportValidationFailure(string message)
        {
            if (_failureReported)
                return;

            _failureReported = true;
            logger?.Warn($"[{TerrarianCompendiumMod.ModName}] Direct crafting is unavailable. {message}");
        }

        private void ReportExecutionFailure(VanillaCraftingAvailabilityNativeExecutionResult execution)
        {
            if (_failureReported)
                return;

            _failureReported = true;

            switch (execution.Status)
            {
                case VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Direct crafting is unavailable because Terraria's " +
                        "owned-item collector could not be resolved for the target runtime.");
                    break;
                case VanillaCraftingAvailabilityNativeExecutionStatus.AdjacencyRestoreFailed:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Direct crafting was aborted because player crafting " +
                        "adjacency could not be restored.");
                    break;
                case VanillaCraftingAvailabilityNativeExecutionStatus.Failed:
                    logger?.Error($"[{TerrarianCompendiumMod.ModName}] Direct crafting failed.", execution.Exception);
                    break;
                default:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Direct crafting failed with status " +
                        $"'{execution.Status}'.");
                    break;
            }
        }
    }
}