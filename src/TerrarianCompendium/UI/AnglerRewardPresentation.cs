using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.UI
{
    internal enum AnglerMilestonePresentationState
    {
        Completed,
        Next,
        Future
    }

    internal sealed class AnglerMilestonePresentation(
        int completedQuests,
        int itemId,
        AnglerMilestonePresentationState state)
    {
        public int CompletedQuests { get; } = completedQuests;

        public int ItemId { get; } = itemId;

        public AnglerMilestonePresentationState State { get; } = state;
    }

    internal enum AnglerRewardGroupKind
    {
        CurrentQuestBonus,
        MainRewards,
        FishingGear,
        Potions,
        Decorations,
        Bait,
        Money
    }

    internal sealed class AnglerRewardGroupPresentation
    {
        public AnglerRewardGroupPresentation(
            AnglerRewardGroupKind kind,
            string title,
            string infoText,
            IEnumerable<int> itemIds)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Reward group title must not be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(infoText))
                throw new ArgumentException("Reward group info text must not be empty.", nameof(infoText));
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));

            var normalizedItemIds = new List<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(itemIds),
                        itemId,
                        "Reward Item IDs must be greater than zero.");
                }

                normalizedItemIds.Add(itemId);
            }

            if (normalizedItemIds.Count == 0)
                throw new ArgumentException("Reward groups must contain at least one Item.", nameof(itemIds));

            Kind = kind;
            Title = title;
            InfoText = infoText;
            ItemIds = new ReadOnlyCollection<int>(normalizedItemIds);
        }

        public AnglerRewardGroupKind Kind { get; }

        public string Title { get; }

        public string InfoText { get; }

        public IReadOnlyList<int> ItemIds { get; }
    }

    internal sealed class AnglerRewardPagePresentation
    {
        public AnglerRewardPagePresentation(
            IEnumerable<AnglerMilestonePresentation> milestones,
            IEnumerable<AnglerRewardGroupPresentation> rewardGroups)
        {
            if (milestones == null)
                throw new ArgumentNullException(nameof(milestones));
            if (rewardGroups == null)
                throw new ArgumentNullException(nameof(rewardGroups));

            Milestones = CreateReadOnlyList(milestones, nameof(milestones));
            RewardGroups = CreateReadOnlyList(rewardGroups, nameof(rewardGroups));
        }

        public IReadOnlyList<AnglerMilestonePresentation> Milestones { get; }

        public IReadOnlyList<AnglerRewardGroupPresentation> RewardGroups { get; }

        private static IReadOnlyList<T> CreateReadOnlyList<T>(IEnumerable<T> source, string parameterName)
            where T : class
        {
            var result = new List<T>();

            foreach (T value in source)
                result.Add(
                    value ??
                    throw new ArgumentException(
                        "Presentation collections must not contain null values.",
                        parameterName));

            return new ReadOnlyCollection<T>(result);
        }
    }

    internal static class AnglerRewardPresentation
    {
        public static AnglerRewardPagePresentation Build(
            AnglerQuestProjection projection,
            AnglerRewardCatalog rewardCatalog,
            CompendiumLocalization localization)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));
            if (rewardCatalog == null)
                throw new ArgumentNullException(nameof(rewardCatalog));
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            var milestones = new List<AnglerMilestonePresentation>(rewardCatalog.Milestones.Count);

            foreach (AnglerMilestoneReward milestone in rewardCatalog.Milestones)
            {
                AnglerMilestonePresentationState state;

                if (projection.QuestsFinished >= milestone.CompletedQuests)
                    state = AnglerMilestonePresentationState.Completed;
                else if (projection.NextMilestone?.CompletedQuests == milestone.CompletedQuests)
                    state = AnglerMilestonePresentationState.Next;
                else
                    state = AnglerMilestonePresentationState.Future;

                milestones.Add(new AnglerMilestonePresentation(milestone.CompletedQuests, milestone.ItemId, state));
            }

            var groups = new List<AnglerRewardGroupPresentation>();

            if (projection.SpecialQuestReward != null)
            {
                string specialInfo = projection.SpecialQuestRewardGuaranteed
                    ? localization.Get(CompendiumTextKeys.Angler.CurrentQuestBonusGuaranteedInfo)
                    : localization.Format(
                        CompendiumTextKeys.Angler.CurrentQuestBonusConditionalInfo,
                        100d / projection.SpecialQuestReward.PreHardmodeTriggerDenominator);

                groups.Add(
                    new AnglerRewardGroupPresentation(
                        AnglerRewardGroupKind.CurrentQuestBonus,
                        localization.Get(CompendiumTextKeys.Angler.CurrentQuestBonus),
                        specialInfo,
                        projection.SpecialQuestReward.RewardItemIds));
            }

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.MainRewards,
                    localization.Get(CompendiumTextKeys.Angler.MainRewards),
                    localization.Get(CompendiumTextKeys.Angler.MainRewardsInfo),
                    FlattenRandomMainRewards(rewardCatalog.RandomMainRewards)));

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.FishingGear,
                    localization.Get(CompendiumTextKeys.Angler.FishingGear),
                    localization.Get(CompendiumTextKeys.Angler.FishingGearInfo),
                    GetMissingRewardItemIds(rewardCatalog.MissingRewards)));

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.Potions,
                    localization.Get(CompendiumTextKeys.Angler.Potions),
                    localization.Format(
                        CompendiumTextKeys.Angler.PotionsInfo,
                        rewardCatalog.PotionFallback.MinimumStack,
                        rewardCatalog.PotionFallback.MaximumStack),
                    rewardCatalog.PotionFallback.ItemIds));

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.Decorations,
                    localization.Get(CompendiumTextKeys.Angler.Decorations),
                    localization.Format(
                        CompendiumTextKeys.Angler.DecorationsInfo,
                        rewardCatalog.DecorationReward.GuaranteedAtCompletedQuests),
                    rewardCatalog.DecorationReward.ItemIds));

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.Bait,
                    localization.Get(CompendiumTextKeys.Angler.Bait),
                    localization.Get(CompendiumTextKeys.Angler.BaitInfo),
                    [
                        rewardCatalog.BaitReward.MasterBaitItemId,
                        rewardCatalog.BaitReward.JourneymanBaitItemId,
                        rewardCatalog.BaitReward.ApprenticeBaitItemId
                    ]));

            groups.Add(
                new AnglerRewardGroupPresentation(
                    AnglerRewardGroupKind.Money,
                    localization.Get(CompendiumTextKeys.Angler.Money),
                    localization.Get(CompendiumTextKeys.Angler.MoneyInfo),
                    [
                        rewardCatalog.MoneyReward.SilverCoinItemId,
                        rewardCatalog.MoneyReward.GoldCoinItemId
                    ]));

            return new AnglerRewardPagePresentation(milestones, groups);
        }

        private static IReadOnlyList<int> FlattenRandomMainRewards(IReadOnlyList<AnglerRandomMainRewardRule> rewards)
        {
            var result = new List<int>();

            foreach (AnglerRandomMainRewardRule reward in rewards)
            {
                foreach (int itemId in reward.ItemIds)
                    result.Add(itemId);
            }

            return new ReadOnlyCollection<int>(result);
        }

        private static IReadOnlyList<int> GetMissingRewardItemIds(IReadOnlyList<AnglerMissingRewardDefinition> rewards)
        {
            var result = new List<int>(rewards.Count);

            foreach (AnglerMissingRewardDefinition reward in rewards)
                result.Add(reward.ItemId);

            return new ReadOnlyCollection<int>(result);
        }
    }
}