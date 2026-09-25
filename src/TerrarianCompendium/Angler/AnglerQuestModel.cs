using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Angler
{
    internal sealed class AnglerQuestProjection(
        ItemCatalogEntry questItem,
        bool finishedToday,
        int questsFinished,
        int nextCompletionCount,
        bool isHardMode,
        bool isExpertMode,
        float baseRarityMultiplier,
        AnglerMilestoneReward nextMilestone,
        AnglerMilestoneReward milestoneOnNextCompletion,
        AnglerSpecialQuestReward specialQuestReward,
        bool specialQuestRewardGuaranteed,
        IReadOnlyList<AnglerRandomMainRewardRule> applicableRandomMainRewards,
        IReadOnlyList<int> missingRewardItemIds,
        bool missingRewardPoolIsComplete,
        bool potionFallbackReachable)
    {
        public ItemCatalogEntry QuestItem { get; } = questItem ?? throw new ArgumentNullException(nameof(questItem));

        public bool FinishedToday { get; } = finishedToday;

        public int QuestsFinished { get; } = questsFinished;

        public int NextCompletionCount { get; } = nextCompletionCount;

        public bool IsHardMode { get; } = isHardMode;

        public bool IsExpertMode { get; } = isExpertMode;

        public float BaseRarityMultiplier { get; } = baseRarityMultiplier;

        public AnglerMilestoneReward NextMilestone { get; } = nextMilestone;

        public AnglerMilestoneReward MilestoneOnNextCompletion { get; } = milestoneOnNextCompletion;

        public AnglerSpecialQuestReward SpecialQuestReward { get; } = specialQuestReward;

        public bool SpecialQuestRewardGuaranteed { get; } = specialQuestRewardGuaranteed;

        public IReadOnlyList<AnglerRandomMainRewardRule> ApplicableRandomMainRewards { get; } =
            applicableRandomMainRewards ?? throw new ArgumentNullException(nameof(applicableRandomMainRewards));

        public IReadOnlyList<int> MissingRewardItemIds { get; } =
            missingRewardItemIds ?? throw new ArgumentNullException(nameof(missingRewardItemIds));

        public bool MissingRewardPoolIsComplete { get; } = missingRewardPoolIsComplete;

        public bool PotionFallbackReachable { get; } = potionFallbackReachable;
    }

    internal sealed class AnglerQuestModel(
        ItemCatalog itemCatalog,
        AnglerRewardCatalog rewardCatalog,
        AnglerQuestState questState)
    {
        private static readonly IReadOnlyList<AnglerRandomMainRewardRule> _emptyRandomRewards =
            new ReadOnlyCollection<AnglerRandomMainRewardRule>(new List<AnglerRandomMainRewardRule>());

        private static readonly IReadOnlyList<int> _emptyItemIds = new ReadOnlyCollection<int>(new List<int>());

        private readonly ItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

        private readonly AnglerQuestState _questState =
            questState ?? throw new ArgumentNullException(nameof(questState));

        private readonly AnglerRewardCatalog _rewardCatalog =
            rewardCatalog ?? throw new ArgumentNullException(nameof(rewardCatalog));

        private AnglerQuestProjection _cachedProjection;
        private long _cachedStateRevision = -1;

        public AnglerRewardCatalog RewardCatalog => _rewardCatalog;

        public long Revision => _questState.Revision;

        public bool TryGetProjection(out AnglerQuestProjection projection)
        {
            if (!_questState.TryGetSnapshot(out AnglerQuestSnapshot snapshot))
            {
                projection = null;
                return false;
            }

            long revision = _questState.Revision;

            if (_cachedProjection != null && _cachedStateRevision == revision)
            {
                projection = _cachedProjection;
                return true;
            }

            if (!_itemCatalog.TryGet(snapshot.QuestItemId, out ItemCatalogEntry questItem))
            {
                projection = null;
                return false;
            }

            _cachedProjection = BuildProjection(snapshot, questItem);
            _cachedStateRevision = revision;
            projection = _cachedProjection;
            return true;
        }

        private AnglerQuestProjection BuildProjection(AnglerQuestSnapshot snapshot, ItemCatalogEntry questItem)
        {
            int nextCompletionCount = checked(snapshot.QuestsFinished + 1);
            AnglerMilestoneReward nextMilestone = _rewardCatalog.GetNextMilestone(snapshot.QuestsFinished);
            AnglerMilestoneReward milestoneOnNextCompletion = _rewardCatalog.GetMilestone(nextCompletionCount);

            AnglerSpecialQuestReward specialQuestReward = null;
            var specialQuestRewardGuaranteed = false;

            if (milestoneOnNextCompletion == null &&
                snapshot.QuestItemId == _rewardCatalog.BumblebeeTunaReward.QuestItemId)
            {
                specialQuestReward = _rewardCatalog.BumblebeeTunaReward;
                specialQuestRewardGuaranteed = snapshot.IsHardMode;
            }

            bool randomMainRewardChainReachable = milestoneOnNextCompletion == null && !specialQuestRewardGuaranteed;

            IReadOnlyList<AnglerRandomMainRewardRule> applicableRandomMainRewards = randomMainRewardChainReachable
                ? GetApplicableRandomMainRewards(nextCompletionCount, snapshot.IsHardMode)
                : _emptyRandomRewards;

            IReadOnlyList<int> missingRewardItemIds = randomMainRewardChainReachable
                ? GetMissingRewardItemIds(snapshot.OwnedRewardComponents)
                : _emptyItemIds;

            bool missingRewardPoolIsComplete = randomMainRewardChainReachable && missingRewardItemIds.Count == 0;
            bool potionFallbackReachable = randomMainRewardChainReachable;

            return new AnglerQuestProjection(
                questItem,
                snapshot.FinishedToday,
                snapshot.QuestsFinished,
                nextCompletionCount,
                snapshot.IsHardMode,
                snapshot.IsExpertMode,
                AnglerRewardCatalog.CalculateBaseRarityMultiplier(nextCompletionCount),
                nextMilestone,
                milestoneOnNextCompletion,
                specialQuestReward,
                specialQuestRewardGuaranteed,
                applicableRandomMainRewards,
                missingRewardItemIds,
                missingRewardPoolIsComplete,
                potionFallbackReachable);
        }

        private IReadOnlyList<AnglerRandomMainRewardRule> GetApplicableRandomMainRewards(
            int completedQuests,
            bool isHardMode)
        {
            var result = new List<AnglerRandomMainRewardRule>();

            foreach (AnglerRandomMainRewardRule rule in _rewardCatalog.RandomMainRewards)
            {
                if (rule.IsEligible(completedQuests, isHardMode))
                    result.Add(rule);
            }

            return result.Count == 0 ? _emptyRandomRewards : new ReadOnlyCollection<AnglerRandomMainRewardRule>(result);
        }

        private IReadOnlyList<int> GetMissingRewardItemIds(AnglerRewardOwnership ownedRewardComponents)
        {
            var result = new List<int>();

            foreach (AnglerMissingRewardDefinition reward in _rewardCatalog.MissingRewards)
            {
                if ((ownedRewardComponents & reward.OwnershipFlag) == AnglerRewardOwnership.None)
                    result.Add(reward.ItemId);
            }

            return result.Count == 0 ? _emptyItemIds : new ReadOnlyCollection<int>(result);
        }
    }
}