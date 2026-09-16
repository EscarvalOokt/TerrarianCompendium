using System;
using System.Collections.Generic;
using Terraria;
using TerrariaModder.Core.Logging;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Crafting
{
    internal sealed class VanillaCraftingAvailabilityScanner(
        RecipeCatalog recipeCatalog,
        CraftingAvailabilityState availabilityState,
        ILogger logger)
    {
        private readonly CraftingAvailabilityState _availabilityState =
            availabilityState ?? throw new ArgumentNullException(nameof(availabilityState));

        private readonly RecipeCatalog _recipeCatalog =
            recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));

        private bool _failureReported;

        public void Update(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var availableRecipeIndices = new List<int>();
            VanillaCraftingAvailabilityNativeExecutionResult execution =
                VanillaCraftingAvailabilityNativeBridge.Execute(
                    player,
                    () =>
                    {
                        Recipe[] recipes = Main.recipe;

                        if (recipes == null)
                            throw new InvalidOperationException("Terraria recipe array is not initialized.");

                        foreach (RecipeCatalogEntry catalogEntry in _recipeCatalog.Recipes)
                        {
                            int runtimeIndex = catalogEntry.RuntimeIndex;

                            if (runtimeIndex < 0 || runtimeIndex >= recipes.Length)
                            {
                                throw new InvalidOperationException(
                                    $"Recipe catalog runtime index {runtimeIndex} is outside Main.recipe bounds.");
                            }

                            Recipe recipe = recipes[runtimeIndex];

                            if (recipe?.createItem == null || recipe.createItem.type <= 0)
                            {
                                throw new InvalidOperationException(
                                    $"Terraria recipe at runtime index {runtimeIndex} is not initialized.");
                            }

                            if (recipe.createItem.type != catalogEntry.ResultItemId ||
                                recipe.createItem.stack != catalogEntry.ResultStack)
                            {
                                throw new InvalidOperationException(
                                    $"Terraria recipe at runtime index {runtimeIndex} no longer matches " +
                                    $"catalog result {catalogEntry.ResultItemId}x{catalogEntry.ResultStack}.");
                            }

                            if (!recipe.PlayerMeetsEnvironmentConditions(player) ||
                                !Recipe.CollectedEnoughItemsToCraft(recipe))
                            {
                                continue;
                            }

                            availableRecipeIndices.Add(runtimeIndex);
                        }
                    });

            if (!execution.Succeeded)
            {
                ReportFailure(execution);
                return;
            }

            _availabilityState.ReplaceSnapshot(availableRecipeIndices);
            _failureReported = false;
        }

        private void ReportFailure(VanillaCraftingAvailabilityNativeExecutionResult execution)
        {
            if (_failureReported)
                return;

            _failureReported = true;

            switch (execution.Status)
            {
                case VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Current crafting availability is unavailable because " +
                        "Terraria's owned-item collector could not be resolved for the target runtime.");
                    break;
                case VanillaCraftingAvailabilityNativeExecutionStatus.AdjacencyRestoreFailed:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Current crafting availability scan was discarded because " +
                        "player crafting adjacency could not be restored.");
                    break;
                case VanillaCraftingAvailabilityNativeExecutionStatus.Failed:
                    logger?.Error(
                        $"[{TerrarianCompendiumMod.ModName}] Current crafting availability scan failed.",
                        execution.Exception);
                    break;
                default:
                    logger?.Warn(
                        $"[{TerrarianCompendiumMod.ModName}] Current crafting availability scan failed with status " +
                        $"'{execution.Status}'.");
                    break;
            }
        }
    }
}