using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.Details
{
    internal sealed class NpcDetailsItemReference
    {
        public NpcDetailsItemReference(
            int itemId,
            string name,
            bool isCollectionTracked,
            bool isFound,
            bool isNavigable)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            ItemId = itemId;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            IsCollectionTracked = isCollectionTracked;
            IsFound = isFound;
            IsNavigable = isNavigable;
        }

        public int ItemId { get; }

        public string Name { get; }

        public bool IsCollectionTracked { get; }

        public bool IsFound { get; }

        public bool IsNavigable { get; }
    }

    internal sealed class NpcDetailsLootRow
    {
        private static readonly IReadOnlyList<string> _emptyDescriptions = Array.Empty<string>();

        public NpcDetailsLootRow(
            NpcDetailsItemReference item,
            int stackMin,
            int stackMax,
            float dropRate,
            IEnumerable<string> conditionDescriptions)
        {
            if (stackMin < 0)
                throw new ArgumentOutOfRangeException(nameof(stackMin));

            if (stackMax < stackMin)
                throw new ArgumentOutOfRangeException(nameof(stackMax));

            if (float.IsNaN(dropRate) || float.IsInfinity(dropRate) || dropRate < 0f)
                throw new ArgumentOutOfRangeException(nameof(dropRate));

            Item = item ?? throw new ArgumentNullException(nameof(item));
            StackMin = stackMin;
            StackMax = stackMax;
            DropRate = dropRate;
            ConditionDescriptions = CopyDescriptions(conditionDescriptions);
        }

        public NpcDetailsItemReference Item { get; }

        public int StackMin { get; }

        public int StackMax { get; }

        public float DropRate { get; }

        public IReadOnlyList<string> ConditionDescriptions { get; }

        private static IReadOnlyList<string> CopyDescriptions(IEnumerable<string> descriptions)
        {
            if (descriptions == null)
                return _emptyDescriptions;

            var snapshot = new List<string>();

            foreach (string description in descriptions)
            {
                if (description == null)
                {
                    throw new ArgumentException(
                        "Condition descriptions must not contain null values.",
                        nameof(descriptions));
                }

                snapshot.Add(description);
            }

            return snapshot.Count == 0 ? _emptyDescriptions : new ReadOnlyCollection<string>(snapshot);
        }
    }


    internal sealed class NpcDetailsMerchantCondition(MerchantSourceCondition condition, string description)
    {
        public MerchantSourceCondition Condition { get; } = condition;

        public string Description { get; } = description ?? throw new ArgumentNullException(nameof(description));
    }

    internal sealed class NpcDetailsMerchantVariant(
        IEnumerable<NpcDetailsMerchantCondition> conditions,
        bool randomStock,
        bool shopCapacityLimited)
    {
        private static readonly IReadOnlyList<NpcDetailsMerchantCondition> _emptyConditions =
            Array.Empty<NpcDetailsMerchantCondition>();

        public IReadOnlyList<NpcDetailsMerchantCondition> Conditions { get; } = CopyConditions(conditions);

        public bool RandomStock { get; } = randomStock;

        public bool ShopCapacityLimited { get; } = shopCapacityLimited;

        private static IReadOnlyList<NpcDetailsMerchantCondition> CopyConditions(
            IEnumerable<NpcDetailsMerchantCondition> conditions)
        {
            if (conditions == null)
                return _emptyConditions;

            var snapshot = new List<NpcDetailsMerchantCondition>();

            foreach (NpcDetailsMerchantCondition condition in conditions)
            {
                if (condition == null)
                {
                    throw new ArgumentException(
                        "Merchant conditions must not contain null values.",
                        nameof(conditions));
                }

                snapshot.Add(condition);
            }

            return snapshot.Count == 0
                ? _emptyConditions
                : new ReadOnlyCollection<NpcDetailsMerchantCondition>(snapshot);
        }
    }

    internal sealed class NpcDetailsMerchantStockEntry
    {
        public NpcDetailsMerchantStockEntry(
            NpcDetailsItemReference item,
            bool isAlwaysAvailable,
            IEnumerable<NpcDetailsMerchantVariant> variants)
        {
            if (variants == null)
                throw new ArgumentNullException(nameof(variants));

            var copiedVariants = new List<NpcDetailsMerchantVariant>();

            foreach (NpcDetailsMerchantVariant variant in variants)
            {
                if (variant == null)
                    throw new ArgumentException("Merchant variants must not contain null values.", nameof(variants));

                copiedVariants.Add(variant);
            }

            Item = item ?? throw new ArgumentNullException(nameof(item));
            IsAlwaysAvailable = isAlwaysAvailable;
            Variants = new ReadOnlyCollection<NpcDetailsMerchantVariant>(copiedVariants);
        }

        public NpcDetailsItemReference Item { get; }

        public bool IsAlwaysAvailable { get; }

        public IReadOnlyList<NpcDetailsMerchantVariant> Variants { get; }
    }

    internal sealed class NpcDetailsProjection
    {
        public NpcDetailsProjection(
            int npcNetId,
            NpcDifficultyMode difficulty,
            NpcBestiaryMetadataSnapshot metadata,
            IEnumerable<NpcDetailsLootRow> lootRows,
            bool merchantSourceDataAvailable = false,
            IEnumerable<NpcDetailsMerchantStockEntry> merchantStock = null,
            NpcBestiaryKillStatisticsSnapshot killStatistics = null,
            NpcDetailsItemReference bannerItem = null,
            int bannerProgress = 0)
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (lootRows == null)
                throw new ArgumentNullException(nameof(lootRows));

            if (bannerProgress < 0)
                throw new ArgumentOutOfRangeException(nameof(bannerProgress));

            if (killStatistics is { BannerItemId: not null })
            {
                int bannerItemId = killStatistics.BannerItemId.GetValueOrDefault();

                if (bannerItem == null || bannerItem.ItemId != bannerItemId)
                {
                    throw new ArgumentException(
                        "Banner item must match the native kill-statistics snapshot.",
                        nameof(bannerItem));
                }

                if (bannerProgress >= killStatistics.KillsPerBanner)
                    throw new ArgumentOutOfRangeException(nameof(bannerProgress));
            }
            else if (bannerItem != null || bannerProgress != 0)
            {
                throw new ArgumentException(
                    "Banner presentation must be empty when the native snapshot has no banner.",
                    nameof(bannerItem));
            }

            var copiedLootRows = new List<NpcDetailsLootRow>();
            var copiedMerchantStock = new List<NpcDetailsMerchantStockEntry>();

            foreach (NpcDetailsLootRow row in lootRows)
            {
                if (row == null)
                    throw new ArgumentException("Loot rows must not contain null values.", nameof(lootRows));

                copiedLootRows.Add(row);
            }

            if (merchantStock != null)
            {
                foreach (NpcDetailsMerchantStockEntry entry in merchantStock)
                {
                    if (entry == null)
                    {
                        throw new ArgumentException(
                            "Merchant stock must not contain null values.",
                            nameof(merchantStock));
                    }

                    copiedMerchantStock.Add(entry);
                }
            }

            NpcNetId = npcNetId;
            Difficulty = difficulty;
            IsEncountered = metadata.IsEncountered;
            Name = metadata.Name;
            Stats = metadata.Stats;
            BestiaryRarityStars = metadata.BestiaryRarityStars;
            RareSpawnRarityLevel = metadata.RareSpawnRarityLevel;
            SpawnConditions = metadata.SpawnConditions;
            BaseDebuffImmunities = metadata.BaseDebuffImmunities;
            HasKillCounter = killStatistics?.HasKillCounter == true;
            SlainCount = killStatistics?.SlainCount ?? 0;
            BannerItem = bannerItem;
            BannerProgress = bannerItem != null ? bannerProgress : 0;
            BannerProgressTarget = killStatistics?.KillsPerBanner ?? 0;
            LootRows = new ReadOnlyCollection<NpcDetailsLootRow>(copiedLootRows);
            MerchantSourceDataAvailable = merchantSourceDataAvailable;
            MerchantStock = new ReadOnlyCollection<NpcDetailsMerchantStockEntry>(copiedMerchantStock);
        }

        public int NpcNetId { get; }

        public NpcDifficultyMode Difficulty { get; }

        public bool IsEncountered { get; }

        public string Name { get; }

        public NpcBestiaryStatsSnapshot Stats { get; }

        public int BestiaryRarityStars { get; }

        public int? RareSpawnRarityLevel { get; }

        public IReadOnlyList<NpcBestiarySpawnCondition> SpawnConditions { get; }

        public IReadOnlyList<NpcBestiaryDebuffImmunity> BaseDebuffImmunities { get; }

        public bool HasKillCounter { get; }

        public int SlainCount { get; }

        public NpcDetailsItemReference BannerItem { get; }

        public int BannerProgress { get; }

        public int BannerProgressTarget { get; }

        public bool HasKillStatistics => HasKillCounter || BannerItem != null;

        public IReadOnlyList<NpcDetailsLootRow> LootRows { get; }

        public bool MerchantSourceDataAvailable { get; }

        public IReadOnlyList<NpcDetailsMerchantStockEntry> MerchantStock { get; }
    }
}