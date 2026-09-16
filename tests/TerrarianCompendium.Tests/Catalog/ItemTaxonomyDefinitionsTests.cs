using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Catalog
{
    [TestFixture]
    public sealed class ItemTaxonomyDefinitionsTests
    {
        [Test]
        public void AllItems_HasExpectedNativeRootsInOrder()
        {
            ItemNavigationNodeId[] expected =
            [
                ItemNavigationNodeId.Weapons,
                ItemNavigationNodeId.Armor,
                ItemNavigationNodeId.Vanity,
                ItemNavigationNodeId.Blocks,
                ItemNavigationNodeId.Furniture,
                ItemNavigationNodeId.Accessories,
                ItemNavigationNodeId.MiscAccessories,
                ItemNavigationNodeId.Consumables,
                ItemNavigationNodeId.Tools,
                ItemNavigationNodeId.Materials,
                ItemNavigationNodeId.Misc
            ];

            Assert.That(GetChildIds(ItemNavigationNodeId.AllItems), Is.EqualTo(expected));
        }

        [Test]
        public void WeaponsTree_MatchesAcceptedProjection()
        {
            Assert.That(
                GetChildIds(ItemNavigationNodeId.Weapons),
                Is.EqualTo(
                [
                    ItemNavigationNodeId.WeaponsMelee,
                    ItemNavigationNodeId.WeaponsMagic,
                    ItemNavigationNodeId.WeaponsRanged,
                    ItemNavigationNodeId.WeaponsSummon
                ]));
            Assert.That(
                GetChildIds(ItemNavigationNodeId.WeaponsMelee),
                Is.EqualTo([ItemNavigationNodeId.WeaponsMeleeYoyos]));
            Assert.That(
                GetChildIds(ItemNavigationNodeId.WeaponsRanged),
                Is.EqualTo(
                [
                    ItemNavigationNodeId.WeaponsRangedArrowFamilyWeapons,
                    ItemNavigationNodeId.WeaponsRangedBulletFamilyWeapons,
                    ItemNavigationNodeId.WeaponsRangedSpecialist,
                    ItemNavigationNodeId.WeaponsRangedAmmo
                ]));
            Assert.That(
                GetChildIds(ItemNavigationNodeId.WeaponsRangedAmmo),
                Is.EqualTo(
                [
                    ItemNavigationNodeId.WeaponsRangedAmmoArrows,
                    ItemNavigationNodeId.WeaponsRangedAmmoBullets,
                    ItemNavigationNodeId.WeaponsRangedAmmoSpecialist
                ]));
            Assert.That(
                GetChildIds(ItemNavigationNodeId.WeaponsSummon),
                Is.EqualTo(
                [
                    ItemNavigationNodeId.WeaponsSummonWhips,
                    ItemNavigationNodeId.WeaponsSummonSentries
                ]));
        }

        [Test]
        public void FurnitureTree_MatchesAcceptedProjection()
        {
            Assert.That(
                GetChildIds(ItemNavigationNodeId.Furniture),
                Is.EqualTo(
                [
                    ItemNavigationNodeId.FurnitureContainers,
                    ItemNavigationNodeId.FurnitureStatues,
                    ItemNavigationNodeId.FurniturePlatforms,
                    ItemNavigationNodeId.FurnitureDoors,
                    ItemNavigationNodeId.FurnitureChairs,
                    ItemNavigationNodeId.FurnitureTables,
                    ItemNavigationNodeId.FurnitureCraftingStations,
                    ItemNavigationNodeId.FurnitureLightSources,
                    ItemNavigationNodeId.FurnitureBanners,
                    ItemNavigationNodeId.FurnitureWallDecorations,
                    ItemNavigationNodeId.FurnitureCritters
                ]));
            Assert.That(
                GetChildIds(ItemNavigationNodeId.FurnitureLightSources),
                Is.EqualTo([ItemNavigationNodeId.FurnitureLightSourcesTorches]));
        }

        [Test]
        public void Materials_RemainsLeafRoot()
        {
            Assert.That(ItemTaxonomyDefinitions.GetChildren(ItemNavigationNodeId.Materials), Is.Empty);
        }

        [Test]
        public void MiscTree_ContainsQuestFish()
        {
            Assert.That(GetChildIds(ItemNavigationNodeId.Misc), Is.EqualTo([ItemNavigationNodeId.MiscQuestFish]));

            ItemNavigationNodeDefinition questFish =
                ItemTaxonomyDefinitions.GetNavigationNode(ItemNavigationNodeId.MiscQuestFish);

            Assert.That(questFish.ParentId, Is.EqualTo(ItemNavigationNodeId.Misc));
            Assert.That(questFish.ScopeKind, Is.EqualTo(ItemNavigationScopeKind.SemanticAny));
            Assert.That(questFish.SemanticAnyOf, Is.EqualTo(ItemSemanticMembership.QuestFish));
        }

        [Test]
        public void RangedNode_IsGroupingOnlyOverRangedWeaponAndAmmo()
        {
            ItemNavigationNodeDefinition ranged =
                ItemTaxonomyDefinitions.GetNavigationNode(ItemNavigationNodeId.WeaponsRanged);

            Assert.That(ranged.IsGroupingOnly, Is.True);
            Assert.That(ranged.ScopeKind, Is.EqualTo(ItemNavigationScopeKind.SemanticAny));
            Assert.That(
                ranged.SemanticAnyOf,
                Is.EqualTo(ItemSemanticMembership.RangedWeapon | ItemSemanticMembership.Ammo));
        }

        [Test]
        public void AmmoSemanticIdentity_IsReusedByNavigationAndFacet()
        {
            ItemNavigationNodeDefinition ammoNode =
                ItemTaxonomyDefinitions.GetNavigationNode(ItemNavigationNodeId.WeaponsRangedAmmo);
            ItemTaxonomyFacetDefinition ammoFacet = ItemTaxonomyDefinitions.GetFacet(ItemTaxonomyFacetId.Ammo);

            Assert.That(ammoNode.SemanticAnyOf, Is.EqualTo(ItemSemanticMembership.Ammo));
            Assert.That(ammoFacet.SemanticMembership, Is.EqualTo(ItemSemanticMembership.Ammo));
        }

        [Test]
        public void FishingFacet_UsesDirectFishingSemanticMembership()
        {
            ItemTaxonomyFacetDefinition fishingFacet = ItemTaxonomyDefinitions.GetFacet(ItemTaxonomyFacetId.Fishing);

            Assert.That(fishingFacet.SemanticMembership, Is.EqualTo(ItemSemanticMembership.Fishing));
        }

        [Test]
        public void SemanticNode_RequiresItsNativeNavigationContext()
        {
            var outsideWeapons = new ItemCatalogEntry(
                1,
                "Ammo outside Weapons",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.Ammo);
            var insideWeapons = new ItemCatalogEntry(
                2,
                "Ammo inside Weapons",
                ItemCategoryMembership.Weapons,
                semanticMemberships: ItemSemanticMembership.Ammo);

            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(outsideWeapons, ItemNavigationNodeId.WeaponsRangedAmmo),
                Is.False);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(insideWeapons, ItemNavigationNodeId.WeaponsRangedAmmo),
                Is.True);
        }

        [Test]
        public void MiscQuestFish_RequiresMiscNavigationContext()
        {
            var outsideMisc = new ItemCatalogEntry(
                1,
                "Quest Fish outside Misc",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.QuestFish);
            var insideMisc = new ItemCatalogEntry(
                2,
                "Quest Fish inside Misc",
                ItemCategoryMembership.Misc,
                semanticMemberships: ItemSemanticMembership.QuestFish);

            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(outsideMisc, ItemNavigationNodeId.MiscQuestFish),
                Is.False);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(insideMisc, ItemNavigationNodeId.MiscQuestFish),
                Is.True);
        }

        [Test]
        public void SemanticMembership_DoesNotImplyItsDeclaredSemanticParent()
        {
            var entry = new ItemCatalogEntry(
                1,
                "Yoyo only",
                ItemCategoryMembership.Weapons,
                semanticMemberships: ItemSemanticMembership.Yoyos);

            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(entry, ItemNavigationNodeId.WeaponsMeleeYoyos),
                Is.False);
        }

        [Test]
        public void Other_SubtractsUnionOfDirectChildren()
        {
            var residual = new ItemCatalogEntry(
                1,
                "Other melee",
                ItemCategoryMembership.Weapons,
                semanticMemberships: ItemSemanticMembership.Melee);
            var yoyo = new ItemCatalogEntry(
                2,
                "Yoyo",
                ItemCategoryMembership.Weapons,
                semanticMemberships: ItemSemanticMembership.Melee | ItemSemanticMembership.Yoyos);

            Assert.That(ItemTaxonomyDefinitions.MatchesOther(residual, ItemNavigationNodeId.WeaponsMelee), Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesOther(yoyo, ItemNavigationNodeId.WeaponsMelee), Is.False);
        }

        [Test]
        public void OtherMisc_ExcludesQuestFishAndKeepsResidualItems()
        {
            var questFish = new ItemCatalogEntry(
                1,
                "Quest Fish",
                ItemCategoryMembership.Misc,
                semanticMemberships: ItemSemanticMembership.QuestFish);
            var residual = new ItemCatalogEntry(2, "Residual Misc", ItemCategoryMembership.Misc);
            var outsideMisc = new ItemCatalogEntry(
                3,
                "Quest Fish outside Misc",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.QuestFish);

            Assert.That(ItemTaxonomyDefinitions.MatchesOther(questFish, ItemNavigationNodeId.Misc), Is.False);
            Assert.That(ItemTaxonomyDefinitions.MatchesOther(residual, ItemNavigationNodeId.Misc), Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesOther(outsideMisc, ItemNavigationNodeId.Misc), Is.False);
        }

        [Test]
        public void Other_AllowsOverlappingDirectChildrenWithoutRequiringExclusiveMembership()
        {
            var overlappingTool = new ItemCatalogEntry(
                1,
                "Multi-tool",
                ItemCategoryMembership.Tools,
                semanticMemberships: ItemSemanticMembership.Pickaxes | ItemSemanticMembership.Axes);

            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(overlappingTool, ItemNavigationNodeId.ToolsPickaxes),
                Is.True);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesNavigationNode(overlappingTool, ItemNavigationNodeId.ToolsAxes),
                Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesOther(overlappingTool, ItemNavigationNodeId.Tools), Is.False);
        }

        [Test]
        public void HasOtherItems_UsesContextualResidualForNavigationNode()
        {
            var catalog = ItemCatalog.Create(
            [
                new ItemCatalogEntry(
                    1,
                    "Covered melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee | ItemSemanticMembership.Yoyos),
                new ItemCatalogEntry(
                    2,
                    "Residual melee",
                    ItemCategoryMembership.Weapons,
                    semanticMemberships: ItemSemanticMembership.Melee)
            ]);

            Assert.That(ItemTaxonomyDefinitions.HasOtherItems(catalog, ItemNavigationNodeId.WeaponsMelee), Is.True);
            Assert.That(
                ItemTaxonomyDefinitions.HasOtherItems(catalog, ItemNavigationNodeId.WeaponsMeleeYoyos),
                Is.False);
        }

        [Test]
        public void Other_IsUnavailableForLeafNodes()
        {
            var entry = new ItemCatalogEntry(
                1,
                "Wing",
                ItemCategoryMembership.Accessories,
                semanticMemberships: ItemSemanticMembership.Wings);

            Assert.That(ItemTaxonomyDefinitions.HasChildren(ItemNavigationNodeId.AccessoriesWings), Is.False);
            Assert.That(ItemTaxonomyDefinitions.MatchesOther(entry, ItemNavigationNodeId.AccessoriesWings), Is.False);
        }

        [Test]
        public void Facets_ExcludeQuestFishNavigationProjection()
        {
            ItemTaxonomyFacetId[] expected =
            [
                ItemTaxonomyFacetId.Materials,
                ItemTaxonomyFacetId.Consumables,
                ItemTaxonomyFacetId.Ammo,
                ItemTaxonomyFacetId.OpenableItems,
                ItemTaxonomyFacetId.BossSummonItems,
                ItemTaxonomyFacetId.MusicBoxes,
                ItemTaxonomyFacetId.Dyes,
                ItemTaxonomyFacetId.Fishing
            ];

            Assert.That(ItemTaxonomyDefinitions.Facets.Select(facet => facet.Id), Is.EqualTo(expected));
            Assert.That(ItemTaxonomyDefinitions.IsKnownFacetMask((ItemTaxonomyFacetId)(1 << 8)), Is.False);
        }

        [Test]
        public void Facets_MatchNativeAndSemanticSources()
        {
            var entry = new ItemCatalogEntry(
                1,
                "Material Ammo",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.Ammo);

            Assert.That(ItemTaxonomyDefinitions.MatchesFacet(entry, ItemTaxonomyFacetId.Materials), Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesFacet(entry, ItemTaxonomyFacetId.Ammo), Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesFacet(entry, ItemTaxonomyFacetId.Consumables), Is.False);
        }

        [Test]
        public void Facets_MultipleIncludedValuesUseAndSemantics()
        {
            var both = new ItemCatalogEntry(
                1,
                "Material Ammo",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.Ammo);
            var ammoOnly = new ItemCatalogEntry(
                2,
                "Ammo",
                ItemCategoryMembership.Misc,
                semanticMemberships: ItemSemanticMembership.Ammo);
            const ItemTaxonomyFacetId included = ItemTaxonomyFacetId.Materials | ItemTaxonomyFacetId.Ammo;

            Assert.That(ItemTaxonomyDefinitions.MatchesFacets(both, included, ItemTaxonomyFacetId.None), Is.True);
            Assert.That(ItemTaxonomyDefinitions.MatchesFacets(ammoOnly, included, ItemTaxonomyFacetId.None), Is.False);
        }

        [Test]
        public void Facets_ExcludedValueRejectsMatchingItems()
        {
            var ammo = new ItemCatalogEntry(1, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo);
            var other = new ItemCatalogEntry(2, "Other");

            Assert.That(
                ItemTaxonomyDefinitions.MatchesFacets(ammo, ItemTaxonomyFacetId.None, ItemTaxonomyFacetId.Ammo),
                Is.False);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesFacets(other, ItemTaxonomyFacetId.None, ItemTaxonomyFacetId.Ammo),
                Is.True);
        }

        [Test]
        public void Facets_MultipleExcludedValuesRejectAnyMatchingFacet()
        {
            var material = new ItemCatalogEntry(1, "Material", ItemCategoryMembership.Materials);
            var ammo = new ItemCatalogEntry(2, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo);
            var neither = new ItemCatalogEntry(3, "Other");
            const ItemTaxonomyFacetId excluded = ItemTaxonomyFacetId.Materials | ItemTaxonomyFacetId.Ammo;

            Assert.That(ItemTaxonomyDefinitions.MatchesFacets(material, ItemTaxonomyFacetId.None, excluded), Is.False);
            Assert.That(ItemTaxonomyDefinitions.MatchesFacets(ammo, ItemTaxonomyFacetId.None, excluded), Is.False);
            Assert.That(ItemTaxonomyDefinitions.MatchesFacets(neither, ItemTaxonomyFacetId.None, excluded), Is.True);
        }

        [Test]
        public void Facets_IncludedAndExcludedValuesCombine()
        {
            var material = new ItemCatalogEntry(1, "Material", ItemCategoryMembership.Materials);
            var materialAmmo = new ItemCatalogEntry(
                2,
                "Material Ammo",
                ItemCategoryMembership.Materials,
                semanticMemberships: ItemSemanticMembership.Ammo);
            var ammo = new ItemCatalogEntry(3, "Ammo", semanticMemberships: ItemSemanticMembership.Ammo);

            Assert.That(
                ItemTaxonomyDefinitions.MatchesFacets(
                    material,
                    ItemTaxonomyFacetId.Materials,
                    ItemTaxonomyFacetId.Ammo),
                Is.True);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesFacets(
                    materialAmmo,
                    ItemTaxonomyFacetId.Materials,
                    ItemTaxonomyFacetId.Ammo),
                Is.False);
            Assert.That(
                ItemTaxonomyDefinitions.MatchesFacets(ammo, ItemTaxonomyFacetId.Materials, ItemTaxonomyFacetId.Ammo),
                Is.False);
        }

        [Test]
        public void Definitions_HaveUniqueStableIdsAndValidParents()
        {
            IReadOnlyList<ItemNavigationNodeDefinition> nodes = ItemTaxonomyDefinitions.NavigationNodes;
            IReadOnlyList<ItemTaxonomyFacetDefinition> facets = ItemTaxonomyDefinitions.Facets;

            Assert.That(nodes.Select(node => node.Id).Distinct().Count(), Is.EqualTo(nodes.Count));
            Assert.That(facets.Select(facet => facet.Id).Distinct().Count(), Is.EqualTo(facets.Count));

            var knownNodeIds = new HashSet<ItemNavigationNodeId>(nodes.Select(node => node.Id));

            foreach (ItemNavigationNodeDefinition node in nodes)
            {
                if (node.ParentId == ItemNavigationNodeId.None)
                {
                    Assert.That(node.Id, Is.EqualTo(ItemNavigationNodeId.AllItems));
                    continue;
                }

                Assert.That(knownNodeIds.Contains(node.ParentId), Is.True, $"Unknown parent for {node.Id}.");
            }
        }

        [Test]
        public void Facets_UnknownIncludedMaskIsRejected()
        {
            var entry = new ItemCatalogEntry(1, "Item");
            var unknown = (ItemTaxonomyFacetId)(1 << 20);

            Assert.That(ItemTaxonomyDefinitions.IsKnownFacetMask(unknown), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { ItemTaxonomyDefinitions.MatchesFacets(entry, unknown, ItemTaxonomyFacetId.None); }));
        }

        [Test]
        public void Facets_UnknownExcludedMaskIsRejected()
        {
            var entry = new ItemCatalogEntry(1, "Item");
            var unknown = (ItemTaxonomyFacetId)(1 << 20);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { ItemTaxonomyDefinitions.MatchesFacets(entry, ItemTaxonomyFacetId.None, unknown); }));
        }

        [Test]
        public void Facets_OverlappingIncludedAndExcludedMasksAreRejected()
        {
            var entry = new ItemCatalogEntry(1, "Item");

            Assert.Throws<ArgumentException>(
                (Action)(() =>
                {
                    ItemTaxonomyDefinitions.MatchesFacets(
                        entry,
                        ItemTaxonomyFacetId.Ammo,
                        ItemTaxonomyFacetId.Ammo);
                }));
        }

        private static ItemNavigationNodeId[] GetChildIds(ItemNavigationNodeId parentId)
        {
            return ItemTaxonomyDefinitions.GetChildren(parentId).Select(child => child.Id).ToArray();
        }
    }
}