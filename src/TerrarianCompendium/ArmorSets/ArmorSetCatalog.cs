using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.ArmorSets
{
    internal sealed class ArmorSetCatalog
    {
        private readonly IReadOnlyList<ArmorSetCatalogEntry> _entries;
        private readonly Dictionary<int, ArmorSetCatalogEntry> _entriesById;

        private ArmorSetCatalog(
            IReadOnlyList<ArmorSetCatalogEntry> entries,
            Dictionary<int, ArmorSetCatalogEntry> entriesById)
        {
            _entries = entries;
            _entriesById = entriesById;
        }

        public IReadOnlyList<ArmorSetCatalogEntry> Entries => _entries;

        public int Count => _entries.Count;

        public static ArmorSetCatalog Create(IEnumerable<ArmorSetCatalogEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var orderedEntries = new List<ArmorSetCatalogEntry>();
            var entriesById = new Dictionary<int, ArmorSetCatalogEntry>();

            foreach (ArmorSetCatalogEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries must not contain null values.", nameof(entries));

                if (entriesById.ContainsKey(entry.Id))
                {
                    throw new ArgumentException(
                        $"Catalog contains duplicate armor-set ID {entry.Id}.",
                        nameof(entries));
                }

                orderedEntries.Add(entry);
                entriesById.Add(entry.Id, entry);
            }

            orderedEntries.Sort((left, right) => left.Id.CompareTo(right.Id));

            return new ArmorSetCatalog(new ReadOnlyCollection<ArmorSetCatalogEntry>(orderedEntries), entriesById);
        }

        public bool Contains(int armorSetId)
        {
            return _entriesById.ContainsKey(armorSetId);
        }

        public bool TryGet(int armorSetId, out ArmorSetCatalogEntry entry)
        {
            return _entriesById.TryGetValue(armorSetId, out entry);
        }
    }
}