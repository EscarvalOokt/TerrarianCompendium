namespace TerrarianCompendium.Catalog
{
    internal static class VanillaItemCatalogInclusionPolicy
    {
        public static bool ShouldInclude(
            int requestedItemId,
            int resolvedItemId,
            string name,
            bool isDeprecated,
            bool shouldNotBeInInventory)
        {
            return requestedItemId > 0 &&
                   !isDeprecated &&
                   !shouldNotBeInInventory &&
                   resolvedItemId == requestedItemId &&
                   !string.IsNullOrWhiteSpace(name);
        }
    }
}