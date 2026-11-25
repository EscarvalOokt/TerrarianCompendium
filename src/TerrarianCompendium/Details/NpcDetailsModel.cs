using System;
using System.Collections.Generic;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Details
{
    internal sealed class NpcDetailsModel
    {
        private readonly ChecklistState _checklistState;
        private readonly ItemCatalog _itemCatalog;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly Func<NpcCatalogEntry, NpcBestiaryKillStatisticsSnapshot> _killStatisticsProvider;
        private readonly CompendiumLocalization _localization;
        private readonly MerchantSourceIndex _merchantSourceIndex;
        private readonly Func<NpcCatalogEntry, NpcDifficultyMode, NpcBestiaryMetadataSnapshot> _metadataProvider;
        private readonly Func<string, string> _nativeTextResolver;
        private readonly NpcCatalog _npcCatalog;
        private readonly NpcLootIndex _npcLootIndex;
        private readonly Func<NpcCatalogEntry, string> _npcNameProvider;

        private long _cachedChecklistRevision = -1;
        private NpcDifficultyMode _cachedDifficulty;
        private List<(NpcLootCondition Condition, string Description)> _cachedDynamicLootQualifiers;
        private long _cachedItemTextRevision = -1;
        private NpcBestiaryKillStatisticsSnapshot _cachedKillStatistics;
        private long _cachedLocalizationRevision = -1;
        private NpcBestiaryMetadataSnapshot _cachedMetadata;
        private int _cachedNpcNetId;
        private NpcDetailsProjection _cachedProjection;

        public NpcDetailsModel(
            NpcCatalog npcCatalog,
            NpcLootIndex npcLootIndex,
            ItemCatalog itemCatalog,
            ItemTextIndex itemTextIndex,
            ChecklistState checklistState,
            CompendiumLocalization localization,
            MerchantSourceIndex merchantSourceIndex = null,
            Func<NpcCatalogEntry, string> npcNameProvider = null,
            Func<string, string> nativeTextResolver = null) : this(
            npcCatalog,
            npcLootIndex,
            itemCatalog,
            itemTextIndex,
            checklistState,
            localization,
            VanillaBestiaryNativeBridge.GetMetadataSnapshot,
            VanillaBestiaryNativeBridge.GetKillStatisticsSnapshot,
            merchantSourceIndex,
            npcNameProvider,
            nativeTextResolver)
        {
        }

        internal NpcDetailsModel(
            NpcCatalog npcCatalog,
            NpcLootIndex npcLootIndex,
            ItemCatalog itemCatalog,
            ItemTextIndex itemTextIndex,
            ChecklistState checklistState,
            CompendiumLocalization localization,
            Func<NpcCatalogEntry, NpcDifficultyMode, NpcBestiaryMetadataSnapshot> metadataProvider,
            Func<NpcCatalogEntry, NpcBestiaryKillStatisticsSnapshot> killStatisticsProvider,
            MerchantSourceIndex merchantSourceIndex = null,
            Func<NpcCatalogEntry, string> npcNameProvider = null,
            Func<string, string> nativeTextResolver = null)
        {
            _npcCatalog = npcCatalog ?? throw new ArgumentNullException(nameof(npcCatalog));
            _npcLootIndex = npcLootIndex;
            _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
            _killStatisticsProvider = killStatisticsProvider ??
                                      throw new ArgumentNullException(nameof(killStatisticsProvider));
            _merchantSourceIndex = merchantSourceIndex;
            _npcNameProvider = npcNameProvider;
            _nativeTextResolver = nativeTextResolver;
        }

        public bool TryGetProjection(int npcNetId, NpcDifficultyMode difficulty, out NpcDetailsProjection projection)
        {
            ValidateDifficulty(difficulty);

            if (!_npcCatalog.TryGet(npcNetId, out NpcCatalogEntry npcEntry))
            {
                projection = null;
                return false;
            }

            NpcBestiaryMetadataSnapshot metadata = _metadataProvider(npcEntry, difficulty) ??
                                                   throw new InvalidOperationException(
                                                       "NPC metadata provider returned null.");
            NpcBestiaryKillStatisticsSnapshot killStatistics = _killStatisticsProvider(npcEntry) ??
                                                               throw new InvalidOperationException(
                                                                   "NPC kill-statistics provider returned null.");
            long checklistRevision = _checklistState.Revision;
            long itemTextRevision = _itemTextIndex.Revision;
            long localizationRevision = _localization.Revision;
            bool itemRowsCurrent = _cachedProjection != null &&
                                   _cachedNpcNetId == npcNetId &&
                                   _cachedChecklistRevision == checklistRevision &&
                                   _cachedItemTextRevision == itemTextRevision &&
                                   _cachedLocalizationRevision == localizationRevision;
            bool lootRowsCurrent =
                itemRowsCurrent && _cachedDifficulty == difficulty && DynamicLootQualifiersUnchanged();
            if (lootRowsCurrent &&
                AreMetadataEquivalent(_cachedMetadata, metadata) &&
                AreKillStatisticsEquivalent(_cachedKillStatistics, killStatistics))
            {
                projection = _cachedProjection;
                return true;
            }

            List<(NpcLootCondition Condition, string Description)> dynamicQualifiers =
                lootRowsCurrent ? _cachedDynamicLootQualifiers : new();
            IReadOnlyList<NpcDetailsLootRow> lootRows = lootRowsCurrent
                ? _cachedProjection.LootRows
                : BuildLootRows(npcEntry, difficulty, dynamicQualifiers);
            IReadOnlyList<NpcDetailsMerchantStockEntry> merchantStock = itemRowsCurrent
                ? _cachedProjection.MerchantStock
                : BuildMerchantStock(npcEntry.NetId);
            NpcDetailsItemReference bannerItem = killStatistics.BannerItemId.HasValue
                ? CreateItemReference(killStatistics.BannerItemId.Value)
                : null;
            int bannerProgress = killStatistics.HasBanner
                ? killStatistics.BannerKillCount % killStatistics.KillsPerBanner
                : 0;

            projection = new NpcDetailsProjection(
                npcNetId,
                difficulty,
                metadata,
                lootRows,
                _merchantSourceIndex != null,
                merchantStock,
                killStatistics,
                bannerItem,
                bannerProgress,
                lootDataAvailable: _npcLootIndex != null);
            _cachedNpcNetId = npcNetId;
            _cachedDifficulty = difficulty;
            _cachedDynamicLootQualifiers = dynamicQualifiers;
            _cachedChecklistRevision = checklistRevision;
            _cachedItemTextRevision = itemTextRevision;
            _cachedLocalizationRevision = localizationRevision;
            _cachedKillStatistics = killStatistics;
            _cachedMetadata = metadata;
            _cachedProjection = projection;

            return true;
        }

        private IReadOnlyList<NpcDetailsLootRow> BuildLootRows(
            NpcCatalogEntry npcEntry,
            NpcDifficultyMode difficulty,
            List<(NpcLootCondition Condition, string Description)> dynamicQualifiers)
        {
            if (_npcLootIndex == null)
                return Array.Empty<NpcDetailsLootRow>();

            IReadOnlyList<NpcLootRelation> relations = _npcLootIndex.GetDropsForNpc(npcEntry.NetId);

            if (relations.Count == 0)
                return Array.Empty<NpcDetailsLootRow>();

            var rows = new List<NpcDetailsLootRow>();

            foreach (NpcLootRelation relation in relations)
            {
                if (!AppliesToDifficulty(relation, difficulty))
                    continue;

                NpcDetailsItemReference item = CreateItemReference(relation.ItemId);
                IReadOnlyList<string> descriptions = GetQualifierDescriptions(relation, difficulty, dynamicQualifiers);

                rows.Add(
                    new NpcDetailsLootRow(item, relation.StackMin, relation.StackMax, relation.DropRate, descriptions));
            }

            return rows.Count == 0 ? Array.Empty<NpcDetailsLootRow>() : rows;
        }

        private IReadOnlyList<NpcDetailsMerchantStockEntry> BuildMerchantStock(int npcNetId)
        {
            if (_merchantSourceIndex == null)
                return Array.Empty<NpcDetailsMerchantStockEntry>();

            IReadOnlyList<MerchantSourceOffer> offers = _merchantSourceIndex.GetStockForMerchant(npcNetId);

            if (offers.Count == 0)
                return Array.Empty<NpcDetailsMerchantStockEntry>();

            var stock = new List<NpcDetailsMerchantStockEntry>(offers.Count);

            foreach (MerchantSourceOffer offer in offers)
            {
                var isAlwaysAvailable = false;

                foreach (MerchantSourceVariant variant in offer.Variants)
                {
                    if (variant.Conditions.Count == 0 &&
                        variant.AvailabilityFlags == MerchantSourceAvailabilityFlags.None)
                    {
                        isAlwaysAvailable = true;
                        break;
                    }
                }

                var variants = new List<NpcDetailsMerchantVariant>();

                if (!isAlwaysAvailable)
                {
                    foreach (MerchantSourceVariant variant in offer.Variants)
                    {
                        var conditions = new List<NpcDetailsMerchantCondition>(variant.Conditions.Count);

                        foreach (MerchantSourceCondition condition in variant.Conditions)
                        {
                            string description = MerchantSourceConditionPresentation.GetDescription(
                                _localization,
                                condition,
                                ResolveMerchantConditionItemName,
                                ResolveMerchantConditionNpcName,
                                _nativeTextResolver);
                            conditions.Add(new NpcDetailsMerchantCondition(condition, description));
                        }

                        variants.Add(
                            new NpcDetailsMerchantVariant(
                                conditions,
                                (variant.AvailabilityFlags & MerchantSourceAvailabilityFlags.RandomStock) != 0,
                                (variant.AvailabilityFlags & MerchantSourceAvailabilityFlags.ShopCapacityLimited) !=
                                0));
                    }
                }

                stock.Add(
                    new NpcDetailsMerchantStockEntry(
                        CreateItemReference(offer.TargetItemId),
                        isAlwaysAvailable,
                        variants));
            }

            return stock;
        }

        private string ResolveMerchantConditionItemName(int itemId)
        {
            if (!_itemCatalog.TryGet(itemId, out ItemCatalogEntry itemEntry))
                return null;

            string name = _itemTextIndex.GetName(itemId);
            return string.IsNullOrWhiteSpace(name) ? itemEntry.Name : name;
        }

        private string ResolveMerchantConditionNpcName(int npcNetId)
        {
            if (!_npcCatalog.TryGet(npcNetId, out NpcCatalogEntry npcEntry))
                return null;

            string name = _npcNameProvider?.Invoke(npcEntry);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }

        private NpcDetailsItemReference CreateItemReference(int itemId)
        {
            if (!_itemCatalog.TryGet(itemId, out ItemCatalogEntry itemEntry))
            {
                return new NpcDetailsItemReference(
                    itemId,
                    _localization.Format(CompendiumTextKeys.Common.ItemIdInline, itemId),
                    isCollectionTracked: false,
                    isFound: false,
                    isNavigable: false);
            }

            string name = _itemTextIndex.GetName(itemId);

            if (string.IsNullOrWhiteSpace(name))
                name = itemEntry.Name;

            return new NpcDetailsItemReference(
                itemId,
                name,
                isCollectionTracked: true,
                isFound: _checklistState.IsFound(itemId),
                isNavigable: true);
        }

        private static bool AppliesToDifficulty(NpcLootRelation relation, NpcDifficultyMode difficulty)
        {
            foreach (NpcLootCondition condition in relation.Conditions)
            {
                if (!condition.AppliesToDifficulty(difficulty))
                    return false;
            }

            return true;
        }

        private bool DynamicLootQualifiersUnchanged()
        {
            foreach ((NpcLootCondition Condition, string Description) qualifier in _cachedDynamicLootQualifiers)
            {
                if (!string.Equals(
                        qualifier.Description,
                        qualifier.Condition.GetDescription(),
                        StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        private static IReadOnlyList<string> GetQualifierDescriptions(
            NpcLootRelation relation,
            NpcDifficultyMode difficulty,
            List<(NpcLootCondition Condition, string Description)> dynamicQualifiers)
        {
            if (relation.Conditions.Count == 0)
                return Array.Empty<string>();

            var descriptions = new List<string>();

            foreach (NpcLootCondition condition in relation.Conditions)
            {
                if (!condition.ShouldShowAsQualifier(difficulty))
                    continue;

                string description = condition.GetDescription();

                if (condition.DescriptionMayChange)
                    dynamicQualifiers.Add((condition, description));

                if (!string.IsNullOrWhiteSpace(description))
                    descriptions.Add(description);
            }

            return descriptions.Count == 0 ? Array.Empty<string>() : descriptions;
        }

        private static bool AreMetadataEquivalent(NpcBestiaryMetadataSnapshot left, NpcBestiaryMetadataSnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null ||
                right == null ||
                left.IsEncountered != right.IsEncountered ||
                !string.Equals(left.Name, right.Name, StringComparison.Ordinal) ||
                left.BestiaryRarityStars != right.BestiaryRarityStars ||
                left.RareSpawnRarityLevel != right.RareSpawnRarityLevel ||
                !AreStatsEquivalent(left.Stats, right.Stats) ||
                !AreSpawnConditionsEquivalent(left.SpawnConditions, right.SpawnConditions) ||
                !AreImmunitiesEquivalent(left.BaseDebuffImmunities, right.BaseDebuffImmunities))
            {
                return false;
            }

            return true;
        }

        private static bool AreKillStatisticsEquivalent(
            NpcBestiaryKillStatisticsSnapshot left,
            NpcBestiaryKillStatisticsSnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return left.HasKillCounter == right.HasKillCounter &&
                   left.SlainCount == right.SlainCount &&
                   left.BannerItemId == right.BannerItemId &&
                   left.BannerKillCount == right.BannerKillCount &&
                   left.KillsPerBanner == right.KillsPerBanner;
        }

        private static bool AreStatsEquivalent(NpcBestiaryStatsSnapshot left, NpcBestiaryStatsSnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return left.Damage == right.Damage &&
                   left.LifeMax == right.LifeMax &&
                   left.Defense == right.Defense &&
                   left.KnockbackResist.Equals(right.KnockbackResist) &&
                   left.MonetaryValue.Equals(right.MonetaryValue);
        }

        private static bool AreSpawnConditionsEquivalent(
            IReadOnlyList<NpcBestiarySpawnCondition> left,
            IReadOnlyList<NpcBestiarySpawnCondition> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index].DisplayNameKey, right[index].DisplayNameKey, StringComparison.Ordinal) ||
                    !string.Equals(left[index].DisplayName, right[index].DisplayName, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreImmunitiesEquivalent(
            IReadOnlyList<NpcBestiaryDebuffImmunity> left,
            IReadOnlyList<NpcBestiaryDebuffImmunity> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index].BuffId != right[index].BuffId ||
                    !string.Equals(left[index].Name, right[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }


        private static void ValidateDifficulty(NpcDifficultyMode difficulty)
        {
            if (difficulty is NpcDifficultyMode.Classic or NpcDifficultyMode.Expert or NpcDifficultyMode.Master)
                return;

            throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Unsupported NPC difficulty mode.");
        }
    }
}