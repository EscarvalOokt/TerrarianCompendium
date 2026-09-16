using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Catalog
{
    internal sealed class ItemCatalog
    {
        private readonly IReadOnlyList<ItemCatalogEntry> _items;
        private readonly Dictionary<int, ItemCatalogEntry> _itemsById;

        private ItemCatalog(IReadOnlyList<ItemCatalogEntry> items, Dictionary<int, ItemCatalogEntry> itemsById)
        {
            _items = items;
            _itemsById = itemsById;
        }

        public IReadOnlyList<ItemCatalogEntry> Items => _items;

        public int Count => _items.Count;

        public static ItemCatalog Create(IEnumerable<ItemCatalogEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var items = new List<ItemCatalogEntry>();
            var itemsById = new Dictionary<int, ItemCatalogEntry>();

            foreach (ItemCatalogEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries must not contain null values.", nameof(entries));

                if (itemsById.ContainsKey(entry.Id))
                {
                    throw new ArgumentException($"Catalog contains duplicate item ID {entry.Id}.", nameof(entries));
                }

                items.Add(entry);
                itemsById.Add(entry.Id, entry);
            }

            items.Sort(CompareById);

            return new ItemCatalog(new ReadOnlyCollection<ItemCatalogEntry>(items), itemsById);
        }

        public bool Contains(int itemId)
        {
            return _itemsById.ContainsKey(itemId);
        }

        public bool TryGet(int itemId, out ItemCatalogEntry entry)
        {
            return _itemsById.TryGetValue(itemId, out entry);
        }

        private static int CompareById(ItemCatalogEntry left, ItemCatalogEntry right)
        {
            return left.Id.CompareTo(right.Id);
        }
    }
}