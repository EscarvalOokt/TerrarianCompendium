using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.ArmorSets
{
    internal sealed class ArmorSetIndex
    {
        private static readonly IReadOnlyList<ArmorSetCatalogEntry> _emptyEntries =
            new ReadOnlyCollection<ArmorSetCatalogEntry>(new List<ArmorSetCatalogEntry>());

        private readonly Dictionary<int, IReadOnlyList<ArmorSetCatalogEntry>> _entriesByItemId;

        private ArmorSetIndex(Dictionary<int, IReadOnlyList<ArmorSetCatalogEntry>> entriesByItemId)
        {
            _entriesByItemId = entriesByItemId;
        }

        public static ArmorSetIndex Create(ArmorSetCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var entriesByItemId = new Dictionary<int, List<ArmorSetCatalogEntry>>();

            foreach (ArmorSetCatalogEntry entry in catalog.Entries)
            {
                var relatedItemIds = new HashSet<int>();

                foreach (ArmorSetVariant variant in entry.Variants)
                {
                    AddItemId(relatedItemIds, variant.HeadItemId);
                    AddItemId(relatedItemIds, variant.BodyItemId);
                    AddItemId(relatedItemIds, variant.LegItemId);
                }

                foreach (int itemId in relatedItemIds)
                {
                    if (!entriesByItemId.TryGetValue(itemId, out List<ArmorSetCatalogEntry> entries))
                    {
                        entries = new List<ArmorSetCatalogEntry>();
                        entriesByItemId.Add(itemId, entries);
                    }

                    entries.Add(entry);
                }
            }

            var frozen = new Dictionary<int, IReadOnlyList<ArmorSetCatalogEntry>>(entriesByItemId.Count);

            foreach (KeyValuePair<int, List<ArmorSetCatalogEntry>> pair in entriesByItemId)
            {
                frozen.Add(
                    pair.Key,
                    new ReadOnlyCollection<ArmorSetCatalogEntry>(new List<ArmorSetCatalogEntry>(pair.Value)));
            }

            return new ArmorSetIndex(frozen);
        }

        public IReadOnlyList<ArmorSetCatalogEntry> GetArmorSetsForItem(int itemId)
        {
            return _entriesByItemId.TryGetValue(itemId, out IReadOnlyList<ArmorSetCatalogEntry> entries)
                ? entries
                : _emptyEntries;
        }

        private static void AddItemId(HashSet<int> itemIds, int itemId)
        {
            if (itemId > 0)
                itemIds.Add(itemId);
        }
    }
}