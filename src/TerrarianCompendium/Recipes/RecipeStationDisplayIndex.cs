using System;
using System.Collections.Generic;
using Terraria.ID;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeStationDisplayIndex
    {
        private readonly Dictionary<int, int> _representativeItemIdByTileId;
        private readonly Dictionary<int, int> _requiredTileIdByItemId;

        private RecipeStationDisplayIndex(
            Dictionary<int, int> representativeItemIdByTileId,
            Dictionary<int, int> requiredTileIdByItemId)
        {
            _representativeItemIdByTileId = representativeItemIdByTileId;
            _requiredTileIdByItemId = requiredTileIdByItemId;
        }

        public static RecipeStationDisplayIndex Create(ItemCatalog catalog, RecipeCatalog recipeCatalog)
        {
            return Create(catalog, recipeCatalog, GetCreateTileId);
        }

        internal static RecipeStationDisplayIndex Create(
            ItemCatalog catalog,
            RecipeCatalog recipeCatalog,
            Func<int, int> createTileIdResolver)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (recipeCatalog == null)
                throw new ArgumentNullException(nameof(recipeCatalog));

            if (createTileIdResolver == null)
                throw new ArgumentNullException(nameof(createTileIdResolver));

            var requiredTileIds = new HashSet<int>();

            foreach (RecipeCatalogEntry recipe in recipeCatalog.Recipes)
            {
                if (recipe.EnvironmentRequirements.RequiredTileId.HasValue)
                    requiredTileIds.Add(recipe.EnvironmentRequirements.RequiredTileId.Value);
            }

            var representativeItemIdByTileId = new Dictionary<int, int>();
            var requiredTileIdByItemId = new Dictionary<int, int>();

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                int tileId = createTileIdResolver(entry.Id);

                if (tileId < 0 || !requiredTileIds.Contains(tileId))
                    continue;

                requiredTileIdByItemId.Add(entry.Id, tileId);

                if (!representativeItemIdByTileId.ContainsKey(tileId))
                    representativeItemIdByTileId.Add(tileId, entry.Id);
            }

            return new RecipeStationDisplayIndex(representativeItemIdByTileId, requiredTileIdByItemId);
        }

        public bool TryGetRepresentativeItemId(int tileId, out int itemId)
        {
            if (tileId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tileId),
                    tileId,
                    "Crafting station tile ID must not be negative.");
            }

            return _representativeItemIdByTileId.TryGetValue(tileId, out itemId);
        }

        public bool TryGetRequiredTileIdForItem(int itemId, out int tileId)
        {
            if (itemId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");
            }

            return _requiredTileIdByItemId.TryGetValue(itemId, out tileId);
        }

        private static int GetCreateTileId(int itemId)
        {
            return ContentSamples.ItemsByType[itemId].createTile;
        }
    }
}