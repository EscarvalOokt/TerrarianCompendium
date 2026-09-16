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
        private readonly CompendiumLocalization _localization;
        private readonly MerchantSourceIndex _merchantSourceIndex;
        private readonly Func<NpcCatalogEntry, NpcDifficultyMode, NpcBestiaryMetadataSnapshot> _metadataProvider;
        private readonly NpcCatalog _npcCatalog;
        private readonly NpcLootIndex _npcLootIndex;
        private readonly Func<NpcCatalogEntry, string> _npcNameProvider;

        private long _cachedChecklistRevision = -1;
        private NpcDifficultyMode _cachedDifficulty;
        private long _cachedItemTextRevision = -1;
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
            Func<NpcCatalogEntry, string> npcNameProvider = null) : this(
            npcCatalog,
            npcLootIndex,
            itemCatalog,
            itemTextIndex,
            checklistState,
            localization,
            VanillaBestiaryNativeBridge.GetMetadataSnapshot,
            merchantSourceIndex,
            npcNameProvider)
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
            MerchantSourceIndex merchantSourceIndex = null,
            Func<NpcCatalogEntry, string> npcNameProvider = null)
        {
            _npcCatalog = npcCatalog ?? throw new ArgumentNullException(nameof(npcCatalog));
            _npcLootIndex = npcLootIndex ?? throw new ArgumentNullException(nameof(npcLootIndex));
            _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _metadataProvider = metadataProvider ?? throw new ArgumentNullException(nameof(metadataProvider));
            _merchantSourceIndex = merchantSourceIndex;
            _npcNameProvider = npcNameProvider;
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
            long checklistRevision = _checklistState.Revision;
            long itemTextRevision = _itemTextIndex.Revision;
            long localizationRevision = _localization.Revision;
            IReadOnlyList<NpcDetailsLootRow> lootRows = BuildLootRows(npcEntry, difficulty);
            bool merchantSourceDataAvailable = _merchantSourceIndex != null;
            IReadOnlyList<NpcDetailsMerchantStockEntry> merchantStock = merchantSourceDataAvailable
                ? BuildMerchantStock(npcEntry.NetId)
                : Array.Empty<NpcDetailsMerchantStockEntry>();

            if (_cachedProjection != null &&
                _cachedNpcNetId == npcNetId &&
                _cachedDifficulty == difficulty &&
                _cachedChecklistRevision == checklistRevision &&
                _cachedItemTextRevision == itemTextRevision &&
                _cachedLocalizationRevision == localizationRevision &&
                AreMetadataEquivalent(_cachedMetadata, metadata) &&
                AreLootRowsEquivalent(_cachedProjection.LootRows, lootRows) &&
                _cachedProjection.MerchantSourceDataAvailable == merchantSourceDataAvailable &&
                AreMerchantStockEquivalent(_cachedProjection.MerchantStock, merchantStock))
            {
                projection = _cachedProjection;
                return true;
            }

            projection = new NpcDetailsProjection(
                npcNetId,
                difficulty,
                metadata,
                lootRows,
                merchantSourceDataAvailable,
                merchantStock);
            _cachedNpcNetId = npcNetId;
            _cachedDifficulty = difficulty;
            _cachedChecklistRevision = checklistRevision;
            _cachedItemTextRevision = itemTextRevision;
            _cachedLocalizationRevision = localizationRevision;
            _cachedMetadata = metadata;
            _cachedProjection = projection;

            return true;
        }

        private IReadOnlyList<NpcDetailsLootRow> BuildLootRows(NpcCatalogEntry npcEntry, NpcDifficultyMode difficulty)
        {
            IReadOnlyList<NpcLootRelation> relations = _npcLootIndex.GetDropsForNpc(npcEntry.NetId);

            if (relations.Count == 0)
                return Array.Empty<NpcDetailsLootRow>();

            var rows = new List<NpcDetailsLootRow>();

            foreach (NpcLootRelation relation in relations)
            {
                if (!AppliesToDifficulty(relation, difficulty))
                    continue;

                NpcDetailsItemReference item = CreateItemReference(relation.ItemId);
                IReadOnlyList<string> descriptions = GetQualifierDescriptions(relation, difficulty);

                rows.Add(
                    new NpcDetailsLootRow(item, relation.StackMin, relation.StackMax, relation.DropRate, descriptions));
            }

            return rows.Count == 0 ? Array.Empty<NpcDetailsLootRow>() : rows;
        }

        private IReadOnlyList<NpcDetailsMerchantStockEntry> BuildMerchantStock(int npcNetId)
        {
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
                        var descriptions = new List<string>(variant.Conditions.Count);

                        foreach (MerchantSourceCondition condition in variant.Conditions)
                        {
                            descriptions.Add(
                                MerchantSourceConditionPresentation.GetDescription(
                                    _localization,
                                    condition,
                                    ResolveMerchantConditionItemName,
                                    ResolveMerchantConditionNpcName));
                        }

                        variants.Add(
                            new NpcDetailsMerchantVariant(
                                descriptions,
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
                return _localization.Format(CompendiumTextKeys.Common.ItemIdInline, itemId);

            string name = _itemTextIndex.GetName(itemId);
            return string.IsNullOrWhiteSpace(name) ? itemEntry.Name : name;
        }

        private string ResolveMerchantConditionNpcName(int npcNetId)
        {
            if (!_npcCatalog.TryGet(npcNetId, out NpcCatalogEntry npcEntry))
                return _localization.Format(CompendiumTextKeys.Common.NpcIdInline, npcNetId);

            string name = _npcNameProvider?.Invoke(npcEntry);
            return string.IsNullOrWhiteSpace(name)
                ? _localization.Format(CompendiumTextKeys.Common.NpcIdInline, npcNetId)
                : name;
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

        private static IReadOnlyList<string> GetQualifierDescriptions(
            NpcLootRelation relation,
            NpcDifficultyMode difficulty)
        {
            if (relation.Conditions.Count == 0)
                return Array.Empty<string>();

            var descriptions = new List<string>();

            foreach (NpcLootCondition condition in relation.Conditions)
            {
                if (!condition.ShouldShowAsQualifier(difficulty))
                    continue;

                string description = condition.GetDescription();

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

        private static bool AreLootRowsEquivalent(
            IReadOnlyList<NpcDetailsLootRow> left,
            IReadOnlyList<NpcDetailsLootRow> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                NpcDetailsLootRow leftRow = left[index];
                NpcDetailsLootRow rightRow = right[index];

                if (leftRow.StackMin != rightRow.StackMin ||
                    leftRow.StackMax != rightRow.StackMax ||
                    !leftRow.DropRate.Equals(rightRow.DropRate) ||
                    !AreItemsEquivalent(leftRow.Item, rightRow.Item) ||
                    !AreStringsEquivalent(leftRow.ConditionDescriptions, rightRow.ConditionDescriptions))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreMerchantStockEquivalent(
            IReadOnlyList<NpcDetailsMerchantStockEntry> left,
            IReadOnlyList<NpcDetailsMerchantStockEntry> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                NpcDetailsMerchantStockEntry leftEntry = left[index];
                NpcDetailsMerchantStockEntry rightEntry = right[index];

                if (leftEntry.IsAlwaysAvailable != rightEntry.IsAlwaysAvailable ||
                    !AreItemsEquivalent(leftEntry.Item, rightEntry.Item) ||
                    leftEntry.Variants.Count != rightEntry.Variants.Count)
                {
                    return false;
                }

                for (var variantIndex = 0; variantIndex < leftEntry.Variants.Count; variantIndex++)
                {
                    NpcDetailsMerchantVariant leftVariant = leftEntry.Variants[variantIndex];
                    NpcDetailsMerchantVariant rightVariant = rightEntry.Variants[variantIndex];

                    if (leftVariant.RandomStock != rightVariant.RandomStock ||
                        leftVariant.ShopCapacityLimited != rightVariant.ShopCapacityLimited ||
                        !AreStringsEquivalent(leftVariant.ConditionDescriptions, rightVariant.ConditionDescriptions))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool AreItemsEquivalent(NpcDetailsItemReference left, NpcDetailsItemReference right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if (left == null || right == null)
                return false;

            return left.ItemId == right.ItemId &&
                   string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
                   left.IsCollectionTracked == right.IsCollectionTracked &&
                   left.IsFound == right.IsFound &&
                   left.IsNavigable == right.IsNavigable;
        }

        private static bool AreStringsEquivalent(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                    return false;
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