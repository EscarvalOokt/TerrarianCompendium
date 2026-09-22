using System;
using System.Collections.Generic;
using System.Reflection;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Localization
{
    internal static class CompendiumTextKeys
    {
        public static IEnumerable<string> EnumerateExpectedKeys()
        {
            foreach (string key in EnumerateConstantKeys(typeof(CompendiumTextKeys)))
                yield return key;

            foreach (ItemNavigationNodeId id in Enum.GetValues(typeof(ItemNavigationNodeId)))
            {
                if (id != ItemNavigationNodeId.None)
                    yield return Taxonomy.Navigation(id);
            }

            foreach (ItemTaxonomyFacetId id in Enum.GetValues(typeof(ItemTaxonomyFacetId)))
            {
                if (id != ItemTaxonomyFacetId.None)
                    yield return Taxonomy.Facet(id);
            }

            foreach (FishingSourceConditionKind kind in Enum.GetValues(typeof(FishingSourceConditionKind)))
                yield return Fishing.Condition(kind);

            foreach (MerchantSourceConditionKind kind in Enum.GetValues(typeof(MerchantSourceConditionKind)))
            {
                yield return Merchant.Condition(kind, negated: false);
                yield return Merchant.Condition(kind, negated: true);
            }
        }

        private static IEnumerable<string> EnumerateConstantKeys(Type type)
        {
            foreach (Type nested in type.GetNestedTypes())
            {
                foreach (FieldInfo field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                        yield return (string)field.GetRawConstantValue();
                }
            }
        }

        public static class Common
        {
            public const string AltClick = "Common.AltClick";
            public const string Back = "Common.Back";
            public const string Forward = "Common.Forward";
            public const string Close = "Common.Close";
            public const string Filters = "Common.Filters";
            public const string FiltersCount = "Common.FiltersCount";
            public const string Overall = "Common.Overall";
            public const string Filtered = "Common.Filtered";
            public const string Ascending = "Common.Ascending";
            public const string Descending = "Common.Descending";
            public const string Yes = "Common.Yes";
            public const string No = "Common.No";
            public const string None = "Common.None";
            public const string OtherFor = "Common.OtherFor";
            public const string UpTo = "Common.UpTo";
            public const string Progress = "Common.Progress";
            public const string SearchPlaceholder = "Common.SearchPlaceholder";
            public const string SortButton = "Common.SortButton";
            public const string SortBy = "Common.SortBy";
            public const string SortNativeAscending = "Common.SortNativeAscending";
            public const string SortNativeDescending = "Common.SortNativeDescending";
            public const string SortNameAscending = "Common.SortNameAscending";
            public const string SortNameDescending = "Common.SortNameDescending";
            public const string SortNumericAscending = "Common.SortNumericAscending";
            public const string SortNumericDescending = "Common.SortNumericDescending";
            public const string ItemId = "Common.ItemId";
            public const string ItemIdInline = "Common.ItemIdInline";
            public const string ItemIdHash = "Common.ItemIdHash";
            public const string NpcId = "Common.NpcId";
            public const string On = "Common.On";
            public const string Off = "Common.Off";
            public const string Include = "Common.Include";
            public const string Exclude = "Common.Exclude";
            public const string StateSuffix = "Common.StateSuffix";
            public const string Or = "Common.Or";
            public const string And = "Common.And";
            public const string Stack = "Common.Stack";
            public const string StackRange = "Common.StackRange";
            public const string Percentage = "Common.Percentage";
        }

        public static class Browser
        {
            public const string ItemsSection = "Browser.Section.Items";
            public const string ArmorSetsSection = "Browser.Section.ArmorSets";
            public const string RecipesSection = "Browser.Section.Recipes";
            public const string BestiarySection = "Browser.Section.Bestiary";
            public const string Details = "Browser.Details";
            public const string NoSelection = "Browser.NoSelection";
            public const string ArmorSetsUnavailable = "Browser.ArmorSetsUnavailable";
            public const string RecipesUnavailable = "Browser.RecipesUnavailable";
            public const string BestiaryUnavailable = "Browser.BestiaryUnavailable";
            public const string ArmorSetDetailsUnavailable = "Browser.ArmorSetDetailsUnavailable";
            public const string RecipeDetailsUnavailable = "Browser.RecipeDetailsUnavailable";
            public const string NpcDetailsUnavailable = "Browser.NpcDetailsUnavailable";
        }

        public static class Items
        {
            public const string EmptyState = "Items.EmptyState";
            public const string ClearFilters = "Items.ClearFilters";
            public const string FiltersTooltip = "Items.FiltersTooltip";
            public const string SortTooltip = "Items.SortTooltip";
            public const string SearchDescriptions = "Items.SearchDescriptions";
            public const string Completion = "Items.Filter.Completion";
            public const string Missing = "Items.Filter.Missing";
            public const string Found = "Items.Filter.Found";
            public const string Research = "Items.Filter.Research";
            public const string Unresearched = "Items.Filter.Unresearched";
            public const string Researched = "Items.Filter.Researched";
            public const string Crafting = "Items.Filter.Crafting";
            public const string HasRecipe = "Items.Filter.HasRecipe";
            public const string HasRecipeTooltip = "Items.Filter.HasRecipeTooltip";
            public const string CraftableNow = "Items.Filter.CraftableNow";
            public const string CraftableNowTooltip = "Items.Filter.CraftableNowTooltip";
            public const string AcquisitionSection = "Items.Filter.Acquisition";
            public const string NpcDrops = "Items.Filter.NpcDrops";
            public const string NpcDropsTooltip = "Items.Filter.NpcDropsTooltip";
            public const string Purchasable = "Items.Filter.Purchasable";
            public const string PurchasableTooltip = "Items.Filter.PurchasableTooltip";
            public const string Tags = "Items.Filter.Tags";
            public const string SortNative = "Items.Sort.Native";
            public const string SortItemId = "Items.Sort.ItemId";
            public const string SortName = "Items.Sort.Name";
            public const string SortValue = "Items.Sort.Value";
            public const string SortRarity = "Items.Sort.Rarity";
            public const string SortDamage = "Items.Sort.Damage";
            public const string SortDefense = "Items.Sort.Defense";
            public const string SortPickPower = "Items.Sort.PickPower";
            public const string SortAxePower = "Items.Sort.AxePower";
            public const string SortHammerPower = "Items.Sort.HammerPower";
            public const string SortFishingPower = "Items.Sort.FishingPower";
        }

        public static class ItemDetails
        {
            public const string MissingItem = "ItemDetails.MissingItem";
            public const string Stats = "ItemDetails.Stats";
            public const string Description = "ItemDetails.Description";
            public const string ArmorSetsSection = "ItemDetails.ArmorSets";
            public const string Sources = "ItemDetails.Sources";
            public const string PossibleDrops = "ItemDetails.PossibleDrops";
            public const string Crafting = "ItemDetails.Crafting";
            public const string NpcDrops = "ItemDetails.NpcDrops";
            public const string Purchasable = "ItemDetails.Purchasable";
            public const string WorldSources = "ItemDetails.WorldSources";
            public const string OpenableItems = "ItemDetails.OpenableItems";
            public const string FishingSection = "ItemDetails.Fishing";
            public const string RecipesSection = "ItemDetails.Recipes";
            public const string CraftingStation = "ItemDetails.CraftingStation";
            public const string StationRecipesTooltip = "ItemDetails.StationRecipesTooltip";
            public const string BaseValueTooltip = "ItemDetails.BaseValueTooltip";
            public const string Unresearched = "ItemDetails.Unresearched";
            public const string Researched = "ItemDetails.Researched";
            public const string ResearchDuplicateStackHint = "ItemDetails.ResearchDuplicateStackHint";
            public const string ResearchDuplicateSingleHint = "ItemDetails.ResearchDuplicateSingleHint";
        }

        public static class ItemStats
        {
            public const string Rarity = "ItemStats.Rarity";
            public const string Damage = "ItemStats.Damage";
            public const string DamageTyped = "ItemStats.DamageTyped";
            public const string Defense = "ItemStats.Defense";
            public const string Knockback = "ItemStats.Knockback";
            public const string CriticalHit = "ItemStats.CriticalHit";
            public const string CriticalHitTooltip = "ItemStats.CriticalHitTooltip";
            public const string UseTime = "ItemStats.UseTime";
            public const string UseTimeTooltip = "ItemStats.UseTimeTooltip";
            public const string TagDamage = "ItemStats.TagDamage";
            public const string PickPower = "ItemStats.PickPower";
            public const string AxePower = "ItemStats.AxePower";
            public const string HammerPower = "ItemStats.HammerPower";
            public const string FishingPower = "ItemStats.FishingPower";
            public const string KnockbackNone = "ItemStats.Knockback.None";
            public const string KnockbackExtremelyWeak = "ItemStats.Knockback.ExtremelyWeak";
            public const string KnockbackVeryWeak = "ItemStats.Knockback.VeryWeak";
            public const string KnockbackWeak = "ItemStats.Knockback.Weak";
            public const string KnockbackAverage = "ItemStats.Knockback.Average";
            public const string KnockbackStrong = "ItemStats.Knockback.Strong";
            public const string KnockbackVeryStrong = "ItemStats.Knockback.VeryStrong";
            public const string KnockbackExtremelyStrong = "ItemStats.Knockback.ExtremelyStrong";
            public const string KnockbackInsane = "ItemStats.Knockback.Insane";
            public const string DamageMelee = "ItemStats.DamageType.Melee";
            public const string DamageRanged = "ItemStats.DamageType.Ranged";
            public const string DamageMagic = "ItemStats.DamageType.Magic";
            public const string DamageSummon = "ItemStats.DamageType.Summon";
            public const string DamageGeneric = "ItemStats.DamageType.Generic";
        }

        public static class ArmorSets
        {
            public const string EmptyState = "ArmorSets.EmptyState";
            public const string Missing = "ArmorSets.Missing";
            public const string SetBonus = "ArmorSets.SetBonus";
            public const string Head = "ArmorSets.Head";
            public const string Body = "ArmorSets.Body";
            public const string Legs = "ArmorSets.Legs";
            public const string Variants = "ArmorSets.Variants";
            public const string Variant = "ArmorSets.Variant";
            public const string ArmorSet = "ArmorSets.ArmorSet";
            public const string SetId = "ArmorSets.SetId";
            public const string RoleVariants = "ArmorSets.RoleVariants";
            public const string NoBonusDescription = "ArmorSets.NoBonusDescription";
        }

        public static class Recipes
        {
            public const string EmptyState = "Recipes.EmptyState";
            public const string ShowFavorites = "Recipes.ShowFavorites";
            public const string ShowAll = "Recipes.ShowAll";
            public const string ClearFilters = "Recipes.ClearFilters";
            public const string FiltersTooltip = "Recipes.FiltersTooltip";
            public const string ByHand = "Recipes.Filter.ByHand";
            public const string ByHandTooltip = "Recipes.Filter.ByHandTooltip";
            public const string CraftableNow = "Recipes.Filter.CraftableNow";
            public const string CraftableNowTooltip = "Recipes.Filter.CraftableNowTooltip";
            public const string Completion = "Recipes.Filter.Completion";
            public const string Missing = "Recipes.Filter.Missing";
            public const string Found = "Recipes.Filter.Found";
            public const string Research = "Recipes.Filter.Research";
            public const string Unresearched = "Recipes.Filter.Unresearched";
            public const string Researched = "Recipes.Filter.Researched";
            public const string Environment = "Recipes.Filter.Environment";
            public const string CraftingStations = "Recipes.Filter.CraftingStations";
            public const string Water = "Recipes.Requirement.Water";
            public const string Honey = "Recipes.Requirement.Honey";
            public const string Lava = "Recipes.Requirement.Lava";
            public const string SnowBiome = "Recipes.Requirement.SnowBiome";
            public const string GraveyardBiome = "Recipes.Requirement.GraveyardBiome";
            public const string Mechdusa = "Recipes.Requirement.Mechdusa";
            public const string TorchGodsFavor = "Recipes.Requirement.TorchGodsFavor";
            public const string Alchemy = "Recipes.Requirement.Alchemy";
            public const string CraftingStation = "Recipes.Requirement.CraftingStation";
            public const string MissingRecipe = "Recipes.Details.MissingRecipe";
            public const string NoMatchingRecipe = "Recipes.Details.NoMatchingRecipe";
            public const string RecipeVariant = "Recipes.Details.RecipeVariant";
            public const string MatchingCount = "Recipes.Details.MatchingCount";
            public const string Crafting = "Recipes.Details.Crafting";
            public const string Ingredients = "Recipes.Details.Ingredients";
            public const string Requirements = "Recipes.Details.Requirements";
            public const string AddFavorite = "Recipes.Details.AddFavorite";
            public const string RemoveFavorite = "Recipes.Details.RemoveFavorite";
            public const string Makes = "Recipes.Details.Makes";
            public const string CraftableNowValue = "Recipes.Details.CraftableNowValue";
            public const string AlchemyYes = "Recipes.Details.AlchemyYes";
            public const string AnyOf = "Recipes.Details.AnyOf";
            public const string Ingredient = "Recipes.Details.Ingredient";
            public const string NoSpecialRequirements = "Recipes.Details.NoSpecialRequirements";
        }

        public static class DirectCraft
        {
            public const string AvailabilityUnavailable = "DirectCraft.AvailabilityUnavailable";
            public const string ChooseOne = "DirectCraft.ChooseOne";
            public const string ChooseMany = "DirectCraft.ChooseMany";
            public const string CannotCraft = "DirectCraft.CannotCraft";
            public const string NoCraftableRecipes = "DirectCraft.NoCraftableRecipes";
            public const string NoIngredients = "DirectCraft.NoIngredients";
            public const string Any = "DirectCraft.Any";
            public const string FallbackItem = "DirectCraft.FallbackItem";
        }

        public static class Bestiary
        {
            public const string EmptyState = "Bestiary.EmptyState";
            public const string ClearFilters = "Bestiary.ClearFilters";
            public const string FiltersTooltip = "Bestiary.FiltersTooltip";
            public const string SortTooltip = "Bestiary.SortTooltip";
            public const string Encounter = "Bestiary.Filter.Encounter";
            public const string Unknown = "Bestiary.Filter.Unknown";
            public const string Encountered = "Bestiary.Filter.Encountered";
            public const string MerchantFilter = "Bestiary.Filter.Merchant";
            public const string HasStock = "Bestiary.Filter.HasStock";
            public const string HasStockTooltip = "Bestiary.Filter.HasStockTooltip";
            public const string Drops = "Bestiary.Filter.Drops";
            public const string HasDrops = "Bestiary.Filter.HasDrops";
            public const string HasDropsTooltip = "Bestiary.Filter.HasDropsTooltip";
            public const string HasMissingDrops = "Bestiary.Filter.HasMissingDrops";
            public const string HasMissingDropsTooltip = "Bestiary.Filter.HasMissingDropsTooltip";
            public const string HasUnresearchedDrops = "Bestiary.Filter.HasUnresearchedDrops";
            public const string HasUnresearchedDropsTooltip = "Bestiary.Filter.HasUnresearchedDropsTooltip";
            public const string NativeFilters = "Bestiary.Filter.Native";
            public const string NativeFilterFallback = "Bestiary.Filter.NativeFallback";
            public const string SortBestiary = "Bestiary.Sort.Bestiary";
            public const string SortName = "Bestiary.Sort.Name";
            public const string SortRarity = "Bestiary.Sort.Rarity";
            public const string SortAttack = "Bestiary.Sort.Attack";
            public const string SortDefense = "Bestiary.Sort.Defense";
            public const string SortCoins = "Bestiary.Sort.Coins";
            public const string SortHp = "Bestiary.Sort.Hp";
            public const string SortNpcId = "Bestiary.Sort.NpcId";
        }

        public static class NpcDetails
        {
            public const string Missing = "NpcDetails.Missing";
            public const string Classic = "NpcDetails.Classic";
            public const string Expert = "NpcDetails.Expert";
            public const string Master = "NpcDetails.Master";
            public const string Difficulty = "NpcDetails.Difficulty";
            public const string Statistics = "NpcDetails.Statistics";
            public const string FoundIn = "NpcDetails.FoundIn";
            public const string NoEnvironmentTags = "NpcDetails.NoEnvironmentTags";
            public const string BaseImmunities = "NpcDetails.BaseImmunities";
            public const string BannerProgress = "NpcDetails.BannerProgress";
            public const string Coins = "NpcDetails.Coins";
            public const string Stock = "NpcDetails.Stock";
            public const string Drops = "NpcDetails.Drops";
            public const string NoDrops = "NpcDetails.NoDrops";
            public const string MonetaryValue = "NpcDetails.MonetaryValue";
            public const string StatsUnavailable = "NpcDetails.StatsUnavailable";
            public const string Damage = "NpcDetails.Damage";
            public const string MaxLife = "NpcDetails.MaxLife";
            public const string Defense = "NpcDetails.Defense";
            public const string KnockbackTaken = "NpcDetails.KnockbackTaken";
            public const string KillStatistics = "NpcDetails.KillStatistics";
            public const string Rarity = "NpcDetails.Rarity";
            public const string RareCreature = "NpcDetails.RareCreature";
            public const string Slain = "NpcDetails.Slain";
            public const string Unavailable = "NpcDetails.Unavailable";
        }

        public static class Merchant
        {
            public const string ConditionalAvailability = "Merchant.ConditionalAvailability";
            public const string ConditionDetailsHint = "Merchant.ConditionDetailsHint";
            public const string RandomStock = "Merchant.RandomStock";
            public const string ShopCapacityLimited = "Merchant.ShopCapacityLimited";
            public const string UnknownItem = "Merchant.UnknownItem";
            public const string UnknownNpc = "Merchant.UnknownNpc";
            public const string UnknownMoonPhase = "Merchant.UnknownMoonPhase";

            public static string Condition(MerchantSourceConditionKind kind, bool negated)
            {
                return "Merchant.Condition." + kind + (negated ? ".Negative" : ".Positive");
            }
        }

        public static class Fishing
        {
            public const string General = "Fishing.General";
            public const string ExcludedCondition = "Fishing.ExcludedCondition";
            public const string ExcludedLavaAndHoney = "Fishing.ExcludedLavaAndHoney";

            public static string Condition(FishingSourceConditionKind kind)
            {
                return "Fishing.Condition." + kind;
            }
        }

        public static class Taxonomy
        {
            public static string Navigation(ItemNavigationNodeId id)
            {
                return "Taxonomy.Navigation." + id;
            }

            public static string Facet(ItemTaxonomyFacetId id)
            {
                return "Taxonomy.Facet." + id;
            }
        }

        public static class Acquisition
        {
            public const string WorldGeneratedChests = "Acquisition.World.generated-chests";
            public const string JungleShrineChests = "Acquisition.World.jungle-shrine-chests";
            public const string WaterChests = "Acquisition.World.water-chests";
            public const string SkywareChests = "Acquisition.World.skyware-chests";
            public const string PyramidChests = "Acquisition.World.pyramid-chests";
            public const string DeadMansChests = "Acquisition.World.dead-mans-chests";
            public const string DungeonChests = "Acquisition.World.dungeon-chests";
            public const string DungeonBiomeChests = "Acquisition.World.dungeon-biome-chests";
            public const string TempleChests = "Acquisition.World.temple-chests";
            public const string Pots = "Acquisition.World.pots";
            public const string ForestTreeShaking = "Acquisition.World.tree-shaking-forest";
            public const string SnowTreeShaking = "Acquisition.World.tree-shaking-snow";
            public const string JungleTreeShaking = "Acquisition.World.tree-shaking-jungle";
            public const string PalmTreeShaking = "Acquisition.World.tree-shaking-palm";
            public const string CorruptionTreeShaking = "Acquisition.World.tree-shaking-corruption";
            public const string HallowTreeShaking = "Acquisition.World.tree-shaking-hallow";
            public const string CrimsonTreeShaking = "Acquisition.World.tree-shaking-crimson";
            public const string AshTreeShaking = "Acquisition.World.tree-shaking-ash";
        }
    }
}