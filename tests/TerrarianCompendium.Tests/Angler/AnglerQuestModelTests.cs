using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Angler;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Angler
{
    [TestFixture]
    public sealed class AnglerQuestModelTests
    {
        private static readonly int[] _rewardItemIds =
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
        public void TryGetProjection_WithoutSnapshot_ReturnsFalse()
        {
            CreateModel(out AnglerQuestModel model, out _, out _);

            Assert.That(model.TryGetProjection(out AnglerQuestProjection projection), Is.False);
            Assert.That(projection, Is.Null);
        }

        [Test]
        public void TryGetProjection_CurrentQuestMissingFromItemCatalog_ReturnsFalse()
        {
            ItemCatalog itemCatalog = CreateItemCatalog(_rewardItemIds);
            var rewardCatalog = AnglerRewardCatalog.Create(itemCatalog);
            var state = new AnglerQuestState();
            state.ReplaceSnapshot(new AnglerQuestSnapshot(9999, false, 0, false, false, AnglerRewardOwnership.None));
            var model = new AnglerQuestModel(itemCatalog, rewardCatalog, state);

            Assert.That(model.TryGetProjection(out _), Is.False);
        }

        [Test]
        public void Projection_UsesCurrentQuestProgressAndPostCompletionCount()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out ItemCatalog catalog);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, true, 4, false, true, AnglerRewardOwnership.None));
            int catalogCount = catalog.Count;
            long stateRevision = state.Revision;

            Assert.That(model.TryGetProjection(out AnglerQuestProjection projection), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(projection.QuestItem, Is.SameAs(GetItem(catalog, 2450)));
                Assert.That(projection.FinishedToday, Is.True);
                Assert.That(projection.QuestsFinished, Is.EqualTo(4));
                Assert.That(projection.NextCompletionCount, Is.EqualTo(5));
                Assert.That(projection.IsHardMode, Is.False);
                Assert.That(projection.IsExpertMode, Is.True);
                Assert.That(
                    projection.BaseRarityMultiplier,
                    Is.EqualTo(AnglerRewardCatalog.CalculateBaseRarityMultiplier(5)));
                Assert.That(catalog.Count, Is.EqualTo(catalogCount));
                Assert.That(state.Revision, Is.EqualTo(stateRevision));
            });
        }

        [Test]
        public void Projection_WhenNextCompletionIsMilestone_SuppressesOtherMainRewardBranches()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 4, false, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.NextMilestone.CompletedQuests, Is.EqualTo(5));
                Assert.That(projection.MilestoneOnNextCompletion.CompletedQuests, Is.EqualTo(5));
                Assert.That(projection.MilestoneOnNextCompletion.ItemId, Is.EqualTo(2428));
                Assert.That(projection.SpecialQuestReward, Is.Null);
                Assert.That(projection.ApplicableRandomMainRewards, Is.Empty);
                Assert.That(projection.MissingRewardItemIds, Is.Empty);
                Assert.That(projection.PotionFallbackReachable, Is.False);
            });
        }

        [Test]
        public void Projection_AfterMilestone_UsesNextFutureMilestoneAndEligibleRandomRules()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 5, false, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.NextMilestone.CompletedQuests, Is.EqualTo(10));
                Assert.That(projection.MilestoneOnNextCompletion, Is.Null);
                Assert.That(projection.ApplicableRandomMainRewards.Count, Is.EqualTo(5));
                Assert.That(
                    projection.ApplicableRandomMainRewards.Select(rule => rule.ItemIds[0]).ToArray(),
                    Is.EqualTo([3183, 2360, 4067, 2417, 2498]));
                Assert.That(projection.MissingRewardItemIds, Is.EqualTo([2373, 2374, 2375, 3120, 3037, 3096, 5139]));
                Assert.That(projection.MissingRewardPoolIsComplete, Is.False);
                Assert.That(projection.PotionFallbackReachable, Is.True);
            });
        }

        [Test]
        public void Projection_HardModeEnablesHardModeGatedRandomRuleAtPostCompletionCount()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 10, true, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.That(
                projection.ApplicableRandomMainRewards.Select(rule => rule.ItemIds[0]).ToArray(),
                Is.EqualTo([2494, 3031, 3032, 3183, 2360, 4067, 2417, 2498]));
        }

        [Test]
        public void Projection_BumblebeeTunaInHardMode_UsesGuaranteedSpecialBranch()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2451, false, 11, true, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.SpecialQuestReward, Is.SameAs(model.RewardCatalog.BumblebeeTunaReward));
                Assert.That(projection.SpecialQuestRewardGuaranteed, Is.True);
                Assert.That(projection.ApplicableRandomMainRewards, Is.Empty);
                Assert.That(projection.MissingRewardItemIds, Is.Empty);
                Assert.That(projection.PotionFallbackReachable, Is.False);
            });
        }

        [Test]
        public void Projection_BumblebeeTunaBeforeHardMode_PreservesRandomFallbackChain()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2451, false, 11, false, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.SpecialQuestReward, Is.SameAs(model.RewardCatalog.BumblebeeTunaReward));
                Assert.That(projection.SpecialQuestRewardGuaranteed, Is.False);
                Assert.That(projection.ApplicableRandomMainRewards, Is.Not.Empty);
                Assert.That(projection.MissingRewardItemIds, Is.Not.Empty);
                Assert.That(projection.PotionFallbackReachable, Is.True);
            });
        }

        [Test]
        public void Projection_MissingRewardPoolUsesOwnershipFlagsWithoutMutatingStaticCatalog()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            int[] staticPoolBefore = model.RewardCatalog.MissingRewards.Select(reward => reward.ItemId).ToArray();
            AnglerRewardOwnership owned = AnglerRewardOwnership.HighTestFishingLine |
                                          AnglerRewardOwnership.AnglerEarring |
                                          AnglerRewardOwnership.TackleBox |
                                          AnglerRewardOwnership.FishingBobber;
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 31, true, false, owned));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.MissingRewardItemIds, Is.EqualTo([3120, 3037, 3096]));
                Assert.That(projection.MissingRewardPoolIsComplete, Is.False);
                Assert.That(
                    model.RewardCatalog.MissingRewards.Select(reward => reward.ItemId).ToArray(),
                    Is.EqualTo(staticPoolBefore));
            });
        }

        [Test]
        public void Projection_WhenAllMissingRewardsAreOwned_MarksFullPoolFallback()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            AnglerRewardOwnership allOwned = AnglerRewardOwnership.HighTestFishingLine |
                                             AnglerRewardOwnership.AnglerEarring |
                                             AnglerRewardOwnership.TackleBox |
                                             AnglerRewardOwnership.FishermansGuide |
                                             AnglerRewardOwnership.WeatherRadio |
                                             AnglerRewardOwnership.Sextant |
                                             AnglerRewardOwnership.FishingBobber;
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 31, true, false, allOwned));

            model.TryGetProjection(out AnglerQuestProjection projection);

            Assert.Multiple(() =>
            {
                Assert.That(projection.MissingRewardItemIds, Is.Empty);
                Assert.That(projection.MissingRewardPoolIsComplete, Is.True);
                Assert.That(projection.PotionFallbackReachable, Is.True);
            });
        }

        [Test]
        public void Projection_CachesUntilStateRevisionChanges()
        {
            CreateModel(out AnglerQuestModel model, out AnglerQuestState state, out _);
            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, false, 31, false, false, AnglerRewardOwnership.None));

            model.TryGetProjection(out AnglerQuestProjection first);
            model.TryGetProjection(out AnglerQuestProjection second);

            Assert.That(second, Is.SameAs(first));

            state.ReplaceSnapshot(new AnglerQuestSnapshot(2450, true, 31, false, false, AnglerRewardOwnership.None));
            model.TryGetProjection(out AnglerQuestProjection third);

            Assert.Multiple(() =>
            {
                Assert.That(third, Is.Not.SameAs(first));
                Assert.That(third.FinishedToday, Is.True);
                Assert.That(model.Revision, Is.EqualTo(state.Revision));
            });
        }

        private static void CreateModel(
            out AnglerQuestModel model,
            out AnglerQuestState state,
            out ItemCatalog itemCatalog)
        {
            itemCatalog = CreateItemCatalog(_rewardItemIds.Concat([2450]));
            var rewardCatalog = AnglerRewardCatalog.Create(itemCatalog);
            state = new AnglerQuestState();
            model = new AnglerQuestModel(itemCatalog, rewardCatalog, state);
        }

        private static ItemCatalogEntry GetItem(ItemCatalog catalog, int itemId)
        {
            Assert.That(catalog.TryGet(itemId, out ItemCatalogEntry entry), Is.True);
            return entry;
        }

        private static ItemCatalog CreateItemCatalog(IEnumerable<int> itemIds)
        {
            return ItemCatalog.Create(
                itemIds.Distinct().Select(itemId => new ItemCatalogEntry(itemId, $"Item {itemId}")));
        }
    }
}