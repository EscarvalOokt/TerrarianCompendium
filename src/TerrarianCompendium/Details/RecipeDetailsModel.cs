using System;
using System.Collections.Generic;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Localization;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Details
{
    internal sealed class RecipeDetailsModel(
        RecipeCatalog recipeCatalog,
        ItemCatalog itemCatalog,
        ItemTextIndex itemTextIndex,
        RecipeIndex recipeIndex,
        RecipeStationDisplayIndex stationDisplayIndex,
        ChecklistState checklistState,
        JourneyResearchState journeyResearchState,
        RecipeFavoriteState favoriteState,
        CraftingAvailabilityState craftingAvailabilityState,
        RecipeFilterState filterState,
        CompendiumLocalization localization)
    {
        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        private readonly CraftingAvailabilityState _craftingAvailabilityState = craftingAvailabilityState ??
            throw new ArgumentNullException(nameof(craftingAvailabilityState));

        private readonly RecipeFavoriteState _favoriteState =
            favoriteState ?? throw new ArgumentNullException(nameof(favoriteState));

        private readonly RecipeFilterState _filterState =
            filterState ?? throw new ArgumentNullException(nameof(filterState));

        private readonly ItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

        private readonly ItemTextIndex _itemTextIndex =
            itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));

        private readonly JourneyResearchState _journeyResearchState = journeyResearchState;

        private readonly CompendiumLocalization _localization =
            localization ?? throw new ArgumentNullException(nameof(localization));

        private readonly RecipeCatalog _recipeCatalog =
            recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));

        private readonly RecipeIndex _recipeIndex = recipeIndex ?? throw new ArgumentNullException(nameof(recipeIndex));

        private readonly RecipeStationDisplayIndex _stationDisplayIndex =
            stationDisplayIndex ?? throw new ArgumentNullException(nameof(stationDisplayIndex));

        private long _cachedChecklistRevision = -1;
        private long _cachedCraftingRevision = -1;
        private long _cachedFavoriteRevision = -1;
        private long _cachedItemTextRevision = -1;
        private long _cachedLocalizationRevision = -1;
        private RecipeDetailsProjection _cachedProjection;
        private long _cachedQueryChecklistRevision = -1;
        private long _cachedQueryCraftingRevision = -1;
        private long _cachedQueryFavoriteRevision = -1;
        private long _cachedQueryFilterRevision = -1;
        private int _cachedQueryItemId = -1;
        private long _cachedQueryItemTextRevision = -1;
        private long _cachedQueryLocalizationRevision = -1;
        private RecipeQueryDetailsProjection _cachedQueryProjection;
        private long _cachedQueryResearchRevision = -1;
        private int _cachedRuntimeIndex = -1;

        public bool TryGetProjection(int runtimeIndex, out RecipeDetailsProjection projection)
        {
            if (!_recipeCatalog.TryGet(runtimeIndex, out RecipeCatalogEntry recipe))
            {
                projection = null;
                return false;
            }

            long checklistRevision = _checklistState.Revision;
            long craftingRevision = _craftingAvailabilityState.Revision;
            long favoriteRevision = _favoriteState.Revision;
            long itemTextRevision = _itemTextIndex.Revision;
            long localizationRevision = _localization.Revision;

            if (_cachedProjection != null &&
                _cachedRuntimeIndex == runtimeIndex &&
                _cachedChecklistRevision == checklistRevision &&
                _cachedCraftingRevision == craftingRevision &&
                _cachedFavoriteRevision == favoriteRevision &&
                _cachedItemTextRevision == itemTextRevision &&
                _cachedLocalizationRevision == localizationRevision)
            {
                projection = _cachedProjection;
                return true;
            }

            var ingredients = new List<RecipeDetailsIngredientProjection>(recipe.Ingredients.Count);

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
                ingredients.Add(CreateIngredientProjection(ingredient));

            RecipeEnvironmentRequirements environment = recipe.EnvironmentRequirements;
            RecipeDetailsItemReference craftingStation = null;

            if (environment.RequiredTileId.HasValue &&
                _stationDisplayIndex.TryGetRepresentativeItemId(
                    environment.RequiredTileId.Value,
                    out int stationItemId))
            {
                craftingStation = CreateItemReference(stationItemId);
            }

            projection = new RecipeDetailsProjection(
                recipe.RuntimeIndex,
                CreateItemReference(recipe.ResultItemId),
                recipe.ResultStack,
                ingredients,
                environment.RequiredTileId.HasValue,
                craftingStation,
                environment.RequiresWater,
                environment.RequiresHoney,
                environment.RequiresLava,
                environment.RequiresSnowBiome,
                environment.RequiresGraveyardBiome,
                environment.RequiresMechdusa,
                environment.RequiresTorchGodsFavor,
                recipe.IsAlchemy,
                _craftingAvailabilityState.IsCraftable(recipe.RuntimeIndex),
                _favoriteState.IsFavorite(recipe.RuntimeIndex));

            _cachedRuntimeIndex = runtimeIndex;
            _cachedProjection = projection;
            _cachedChecklistRevision = checklistRevision;
            _cachedCraftingRevision = craftingRevision;
            _cachedFavoriteRevision = favoriteRevision;
            _cachedItemTextRevision = itemTextRevision;
            _cachedLocalizationRevision = localizationRevision;

            return true;
        }

        public bool TryGetQueryProjection(int resultItemId, out RecipeQueryDetailsProjection projection)
        {
            IReadOnlyList<RecipeCatalogEntry> recipes = _recipeIndex.GetRecipesProducing(resultItemId);

            if (recipes.Count == 0)
            {
                projection = null;
                return false;
            }

            long filterRevision = _filterState.Revision;
            long checklistRevision = _checklistState.Revision;
            long researchRevision = _filterState.UsesJourneyResearchState
                ? _journeyResearchState?.Revision ?? -1
                : _cachedQueryResearchRevision;
            long craftingRevision = _filterState.UsesCraftingAvailabilityState
                ? _craftingAvailabilityState.Revision
                : _cachedQueryCraftingRevision;
            long favoriteRevision = _filterState.UsesRecipeFavoriteState
                ? _favoriteState.Revision
                : _cachedQueryFavoriteRevision;
            long itemTextRevision = _itemTextIndex.Revision;
            long localizationRevision = _localization.Revision;

            if (_cachedQueryProjection != null &&
                _cachedQueryItemId == resultItemId &&
                _cachedQueryFilterRevision == filterRevision &&
                _cachedQueryChecklistRevision == checklistRevision &&
                _cachedQueryResearchRevision == researchRevision &&
                _cachedQueryCraftingRevision == craftingRevision &&
                _cachedQueryFavoriteRevision == favoriteRevision &&
                _cachedQueryItemTextRevision == itemTextRevision &&
                _cachedQueryLocalizationRevision == localizationRevision)
            {
                projection = _cachedQueryProjection;
                return true;
            }

            var matchingRuntimeIndices = new List<int>(recipes.Count);

            foreach (RecipeCatalogEntry recipe in recipes)
            {
                if (!_filterState.IsActive ||
                    RecipeFilterMatcher.Matches(
                        recipe,
                        _filterState,
                        _checklistState,
                        _journeyResearchState,
                        _favoriteState,
                        _craftingAvailabilityState))
                {
                    matchingRuntimeIndices.Add(recipe.RuntimeIndex);
                }
            }

            projection = new RecipeQueryDetailsProjection(
                CreateItemReference(resultItemId),
                recipes.Count,
                matchingRuntimeIndices);

            _cachedQueryItemId = resultItemId;
            _cachedQueryProjection = projection;
            _cachedQueryFilterRevision = filterRevision;
            _cachedQueryChecklistRevision = checklistRevision;
            _cachedQueryResearchRevision = researchRevision;
            _cachedQueryCraftingRevision = craftingRevision;
            _cachedQueryFavoriteRevision = favoriteRevision;
            _cachedQueryItemTextRevision = itemTextRevision;
            _cachedQueryLocalizationRevision = localizationRevision;
            return true;
        }

        private RecipeDetailsIngredientProjection CreateIngredientProjection(RecipeIngredient ingredient)
        {
            var validItems = new List<RecipeDetailsItemReference>(ingredient.Requirement.ValidItemIds.Count);

            foreach (int validItemId in ingredient.Requirement.ValidItemIds)
                validItems.Add(CreateItemReference(validItemId));

            return new RecipeDetailsIngredientProjection(
                CreateItemReference(ingredient.DisplayItemId),
                ingredient.Stack,
                ingredient.Requirement.Kind,
                validItems);
        }

        private RecipeDetailsItemReference CreateItemReference(int itemId)
        {
            if (_itemCatalog.Contains(itemId))
            {
                return new RecipeDetailsItemReference(
                    itemId,
                    _itemTextIndex.GetName(itemId),
                    isCollectionTracked: true,
                    isFound: _checklistState.IsFound(itemId));
            }

            return new RecipeDetailsItemReference(
                itemId,
                _localization.Format(CompendiumTextKeys.Common.ItemIdHash, itemId),
                isCollectionTracked: false,
                isFound: false);
        }
    }
}