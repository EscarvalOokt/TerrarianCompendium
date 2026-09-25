using System;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Localization;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class AnglerRewardPresentationTests
    {
        private static readonly int[] _allRequiredItemIds =
        [
            72, 73,
            2294,
            2354, 2355, 2356,
            2360, 2367, 2368, 2369,
            2373, 2374, 2375,
            2417, 2418, 2419, 2422, 2428,
            2442, 2443, 2444, 2445, 2446, 2447, 2448, 2449,
            2450, 2451,
            2490, 2494, 2495, 2496, 2497, 2498, 2499, 2500,
            2674, 2675, 2676,
            3031, 3032, 3036, 3037,
            3096,
            3120, 3123, 3124,
            3183,
            3721,
            4067,
            5064,
            5139, 5140, 5141, 5142, 5143, 5144, 5145, 5146,
            5235, 5252, 5256, 5259, 5263, 5264, 5265,
            5302, 5303,
            5358, 5359, 5360, 5361
        ];

        private static readonly CompendiumLocalization _localization = CompendiumLocalization.LoadFromAssembly(
            typeof(AnglerRewardPresentation).Assembly,
            CompendiumLocalization.SourceCultureName);

        [Test]
        public void Build_AtZeroProgress_MarksFirstMilestoneAsNextAndOthersAsFuture()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(itemCatalog, rewardCatalog, questsFinished: 0),
                rewardCatalog,
                _localization);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.Milestones.Count, Is.EqualTo(6));
                Assert.That(
                    presentation.Milestones.Select(milestone => milestone.CompletedQuests).ToArray(),
                    Is.EqualTo([5, 10, 15, 20, 25, 30]));
                Assert.That(
                    presentation.Milestones.Select(milestone => milestone.State).ToArray(),
                    Is.EqualTo(
                    [
                        AnglerMilestonePresentationState.Next,
                        AnglerMilestonePresentationState.Future,
                        AnglerMilestonePresentationState.Future,
                        AnglerMilestonePresentationState.Future,
                        AnglerMilestonePresentationState.Future,
                        AnglerMilestonePresentationState.Future
                    ]));
            });
        }

        [Test]
        public void Build_BetweenMilestones_UsesCompletedNextAndFutureStates()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(itemCatalog, rewardCatalog, questsFinished: 7),
                rewardCatalog,
                _localization);

            Assert.That(
                presentation.Milestones.Select(milestone => milestone.State).ToArray(),
                Is.EqualTo(
                [
                    AnglerMilestonePresentationState.Completed,
                    AnglerMilestonePresentationState.Next,
                    AnglerMilestonePresentationState.Future,
                    AnglerMilestonePresentationState.Future,
                    AnglerMilestonePresentationState.Future,
                    AnglerMilestonePresentationState.Future
                ]));
        }

        [Test]
        public void Build_AfterLastMilestone_MarksEveryMilestoneCompleted()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(itemCatalog, rewardCatalog, questsFinished: 30),
                rewardCatalog,
                _localization);

            Assert.That(
                presentation.Milestones.All(milestone => milestone.State == AnglerMilestonePresentationState.Completed),
                Is.True);
        }

        [Test]
        public void Build_OrdinaryQuest_UsesStableRewardGroupOrderAndCatalogItems()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(itemCatalog, rewardCatalog, questsFinished: 31),
                rewardCatalog,
                _localization);

            Assert.Multiple(() =>
            {
                Assert.That(
                    presentation.RewardGroups.Select(group => group.Kind).ToArray(),
                    Is.EqualTo(
                    [
                        AnglerRewardGroupKind.MainRewards,
                        AnglerRewardGroupKind.FishingGear,
                        AnglerRewardGroupKind.Potions,
                        AnglerRewardGroupKind.Decorations,
                        AnglerRewardGroupKind.Bait,
                        AnglerRewardGroupKind.Money
                    ]));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.MainRewards).ItemIds,
                    Is.EqualTo(FlattenRandomRewards(rewardCatalog)));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.FishingGear).ItemIds,
                    Is.EqualTo(rewardCatalog.MissingRewards.Select(reward => reward.ItemId).ToArray()));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.Potions).ItemIds,
                    Is.EqualTo(rewardCatalog.PotionFallback.ItemIds));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.Decorations).ItemIds,
                    Is.EqualTo(rewardCatalog.DecorationReward.ItemIds));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.Bait).ItemIds,
                    Is.EqualTo(
                    [
                        rewardCatalog.BaitReward.MasterBaitItemId,
                        rewardCatalog.BaitReward.JourneymanBaitItemId,
                        rewardCatalog.BaitReward.ApprenticeBaitItemId
                    ]));
                Assert.That(
                    GetGroup(presentation, AnglerRewardGroupKind.Money).ItemIds,
                    Is.EqualTo([rewardCatalog.MoneyReward.SilverCoinItemId, rewardCatalog.MoneyReward.GoldCoinItemId]));
            });
        }

        [Test]
        public void Build_BumblebeeTunaInHardmode_PrependsGuaranteedCurrentQuestBonus()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(
                    itemCatalog,
                    rewardCatalog,
                    questItemId: 2451,
                    questsFinished: 11,
                    isHardMode: true,
                    specialQuestReward: rewardCatalog.BumblebeeTunaReward,
                    specialQuestRewardGuaranteed: true),
                rewardCatalog,
                _localization);
            AnglerRewardGroupPresentation special = GetGroup(presentation, AnglerRewardGroupKind.CurrentQuestBonus);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.RewardGroups[0].Kind, Is.EqualTo(AnglerRewardGroupKind.CurrentQuestBonus));
                Assert.That(special.ItemIds, Is.EqualTo([5302, 5303]));
                Assert.That(special.InfoText, Does.Contain("Hardmode").And.Contain("guarantees"));
            });
        }

        [Test]
        public void Build_BumblebeeTunaBeforeHardmode_UsesPlayerFacingPercentage()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(
                    itemCatalog,
                    rewardCatalog,
                    questItemId: 2451,
                    questsFinished: 11,
                    specialQuestReward: rewardCatalog.BumblebeeTunaReward,
                    specialQuestRewardGuaranteed: false),
                rewardCatalog,
                _localization);
            AnglerRewardGroupPresentation special = GetGroup(presentation, AnglerRewardGroupKind.CurrentQuestBonus);

            Assert.Multiple(() =>
            {
                Assert.That(special.InfoText, Does.Contain("50%"));
                Assert.That(special.InfoText, Does.Not.Contain("denominator"));
            });
        }

        [Test]
        public void Build_RewardInfoTexts_AvoidInternalAlgorithmTerminology()
        {
            CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog);
            AnglerRewardPagePresentation presentation = AnglerRewardPresentation.Build(
                CreateProjection(itemCatalog, rewardCatalog, questsFinished: 31),
                rewardCatalog,
                _localization);

            var combined = string.Join("\n", presentation.RewardGroups.Select(group => group.InfoText));

            Assert.Multiple(() =>
            {
                Assert.That(combined, Does.Not.Contain("Next("));
                Assert.That(combined, Does.Not.Contain("denominator"));
                Assert.That(combined, Does.Not.Contain("ordered main-reward chain"));
                Assert.That(combined, Does.Not.Contain("rarity multiplier"));
            });
        }

        private static AnglerRewardGroupPresentation GetGroup(
            AnglerRewardPagePresentation presentation,
            AnglerRewardGroupKind kind)
        {
            return presentation.RewardGroups.Single(group => group.Kind == kind);
        }

        private static int[] FlattenRandomRewards(AnglerRewardCatalog rewardCatalog)
        {
            return rewardCatalog.RandomMainRewards.SelectMany(reward => reward.ItemIds).ToArray();
        }

        private static AnglerQuestProjection CreateProjection(
            ItemCatalog itemCatalog,
            AnglerRewardCatalog rewardCatalog,
            int questItemId = 2450,
            bool finishedToday = false,
            int questsFinished = 0,
            bool isHardMode = false,
            bool isExpertMode = false,
            AnglerSpecialQuestReward specialQuestReward = null,
            bool specialQuestRewardGuaranteed = false)
        {
            Assert.That(itemCatalog.TryGet(questItemId, out ItemCatalogEntry questItem), Is.True);
            int nextCompletionCount = questsFinished + 1;

            return new AnglerQuestProjection(
                questItem,
                finishedToday,
                questsFinished,
                nextCompletionCount,
                isHardMode,
                isExpertMode,
                AnglerRewardCatalog.CalculateBaseRarityMultiplier(nextCompletionCount),
                rewardCatalog.GetNextMilestone(questsFinished),
                rewardCatalog.GetMilestone(nextCompletionCount),
                specialQuestReward,
                specialQuestRewardGuaranteed,
                Array.Empty<AnglerRandomMainRewardRule>(),
                Array.Empty<int>(),
                missingRewardPoolIsComplete: false,
                potionFallbackReachable: false);
        }

        private static void CreateContext(out ItemCatalog itemCatalog, out AnglerRewardCatalog rewardCatalog)
        {
            itemCatalog = ItemCatalog.Create(
                _allRequiredItemIds.Distinct().Select(itemId => new ItemCatalogEntry(itemId, $"Item {itemId}")));
            rewardCatalog = AnglerRewardCatalog.Create(itemCatalog);
        }
    }
}