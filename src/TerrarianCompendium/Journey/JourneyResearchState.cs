using System;
using System.Collections.Generic;

namespace TerrarianCompendium.Journey
{
    internal readonly struct JourneyResearchDefinition
    {
        public JourneyResearchDefinition(
            int itemId,
            int canonicalItemId,
            int amountNeeded,
            bool hasSharedResearchIdentity)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            if (canonicalItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(canonicalItemId),
                    canonicalItemId,
                    "Canonical research item ID must be greater than zero.");
            }

            if (amountNeeded <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amountNeeded),
                    amountNeeded,
                    "Required research amount must be greater than zero.");
            }

            ItemId = itemId;
            CanonicalItemId = canonicalItemId;
            AmountNeeded = amountNeeded;
            HasSharedResearchIdentity = hasSharedResearchIdentity;
        }

        public int ItemId { get; }

        public int CanonicalItemId { get; }

        public int AmountNeeded { get; }

        public bool HasSharedResearchIdentity { get; }
    }

    internal sealed class JourneyResearchState
    {
        private readonly HashSet<int> _canonicalItemIds = new();
        private readonly Dictionary<int, JourneyResearchDefinition> _definitionsByItemId = new();
        private readonly Dictionary<int, int> _progressByCanonicalItemId = new();
        private long _revision;

        public JourneyResearchState(IEnumerable<JourneyResearchDefinition> definitions)
        {
            if (definitions == null)
                throw new ArgumentNullException(nameof(definitions));

            foreach (JourneyResearchDefinition definition in definitions)
            {
                if (_definitionsByItemId.ContainsKey(definition.ItemId))
                {
                    throw new ArgumentException(
                        $"Research definitions contain duplicate item ID {definition.ItemId}.",
                        nameof(definitions));
                }

                _definitionsByItemId.Add(definition.ItemId, definition);
                _canonicalItemIds.Add(definition.CanonicalItemId);
            }
        }

        public long Revision => _revision;

        public bool IsResearchable(int itemId)
        {
            return _definitionsByItemId.ContainsKey(itemId);
        }

        public bool IsFullyResearched(int itemId)
        {
            if (!_definitionsByItemId.TryGetValue(itemId, out JourneyResearchDefinition definition))
                return false;

            return GetProgress(definition.CanonicalItemId) >= definition.AmountNeeded;
        }

        public bool IsUnresearched(int itemId)
        {
            if (!_definitionsByItemId.TryGetValue(itemId, out JourneyResearchDefinition definition))
                return false;

            return GetProgress(definition.CanonicalItemId) < definition.AmountNeeded;
        }

        public bool ContainsCanonicalItemId(int canonicalItemId)
        {
            return _canonicalItemIds.Contains(canonicalItemId);
        }

        public bool ReplaceProgressSnapshot(IEnumerable<KeyValuePair<int, int>> progressByCanonicalItemId)
        {
            if (progressByCanonicalItemId == null)
                throw new ArgumentNullException(nameof(progressByCanonicalItemId));

            var normalizedProgress = new Dictionary<int, int>();

            foreach (KeyValuePair<int, int> pair in progressByCanonicalItemId)
            {
                if (pair.Value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(progressByCanonicalItemId),
                        pair.Value,
                        "Research progress must not be negative.");
                }

                if (pair.Value == 0 || !_canonicalItemIds.Contains(pair.Key))
                    continue;

                normalizedProgress[pair.Key] = pair.Value;
            }

            if (HasSameProgress(normalizedProgress))
                return false;

            _progressByCanonicalItemId.Clear();

            foreach (KeyValuePair<int, int> pair in normalizedProgress)
                _progressByCanonicalItemId.Add(pair.Key, pair.Value);

            _revision++;

            return true;
        }

        public IReadOnlyList<KeyValuePair<int, int>> CreateProgressSnapshot()
        {
            var progress = new List<KeyValuePair<int, int>>(_progressByCanonicalItemId);
            progress.Sort((left, right) => left.Key.CompareTo(right.Key));

            return progress.AsReadOnly();
        }

        public IReadOnlyList<int> CreateFoundCandidateItemIdSnapshot()
        {
            var itemIds = new List<int>();

            foreach (JourneyResearchDefinition definition in _definitionsByItemId.Values)
            {
                if (definition.HasSharedResearchIdentity)
                    continue;

                if (GetProgress(definition.CanonicalItemId) <= 0)
                    continue;

                itemIds.Add(definition.ItemId);
            }

            itemIds.Sort();

            return itemIds.AsReadOnly();
        }

        private int GetProgress(int canonicalItemId)
        {
            return _progressByCanonicalItemId.TryGetValue(canonicalItemId, out int progress) ? progress : 0;
        }

        private bool HasSameProgress(Dictionary<int, int> progress)
        {
            if (_progressByCanonicalItemId.Count != progress.Count)
                return false;

            foreach (KeyValuePair<int, int> pair in progress)
            {
                if (!_progressByCanonicalItemId.TryGetValue(pair.Key, out int currentProgress) ||
                    currentProgress != pair.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}