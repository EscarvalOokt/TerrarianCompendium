using System;
using System.Collections.Generic;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Catalog
{
    internal enum ItemNavigationScopeKind
    {
        AllItems,
        NativeCategory,
        SemanticAny
    }

    internal readonly struct ItemNavigationNodeDefinition(
        ItemNavigationNodeId id,
        ItemNavigationNodeId parentId,
        string displayNameKey,
        ItemNavigationScopeKind scopeKind,
        ItemCategoryMembership nativeCategory = ItemCategoryMembership.None,
        ItemSemanticMembership semanticAnyOf = ItemSemanticMembership.None,
        bool isGroupingOnly = false)
    {
        public ItemNavigationNodeId Id { get; } = id;

        public ItemNavigationNodeId ParentId { get; } = parentId;

        public string DisplayNameKey { get; } = displayNameKey;

        public ItemNavigationScopeKind ScopeKind { get; } = scopeKind;

        public ItemCategoryMembership NativeCategory { get; } = nativeCategory;

        public ItemSemanticMembership SemanticAnyOf { get; } = semanticAnyOf;

        public bool IsGroupingOnly { get; } = isGroupingOnly;
    }

    internal readonly struct ItemTaxonomyFacetDefinition(
        ItemTaxonomyFacetId id,
        string displayNameKey,
        ItemCategoryMembership nativeCategory = ItemCategoryMembership.None,
        ItemSemanticMembership semanticMembership = ItemSemanticMembership.None)
    {
        public ItemTaxonomyFacetId Id { get; } = id;

        public string DisplayNameKey { get; } = displayNameKey;

        public ItemCategoryMembership NativeCategory { get; } = nativeCategory;

        public ItemSemanticMembership SemanticMembership { get; } = semanticMembership;
    }

    internal static class ItemTaxonomyDefinitions
    {
        private static readonly ItemNavigationNodeDefinition[] _navigationNodes =
        [
            new(
                ItemNavigationNodeId.AllItems,
                ItemNavigationNodeId.None,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.AllItems),
                ItemNavigationScopeKind.AllItems),

            new(
                ItemNavigationNodeId.Weapons,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Weapons),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Weapons),
            new(
                ItemNavigationNodeId.WeaponsMelee,
                ItemNavigationNodeId.Weapons,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsMelee),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Melee),
            new(
                ItemNavigationNodeId.WeaponsMeleeYoyos,
                ItemNavigationNodeId.WeaponsMelee,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsMeleeYoyos),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Yoyos),
            new(
                ItemNavigationNodeId.WeaponsMagic,
                ItemNavigationNodeId.Weapons,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsMagic),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Magic),
            new(
                ItemNavigationNodeId.WeaponsRanged,
                ItemNavigationNodeId.Weapons,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRanged),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.RangedWeapon | ItemSemanticMembership.Ammo,
                isGroupingOnly: true),
            new(
                ItemNavigationNodeId.WeaponsRangedArrowFamilyWeapons,
                ItemNavigationNodeId.WeaponsRanged,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedArrowFamilyWeapons),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ArrowFamilyWeapons),
            new(
                ItemNavigationNodeId.WeaponsRangedBulletFamilyWeapons,
                ItemNavigationNodeId.WeaponsRanged,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedBulletFamilyWeapons),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.BulletFamilyWeapons),
            new(
                ItemNavigationNodeId.WeaponsRangedSpecialist,
                ItemNavigationNodeId.WeaponsRanged,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedSpecialist),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.SpecialistWeapons),
            new(
                ItemNavigationNodeId.WeaponsRangedAmmo,
                ItemNavigationNodeId.WeaponsRanged,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedAmmo),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Ammo),
            new(
                ItemNavigationNodeId.WeaponsRangedAmmoArrows,
                ItemNavigationNodeId.WeaponsRangedAmmo,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedAmmoArrows),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ArrowAmmo),
            new(
                ItemNavigationNodeId.WeaponsRangedAmmoBullets,
                ItemNavigationNodeId.WeaponsRangedAmmo,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedAmmoBullets),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.BulletAmmo),
            new(
                ItemNavigationNodeId.WeaponsRangedAmmoSpecialist,
                ItemNavigationNodeId.WeaponsRangedAmmo,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsRangedAmmoSpecialist),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.SpecialistAmmo),
            new(
                ItemNavigationNodeId.WeaponsSummon,
                ItemNavigationNodeId.Weapons,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsSummon),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Summon),
            new(
                ItemNavigationNodeId.WeaponsSummonWhips,
                ItemNavigationNodeId.WeaponsSummon,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsSummonWhips),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Whips),
            new(
                ItemNavigationNodeId.WeaponsSummonSentries,
                ItemNavigationNodeId.WeaponsSummon,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.WeaponsSummonSentries),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Sentries),

            new(
                ItemNavigationNodeId.Armor,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Armor),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Armor),
            new(
                ItemNavigationNodeId.ArmorHead,
                ItemNavigationNodeId.Armor,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ArmorHead),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ArmorHead),
            new(
                ItemNavigationNodeId.ArmorBody,
                ItemNavigationNodeId.Armor,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ArmorBody),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ArmorBody),
            new(
                ItemNavigationNodeId.ArmorLegs,
                ItemNavigationNodeId.Armor,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ArmorLegs),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ArmorLegs),

            new(
                ItemNavigationNodeId.Vanity,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Vanity),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Vanity),
            new(
                ItemNavigationNodeId.VanityHead,
                ItemNavigationNodeId.Vanity,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.VanityHead),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.VanityHead),
            new(
                ItemNavigationNodeId.VanityBody,
                ItemNavigationNodeId.Vanity,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.VanityBody),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.VanityBody),
            new(
                ItemNavigationNodeId.VanityLegs,
                ItemNavigationNodeId.Vanity,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.VanityLegs),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.VanityLegs),

            new(
                ItemNavigationNodeId.Blocks,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Blocks),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Blocks),
            new(
                ItemNavigationNodeId.BlocksSolidBlocks,
                ItemNavigationNodeId.Blocks,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.BlocksSolidBlocks),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.SolidBlocks),
            new(
                ItemNavigationNodeId.BlocksWalls,
                ItemNavigationNodeId.Blocks,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.BlocksWalls),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Walls),

            new(
                ItemNavigationNodeId.Furniture,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Furniture),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Furniture),
            new(
                ItemNavigationNodeId.FurnitureContainers,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureContainers),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Containers),
            new(
                ItemNavigationNodeId.FurnitureStatues,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureStatues),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Statues),
            new(
                ItemNavigationNodeId.FurniturePlatforms,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurniturePlatforms),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Platforms),
            new(
                ItemNavigationNodeId.FurnitureDoors,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureDoors),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Doors),
            new(
                ItemNavigationNodeId.FurnitureChairs,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureChairs),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Chairs),
            new(
                ItemNavigationNodeId.FurnitureTables,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureTables),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Tables),
            new(
                ItemNavigationNodeId.FurnitureCraftingStations,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureCraftingStations),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.CraftingStations),
            new(
                ItemNavigationNodeId.FurnitureLightSources,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureLightSources),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.LightSources),
            new(
                ItemNavigationNodeId.FurnitureLightSourcesTorches,
                ItemNavigationNodeId.FurnitureLightSources,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureLightSourcesTorches),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Torches),
            new(
                ItemNavigationNodeId.FurnitureBanners,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureBanners),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Banners),
            new(
                ItemNavigationNodeId.FurnitureWallDecorations,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureWallDecorations),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.WallDecorations),
            new(
                ItemNavigationNodeId.FurnitureCritters,
                ItemNavigationNodeId.Furniture,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.FurnitureCritters),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Critters),

            new(
                ItemNavigationNodeId.Accessories,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Accessories),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Accessories),
            new(
                ItemNavigationNodeId.AccessoriesWings,
                ItemNavigationNodeId.Accessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.AccessoriesWings),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Wings),

            new(
                ItemNavigationNodeId.MiscAccessories,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessories),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.MiscAccessories),
            new(
                ItemNavigationNodeId.MiscAccessoriesPets,
                ItemNavigationNodeId.MiscAccessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessoriesPets),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Pets),
            new(
                ItemNavigationNodeId.MiscAccessoriesLightPets,
                ItemNavigationNodeId.MiscAccessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessoriesLightPets),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.LightPets),
            new(
                ItemNavigationNodeId.MiscAccessoriesMounts,
                ItemNavigationNodeId.MiscAccessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessoriesMounts),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Mounts),
            new(
                ItemNavigationNodeId.MiscAccessoriesMinecarts,
                ItemNavigationNodeId.MiscAccessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessoriesMinecarts),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Minecarts),
            new(
                ItemNavigationNodeId.MiscAccessoriesHooks,
                ItemNavigationNodeId.MiscAccessories,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscAccessoriesHooks),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Hooks),

            new(
                ItemNavigationNodeId.Consumables,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Consumables),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Consumables),
            new(
                ItemNavigationNodeId.ConsumablesHealthPotions,
                ItemNavigationNodeId.Consumables,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ConsumablesHealthPotions),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.HealthPotions),
            new(
                ItemNavigationNodeId.ConsumablesManaPotions,
                ItemNavigationNodeId.Consumables,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ConsumablesManaPotions),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.ManaPotions),
            new(
                ItemNavigationNodeId.ConsumablesBuffPotions,
                ItemNavigationNodeId.Consumables,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ConsumablesBuffPotions),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.BuffPotions),
            new(
                ItemNavigationNodeId.ConsumablesFlasks,
                ItemNavigationNodeId.Consumables,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ConsumablesFlasks),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Flasks),
            new(
                ItemNavigationNodeId.ConsumablesFood,
                ItemNavigationNodeId.Consumables,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ConsumablesFood),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Food),

            new(
                ItemNavigationNodeId.Tools,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Tools),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Tools),
            new(
                ItemNavigationNodeId.ToolsPickaxes,
                ItemNavigationNodeId.Tools,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ToolsPickaxes),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Pickaxes),
            new(
                ItemNavigationNodeId.ToolsAxes,
                ItemNavigationNodeId.Tools,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ToolsAxes),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Axes),
            new(
                ItemNavigationNodeId.ToolsHammers,
                ItemNavigationNodeId.Tools,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ToolsHammers),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.Hammers),
            new(
                ItemNavigationNodeId.ToolsFishingRods,
                ItemNavigationNodeId.Tools,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.ToolsFishingRods),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.FishingRods),

            new(
                ItemNavigationNodeId.Materials,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Materials),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Materials),
            new(
                ItemNavigationNodeId.Misc,
                ItemNavigationNodeId.AllItems,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.Misc),
                ItemNavigationScopeKind.NativeCategory,
                ItemCategoryMembership.Misc),
            new(
                ItemNavigationNodeId.MiscQuestFish,
                ItemNavigationNodeId.Misc,
                CompendiumTextKeys.Taxonomy.Navigation(ItemNavigationNodeId.MiscQuestFish),
                ItemNavigationScopeKind.SemanticAny,
                semanticAnyOf: ItemSemanticMembership.QuestFish)
        ];

        private static readonly ItemTaxonomyFacetDefinition[] _facets =
        [
            new(
                ItemTaxonomyFacetId.Materials,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.Materials),
                nativeCategory: ItemCategoryMembership.Materials),
            new(
                ItemTaxonomyFacetId.Consumables,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.Consumables),
                nativeCategory: ItemCategoryMembership.Consumables),
            new(
                ItemTaxonomyFacetId.Ammo,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.Ammo),
                semanticMembership: ItemSemanticMembership.Ammo),
            new(
                ItemTaxonomyFacetId.OpenableItems,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.OpenableItems),
                semanticMembership: ItemSemanticMembership.OpenableItems),
            new(
                ItemTaxonomyFacetId.BossSummonItems,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.BossSummonItems),
                semanticMembership: ItemSemanticMembership.BossSummonItems),
            new(
                ItemTaxonomyFacetId.MusicBoxes,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.MusicBoxes),
                semanticMembership: ItemSemanticMembership.MusicBoxes),
            new(
                ItemTaxonomyFacetId.Dyes,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.Dyes),
                semanticMembership: ItemSemanticMembership.Dyes),
            new(
                ItemTaxonomyFacetId.Fishing,
                CompendiumTextKeys.Taxonomy.Facet(ItemTaxonomyFacetId.Fishing),
                semanticMembership: ItemSemanticMembership.Fishing)
        ];

        private static readonly Dictionary<ItemNavigationNodeId, ItemNavigationNodeDefinition> _navigationById;

        private static readonly Dictionary<ItemNavigationNodeId, IReadOnlyList<ItemNavigationNodeDefinition>>
            _childrenByParent;

        private static readonly Dictionary<ItemTaxonomyFacetId, ItemTaxonomyFacetDefinition> _facetsById;

        private static readonly ItemNavigationNodeDefinition[]
            _emptyNodes = Array.Empty<ItemNavigationNodeDefinition>();

        private static readonly ItemTaxonomyFacetId _knownFacetMask;

        static ItemTaxonomyDefinitions()
        {
            _navigationById = new Dictionary<ItemNavigationNodeId, ItemNavigationNodeDefinition>();
            var childLists = new Dictionary<ItemNavigationNodeId, List<ItemNavigationNodeDefinition>>();

            foreach (ItemNavigationNodeDefinition node in _navigationNodes)
            {
                _navigationById.Add(node.Id, node);

                if (node.ParentId == ItemNavigationNodeId.None)
                    continue;

                if (!childLists.TryGetValue(node.ParentId, out List<ItemNavigationNodeDefinition> children))
                {
                    children = new List<ItemNavigationNodeDefinition>();
                    childLists.Add(node.ParentId, children);
                }

                children.Add(node);
            }

            _childrenByParent = new Dictionary<ItemNavigationNodeId, IReadOnlyList<ItemNavigationNodeDefinition>>();

            foreach (KeyValuePair<ItemNavigationNodeId, List<ItemNavigationNodeDefinition>> pair in childLists)
                _childrenByParent.Add(pair.Key, pair.Value.ToArray());

            _facetsById = new Dictionary<ItemTaxonomyFacetId, ItemTaxonomyFacetDefinition>();
            ItemTaxonomyFacetId knownFacetMask = ItemTaxonomyFacetId.None;

            foreach (ItemTaxonomyFacetDefinition facet in _facets)
            {
                _facetsById.Add(facet.Id, facet);
                knownFacetMask |= facet.Id;
            }

            _knownFacetMask = knownFacetMask;
        }

        public static IReadOnlyList<ItemNavigationNodeDefinition> NavigationNodes => _navigationNodes;

        public static IReadOnlyList<ItemTaxonomyFacetDefinition> Facets => _facets;

        public static ItemNavigationNodeDefinition GetNavigationNode(ItemNavigationNodeId id)
        {
            if (!_navigationById.TryGetValue(id, out ItemNavigationNodeDefinition definition))
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown item navigation node.");

            return definition;
        }

        public static IReadOnlyList<ItemNavigationNodeDefinition> GetChildren(ItemNavigationNodeId parentId)
        {
            return _childrenByParent.TryGetValue(parentId, out IReadOnlyList<ItemNavigationNodeDefinition> children)
                ? children
                : _emptyNodes;
        }

        public static bool HasChildren(ItemNavigationNodeId nodeId)
        {
            return GetChildren(nodeId).Count > 0;
        }

        public static bool MatchesNavigationNode(ItemCatalogEntry entry, ItemNavigationNodeId nodeId)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            ItemNavigationNodeDefinition node = GetNavigationNode(nodeId);

            if (node.ParentId != ItemNavigationNodeId.None && !MatchesNavigationNode(entry, node.ParentId))
            {
                return false;
            }

            switch (node.ScopeKind)
            {
                case ItemNavigationScopeKind.AllItems:
                    return true;

                case ItemNavigationScopeKind.NativeCategory:
                    return (entry.CategoryMemberships & node.NativeCategory) == node.NativeCategory;

                case ItemNavigationScopeKind.SemanticAny:
                    return (entry.SemanticMemberships & node.SemanticAnyOf) != ItemSemanticMembership.None;

                default:
                    throw new InvalidOperationException("Unsupported item navigation scope kind.");
            }
        }

        public static bool MatchesOther(ItemCatalogEntry entry, ItemNavigationNodeId nodeId)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            IReadOnlyList<ItemNavigationNodeDefinition> children = GetChildren(nodeId);

            if (children.Count == 0 || !MatchesNavigationNode(entry, nodeId))
                return false;

            foreach (ItemNavigationNodeDefinition child in children)
            {
                if (MatchesNavigationNode(entry, child.Id))
                    return false;
            }

            return true;
        }

        public static bool HasOtherItems(ItemCatalog catalog, ItemNavigationNodeId nodeId)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (!HasChildren(nodeId))
                return false;

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                if (MatchesOther(entry, nodeId))
                    return true;
            }

            return false;
        }

        public static ItemTaxonomyFacetDefinition GetFacet(ItemTaxonomyFacetId id)
        {
            if (!_facetsById.TryGetValue(id, out ItemTaxonomyFacetDefinition definition))
                throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown item taxonomy facet.");

            return definition;
        }

        public static bool IsKnownFacetMask(ItemTaxonomyFacetId facets)
        {
            return ((int)facets & ~(int)_knownFacetMask) == 0;
        }

        public static bool MatchesFacet(ItemCatalogEntry entry, ItemTaxonomyFacetId facetId)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            ItemTaxonomyFacetDefinition facet = GetFacet(facetId);

            if (facet.NativeCategory != ItemCategoryMembership.None)
                return (entry.CategoryMemberships & facet.NativeCategory) == facet.NativeCategory;

            return facet.SemanticMembership != ItemSemanticMembership.None &&
                   (entry.SemanticMemberships & facet.SemanticMembership) == facet.SemanticMembership;
        }

        public static bool MatchesFacets(
            ItemCatalogEntry entry,
            ItemTaxonomyFacetId includedFacets,
            ItemTaxonomyFacetId excludedFacets)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            if (!IsKnownFacetMask(includedFacets))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(includedFacets),
                    includedFacets,
                    "Unsupported included item taxonomy facet mask.");
            }

            if (!IsKnownFacetMask(excludedFacets))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(excludedFacets),
                    excludedFacets,
                    "Unsupported excluded item taxonomy facet mask.");
            }

            if ((includedFacets & excludedFacets) != ItemTaxonomyFacetId.None)
            {
                throw new ArgumentException(
                    "Included and excluded item taxonomy facet masks must not overlap.",
                    nameof(excludedFacets));
            }

            foreach (ItemTaxonomyFacetDefinition facet in _facets)
            {
                if ((includedFacets & facet.Id) == facet.Id && !MatchesFacet(entry, facet.Id))
                    return false;

                if ((excludedFacets & facet.Id) == facet.Id && MatchesFacet(entry, facet.Id))
                    return false;
            }

            return true;
        }
    }
}