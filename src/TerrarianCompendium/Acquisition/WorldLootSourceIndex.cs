using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Acquisition
{
    internal enum WorldLootSourceKind
    {
        Chest,
        Pot,
        TreeShaking
    }

    internal sealed class WorldLootSource(
        string key,
        string displayNameKey,
        WorldLootSourceKind kind,
        int sortOrder,
        int? representativeItemId)
    {
        public string Key { get; } = string.IsNullOrWhiteSpace(key)
            ? throw new ArgumentException("World loot source key must not be empty.", nameof(key))
            : key;

        public string DisplayNameKey { get; } = string.IsNullOrWhiteSpace(displayNameKey)
            ? throw new ArgumentException(
                "World loot source display-name key must not be empty.",
                nameof(displayNameKey))
            : displayNameKey;

        public WorldLootSourceKind Kind { get; } = kind;

        public int SortOrder { get; } = sortOrder;

        public int? RepresentativeItemId { get; } = representativeItemId;
    }

    internal readonly struct WorldLootRelation(WorldLootSource source, int targetItemId)
    {
        public WorldLootSource Source { get; } = source ?? throw new ArgumentNullException(nameof(source));

        public int TargetItemId { get; } = targetItemId;
    }

    internal sealed class WorldLootSourceIndex
    {
        private static readonly IReadOnlyList<WorldLootSource> _emptySources = Array.Empty<WorldLootSource>();
        private readonly Dictionary<int, IReadOnlyList<WorldLootSource>> _sourcesByTargetItemId;

        public WorldLootSourceIndex(IEnumerable<WorldLootRelation> relations)
        {
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            var mutable = new Dictionary<int, Dictionary<string, WorldLootSource>>();

            foreach (WorldLootRelation relation in relations)
            {
                if (relation.TargetItemId <= 0)
                    continue;

                if (!mutable.TryGetValue(relation.TargetItemId, out Dictionary<string, WorldLootSource> sources))
                {
                    sources = new Dictionary<string, WorldLootSource>(StringComparer.Ordinal);
                    mutable.Add(relation.TargetItemId, sources);
                }

                sources[relation.Source.Key] = relation.Source;
            }

            _sourcesByTargetItemId = new Dictionary<int, IReadOnlyList<WorldLootSource>>(mutable.Count);

            foreach (KeyValuePair<int, Dictionary<string, WorldLootSource>> pair in mutable)
            {
                var ordered = new List<WorldLootSource>(pair.Value.Values);
                ordered.Sort(CompareSources);
                _sourcesByTargetItemId.Add(pair.Key, new ReadOnlyCollection<WorldLootSource>(ordered));
            }
        }

        public IReadOnlyList<WorldLootSource> GetSourcesForItem(int itemId)
        {
            return itemId > 0 && _sourcesByTargetItemId.TryGetValue(itemId, out IReadOnlyList<WorldLootSource> sources)
                ? sources
                : _emptySources;
        }

        private static int CompareSources(WorldLootSource left, WorldLootSource right)
        {
            int orderComparison = left.SortOrder.CompareTo(right.SortOrder);

            return orderComparison != 0
                ? orderComparison
                : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
        }
    }
}