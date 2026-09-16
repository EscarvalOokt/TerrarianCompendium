using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcCatalog
    {
        private readonly IReadOnlyList<NpcCatalogEntry> _entries;
        private readonly Dictionary<int, NpcCatalogEntry> _entriesByNetId;

        private NpcCatalog(IReadOnlyList<NpcCatalogEntry> entries, Dictionary<int, NpcCatalogEntry> entriesByNetId)
        {
            _entries = entries;
            _entriesByNetId = entriesByNetId;
        }

        public IReadOnlyList<NpcCatalogEntry> Entries => _entries;

        public int Count => _entries.Count;

        public static NpcCatalog Create(IEnumerable<NpcCatalogEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var orderedEntries = new List<NpcCatalogEntry>();
            var entriesByNetId = new Dictionary<int, NpcCatalogEntry>();
            var usedBestiaryOrders = new HashSet<int>();

            foreach (NpcCatalogEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries must not contain null values.", nameof(entries));

                if (entriesByNetId.ContainsKey(entry.NetId))
                {
                    throw new ArgumentException(
                        $"Catalog contains duplicate NPC net ID {entry.NetId}.",
                        nameof(entries));
                }

                if (!usedBestiaryOrders.Add(entry.BestiaryOrder))
                {
                    throw new ArgumentException(
                        $"Catalog contains duplicate Bestiary order {entry.BestiaryOrder}.",
                        nameof(entries));
                }

                orderedEntries.Add(entry);
                entriesByNetId.Add(entry.NetId, entry);
            }

            orderedEntries.Sort(CompareByBestiaryOrder);

            return new NpcCatalog(new ReadOnlyCollection<NpcCatalogEntry>(orderedEntries), entriesByNetId);
        }

        public bool Contains(int netId)
        {
            return _entriesByNetId.ContainsKey(netId);
        }

        public bool TryGet(int netId, out NpcCatalogEntry entry)
        {
            return _entriesByNetId.TryGetValue(netId, out entry);
        }

        private static int CompareByBestiaryOrder(NpcCatalogEntry left, NpcCatalogEntry right)
        {
            return left.BestiaryOrder.CompareTo(right.BestiaryOrder);
        }
    }
}