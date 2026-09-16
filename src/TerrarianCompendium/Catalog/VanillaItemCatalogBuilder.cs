using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.GameContent.Items;
using Terraria.ID;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Catalog
{
    internal sealed class VanillaItemCatalogBuilder(ILogger logger, Func<int, bool> fishingItemPredicate = null)
    {
        public ItemCatalog Build()
        {
            int itemCount = ItemID.Count;
            var entries = new List<ItemCatalogEntry>(Math.Max(0, itemCount - 1));

            var weaponFilter = new ItemFilters.Weapon();
            var armorFilter = new ItemFilters.Armor();
            var vanityFilter = new ItemFilters.Vanity();
            var buildingBlockFilter = new ItemFilters.BuildingBlock();
            var furnitureFilter = new ItemFilters.Furniture();
            var accessoriesFilter = new ItemFilters.Accessories();
            var miscAccessoriesFilter = new ItemFilters.MiscAccessories();
            var consumablesFilter = new ItemFilters.Consumables();
            var toolsFilter = new ItemFilters.Tools();
            var materialsFilter = new ItemFilters.Materials();

            var nativeFilters = new List<IItemEntryFilter>
            {
                weaponFilter,
                armorFilter,
                vanityFilter,
                buildingBlockFilter,
                furnitureFilter,
                accessoriesFilter,
                miscAccessoriesFilter,
                consumablesFilter,
                toolsFilter,
                materialsFilter
            };

            var miscFilter = new ItemFilters.MiscFallback(nativeFilters);

            var policyExcludedCount = 0;
            var skippedCount = 0;
            var failedCount = 0;
            var firstFailedItemId = 0;
            Exception firstFailure = null;

            for (var itemId = 1; itemId < itemCount; itemId++)
            {
                try
                {
                    bool isDeprecated = HasFlag(ItemID.Sets.Deprecated, itemId);
                    bool shouldNotBeInInventory = HasFlag(ItemID.Sets.ItemsThatShouldNotBeInInventory, itemId);

                    var item = new Item();
                    item.SetDefaults(itemId);

                    string name = item.Name;

                    if (!VanillaItemCatalogInclusionPolicy.ShouldInclude(
                            itemId,
                            item.type,
                            name,
                            isDeprecated,
                            shouldNotBeInInventory))
                    {
                        if (isDeprecated || shouldNotBeInInventory)
                            policyExcludedCount++;
                        else
                            skippedCount++;

                        continue;
                    }

                    ItemCategoryMembership categoryMemberships = ItemCategoryMembership.None;

                    if (weaponFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Weapons;

                    if (armorFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Armor;

                    if (vanityFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Vanity;

                    if (buildingBlockFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Blocks;

                    if (furnitureFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Furniture;

                    if (accessoriesFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Accessories;

                    if (miscAccessoriesFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.MiscAccessories;

                    if (consumablesFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Consumables;

                    if (toolsFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Tools;

                    if (materialsFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Materials;

                    if (miscFilter.FitsFilter(item))
                        categoryMemberships |= ItemCategoryMembership.Misc;

                    ContentSamples.CreativeHelper.ItemGroupAndOrderInGroup nativeSort =
                        ContentSamples.ItemCreativeSortingId[itemId];

                    ItemSemanticMembership semanticMemberships = CreateSemanticMemberships(item, nativeSort.Group);
                    int tagDamage = GetStandaloneTagDamage(itemId);
                    ItemDetailsStats detailsStats = ItemDetailsStatsProjector.ProjectBaseItem(
                        item.damage,
                        item.melee,
                        item.ranged,
                        item.magic,
                        item.summon,
                        item.knockBack,
                        item.crit,
                        item.useTime,
                        item.useStyle,
                        tagDamage);

                    entries.Add(
                        new ItemCatalogEntry(
                            itemId,
                            name,
                            categoryMemberships,
                            (int)nativeSort.Group,
                            nativeSort.OrderInGroup,
                            semanticMemberships,
                            new ItemSortMetrics(
                                item.value,
                                item.rare,
                                item.damage,
                                item.defense,
                                item.pick,
                                item.axe * 5,
                                item.hammer,
                                item.fishingPole),
                            detailsStats));
                }
                catch (Exception exception)
                {
                    failedCount++;

                    if (firstFailure == null)
                    {
                        firstFailedItemId = itemId;
                        firstFailure = exception;
                    }
                }
            }

            var catalog = ItemCatalog.Create(entries);

            logger?.Info(
                $"Vanilla item catalog built: {catalog.Count} entries from ItemID.Count={itemCount}; " +
                $"policyExcluded={policyExcludedCount}; skipped={skippedCount}; failed={failedCount}.");

            if (firstFailure != null)
            {
                logger?.Warn(
                    $"Failed to initialize {failedCount} vanilla item definition(s). " +
                    $"First failure at item ID {firstFailedItemId}: " +
                    $"{firstFailure.GetType().Name}: {firstFailure.Message}");
            }

            return catalog;
        }

        private ItemSemanticMembership CreateSemanticMemberships(
            Item item,
            ContentSamples.CreativeHelper.ItemGroup nativeGroup)
        {
            ItemSemanticMembership result = ItemSemanticMembership.None;

            if (item.melee && item.pick <= 0 && item.axe <= 0 && item.hammer <= 0)
                result |= ItemSemanticMembership.Melee;

            if (HasFlag(ItemID.Sets.Yoyo, item.type))
                result |= ItemSemanticMembership.Yoyos;

            if (item.magic)
                result |= ItemSemanticMembership.Magic;

            if (item.ranged && item.ammo == AmmoID.None)
                result |= ItemSemanticMembership.RangedWeapon;

            if (item.ranged && HasFlag(AmmoID.Sets.IsArrow, item.useAmmo))
                result |= ItemSemanticMembership.ArrowFamilyWeapons;

            if (item.ranged && HasFlag(AmmoID.Sets.IsBullet, item.useAmmo))
                result |= ItemSemanticMembership.BulletFamilyWeapons;

            if (item.ranged &&
                (HasFlag(AmmoID.Sets.IsSpecialist, item.useAmmo) ||
                 HasFlag(ItemID.Sets.IsRangedSpecialistWeapon, item.type)))
            {
                result |= ItemSemanticMembership.SpecialistWeapons;
            }

            if (item.ammo != AmmoID.None)
            {
                result |= ItemSemanticMembership.Ammo;

                if (HasFlag(AmmoID.Sets.IsArrow, item.ammo))
                    result |= ItemSemanticMembership.ArrowAmmo;

                if (HasFlag(AmmoID.Sets.IsBullet, item.ammo))
                    result |= ItemSemanticMembership.BulletAmmo;

                if (HasFlag(AmmoID.Sets.IsSpecialist, item.ammo))
                    result |= ItemSemanticMembership.SpecialistAmmo;
            }

            if (item.summon)
                result |= ItemSemanticMembership.Summon;

            if (item.shoot > 0 && HasFlag(ProjectileID.Sets.IsAWhip, item.shoot))
                result |= ItemSemanticMembership.Whips;

            if (item.summon && item.sentry)
                result |= ItemSemanticMembership.Sentries;

            if (!item.vanity)
            {
                if (item.headSlot != -1)
                    result |= ItemSemanticMembership.ArmorHead;

                if (item.bodySlot != -1)
                    result |= ItemSemanticMembership.ArmorBody;

                if (item.legSlot != -1)
                    result |= ItemSemanticMembership.ArmorLegs;
            }
            else
            {
                if (item.headSlot != -1)
                    result |= ItemSemanticMembership.VanityHead;

                if (item.bodySlot != -1)
                    result |= ItemSemanticMembership.VanityBody;

                if (item.legSlot != -1)
                    result |= ItemSemanticMembership.VanityLegs;
            }

            if (HasFlag(Main.tileSolid, item.createTile) &&
                !HasFlag(Main.tileSolidTop, item.createTile) &&
                !HasFlag(Main.tileFrameImportant, item.createTile))
            {
                result |= ItemSemanticMembership.SolidBlocks;
            }

            if (item.createWall != -1)
                result |= ItemSemanticMembership.Walls;

            if (HasFlag(Main.tileContainer, item.createTile) || IsPersonalStorageTile(item.createTile))
                result |= ItemSemanticMembership.Containers;

            if (IsStatue(item))
                result |= ItemSemanticMembership.Statues;

            if (HasFlag(TileID.Sets.Platforms, item.createTile))
                result |= ItemSemanticMembership.Platforms;

            if (HasFlag(TileID.Sets.RoomNeeds.CountsAsDoor, item.createTile))
                result |= ItemSemanticMembership.Doors;

            if (HasFlag(TileID.Sets.RoomNeeds.CountsAsChair, item.createTile))
                result |= ItemSemanticMembership.Chairs;

            if (HasFlag(TileID.Sets.RoomNeeds.CountsAsTable, item.createTile))
                result |= ItemSemanticMembership.Tables;

            if (HasFlag(Recipe.TileUsedInRecipes, item.createTile))
                result |= ItemSemanticMembership.CraftingStations;

            if (HasFlag(TileID.Sets.RoomNeeds.CountsAsTorch, item.createTile))
                result |= ItemSemanticMembership.LightSources;

            if (HasFlag(TileID.Sets.Torches, item.createTile))
                result |= ItemSemanticMembership.Torches;

            if (item.createTile == TileID.Banners)
                result |= ItemSemanticMembership.Banners;

            if (HasFlag(TileID.Sets.Paintings, item.createTile))
                result |= ItemSemanticMembership.WallDecorations;

            if (item.makeNPC > 0)
                result |= ItemSemanticMembership.Critters;

            if (item.wingSlot > 0)
                result |= ItemSemanticMembership.Wings;

            if (item.buffType > 0 && HasFlag(Main.vanityPet, item.buffType) && !HasFlag(Main.lightPet, item.buffType))
                result |= ItemSemanticMembership.Pets;

            if (item.buffType > 0 && HasFlag(Main.lightPet, item.buffType))
                result |= ItemSemanticMembership.LightPets;

            if (item.mountType >= 0 && !HasFlag(MountID.Sets.Cart, item.mountType))
                result |= ItemSemanticMembership.Mounts;

            if (item.mountType >= 0 && HasFlag(MountID.Sets.Cart, item.mountType))
                result |= ItemSemanticMembership.Minecarts;

            if (item.mountType == -1 && HasFlag(Main.projHook, item.shoot))
                result |= ItemSemanticMembership.Hooks;

            if (item.healLife > 0)
                result |= ItemSemanticMembership.HealthPotions;

            if (item.healMana > 0)
                result |= ItemSemanticMembership.ManaPotions;

            if (nativeGroup == ContentSamples.CreativeHelper.ItemGroup.BuffPotion && item.buffType > 0)
                result |= ItemSemanticMembership.BuffPotions;

            if (nativeGroup == ContentSamples.CreativeHelper.ItemGroup.Flask)
                result |= ItemSemanticMembership.Flasks;

            if (nativeGroup == ContentSamples.CreativeHelper.ItemGroup.Food)
                result |= ItemSemanticMembership.Food;

            if (item.pick > 0)
                result |= ItemSemanticMembership.Pickaxes;

            if (item.axe > 0)
                result |= ItemSemanticMembership.Axes;

            if (item.hammer > 0)
                result |= ItemSemanticMembership.Hammers;

            if (item.fishingPole > 0)
                result |= ItemSemanticMembership.FishingRods;

            if (HasFlag(ItemID.Sets.OpenableBag, item.type))
                result |= ItemSemanticMembership.OpenableItems;

            if (nativeGroup == ContentSamples.CreativeHelper.ItemGroup.BossItem)
                result |= ItemSemanticMembership.BossSummonItems;

            if (item.createTile == TileID.MusicBoxes || item.type == ItemID.MusicBox)
                result |= ItemSemanticMembership.MusicBoxes;

            if (item.dye > 0)
                result |= ItemSemanticMembership.Dyes;

            if (HasFlag(ItemID.Sets.IsQuestFish, item.type))
                result |= ItemSemanticMembership.QuestFish;

            if (fishingItemPredicate?.Invoke(item.type) == true)
                result |= ItemSemanticMembership.Fishing;

            return result;
        }

        private static int GetStandaloneTagDamage(int itemId)
        {
            UniqueTagEffect[] effects = ItemID.Sets.UniqueTagEffects;

            if (effects == null || itemId < 0 || itemId >= effects.Length)
                return 0;

            return effects[itemId] is WhipTagEffect whipTagEffect ? whipTagEffect.TagDamage : 0;
        }

        private static bool HasFlag(bool[] flags, int index)
        {
            return flags != null && index >= 0 && index < flags.Length && flags[index];
        }

        private static bool IsPersonalStorageTile(int tileType)
        {
            return tileType == TileID.PiggyBank ||
                   tileType == TileID.Safes ||
                   tileType == TileID.DefendersForge ||
                   tileType == TileID.VoidVault;
        }

        private static bool IsStatue(Item item)
        {
            if (item.createTile == TileID.Statues)
            {
                return item.placeStyle != 46 && item.placeStyle != 47 && item.placeStyle != 48 && item.placeStyle != 49;
            }

            return item.createTile == TileID.AlphabetStatues ||
                   item.createTile == TileID.MushroomStatue ||
                   item.createTile == TileID.CatBast ||
                   item.createTile == TileID.BoulderStatue;
        }
    }
}