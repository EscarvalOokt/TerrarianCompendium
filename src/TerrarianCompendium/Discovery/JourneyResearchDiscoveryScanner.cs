using System;
using System.Collections.Generic;
using Terraria.GameContent.Creative;
using Terraria.ID;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Discovery
{
    internal sealed class JourneyResearchDiscoveryScanner
    {
        private readonly ChecklistState _checklistState;
        private readonly JourneyResearchState _researchState;
        private readonly ItemsSacrificedUnlocksTracker _tracker;
        private int _lastObservedEditId;

        public JourneyResearchDiscoveryScanner(
            ItemCatalog catalog,
            ChecklistState checklistState,
            ItemsSacrificedUnlocksTracker tracker)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));

            _researchState = CreateResearchState(catalog, tracker);
            _lastObservedEditId = tracker.LastEditId;

            SynchronizeFoundItems();
        }

        public JourneyResearchState ResearchState => _researchState;

        public void Update()
        {
            if (_tracker.LastEditId == _lastObservedEditId)
                return;

            RefreshProgress();
            _lastObservedEditId = _tracker.LastEditId;
            SynchronizeFoundItems();
        }

        public void SynchronizeFoundItems()
        {
            foreach (int itemId in _researchState.CreateFoundCandidateItemIdSnapshot())
                _checklistState.MarkFound(itemId);
        }

        private static JourneyResearchState CreateResearchState(
            ItemCatalog catalog,
            ItemsSacrificedUnlocksTracker tracker)
        {
            HashSet<int> sharedCanonicalItemIds = CreateSharedCanonicalItemIds();
            var definitions = new List<JourneyResearchDefinition>();
            var initialProgress = new Dictionary<int, int>();

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                if (!tracker.TryGetSacrificeNumbers(entry.Id, out int amountWeHave, out int amountNeeded))
                    continue;

                int canonicalItemId = GetCanonicalResearchItemId(entry.Id);
                bool hasSharedResearchIdentity = sharedCanonicalItemIds.Contains(canonicalItemId);

                definitions.Add(
                    new JourneyResearchDefinition(entry.Id, canonicalItemId, amountNeeded, hasSharedResearchIdentity));

                if (amountWeHave > 0)
                    initialProgress[canonicalItemId] = amountWeHave;
            }

            var researchState = new JourneyResearchState(definitions);
            researchState.ReplaceProgressSnapshot(initialProgress);

            return researchState;
        }

        private void RefreshProgress()
        {
            var progress = new Dictionary<int, int>();

            _tracker.ForEachItemWithResearchProgress(itemId =>
            {
                int canonicalItemId = GetCanonicalResearchItemId(itemId);

                if (!_researchState.ContainsCanonicalItemId(canonicalItemId))
                    return;

                int amount = _tracker.GetSacrificeCount(itemId);

                if (amount > 0)
                    progress[canonicalItemId] = amount;
            });

            _researchState.ReplaceProgressSnapshot(progress);
        }

        private static HashSet<int> CreateSharedCanonicalItemIds()
        {
            var result = new HashSet<int>();

            foreach (KeyValuePair<int, int> pair in ContentSamples.CreativeResearchItemPersistentIdOverride)
                result.Add(pair.Value);

            return result;
        }

        private static int GetCanonicalResearchItemId(int itemId)
        {
            return ContentSamples.CreativeResearchItemPersistentIdOverride.TryGetValue(itemId, out int canonicalItemId)
                ? canonicalItemId
                : itemId;
        }
    }
}