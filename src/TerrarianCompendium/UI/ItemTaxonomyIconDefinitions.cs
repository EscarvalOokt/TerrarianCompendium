using System;
using System.Collections.Generic;
using Terraria.ID;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.UI
{
    internal static class ItemTaxonomyIconDefinitions
    {
        private static readonly Dictionary<ItemNavigationNodeId, int> _journeyFrameIndices = new()
        {
            [ItemNavigationNodeId.Weapons] = 0,
            [ItemNavigationNodeId.Accessories] = 1,
            [ItemNavigationNodeId.Armor] = 2,
            [ItemNavigationNodeId.Consumables] = 3,
            [ItemNavigationNodeId.Blocks] = 4,
            [ItemNavigationNodeId.Misc] = 5,
            [ItemNavigationNodeId.Tools] = 6,
            [ItemNavigationNodeId.Furniture] = 7,
            [ItemNavigationNodeId.Vanity] = 8,
            [ItemNavigationNodeId.MiscAccessories] = 9,
            [ItemNavigationNodeId.Materials] = 10
        };

        private static readonly Dictionary<ItemNavigationNodeId, int> _navigationItemIds = new()
        {
            [ItemNavigationNodeId.WeaponsMelee] = ItemID.WoodenSword,
            [ItemNavigationNodeId.WeaponsMeleeYoyos] = ItemID.WoodYoyo,
            [ItemNavigationNodeId.WeaponsMagic] = ItemID.SpaceGun,
            [ItemNavigationNodeId.WeaponsRanged] = ItemID.WoodenBow,
            [ItemNavigationNodeId.WeaponsRangedArrowFamilyWeapons] = ItemID.WoodenBow,
            [ItemNavigationNodeId.WeaponsRangedBulletFamilyWeapons] = ItemID.Minishark,
            [ItemNavigationNodeId.WeaponsRangedSpecialist] = ItemID.RocketLauncher,
            [ItemNavigationNodeId.WeaponsRangedAmmo] = ItemID.MusketBall,
            [ItemNavigationNodeId.WeaponsRangedAmmoArrows] = ItemID.WoodenArrow,
            [ItemNavigationNodeId.WeaponsRangedAmmoBullets] = ItemID.MusketBall,
            [ItemNavigationNodeId.WeaponsRangedAmmoSpecialist] = ItemID.RocketI,
            [ItemNavigationNodeId.WeaponsSummon] = ItemID.ImpStaff,
            [ItemNavigationNodeId.WeaponsSummonWhips] = ItemID.ThornWhip,
            [ItemNavigationNodeId.WeaponsSummonSentries] = ItemID.QueenSpiderStaff,

            [ItemNavigationNodeId.ArmorHead] = ItemID.IronHelmet,
            [ItemNavigationNodeId.ArmorBody] = ItemID.IronChainmail,
            [ItemNavigationNodeId.ArmorLegs] = ItemID.IronGreaves,

            [ItemNavigationNodeId.VanityHead] = ItemID.TopHat,
            [ItemNavigationNodeId.VanityBody] = ItemID.TuxedoShirt,
            [ItemNavigationNodeId.VanityLegs] = ItemID.TuxedoPants,

            [ItemNavigationNodeId.BlocksSolidBlocks] = ItemID.DirtBlock,
            [ItemNavigationNodeId.BlocksWalls] = ItemID.DirtWall,

            [ItemNavigationNodeId.FurnitureContainers] = ItemID.Chest,
            [ItemNavigationNodeId.FurnitureStatues] = ItemID.AngelStatue,
            [ItemNavigationNodeId.FurniturePlatforms] = ItemID.WoodPlatform,
            [ItemNavigationNodeId.FurnitureDoors] = ItemID.WoodenDoor,
            [ItemNavigationNodeId.FurnitureChairs] = ItemID.WoodenChair,
            [ItemNavigationNodeId.FurnitureTables] = ItemID.WoodenTable,
            [ItemNavigationNodeId.FurnitureCraftingStations] = ItemID.WorkBench,
            [ItemNavigationNodeId.FurnitureLightSources] = ItemID.Candle,
            [ItemNavigationNodeId.FurnitureLightSourcesTorches] = ItemID.Torch,
            [ItemNavigationNodeId.FurnitureBanners] = ItemID.RedBanner,
            [ItemNavigationNodeId.FurnitureWallDecorations] = ItemID.AmericanExplosive,
            [ItemNavigationNodeId.FurnitureCritters] = ItemID.Bunny,

            [ItemNavigationNodeId.AccessoriesWings] = ItemID.BeeWings,

            [ItemNavigationNodeId.MiscAccessoriesPets] = ItemID.ZephyrFish,
            [ItemNavigationNodeId.MiscAccessoriesLightPets] = ItemID.MagicLantern,
            [ItemNavigationNodeId.MiscAccessoriesMounts] = ItemID.SlimySaddle,
            [ItemNavigationNodeId.MiscAccessoriesMinecarts] = ItemID.Minecart,
            [ItemNavigationNodeId.MiscAccessoriesHooks] = ItemID.GrapplingHook,

            [ItemNavigationNodeId.ConsumablesHealthPotions] = ItemID.HealingPotion,
            [ItemNavigationNodeId.ConsumablesManaPotions] = ItemID.ManaPotion,
            [ItemNavigationNodeId.ConsumablesBuffPotions] = ItemID.IronskinPotion,
            [ItemNavigationNodeId.ConsumablesFlasks] = ItemID.FlaskofFire,
            [ItemNavigationNodeId.ConsumablesFood] = ItemID.BowlofSoup,

            [ItemNavigationNodeId.ToolsPickaxes] = ItemID.IronPickaxe,
            [ItemNavigationNodeId.ToolsAxes] = ItemID.IronAxe,
            [ItemNavigationNodeId.ToolsHammers] = ItemID.IronHammer,
            [ItemNavigationNodeId.ToolsFishingRods] = ItemID.WoodFishingPole,

            [ItemNavigationNodeId.MiscQuestFish] = ItemID.Fishotron
        };

        private static readonly Dictionary<ItemTaxonomyFacetId, int> _facetItemIds = new()
        {
            [ItemTaxonomyFacetId.Materials] = ItemID.IronBar,
            [ItemTaxonomyFacetId.Consumables] = ItemID.HealingPotion,
            [ItemTaxonomyFacetId.Ammo] = ItemID.MusketBall,
            [ItemTaxonomyFacetId.OpenableItems] = ItemID.KingSlimeBossBag,
            [ItemTaxonomyFacetId.BossSummonItems] = ItemID.SuspiciousLookingEye,
            [ItemTaxonomyFacetId.MusicBoxes] = ItemID.MusicBox,
            [ItemTaxonomyFacetId.Dyes] = ItemID.RedDye,
            [ItemTaxonomyFacetId.Fishing] = ItemID.Bass
        };

        public static bool TryGetJourneyFrameIndex(ItemNavigationNodeId nodeId, out int frameIndex)
        {
            return _journeyFrameIndices.TryGetValue(nodeId, out frameIndex);
        }

        public static int GetNavigationItemId(ItemNavigationNodeId nodeId)
        {
            if (!_navigationItemIds.TryGetValue(nodeId, out int itemId))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nodeId),
                    nodeId,
                    "No representative item icon is defined for this navigation node.");
            }

            return itemId;
        }

        public static bool TryGetNavigationItemId(ItemNavigationNodeId nodeId, out int itemId)
        {
            return _navigationItemIds.TryGetValue(nodeId, out itemId);
        }

        public static int GetFacetItemId(ItemTaxonomyFacetId facetId)
        {
            if (!_facetItemIds.TryGetValue(facetId, out int itemId))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(facetId),
                    facetId,
                    "No representative item icon is defined for this taxonomy facet.");
            }

            return itemId;
        }
    }
}