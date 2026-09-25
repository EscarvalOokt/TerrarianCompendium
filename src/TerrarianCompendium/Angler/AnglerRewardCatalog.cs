using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Angler
{
    [Flags]
    internal enum AnglerRewardOwnership
    {
        None = 0,
        HighTestFishingLine = 1 << 0,
        AnglerEarring = 1 << 1,
        TackleBox = 1 << 2,
        FishermansGuide = 1 << 3,
        WeatherRadio = 1 << 4,
        Sextant = 1 << 5,
        FishingBobber = 1 << 6
    }

    internal sealed class AnglerMilestoneReward
    {
        public AnglerMilestoneReward(int completedQuests, int itemId)
        {
            if (completedQuests <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedQuests),
                    completedQuests,
                    "Completed quest count must be greater than zero.");
            }

            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            CompletedQuests = completedQuests;
            ItemId = itemId;
        }

        public int CompletedQuests { get; }

        public int ItemId { get; }
    }

    internal sealed class AnglerSpecialQuestReward
    {
        public AnglerSpecialQuestReward(
            int questItemId,
            IEnumerable<int> rewardItemIds,
            int preHardmodeTriggerDenominator,
            int rewardSelectionDenominator)
        {
            if (questItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(questItemId),
                    questItemId,
                    "Quest item ID must be greater than zero.");
            }

            if (preHardmodeTriggerDenominator <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(preHardmodeTriggerDenominator),
                    preHardmodeTriggerDenominator,
                    "Trigger denominator must be greater than zero.");
            }

            if (rewardSelectionDenominator <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rewardSelectionDenominator),
                    rewardSelectionDenominator,
                    "Selection denominator must be greater than zero.");
            }

            QuestItemId = questItemId;
            RewardItemIds = CreateValidatedItemIdList(rewardItemIds, nameof(rewardItemIds));
            PreHardmodeTriggerDenominator = preHardmodeTriggerDenominator;
            RewardSelectionDenominator = rewardSelectionDenominator;
        }

        public int QuestItemId { get; }

        public IReadOnlyList<int> RewardItemIds { get; }

        public int PreHardmodeTriggerDenominator { get; }

        public int RewardSelectionDenominator { get; }

        private static IReadOnlyList<int> CreateValidatedItemIdList(IEnumerable<int> itemIds, string parameterName)
        {
            if (itemIds == null)
                throw new ArgumentNullException(parameterName);

            var result = new List<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                    throw new ArgumentOutOfRangeException(parameterName, itemId, "Item IDs must be greater than zero.");

                result.Add(itemId);
            }

            if (result.Count == 0)
                throw new ArgumentException("At least one reward item ID is required.", parameterName);

            return new ReadOnlyCollection<int>(result);
        }
    }

    internal sealed class AnglerRandomMainRewardRule
    {
        public AnglerRandomMainRewardRule(
            IEnumerable<int> itemIds,
            int rollDenominatorFactor,
            int minimumCompletedQuestsExclusive = -1,
            bool requiresHardMode = false)
        {
            if (rollDenominatorFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rollDenominatorFactor),
                    rollDenominatorFactor,
                    "Roll denominator factor must be greater than zero.");
            }

            if (minimumCompletedQuestsExclusive < -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minimumCompletedQuestsExclusive),
                    minimumCompletedQuestsExclusive,
                    "Minimum completed quest count must be -1 or greater.");
            }

            ItemIds = CreateValidatedItemIdList(itemIds);
            RollDenominatorFactor = rollDenominatorFactor;
            MinimumCompletedQuestsExclusive = minimumCompletedQuestsExclusive;
            RequiresHardMode = requiresHardMode;
        }

        public IReadOnlyList<int> ItemIds { get; }

        public int RollDenominatorFactor { get; }

        public int MinimumCompletedQuestsExclusive { get; }

        public bool RequiresHardMode { get; }

        public bool IsEligible(int completedQuests, bool isHardMode)
        {
            if (completedQuests < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedQuests),
                    completedQuests,
                    "Completed quest count must not be negative.");
            }

            return completedQuests > MinimumCompletedQuestsExclusive && (!RequiresHardMode || isHardMode);
        }

        private static IReadOnlyList<int> CreateValidatedItemIdList(IEnumerable<int> itemIds)
        {
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));

            var result = new List<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(itemIds),
                        itemId,
                        "Item IDs must be greater than zero.");

                result.Add(itemId);
            }

            if (result.Count == 0)
                throw new ArgumentException("At least one reward item ID is required.", nameof(itemIds));

            return new ReadOnlyCollection<int>(result);
        }
    }

    internal sealed class AnglerMissingRewardDefinition
    {
        public AnglerMissingRewardDefinition(
            int itemId,
            AnglerRewardOwnership ownershipFlag,
            IEnumerable<int> ownedEquivalentItemIds)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            var ownershipFlagValue = (int)ownershipFlag;

            if (ownershipFlag == AnglerRewardOwnership.None || (ownershipFlagValue & (ownershipFlagValue - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ownershipFlag),
                    ownershipFlag,
                    "Ownership flag must contain exactly one value.");
            }

            if (ownedEquivalentItemIds == null)
                throw new ArgumentNullException(nameof(ownedEquivalentItemIds));

            var normalizedOwnedItemIds = new List<int> { itemId };
            var seenItemIds = new HashSet<int> { itemId };

            foreach (int ownedItemId in ownedEquivalentItemIds)
            {
                if (ownedItemId <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(ownedEquivalentItemIds),
                        ownedItemId,
                        "Owned-equivalent Item IDs must be greater than zero.");
                }

                if (seenItemIds.Add(ownedItemId))
                    normalizedOwnedItemIds.Add(ownedItemId);
            }

            ItemId = itemId;
            OwnershipFlag = ownershipFlag;
            OwnedEquivalentItemIds = new ReadOnlyCollection<int>(normalizedOwnedItemIds);
        }

        public int ItemId { get; }

        public AnglerRewardOwnership OwnershipFlag { get; }

        public IReadOnlyList<int> OwnedEquivalentItemIds { get; }
    }

    internal sealed class AnglerPotionFallbackDefinition
    {
        public AnglerPotionFallbackDefinition(IEnumerable<int> itemIds, int minimumStack, int maximumStack)
        {
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));

            if (minimumStack <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(minimumStack),
                    minimumStack,
                    "Minimum stack must be positive.");

            if (maximumStack < minimumStack)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumStack),
                    maximumStack,
                    "Maximum stack must not be less than minimum stack.");
            }

            var normalizedItemIds = new List<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(itemIds),
                        itemId,
                        "Item IDs must be greater than zero.");

                normalizedItemIds.Add(itemId);
            }

            if (normalizedItemIds.Count == 0)
                throw new ArgumentException("At least one potion Item ID is required.", nameof(itemIds));

            ItemIds = new ReadOnlyCollection<int>(normalizedItemIds);
            MinimumStack = minimumStack;
            MaximumStack = maximumStack;
        }

        public IReadOnlyList<int> ItemIds { get; }

        public int MinimumStack { get; }

        public int MaximumStack { get; }
    }

    internal sealed class AnglerDecorationRewardDefinition
    {
        public AnglerDecorationRewardDefinition(IEnumerable<int> itemIds, int guaranteedAtCompletedQuests)
        {
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));

            if (guaranteedAtCompletedQuests <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(guaranteedAtCompletedQuests),
                    guaranteedAtCompletedQuests,
                    "Guaranteed quest count must be greater than zero.");
            }

            var normalizedItemIds = new List<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId <= 0)
                    throw new ArgumentOutOfRangeException(
                        nameof(itemIds),
                        itemId,
                        "Item IDs must be greater than zero.");

                normalizedItemIds.Add(itemId);
            }

            if (normalizedItemIds.Count == 0)
                throw new ArgumentException("At least one decoration Item ID is required.", nameof(itemIds));

            ItemIds = new ReadOnlyCollection<int>(normalizedItemIds);
            GuaranteedAtCompletedQuests = guaranteedAtCompletedQuests;
        }

        public IReadOnlyList<int> ItemIds { get; }

        public int GuaranteedAtCompletedQuests { get; }
    }

    internal sealed class AnglerBaitRewardDefinition
    {
        public AnglerBaitRewardDefinition(
            int stageRollDenominatorFactor,
            int stageRollInclusiveThreshold,
            int masterBaitItemId,
            int masterBaitRollDenominatorFactor,
            int journeymanBaitItemId,
            int journeymanBaitRollDenominatorFactor,
            int apprenticeBaitItemId,
            IEnumerable<int> additionalStackThresholds)
        {
            if (stageRollDenominatorFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageRollDenominatorFactor),
                    stageRollDenominatorFactor,
                    "Stage denominator factor must be greater than zero.");
            }

            if (stageRollInclusiveThreshold < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stageRollInclusiveThreshold),
                    stageRollInclusiveThreshold,
                    "Stage threshold must not be negative.");
            }

            if (masterBaitItemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(masterBaitItemId));
            if (journeymanBaitItemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(journeymanBaitItemId));
            if (apprenticeBaitItemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(apprenticeBaitItemId));
            if (masterBaitRollDenominatorFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(masterBaitRollDenominatorFactor));
            if (journeymanBaitRollDenominatorFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(journeymanBaitRollDenominatorFactor));
            if (additionalStackThresholds == null)
                throw new ArgumentNullException(nameof(additionalStackThresholds));

            var thresholds = new List<int>();

            foreach (int threshold in additionalStackThresholds)
            {
                if (threshold <= 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(additionalStackThresholds),
                        threshold,
                        "Stack thresholds must be greater than zero.");
                }

                thresholds.Add(threshold);
            }

            StageRollDenominatorFactor = stageRollDenominatorFactor;
            StageRollInclusiveThreshold = stageRollInclusiveThreshold;
            MasterBaitItemId = masterBaitItemId;
            MasterBaitRollDenominatorFactor = masterBaitRollDenominatorFactor;
            JourneymanBaitItemId = journeymanBaitItemId;
            JourneymanBaitRollDenominatorFactor = journeymanBaitRollDenominatorFactor;
            ApprenticeBaitItemId = apprenticeBaitItemId;
            AdditionalStackThresholds = new ReadOnlyCollection<int>(thresholds);
        }

        public int StageRollDenominatorFactor { get; }

        public int StageRollInclusiveThreshold { get; }

        public int MasterBaitItemId { get; }

        public int MasterBaitRollDenominatorFactor { get; }

        public int JourneymanBaitItemId { get; }

        public int JourneymanBaitRollDenominatorFactor { get; }

        public int ApprenticeBaitItemId { get; }

        public IReadOnlyList<int> AdditionalStackThresholds { get; }
    }

    internal sealed class AnglerMoneyRewardDefinition
    {
        public int QuestCountOffset => 50;

        public int QuestCountDivisor => 2;

        public int RandomMultiplierMinimumInclusive => 50;

        public int RandomMultiplierMaximumExclusive => 201;

        public float RandomMultiplierScale => 0.015f;

        public float FinalScale => 1.5f;

        public int HardModeMultiplier => 2;

        public int ExpertModeMultiplier => 2;

        public int GoldCoinThresholdExclusive => 100;

        public int GoldCoinItemId => 73;

        public int GoldCoinMaximumStack => 10;

        public int SilverCoinItemId => 72;

        public int SilverCoinMaximumStack => 99;
    }

    internal sealed class AnglerRewardCatalog
    {
        private const int BumblebeeTunaItemId = 2451;
        private const int FuzzyCarrotItemId = 2428;
        private const int AnglerHatItemId = 2367;
        private const int AnglerVestItemId = 2368;
        private const int AnglerPantsItemId = 2369;
        private const int BottomlessBucketItemId = 3031;
        private const int GoldenFishingRodItemId = 2294;
        private const int BottomlessHoneyBucketItemId = 5302;
        private const int HoneyAbsorbantSpongeItemId = 5303;
        private const int HotlineFishingHookItemId = 2422;
        private const int FinWingsItemId = 2494;
        private const int SuperAbsorbantSpongeItemId = 3032;
        private const int GoldenBugNetItemId = 3183;
        private const int FishHookItemId = 2360;
        private const int FishMinecartItemId = 4067;
        private const int SeashellHairpinItemId = 2417;
        private const int MermaidAdornmentItemId = 2418;
        private const int MermaidTailItemId = 2419;
        private const int FishCostumeMaskItemId = 2498;
        private const int FishCostumeShirtItemId = 2499;
        private const int FishCostumeFinskirtItemId = 2500;
        private const int HighTestFishingLineItemId = 2373;
        private const int AnglerEarringItemId = 2374;
        private const int TackleBoxItemId = 2375;
        private const int FishermansGuideItemId = 3120;
        private const int WeatherRadioItemId = 3037;
        private const int SextantItemId = 3096;
        private const int FishingBobberItemId = 5139;
        private const int AnglerTackleBagItemId = 3721;
        private const int LavaproofTackleBagItemId = 5064;
        private const int FishFinderItemId = 3036;
        private const int PdaItemId = 3123;
        private const int CellPhoneItemId = 3124;
        private const int ShellphoneItemId = 5358;
        private const int ShellphoneSpawnItemId = 5359;
        private const int ShellphoneOceanItemId = 5360;
        private const int ShellphoneHellItemId = 5361;
        private const int FishingBobberGlowingStarItemId = 5140;
        private const int FishingBobberGlowingLavaItemId = 5141;
        private const int FishingBobberGlowingKryptonItemId = 5142;
        private const int FishingBobberGlowingXenonItemId = 5143;
        private const int FishingBobberGlowingArgonItemId = 5144;
        private const int FishingBobberGlowingVioletItemId = 5145;
        private const int FishingBobberGlowingRainbowItemId = 5146;
        private const int FishingPotionItemId = 2354;
        private const int SonarPotionItemId = 2355;
        private const int CratePotionItemId = 2356;
        private const int ApprenticeBaitItemId = 2674;
        private const int JourneymanBaitItemId = 2675;
        private const int MasterBaitItemId = 2676;

        private readonly Dictionary<int, AnglerRewardOwnership> _ownershipFlagsByOwnedItemId;

        private AnglerRewardCatalog(
            IReadOnlyList<AnglerMilestoneReward> milestones,
            AnglerSpecialQuestReward bumblebeeTunaReward,
            IReadOnlyList<AnglerRandomMainRewardRule> randomMainRewards,
            IReadOnlyList<AnglerMissingRewardDefinition> missingRewards,
            IReadOnlyList<int> missingRewardRollDenominatorFactors,
            float missingRewardChanceScale,
            AnglerPotionFallbackDefinition potionFallback,
            AnglerDecorationRewardDefinition decorationReward,
            AnglerMoneyRewardDefinition moneyReward,
            AnglerBaitRewardDefinition baitReward,
            Dictionary<int, AnglerRewardOwnership> ownershipFlagsByOwnedItemId)
        {
            Milestones = milestones;
            BumblebeeTunaReward = bumblebeeTunaReward;
            RandomMainRewards = randomMainRewards;
            MissingRewards = missingRewards;
            MissingRewardRollDenominatorFactors = missingRewardRollDenominatorFactors;
            MissingRewardChanceScale = missingRewardChanceScale;
            PotionFallback = potionFallback;
            DecorationReward = decorationReward;
            MoneyReward = moneyReward;
            BaitReward = baitReward;
            _ownershipFlagsByOwnedItemId = ownershipFlagsByOwnedItemId;
        }

        public IReadOnlyList<AnglerMilestoneReward> Milestones { get; }

        public AnglerSpecialQuestReward BumblebeeTunaReward { get; }

        public IReadOnlyList<AnglerRandomMainRewardRule> RandomMainRewards { get; }

        public IReadOnlyList<AnglerMissingRewardDefinition> MissingRewards { get; }

        public IReadOnlyList<int> MissingRewardRollDenominatorFactors { get; }

        public float MissingRewardChanceScale { get; }

        public AnglerPotionFallbackDefinition PotionFallback { get; }

        public AnglerDecorationRewardDefinition DecorationReward { get; }

        public AnglerMoneyRewardDefinition MoneyReward { get; }

        public AnglerBaitRewardDefinition BaitReward { get; }

        public static AnglerRewardCatalog Create(ItemCatalog itemCatalog)
        {
            if (itemCatalog == null)
                throw new ArgumentNullException(nameof(itemCatalog));

            IReadOnlyList<AnglerMilestoneReward> milestones = new ReadOnlyCollection<AnglerMilestoneReward>(
                new List<AnglerMilestoneReward>
                {
                    new(5, FuzzyCarrotItemId),
                    new(10, AnglerHatItemId),
                    new(15, AnglerVestItemId),
                    new(20, AnglerPantsItemId),
                    new(25, BottomlessBucketItemId),
                    new(30, GoldenFishingRodItemId)
                });

            var bumblebeeTunaReward = new AnglerSpecialQuestReward(
                BumblebeeTunaItemId,
                [BottomlessHoneyBucketItemId, HoneyAbsorbantSpongeItemId],
                preHardmodeTriggerDenominator: 2,
                rewardSelectionDenominator: 2);

            IReadOnlyList<AnglerRandomMainRewardRule> randomMainRewards =
                new ReadOnlyCollection<AnglerRandomMainRewardRule>(
                    new List<AnglerRandomMainRewardRule>
                    {
                        new([GoldenFishingRodItemId], 250, minimumCompletedQuestsExclusive: 75),
                        new(
                            [HotlineFishingHookItemId],
                            100,
                            minimumCompletedQuestsExclusive: 25,
                            requiresHardMode: true),
                        new([FinWingsItemId], 70, minimumCompletedQuestsExclusive: 10, requiresHardMode: true),
                        new([BottomlessBucketItemId], 70, minimumCompletedQuestsExclusive: 10),
                        new([SuperAbsorbantSpongeItemId], 70, minimumCompletedQuestsExclusive: 10),
                        new([GoldenBugNetItemId], 80),
                        new([FishHookItemId], 60),
                        new([FishMinecartItemId], 60),
                        new([SeashellHairpinItemId, MermaidAdornmentItemId, MermaidTailItemId], 80),
                        new([FishCostumeMaskItemId, FishCostumeShirtItemId, FishCostumeFinskirtItemId], 80)
                    });

            IReadOnlyList<AnglerMissingRewardDefinition> missingRewards =
                new ReadOnlyCollection<AnglerMissingRewardDefinition>(
                    new List<AnglerMissingRewardDefinition>
                    {
                        new(
                            HighTestFishingLineItemId,
                            AnglerRewardOwnership.HighTestFishingLine,
                            [AnglerTackleBagItemId, LavaproofTackleBagItemId]),
                        new(
                            AnglerEarringItemId,
                            AnglerRewardOwnership.AnglerEarring,
                            [AnglerTackleBagItemId, LavaproofTackleBagItemId]),
                        new(
                            TackleBoxItemId,
                            AnglerRewardOwnership.TackleBox,
                            [AnglerTackleBagItemId, LavaproofTackleBagItemId]),
                        new(
                            FishermansGuideItemId,
                            AnglerRewardOwnership.FishermansGuide,
                            [
                                FishFinderItemId,
                                PdaItemId,
                                CellPhoneItemId,
                                ShellphoneItemId,
                                ShellphoneSpawnItemId,
                                ShellphoneOceanItemId,
                                ShellphoneHellItemId
                            ]),
                        new(
                            WeatherRadioItemId,
                            AnglerRewardOwnership.WeatherRadio,
                            [
                                FishFinderItemId,
                                PdaItemId,
                                CellPhoneItemId,
                                ShellphoneItemId,
                                ShellphoneSpawnItemId,
                                ShellphoneOceanItemId,
                                ShellphoneHellItemId
                            ]),
                        new(
                            SextantItemId,
                            AnglerRewardOwnership.Sextant,
                            [
                                FishFinderItemId,
                                PdaItemId,
                                CellPhoneItemId,
                                ShellphoneItemId,
                                ShellphoneSpawnItemId,
                                ShellphoneOceanItemId,
                                ShellphoneHellItemId
                            ]),
                        new(
                            FishingBobberItemId,
                            AnglerRewardOwnership.FishingBobber,
                            [
                                FishingBobberGlowingStarItemId,
                                FishingBobberGlowingLavaItemId,
                                FishingBobberGlowingKryptonItemId,
                                FishingBobberGlowingXenonItemId,
                                FishingBobberGlowingArgonItemId,
                                FishingBobberGlowingVioletItemId,
                                FishingBobberGlowingRainbowItemId
                            ])
                    });

            IReadOnlyList<int> missingRewardRollDenominatorFactors = new ReadOnlyCollection<int>(
                new List<int> { 40, 40, 40, 30, 30, 30, 25 });

            var potionFallback = new AnglerPotionFallbackDefinition(
                [FishingPotionItemId, SonarPotionItemId, CratePotionItemId],
                minimumStack: 2,
                maximumStack: 5);

            var decorationReward = new AnglerDecorationRewardDefinition(
                [
                    2442,
                    2443,
                    2444,
                    2445,
                    2497,
                    2495,
                    2446,
                    2447,
                    2448,
                    2449,
                    2490,
                    2496,
                    5235,
                    5252,
                    5256,
                    5259,
                    5263,
                    5264,
                    5265
                ],
                guaranteedAtCompletedQuests: 100);

            var moneyReward = new AnglerMoneyRewardDefinition();

            var baitReward = new AnglerBaitRewardDefinition(
                stageRollDenominatorFactor: 100,
                stageRollInclusiveThreshold: 50,
                masterBaitItemId: MasterBaitItemId,
                masterBaitRollDenominatorFactor: 15,
                journeymanBaitItemId: JourneymanBaitItemId,
                journeymanBaitRollDenominatorFactor: 5,
                apprenticeBaitItemId: ApprenticeBaitItemId,
                additionalStackThresholds: [25, 50, 100, 150, 200, 250]);

            Dictionary<int, AnglerRewardOwnership> ownershipFlagsByOwnedItemId = BuildOwnershipLookup(missingRewards);

            ValidateReferencedItems(
                itemCatalog,
                milestones,
                bumblebeeTunaReward,
                randomMainRewards,
                missingRewards,
                potionFallback,
                decorationReward,
                moneyReward,
                baitReward);

            return new AnglerRewardCatalog(
                milestones,
                bumblebeeTunaReward,
                randomMainRewards,
                missingRewards,
                missingRewardRollDenominatorFactors,
                0.8f,
                potionFallback,
                decorationReward,
                moneyReward,
                baitReward,
                ownershipFlagsByOwnedItemId);
        }

        public AnglerRewardOwnership GetOwnedMissingRewardFlagsForItem(int itemId)
        {
            return itemId > 0 && _ownershipFlagsByOwnedItemId.TryGetValue(itemId, out AnglerRewardOwnership flags)
                ? flags
                : AnglerRewardOwnership.None;
        }

        public AnglerMilestoneReward GetNextMilestone(int completedQuests)
        {
            if (completedQuests < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedQuests),
                    completedQuests,
                    "Completed quest count must not be negative.");
            }

            for (var index = 0; index < Milestones.Count; index++)
            {
                AnglerMilestoneReward milestone = Milestones[index];

                if (milestone.CompletedQuests > completedQuests)
                    return milestone;
            }

            return null;
        }

        public AnglerMilestoneReward GetMilestone(int completedQuests)
        {
            if (completedQuests < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedQuests),
                    completedQuests,
                    "Completed quest count must not be negative.");
            }

            for (var index = 0; index < Milestones.Count; index++)
            {
                AnglerMilestoneReward milestone = Milestones[index];

                if (milestone.CompletedQuests == completedQuests)
                    return milestone;
            }

            return null;
        }

        public static float CalculateBaseRarityMultiplier(int completedQuests)
        {
            if (completedQuests < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(completedQuests),
                    completedQuests,
                    "Completed quest count must not be negative.");
            }

            float multiplier;

            if (completedQuests <= 50)
                multiplier = 1f - completedQuests * 0.01f;
            else if (completedQuests <= 100)
                multiplier = 0.5f - (completedQuests - 50) * 0.005f;
            else if (completedQuests <= 150)
                multiplier = 0.25f - (completedQuests - 100) * 0.002f;
            else
                multiplier = 0.15f;

            return multiplier * 0.9f;
        }

        public static float CalculateAdjustedRarityMultiplier(int completedQuests, float priceAdjustment)
        {
            return CalculateBaseRarityMultiplier(completedQuests) * ((priceAdjustment + 1f) / 2f);
        }

        private static Dictionary<int, AnglerRewardOwnership> BuildOwnershipLookup(
            IReadOnlyList<AnglerMissingRewardDefinition> definitions)
        {
            var result = new Dictionary<int, AnglerRewardOwnership>();

            foreach (AnglerMissingRewardDefinition definition in definitions)
            {
                foreach (int ownedItemId in definition.OwnedEquivalentItemIds)
                {
                    result.TryGetValue(ownedItemId, out AnglerRewardOwnership existingFlags);
                    result[ownedItemId] = existingFlags | definition.OwnershipFlag;
                }
            }

            return result;
        }

        private static void ValidateReferencedItems(
            ItemCatalog itemCatalog,
            IReadOnlyList<AnglerMilestoneReward> milestones,
            AnglerSpecialQuestReward specialReward,
            IReadOnlyList<AnglerRandomMainRewardRule> randomMainRewards,
            IReadOnlyList<AnglerMissingRewardDefinition> missingRewards,
            AnglerPotionFallbackDefinition potionFallback,
            AnglerDecorationRewardDefinition decorationReward,
            AnglerMoneyRewardDefinition moneyReward,
            AnglerBaitRewardDefinition baitReward)
        {
            var referencedItemIds = new HashSet<int>();

            foreach (AnglerMilestoneReward milestone in milestones)
                referencedItemIds.Add(milestone.ItemId);

            referencedItemIds.Add(specialReward.QuestItemId);
            AddItemIds(referencedItemIds, specialReward.RewardItemIds);

            foreach (AnglerRandomMainRewardRule rule in randomMainRewards)
                AddItemIds(referencedItemIds, rule.ItemIds);

            foreach (AnglerMissingRewardDefinition definition in missingRewards)
            {
                referencedItemIds.Add(definition.ItemId);
                AddItemIds(referencedItemIds, definition.OwnedEquivalentItemIds);
            }

            AddItemIds(referencedItemIds, potionFallback.ItemIds);
            AddItemIds(referencedItemIds, decorationReward.ItemIds);
            referencedItemIds.Add(moneyReward.GoldCoinItemId);
            referencedItemIds.Add(moneyReward.SilverCoinItemId);
            referencedItemIds.Add(baitReward.MasterBaitItemId);
            referencedItemIds.Add(baitReward.JourneymanBaitItemId);
            referencedItemIds.Add(baitReward.ApprenticeBaitItemId);

            foreach (int itemId in referencedItemIds)
            {
                if (!itemCatalog.Contains(itemId))
                {
                    throw new InvalidOperationException(
                        $"Vanilla Angler reward Item ID {itemId} is not present in the item catalog.");
                }
            }
        }

        private static void AddItemIds(HashSet<int> target, IReadOnlyList<int> itemIds)
        {
            for (var index = 0; index < itemIds.Count; index++)
                target.Add(itemIds[index]);
        }
    }
}