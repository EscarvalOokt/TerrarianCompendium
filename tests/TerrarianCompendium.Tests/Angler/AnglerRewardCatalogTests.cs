using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Angler
{
    [TestFixture]
    public sealed class AnglerRewardCatalogTests
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
            2451,
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

        [Test]
        public void Create_WithNullItemCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => AnglerRewardCatalog.Create(null));
        }

        [Test]
        public void Create_WhenReferencedItemIsMissing_Throws()
        {
            ItemCatalog catalog = CreateCatalog(_allRequiredItemIds.Where(itemId => itemId != 2294));

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => AnglerRewardCatalog.Create(catalog))!;

            Assert.That(exception.Message, Does.Contain("2294"));
        }

        [Test]
        public void Create_PreservesMilestonesAndOrderedMainRewardRules()
        {
            AnglerRewardCatalog catalog = CreateRewardCatalog();

            Assert.That(
                catalog.Milestones.Select(reward => (reward.CompletedQuests, reward.ItemId)).ToArray(),
                Is.EqualTo(
                [
                    (5, 2428),
                    (10, 2367),
                    (15, 2368),
                    (20, 2369),
                    (25, 3031),
                    (30, 2294)
                ]));

            Assert.That(catalog.BumblebeeTunaReward.QuestItemId, Is.EqualTo(2451));
            Assert.That(catalog.BumblebeeTunaReward.RewardItemIds, Is.EqualTo([5302, 5303]));
            Assert.That(catalog.BumblebeeTunaReward.PreHardmodeTriggerDenominator, Is.EqualTo(2));
            Assert.That(catalog.BumblebeeTunaReward.RewardSelectionDenominator, Is.EqualTo(2));

            Assert.That(catalog.RandomMainRewards.Count, Is.EqualTo(10));
            AssertRule(catalog.RandomMainRewards[0], [2294], 250, 75, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[1], [2422], 100, 25, requiresHardMode: true);
            AssertRule(catalog.RandomMainRewards[2], [2494], 70, 10, requiresHardMode: true);
            AssertRule(catalog.RandomMainRewards[3], [3031], 70, 10, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[4], [3032], 70, 10, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[5], [3183], 80, -1, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[6], [2360], 60, -1, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[7], [4067], 60, -1, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[8], [2417, 2418, 2419], 80, -1, requiresHardMode: false);
            AssertRule(catalog.RandomMainRewards[9], [2498, 2499, 2500], 80, -1, requiresHardMode: false);
        }

        [Test]
        public void Create_PreservesMissingRewardPoolAndEquivalentOwnership()
        {
            AnglerRewardCatalog catalog = CreateRewardCatalog();

            Assert.That(
                catalog.MissingRewards.Select(reward => reward.ItemId).ToArray(),
                Is.EqualTo([2373, 2374, 2375, 3120, 3037, 3096, 5139]));

            AnglerRewardOwnership tackleBagFlags = AnglerRewardOwnership.HighTestFishingLine |
                                                   AnglerRewardOwnership.AnglerEarring |
                                                   AnglerRewardOwnership.TackleBox;
            AnglerRewardOwnership fishFinderFlags = AnglerRewardOwnership.FishermansGuide |
                                                    AnglerRewardOwnership.WeatherRadio |
                                                    AnglerRewardOwnership.Sextant;

            Assert.Multiple(() =>
            {
                Assert.That(
                    catalog.GetOwnedMissingRewardFlagsForItem(2373),
                    Is.EqualTo(AnglerRewardOwnership.HighTestFishingLine));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(3721), Is.EqualTo(tackleBagFlags));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(5064), Is.EqualTo(tackleBagFlags));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(3036), Is.EqualTo(fishFinderFlags));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(3123), Is.EqualTo(fishFinderFlags));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(5358), Is.EqualTo(fishFinderFlags));
                Assert.That(
                    catalog.GetOwnedMissingRewardFlagsForItem(5140),
                    Is.EqualTo(AnglerRewardOwnership.FishingBobber));
                Assert.That(
                    catalog.GetOwnedMissingRewardFlagsForItem(5146),
                    Is.EqualTo(AnglerRewardOwnership.FishingBobber));
                Assert.That(catalog.GetOwnedMissingRewardFlagsForItem(1), Is.EqualTo(AnglerRewardOwnership.None));
            });
        }

        [Test]
        public void Create_PreservesFallbackDecorationBaitMoneyAndMissingRollMetadata()
        {
            AnglerRewardCatalog catalog = CreateRewardCatalog();

            Assert.Multiple(() =>
            {
                Assert.That(catalog.MissingRewardRollDenominatorFactors, Is.EqualTo([40, 40, 40, 30, 30, 30, 25]));
                Assert.That(catalog.MissingRewardChanceScale, Is.EqualTo(0.8f));

                Assert.That(catalog.PotionFallback.ItemIds, Is.EqualTo([2354, 2355, 2356]));
                Assert.That(catalog.PotionFallback.MinimumStack, Is.EqualTo(2));
                Assert.That(catalog.PotionFallback.MaximumStack, Is.EqualTo(5));

                Assert.That(
                    catalog.DecorationReward.ItemIds,
                    Is.EqualTo(
                    [
                        2442, 2443, 2444, 2445, 2497, 2495, 2446, 2447, 2448, 2449,
                        2490, 2496, 5235, 5252, 5256, 5259, 5263, 5264, 5265
                    ]));
                Assert.That(catalog.DecorationReward.GuaranteedAtCompletedQuests, Is.EqualTo(100));

                Assert.That(catalog.BaitReward.StageRollDenominatorFactor, Is.EqualTo(100));
                Assert.That(catalog.BaitReward.StageRollInclusiveThreshold, Is.EqualTo(50));
                Assert.That(catalog.BaitReward.MasterBaitItemId, Is.EqualTo(2676));
                Assert.That(catalog.BaitReward.MasterBaitRollDenominatorFactor, Is.EqualTo(15));
                Assert.That(catalog.BaitReward.JourneymanBaitItemId, Is.EqualTo(2675));
                Assert.That(catalog.BaitReward.JourneymanBaitRollDenominatorFactor, Is.EqualTo(5));
                Assert.That(catalog.BaitReward.ApprenticeBaitItemId, Is.EqualTo(2674));
                Assert.That(catalog.BaitReward.AdditionalStackThresholds, Is.EqualTo([25, 50, 100, 150, 200, 250]));

                Assert.That(catalog.MoneyReward.QuestCountOffset, Is.EqualTo(50));
                Assert.That(catalog.MoneyReward.QuestCountDivisor, Is.EqualTo(2));
                Assert.That(catalog.MoneyReward.RandomMultiplierMinimumInclusive, Is.EqualTo(50));
                Assert.That(catalog.MoneyReward.RandomMultiplierMaximumExclusive, Is.EqualTo(201));
                Assert.That(catalog.MoneyReward.RandomMultiplierScale, Is.EqualTo(0.015f));
                Assert.That(catalog.MoneyReward.FinalScale, Is.EqualTo(1.5f));
                Assert.That(catalog.MoneyReward.HardModeMultiplier, Is.EqualTo(2));
                Assert.That(catalog.MoneyReward.ExpertModeMultiplier, Is.EqualTo(2));
                Assert.That(catalog.MoneyReward.GoldCoinThresholdExclusive, Is.EqualTo(100));
                Assert.That(catalog.MoneyReward.GoldCoinItemId, Is.EqualTo(73));
                Assert.That(catalog.MoneyReward.GoldCoinMaximumStack, Is.EqualTo(10));
                Assert.That(catalog.MoneyReward.SilverCoinItemId, Is.EqualTo(72));
                Assert.That(catalog.MoneyReward.SilverCoinMaximumStack, Is.EqualTo(99));
            });
        }

        [TestCase(0, 0.9f)]
        [TestCase(50, 0.45f)]
        [TestCase(100, 0.225f)]
        [TestCase(150, 0.135f)]
        [TestCase(151, 0.135f)]
        public void CalculateBaseRarityMultiplier_UsesTargetPiecewiseFormula(int questsDone, float expected)
        {
            Assert.That(
                AnglerRewardCatalog.CalculateBaseRarityMultiplier(questsDone),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void CalculateAdjustedRarityMultiplier_AppliesShoppingAdjustmentAfterBaseMultiplier()
        {
            float expected = AnglerRewardCatalog.CalculateBaseRarityMultiplier(25) * 0.9f;

            Assert.That(
                AnglerRewardCatalog.CalculateAdjustedRarityMultiplier(25, 0.8f),
                Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ExposedCollections_AreReadOnly()
        {
            AnglerRewardCatalog catalog = CreateRewardCatalog();

            Assert.Multiple(() =>
            {
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<AnglerMilestoneReward>)catalog.Milestones).Add(new AnglerMilestoneReward(40, 2294)));
                Assert.Throws<NotSupportedException>(() => ((IList<int>)catalog.PotionFallback.ItemIds).Add(2294));
                Assert.Throws<NotSupportedException>(() =>
                    ((IList<int>)catalog.MissingRewardRollDenominatorFactors).Add(10));
            });
        }

        private static void AssertRule(
            AnglerRandomMainRewardRule rule,
            int[] itemIds,
            int denominatorFactor,
            int minimumCompletedQuestsExclusive,
            bool requiresHardMode)
        {
            Assert.Multiple(() =>
            {
                Assert.That(rule.ItemIds, Is.EqualTo(itemIds));
                Assert.That(rule.RollDenominatorFactor, Is.EqualTo(denominatorFactor));
                Assert.That(rule.MinimumCompletedQuestsExclusive, Is.EqualTo(minimumCompletedQuestsExclusive));
                Assert.That(rule.RequiresHardMode, Is.EqualTo(requiresHardMode));
            });
        }

        private static AnglerRewardCatalog CreateRewardCatalog()
        {
            return AnglerRewardCatalog.Create(CreateCatalog(_allRequiredItemIds));
        }

        private static ItemCatalog CreateCatalog(IEnumerable<int> itemIds)
        {
            return ItemCatalog.Create(itemIds.Select(itemId => new ItemCatalogEntry(itemId, $"Item {itemId}")));
        }
    }
}