using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Shimmer
{
    internal sealed class ShimmerTransformationCatalog
    {
        private readonly IReadOnlyList<ShimmerTransformationEntry> _entries;
        private readonly Dictionary<int, ShimmerTransformationEntry> _entriesByInputItemId;

        private ShimmerTransformationCatalog(
            IReadOnlyList<ShimmerTransformationEntry> entries,
            Dictionary<int, ShimmerTransformationEntry> entriesByInputItemId)
        {
            _entries = entries;
            _entriesByInputItemId = entriesByInputItemId;
        }

        public IReadOnlyList<ShimmerTransformationEntry> Entries => _entries;

        public int Count => _entries.Count;

        public static ShimmerTransformationCatalog Create(IEnumerable<ShimmerTransformationEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var orderedEntries = new List<ShimmerTransformationEntry>();
            var entriesByInputItemId = new Dictionary<int, ShimmerTransformationEntry>();

            foreach (ShimmerTransformationEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries must not contain null values.", nameof(entries));

                if (entriesByInputItemId.ContainsKey(entry.InputItemId))
                {
                    throw new ArgumentException(
                        $"Catalog contains duplicate Shimmer input item ID {entry.InputItemId}.",
                        nameof(entries));
                }

                orderedEntries.Add(entry);
                entriesByInputItemId.Add(entry.InputItemId, entry);
            }

            orderedEntries.Sort((left, right) => left.InputItemId.CompareTo(right.InputItemId));

            return new ShimmerTransformationCatalog(
                new ReadOnlyCollection<ShimmerTransformationEntry>(orderedEntries),
                entriesByInputItemId);
        }

        public bool Contains(int inputItemId)
        {
            return _entriesByInputItemId.ContainsKey(inputItemId);
        }

        public bool TryGet(int inputItemId, out ShimmerTransformationEntry entry)
        {
            return _entriesByInputItemId.TryGetValue(inputItemId, out entry);
        }
    }
}