using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Acquisition
{
    internal readonly struct OpenableItemLootRelation(int sourceItemId, int targetItemId)
    {
        public int SourceItemId { get; } = sourceItemId;

        public int TargetItemId { get; } = targetItemId;
    }

    internal sealed class OpenableItemLootIndex
    {
        private static readonly IReadOnlyList<int> _emptyItemIds = Array.Empty<int>();
        private readonly Dictionary<int, IReadOnlyList<int>> _contentsBySourceItemId;
        private readonly Dictionary<int, IReadOnlyList<int>> _sourcesByTargetItemId;

        public OpenableItemLootIndex(IEnumerable<OpenableItemLootRelation> relations)
        {
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            var sourcesByTarget = new Dictionary<int, HashSet<int>>();
            var contentsBySource = new Dictionary<int, HashSet<int>>();

            foreach (OpenableItemLootRelation relation in relations)
            {
                if (relation.SourceItemId <= 0 || relation.TargetItemId <= 0)
                    continue;

                AddRelation(sourcesByTarget, relation.TargetItemId, relation.SourceItemId);
                AddRelation(contentsBySource, relation.SourceItemId, relation.TargetItemId);
            }

            _sourcesByTargetItemId = FreezeRelations(sourcesByTarget);
            _contentsBySourceItemId = FreezeRelations(contentsBySource);
        }

        public IReadOnlyList<int> GetSourcesForItem(int itemId)
        {
            return itemId > 0 && _sourcesByTargetItemId.TryGetValue(itemId, out IReadOnlyList<int> sources)
                ? sources
                : _emptyItemIds;
        }

        public IReadOnlyList<int> GetContentsForItem(int itemId)
        {
            return itemId > 0 && _contentsBySourceItemId.TryGetValue(itemId, out IReadOnlyList<int> contents)
                ? contents
                : _emptyItemIds;
        }

        private static void AddRelation(Dictionary<int, HashSet<int>> relations, int key, int value)
        {
            if (!relations.TryGetValue(key, out HashSet<int> values))
            {
                values = new HashSet<int>();
                relations.Add(key, values);
            }

            values.Add(value);
        }

        private static Dictionary<int, IReadOnlyList<int>> FreezeRelations(Dictionary<int, HashSet<int>> mutable)
        {
            var frozen = new Dictionary<int, IReadOnlyList<int>>(mutable.Count);

            foreach (KeyValuePair<int, HashSet<int>> pair in mutable)
            {
                var ordered = new List<int>(pair.Value);
                ordered.Sort();
                frozen.Add(pair.Key, new ReadOnlyCollection<int>(ordered));
            }

            return frozen;
        }
    }
}