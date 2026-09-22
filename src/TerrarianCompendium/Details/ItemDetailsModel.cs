using System;
using System.Collections.Generic;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.ArmorSets;
using TerrarianCompendium.Bestiary;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Details
{
    internal sealed class ItemDetailsModel
    {
        private readonly ArmorSetCatalog _armorSetCatalog;
        private readonly ArmorSetIndex _armorSetIndex;
        private readonly ItemCatalog _catalog;
        private readonly ChecklistState _checklistState;
        private readonly CraftingAvailabilityState _craftingAvailabilityState;
        private readonly FishingSourceIndex _fishingSourceIndex;
        private readonly ItemTextIndex _itemTextIndex;
        private readonly JourneyResearchState _journeyResearchState;
        private readonly MerchantSourceIndex _merchantSourceIndex;
        private readonly NpcCatalog _npcCatalog;
        private readonly NpcLootIndex _npcLootIndex;
        private readonly Func<NpcCatalogEntry, string> _npcNameProvider;
        private readonly OpenableItemLootIndex _openableItemLootIndex;
        private readonly RecipeIndex _recipeIndex;
        private readonly RecipeStationDisplayIndex _recipeStationDisplayIndex;
        private readonly WorldLootSourceIndex _worldLootSourceIndex;

        private long _cachedChecklistRevision = -1;
        private long _cachedCraftingRevision = -1;
        private int _cachedItemId;
        private long _cachedItemTextRevision = -1;
        private ItemDetailsProjection _cachedProjection;
        private long _cachedResearchRevision = -1;

        public ItemDetailsModel(
            ItemCatalog catalog,
            ChecklistState checklistState,
            ItemTextIndex itemTextIndex,
            JourneyResearchState journeyResearchState = null,
            RecipeIndex recipeIndex = null,
            CraftingAvailabilityState craftingAvailabilityState = null,
            RecipeStationDisplayIndex recipeStationDisplayIndex = null,
            NpcCatalog npcCatalog = null,
            NpcLootIndex npcLootIndex = null,
            Func<NpcCatalogEntry, string> npcNameProvider = null,
            ArmorSetCatalog armorSetCatalog = null,
            ArmorSetIndex armorSetIndex = null,
            WorldLootSourceIndex worldLootSourceIndex = null,
            OpenableItemLootIndex openableItemLootIndex = null,
            FishingSourceIndex fishingSourceIndex = null,
            MerchantSourceIndex merchantSourceIndex = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _checklistState = checklistState ?? throw new ArgumentNullException(nameof(checklistState));
            _itemTextIndex = itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));
            _journeyResearchState = journeyResearchState;
            _recipeIndex = recipeIndex;
            _craftingAvailabilityState = craftingAvailabilityState;
            _recipeStationDisplayIndex = recipeStationDisplayIndex;

            bool hasNpcRelationIndex = npcLootIndex != null || merchantSourceIndex != null;

            if (hasNpcRelationIndex && npcCatalog == null)
            {
                throw new ArgumentException(
                    "NPC catalog is required when NPC loot or merchant source data is provided.");
            }

            if (npcCatalog != null && !hasNpcRelationIndex)
            {
                throw new ArgumentException("NPC catalog requires at least one NPC relation index.");
            }

            if ((armorSetCatalog == null) != (armorSetIndex == null))
            {
                throw new ArgumentException(
                    "Armor-set catalog and armor-set index must either both be provided or both be omitted.");
            }

            _npcCatalog = npcCatalog;
            _npcLootIndex = npcLootIndex;
            _merchantSourceIndex = merchantSourceIndex;
            _armorSetCatalog = armorSetCatalog;
            _armorSetIndex = armorSetIndex;
            _worldLootSourceIndex = worldLootSourceIndex;
            _openableItemLootIndex = openableItemLootIndex;
            _fishingSourceIndex = fishingSourceIndex;

            if (_npcCatalog != null)
            {
                _npcNameProvider = npcNameProvider ??
                                   throw new ArgumentNullException(
                                       nameof(npcNameProvider),
                                       "NPC name provider is required when NPC relation data is provided.");
            }
        }

        public bool TryGetProjection(int itemId, out ItemDetailsProjection projection)
        {
            if (!_catalog.TryGet(itemId, out ItemCatalogEntry entry))
            {
                projection = null;
                return false;
            }

            long checklistRevision = _checklistState.Revision;
            long researchRevision = _journeyResearchState?.Revision ?? -1;
            long craftingRevision = _craftingAvailabilityState?.Revision ?? -1;
            long itemTextRevision = _itemTextIndex.Revision;
            bool npcLootDataAvailable = _npcCatalog != null && _npcLootIndex != null;
            IReadOnlyList<ItemDetailsNpcSourceReference> droppedByNpcSources = npcLootDataAvailable
                ? BuildDroppedByNpcSources(itemId)
                : [];
            bool merchantSourceDataAvailable = _npcCatalog != null && _merchantSourceIndex != null;
            IReadOnlyList<ItemDetailsNpcSourceReference> purchasableFromMerchants = merchantSourceDataAvailable
                ? BuildPurchasableMerchantSources(itemId)
                : [];
            bool worldLootDataAvailable = _worldLootSourceIndex != null;
            IReadOnlyList<ItemDetailsWorldSourceReference> worldSources = worldLootDataAvailable
                ? BuildWorldSources(itemId)
                : [];
            bool openableItemLootDataAvailable = _openableItemLootIndex != null;
            IReadOnlyList<ItemDetailsOpenableSourceReference> openableSources = openableItemLootDataAvailable
                ? BuildOpenableSources(itemId)
                : [];
            IReadOnlyList<ItemDetailsOpenableContentReference> openableContents = openableItemLootDataAvailable
                ? BuildOpenableContents(itemId)
                : [];
            bool fishingSourceDataAvailable = _fishingSourceIndex != null;
            IReadOnlyList<ItemDetailsFishingVariantReference> fishingVariants = fishingSourceDataAvailable
                ? BuildFishingVariants(itemId)
                : [];

            if (_cachedProjection != null &&
                _cachedItemId == itemId &&
                _cachedChecklistRevision == checklistRevision &&
                _cachedResearchRevision == researchRevision &&
                _cachedCraftingRevision == craftingRevision &&
                _cachedItemTextRevision == itemTextRevision &&
                _cachedProjection.NpcLootDataAvailable == npcLootDataAvailable &&
                AreNpcSourcesEquivalent(_cachedProjection.DroppedByNpcSources, droppedByNpcSources) &&
                _cachedProjection.MerchantSourceDataAvailable == merchantSourceDataAvailable &&
                AreNpcSourcesEquivalent(_cachedProjection.PurchasableFromMerchants, purchasableFromMerchants) &&
                _cachedProjection.WorldLootDataAvailable == worldLootDataAvailable &&
                _cachedProjection.OpenableItemLootDataAvailable == openableItemLootDataAvailable &&
                AreOpenableSourcesEquivalent(_cachedProjection.OpenableSources, openableSources) &&
                AreOpenableContentsEquivalent(_cachedProjection.OpenableContents, openableContents) &&
                _cachedProjection.FishingSourceDataAvailable == fishingSourceDataAvailable &&
                AreFishingVariantsEquivalent(_cachedProjection.FishingVariants, fishingVariants))
            {
                projection = _cachedProjection;
                return true;
            }

            ItemSortMetrics metrics = entry.SortMetrics;
            ItemDetailsStats detailsStats = entry.DetailsStats;
            ItemResearchStatus researchStatus = GetResearchStatus(itemId);
            IReadOnlyList<ItemDetailsArmorSetReference> armorSets = BuildArmorSetReferences(itemId);
            bool recipeDataAvailable = _recipeIndex != null;
            var producingRecipeCount = 0;
            var hasRecipeRelations = false;
            var hasRecipe = false;
            int? craftingStationRequiredTileId = null;

            if (recipeDataAvailable)
            {
                IReadOnlyList<RecipeCatalogEntry> producingRecipes = _recipeIndex.GetRecipesProducing(itemId);
                producingRecipeCount = producingRecipes.Count;
                hasRecipe = producingRecipeCount > 0;
                hasRecipeRelations = hasRecipe || _recipeIndex.GetRecipesUsing(itemId).Count > 0;
            }

            if (_recipeStationDisplayIndex != null &&
                _recipeStationDisplayIndex.TryGetRequiredTileIdForItem(itemId, out int requiredTileId))
            {
                craftingStationRequiredTileId = requiredTileId;
            }

            bool craftabilityAvailable = _recipeIndex != null && _craftingAvailabilityState != null;
            bool isCraftableNow = craftabilityAvailable &&
                                  ItemCraftingAvailability.IsCraftableNow(
                                      itemId,
                                      _recipeIndex,
                                      _craftingAvailabilityState);

            projection = new ItemDetailsProjection(
                entry.Id,
                _itemTextIndex.GetName(entry.Id),
                _itemTextIndex.GetDescription(entry.Id),
                metrics.Value,
                metrics.Rarity,
                metrics.Damage,
                metrics.Defense,
                metrics.PickPower,
                metrics.AxePower,
                metrics.HammerPower,
                metrics.FishingPower,
                detailsStats,
                _checklistState.IsFound(itemId),
                researchStatus,
                armorSets,
                recipeDataAvailable,
                producingRecipeCount,
                hasRecipeRelations,
                hasRecipe,
                isCraftableNow,
                craftingStationRequiredTileId,
                npcLootDataAvailable,
                droppedByNpcSources,
                worldLootDataAvailable,
                worldSources,
                openableItemLootDataAvailable,
                openableSources,
                openableContents,
                fishingSourceDataAvailable,
                fishingVariants,
                merchantSourceDataAvailable,
                purchasableFromMerchants);

            _cachedItemId = itemId;
            _cachedProjection = projection;
            _cachedChecklistRevision = checklistRevision;
            _cachedResearchRevision = researchRevision;
            _cachedCraftingRevision = craftingRevision;
            _cachedItemTextRevision = itemTextRevision;

            return true;
        }

        private IReadOnlyList<ItemDetailsArmorSetReference> BuildArmorSetReferences(int itemId)
        {
            if (_armorSetCatalog == null || _armorSetIndex == null)
                return [];

            IReadOnlyList<ArmorSetCatalogEntry> entries = _armorSetIndex.GetArmorSetsForItem(itemId);

            if (entries.Count == 0)
                return [];

            var references = new List<ItemDetailsArmorSetReference>(entries.Count);

            foreach (ArmorSetCatalogEntry entry in entries)
            {
                if (!_armorSetCatalog.Contains(entry.Id))
                {
                    throw new InvalidOperationException(
                        $"Armor-set index references catalog-missing armor-set ID {entry.Id}.");
                }

                references.Add(new ItemDetailsArmorSetReference(entry.Id, entry.RepresentativeItemId));
            }

            return references;
        }

        private IReadOnlyList<ItemDetailsNpcSourceReference> BuildDroppedByNpcSources(int itemId)
        {
            IReadOnlyList<NpcLootRelation> relations = _npcLootIndex.GetNpcSourcesForItem(itemId);

            if (relations.Count == 0)
                return [];

            var npcNetIds = new List<int>(relations.Count);

            foreach (NpcLootRelation relation in relations)
                npcNetIds.Add(relation.NpcNetId);

            return BuildNpcSources(npcNetIds, "NPC loot relation");
        }

        private IReadOnlyList<ItemDetailsNpcSourceReference> BuildPurchasableMerchantSources(int itemId)
        {
            IReadOnlyList<MerchantSourceOffer> offers = _merchantSourceIndex.GetOffersForItem(itemId);

            if (offers.Count == 0)
                return [];

            var npcNetIds = new List<int>(offers.Count);

            foreach (MerchantSourceOffer offer in offers)
                npcNetIds.Add(offer.MerchantNpcId);

            return BuildNpcSources(npcNetIds, "Merchant source relation");
        }

        private IReadOnlyList<ItemDetailsNpcSourceReference> BuildNpcSources(
            IEnumerable<int> npcNetIds,
            string relationDescription)
        {
            var seenNpcNetIds = new HashSet<int>();
            var sources = new List<(NpcCatalogEntry Entry, ItemDetailsNpcSourceReference Reference)>();

            foreach (int npcNetId in npcNetIds)
            {
                if (!_npcCatalog.TryGet(npcNetId, out NpcCatalogEntry npcEntry))
                {
                    throw new InvalidOperationException(
                        $"{relationDescription} references catalog-missing NPC net ID {npcNetId}.");
                }

                if (!seenNpcNetIds.Add(npcEntry.NetId))
                    continue;

                sources.Add(
                    (npcEntry,
                        new ItemDetailsNpcSourceReference(npcEntry.NetId, _npcNameProvider(npcEntry) ?? string.Empty)));
            }

            if (sources.Count == 0)
                return [];

            sources.Sort((left, right) => left.Entry.BestiaryOrder.CompareTo(right.Entry.BestiaryOrder));

            var result = new List<ItemDetailsNpcSourceReference>(sources.Count);

            foreach ((NpcCatalogEntry Entry, ItemDetailsNpcSourceReference Reference) source in sources)
                result.Add(source.Reference);

            return result;
        }

        private IReadOnlyList<ItemDetailsWorldSourceReference> BuildWorldSources(int itemId)
        {
            IReadOnlyList<WorldLootSource> sources = _worldLootSourceIndex.GetSourcesForItem(itemId);
            if (sources.Count == 0)
                return [];

            var result = new List<ItemDetailsWorldSourceReference>(sources.Count);
            foreach (WorldLootSource source in sources)
            {
                result.Add(
                    new ItemDetailsWorldSourceReference(
                        source.Key,
                        source.DisplayNameKey,
                        source.Kind,
                        source.RepresentativeItemId));
            }

            return result;
        }

        private IReadOnlyList<ItemDetailsOpenableSourceReference> BuildOpenableSources(int itemId)
        {
            IReadOnlyList<int> sourceItemIds = _openableItemLootIndex.GetSourcesForItem(itemId);
            if (sourceItemIds.Count == 0)
                return [];

            var result = new List<ItemDetailsOpenableSourceReference>(sourceItemIds.Count);
            foreach (int sourceItemId in sourceItemIds)
            {
                if (!_catalog.Contains(sourceItemId))
                    continue;

                result.Add(
                    new ItemDetailsOpenableSourceReference(
                        sourceItemId,
                        _itemTextIndex.GetName(sourceItemId),
                        _checklistState.IsFound(sourceItemId)));
            }

            return result;
        }

        private IReadOnlyList<ItemDetailsOpenableContentReference> BuildOpenableContents(int itemId)
        {
            IReadOnlyList<int> targetItemIds = _openableItemLootIndex.GetContentsForItem(itemId);
            if (targetItemIds.Count == 0)
                return [];

            var result = new List<ItemDetailsOpenableContentReference>(targetItemIds.Count);
            foreach (int targetItemId in targetItemIds)
            {
                if (!_catalog.Contains(targetItemId))
                    continue;

                result.Add(
                    new ItemDetailsOpenableContentReference(
                        targetItemId,
                        _itemTextIndex.GetName(targetItemId),
                        _checklistState.IsFound(targetItemId)));
            }

            return result;
        }

        private IReadOnlyList<ItemDetailsFishingVariantReference> BuildFishingVariants(int itemId)
        {
            IReadOnlyList<FishingSourceVariant> variants = _fishingSourceIndex.GetVariantsForItem(itemId);
            if (variants.Count == 0)
                return [];

            var result = new List<ItemDetailsFishingVariantReference>(variants.Count);
            foreach (FishingSourceVariant variant in variants)
            {
                NormalizeFishingPresentationExclusions(
                    variant.Conditions,
                    variant.ExcludedConditions,
                    out IReadOnlyList<FishingSourceConditionKind> excludedConditions,
                    out bool excludesLavaAndHoney);

                IReadOnlyList<IReadOnlyList<FishingSourceConditionKind>> presentationVariants =
                    ExpandFishingVariantConditions(variant.Conditions);

                foreach (IReadOnlyList<FishingSourceConditionKind> conditions in presentationVariants)
                {
                    if (!ContainsEquivalentFishingVariant(result, conditions, excludedConditions, excludesLavaAndHoney))
                    {
                        result.Add(
                            new ItemDetailsFishingVariantReference(
                                conditions,
                                excludedConditions,
                                excludesLavaAndHoney));
                    }
                }
            }

            return result;
        }

        private static void NormalizeFishingPresentationExclusions(
            IReadOnlyList<FishingSourceConditionKind> conditions,
            IReadOnlyList<FishingSourceConditionKind> excludedConditions,
            out IReadOnlyList<FishingSourceConditionKind> normalizedExcludedConditions,
            out bool excludesLavaAndHoney)
        {
            bool requiresHoney = ContainsFishingCondition(conditions, FishingSourceConditionKind.InHoney);
            bool excludesLava = ContainsFishingCondition(excludedConditions, FishingSourceConditionKind.InLava);
            bool excludesHoney = ContainsFishingCondition(excludedConditions, FishingSourceConditionKind.InHoney);

            excludesLavaAndHoney = !requiresHoney && excludesLava && excludesHoney;
            var normalized = new List<FishingSourceConditionKind>(excludedConditions.Count);

            foreach (FishingSourceConditionKind condition in excludedConditions)
            {
                if (requiresHoney && condition == FishingSourceConditionKind.InLava)
                    continue;

                if (excludesLavaAndHoney &&
                    condition is FishingSourceConditionKind.InLava or FishingSourceConditionKind.InHoney)
                {
                    continue;
                }

                normalized.Add(condition);
            }

            normalizedExcludedConditions = normalized;
        }

        private static bool ContainsFishingCondition(
            IReadOnlyList<FishingSourceConditionKind> conditions,
            FishingSourceConditionKind expected)
        {
            for (var index = 0; index < conditions.Count; index++)
            {
                if (conditions[index] == expected)
                    return true;
            }

            return false;
        }

        private static bool ContainsEquivalentFishingVariant(
            IReadOnlyList<ItemDetailsFishingVariantReference> variants,
            IReadOnlyList<FishingSourceConditionKind> conditions,
            IReadOnlyList<FishingSourceConditionKind> excludedConditions,
            bool excludesLavaAndHoney)
        {
            foreach (ItemDetailsFishingVariantReference variant in variants)
            {
                if (variant.ExcludesLavaAndHoney == excludesLavaAndHoney &&
                    AreFishingConditionListsEqual(variant.Conditions, conditions) &&
                    AreFishingConditionListsEqual(variant.ExcludedConditions, excludedConditions))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<IReadOnlyList<FishingSourceConditionKind>> ExpandFishingVariantConditions(
            IReadOnlyList<FishingSourceConditionKind> conditions)
        {
            var combinations = new List<List<FishingSourceConditionKind>>
            {
                new()
            };

            foreach (FishingSourceConditionKind condition in conditions)
            {
                switch (condition)
                {
                    case FishingSourceConditionKind.Height1And2:
                        ExpandFishingConditionAlternative(
                            combinations,
                            FishingSourceConditionKind.Height1,
                            FishingSourceConditionKind.Height2);
                        break;
                    case FishingSourceConditionKind.Ocean:
                        ExpandFishingConditionAlternative(
                            combinations,
                            FishingSourceConditionKind.OriginalOcean,
                            FishingSourceConditionKind.RemixOcean);
                        break;
                    default:
                        foreach (List<FishingSourceConditionKind> combination in combinations)
                            combination.Add(condition);
                        break;
                }
            }

            var result = new List<IReadOnlyList<FishingSourceConditionKind>>(combinations.Count);
            foreach (List<FishingSourceConditionKind> combination in combinations)
            {
                var unique = new HashSet<FishingSourceConditionKind>(combination);
                var ordered = new List<FishingSourceConditionKind>(unique);
                ordered.Sort();
                result.Add(ordered);
            }

            return result;
        }

        private static void ExpandFishingConditionAlternative(
            List<List<FishingSourceConditionKind>> combinations,
            FishingSourceConditionKind first,
            FishingSourceConditionKind second)
        {
            var expanded = new List<List<FishingSourceConditionKind>>(combinations.Count * 2);

            foreach (List<FishingSourceConditionKind> combination in combinations)
            {
                expanded.Add([..combination, first]);
                expanded.Add([..combination, second]);
            }

            combinations.Clear();
            combinations.AddRange(expanded);
        }

        private ItemResearchStatus GetResearchStatus(int itemId)
        {
            if (_journeyResearchState == null)
                return ItemResearchStatus.Unavailable;

            if (!_journeyResearchState.IsResearchable(itemId))
                return ItemResearchStatus.NotResearchable;

            return _journeyResearchState.IsFullyResearched(itemId)
                ? ItemResearchStatus.Researched
                : ItemResearchStatus.Unresearched;
        }

        private static bool AreNpcSourcesEquivalent(
            IReadOnlyList<ItemDetailsNpcSourceReference> left,
            IReadOnlyList<ItemDetailsNpcSourceReference> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index].NpcNetId != right[index].NpcNetId ||
                    !string.Equals(left[index].Name, right[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOpenableSourcesEquivalent(
            IReadOnlyList<ItemDetailsOpenableSourceReference> left,
            IReadOnlyList<ItemDetailsOpenableSourceReference> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index].ItemId != right[index].ItemId ||
                    left[index].IsFound != right[index].IsFound ||
                    !string.Equals(left[index].Name, right[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreOpenableContentsEquivalent(
            IReadOnlyList<ItemDetailsOpenableContentReference> left,
            IReadOnlyList<ItemDetailsOpenableContentReference> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index].ItemId != right[index].ItemId ||
                    left[index].IsFound != right[index].IsFound ||
                    !string.Equals(left[index].Name, right[index].Name, StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreFishingVariantsEquivalent(
            IReadOnlyList<ItemDetailsFishingVariantReference> left,
            IReadOnlyList<ItemDetailsFishingVariantReference> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var variantIndex = 0; variantIndex < left.Count; variantIndex++)
            {
                if (left[variantIndex].ExcludesLavaAndHoney != right[variantIndex].ExcludesLavaAndHoney ||
                    !AreFishingConditionListsEqual(left[variantIndex].Conditions, right[variantIndex].Conditions) ||
                    !AreFishingConditionListsEqual(
                        left[variantIndex].ExcludedConditions,
                        right[variantIndex].ExcludedConditions))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AreFishingConditionListsEqual(
            IReadOnlyList<FishingSourceConditionKind> left,
            IReadOnlyList<FishingSourceConditionKind> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }
    }
}