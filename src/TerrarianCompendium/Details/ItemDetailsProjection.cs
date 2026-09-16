using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Details
{
    internal enum ItemResearchStatus
    {
        Unavailable,
        NotResearchable,
        Unresearched,
        Researched
    }

    internal sealed class ItemDetailsRecipeResultReference(int itemId, string name, bool isFound)
    {
        public int ItemId { get; } = itemId;
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public bool IsFound { get; } = isFound;
    }

    internal sealed class ItemDetailsNpcSourceReference(int npcNetId, string name)
    {
        public int NpcNetId { get; } = npcNetId;
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    }

    internal sealed class ItemDetailsArmorSetReference(int armorSetId, int representativeItemId)
    {
        public int ArmorSetId { get; } = armorSetId;
        public int RepresentativeItemId { get; } = representativeItemId;
    }

    internal sealed class ItemDetailsWorldSourceReference(
        string key,
        string displayNameKey,
        WorldLootSourceKind kind,
        int? representativeItemId)
    {
        public string Key { get; } = key ?? throw new ArgumentNullException(nameof(key));

        public string DisplayNameKey { get; } =
            displayNameKey ?? throw new ArgumentNullException(nameof(displayNameKey));

        public WorldLootSourceKind Kind { get; } = kind;
        public int? RepresentativeItemId { get; } = representativeItemId;
    }

    internal sealed class ItemDetailsOpenableSourceReference(int itemId, string name, bool isFound)
    {
        public int ItemId { get; } = itemId;
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public bool IsFound { get; } = isFound;
    }

    internal sealed class ItemDetailsOpenableContentReference(int itemId, string name, bool isFound)
    {
        public int ItemId { get; } = itemId;
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
        public bool IsFound { get; } = isFound;
    }

    internal sealed class ItemDetailsFishingVariantReference
    {
        public ItemDetailsFishingVariantReference(IEnumerable<FishingSourceConditionKind> conditions) : this(
            conditions,
            Array.Empty<FishingSourceConditionKind>(),
            excludesLavaAndHoney: false)
        {
        }

        public ItemDetailsFishingVariantReference(
            IEnumerable<FishingSourceConditionKind> conditions,
            IEnumerable<FishingSourceConditionKind> excludedConditions,
            bool excludesLavaAndHoney = false)
        {
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));
            if (excludedConditions == null)
                throw new ArgumentNullException(nameof(excludedConditions));

            Conditions = new ReadOnlyCollection<FishingSourceConditionKind>(
                new List<FishingSourceConditionKind>(conditions));
            ExcludedConditions = new ReadOnlyCollection<FishingSourceConditionKind>(
                new List<FishingSourceConditionKind>(excludedConditions));
            ExcludesLavaAndHoney = excludesLavaAndHoney;
        }

        public IReadOnlyList<FishingSourceConditionKind> Conditions { get; }
        public IReadOnlyList<FishingSourceConditionKind> ExcludedConditions { get; }
        public bool ExcludesLavaAndHoney { get; }
    }

    internal sealed class ItemDetailsProjection
    {
        public ItemDetailsProjection(
            int itemId,
            string name,
            string description,
            int value,
            int rarity,
            int damage,
            int defense,
            int pickPower,
            int axePower,
            int hammerPower,
            int fishingPower,
            ItemDetailsStats detailsStats,
            bool isFound,
            ItemResearchStatus researchStatus,
            IEnumerable<ItemDetailsArmorSetReference> armorSets,
            bool recipeDataAvailable,
            int producingRecipeCount,
            IEnumerable<ItemDetailsRecipeResultReference> usedInResults,
            bool hasRecipe,
            bool isCraftableNow,
            int? craftingStationRequiredTileId,
            bool npcLootDataAvailable,
            IEnumerable<ItemDetailsNpcSourceReference> droppedByNpcSources,
            bool worldLootDataAvailable,
            IEnumerable<ItemDetailsWorldSourceReference> worldSources,
            bool openableItemLootDataAvailable,
            IEnumerable<ItemDetailsOpenableSourceReference> openableSources,
            IEnumerable<ItemDetailsOpenableContentReference> openableContents,
            bool fishingSourceDataAvailable,
            IEnumerable<ItemDetailsFishingVariantReference> fishingVariants,
            bool merchantSourceDataAvailable = false,
            IEnumerable<ItemDetailsNpcSourceReference> purchasableFromMerchants = null)
        {
            if (armorSets == null) throw new ArgumentNullException(nameof(armorSets));
            if (usedInResults == null) throw new ArgumentNullException(nameof(usedInResults));
            if (droppedByNpcSources == null) throw new ArgumentNullException(nameof(droppedByNpcSources));
            if (worldSources == null) throw new ArgumentNullException(nameof(worldSources));
            if (openableSources == null) throw new ArgumentNullException(nameof(openableSources));
            if (openableContents == null) throw new ArgumentNullException(nameof(openableContents));
            if (fishingVariants == null) throw new ArgumentNullException(nameof(fishingVariants));

            ArmorSets = Copy(armorSets, nameof(armorSets));
            UsedInResults = Copy(usedInResults, nameof(usedInResults));
            DroppedByNpcSources = Copy(droppedByNpcSources, nameof(droppedByNpcSources));
            WorldSources = Copy(worldSources, nameof(worldSources));
            OpenableSources = Copy(openableSources, nameof(openableSources));
            OpenableContents = Copy(openableContents, nameof(openableContents));
            FishingVariants = Copy(fishingVariants, nameof(fishingVariants));
            PurchasableFromMerchants = purchasableFromMerchants == null
                ? Array.Empty<ItemDetailsNpcSourceReference>()
                : Copy(purchasableFromMerchants, nameof(purchasableFromMerchants));

            ItemId = itemId;
            Name = name;
            Description = description ?? string.Empty;
            Value = value;
            Rarity = rarity;
            Damage = damage;
            Defense = defense;
            PickPower = pickPower;
            AxePower = axePower;
            HammerPower = hammerPower;
            FishingPower = fishingPower;
            DamageType = detailsStats.DamageType;
            Knockback = detailsStats.Knockback;
            BaseCriticalHitChance = detailsStats.BaseCriticalHitChance;
            UseTimeTicks = detailsStats.UseTimeTicks;
            TagDamage = detailsStats.TagDamage;
            IsFound = isFound;
            ResearchStatus = researchStatus;
            RecipeDataAvailable = recipeDataAvailable;
            ProducingRecipeCount = producingRecipeCount;
            HasRecipe = hasRecipe;
            IsCraftableNow = isCraftableNow;
            CraftingStationRequiredTileId = craftingStationRequiredTileId;
            NpcLootDataAvailable = npcLootDataAvailable;
            WorldLootDataAvailable = worldLootDataAvailable;
            OpenableItemLootDataAvailable = openableItemLootDataAvailable;
            FishingSourceDataAvailable = fishingSourceDataAvailable;
            MerchantSourceDataAvailable = merchantSourceDataAvailable;
        }

        public int ItemId { get; }
        public string Name { get; }
        public string Description { get; }
        public int Value { get; }
        public int Rarity { get; }
        public int Damage { get; }
        public int Defense { get; }
        public int PickPower { get; }
        public int AxePower { get; }
        public int HammerPower { get; }
        public int FishingPower { get; }
        public ItemDetailsDamageType DamageType { get; }
        public float? Knockback { get; }
        public int? BaseCriticalHitChance { get; }
        public int? UseTimeTicks { get; }
        public int? TagDamage { get; }
        public bool IsFound { get; }
        public ItemResearchStatus ResearchStatus { get; }
        public IReadOnlyList<ItemDetailsArmorSetReference> ArmorSets { get; }
        public bool RecipeDataAvailable { get; }
        public int ProducingRecipeCount { get; }
        public IReadOnlyList<ItemDetailsRecipeResultReference> UsedInResults { get; }
        public bool HasRecipe { get; }
        public bool IsCraftableNow { get; }
        public int? CraftingStationRequiredTileId { get; }
        public bool NpcLootDataAvailable { get; }
        public IReadOnlyList<ItemDetailsNpcSourceReference> DroppedByNpcSources { get; }
        public bool WorldLootDataAvailable { get; }
        public IReadOnlyList<ItemDetailsWorldSourceReference> WorldSources { get; }
        public bool OpenableItemLootDataAvailable { get; }
        public IReadOnlyList<ItemDetailsOpenableSourceReference> OpenableSources { get; }
        public IReadOnlyList<ItemDetailsOpenableContentReference> OpenableContents { get; }
        public bool FishingSourceDataAvailable { get; }
        public IReadOnlyList<ItemDetailsFishingVariantReference> FishingVariants { get; }
        public bool MerchantSourceDataAvailable { get; }
        public IReadOnlyList<ItemDetailsNpcSourceReference> PurchasableFromMerchants { get; }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameterName) where T : class
        {
            var copied = new List<T>();
            foreach (T value in values)
            {
                if (value == null)
                    throw new ArgumentException("References must not contain null values.", parameterName);
                copied.Add(value);
            }

            return new ReadOnlyCollection<T>(copied);
        }
    }
}