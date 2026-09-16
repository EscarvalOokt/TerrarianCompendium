using System;

namespace TerrarianCompendium.Catalog
{
    internal sealed class ItemCatalogEntry
    {
        public ItemCatalogEntry(
            int id,
            string name,
            ItemCategoryMembership categoryMemberships = ItemCategoryMembership.None,
            int nativeSortGroup = 0,
            int nativeSortOrder = 0,
            ItemSemanticMembership semanticMemberships = ItemSemanticMembership.None,
            ItemSortMetrics sortMetrics = default,
            ItemDetailsStats detailsStats = default)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Item ID must be greater than zero.");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Item name must not be empty or whitespace.", nameof(name));

            Id = id;
            Name = name;
            CategoryMemberships = categoryMemberships;
            NativeSortGroup = nativeSortGroup;
            NativeSortOrder = nativeSortOrder;
            SemanticMemberships = semanticMemberships;
            SortMetrics = sortMetrics;
            DetailsStats = detailsStats;
        }

        public int Id { get; }

        public string Name { get; }

        public ItemCategoryMembership CategoryMemberships { get; }

        public int NativeSortGroup { get; }

        public int NativeSortOrder { get; }

        public ItemSemanticMembership SemanticMemberships { get; }

        public ItemSortMetrics SortMetrics { get; }

        public ItemDetailsStats DetailsStats { get; }
    }
}