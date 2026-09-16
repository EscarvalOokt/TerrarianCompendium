using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class ItemTaxonomyIconDefinitionsTests
    {
        [Test]
        public void JourneyIcons_MatchConfirmedTopLevelFrames()
        {
            var expected = new Dictionary<ItemNavigationNodeId, int>
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

            foreach (KeyValuePair<ItemNavigationNodeId, int> pair in expected)
            {
                Assert.That(
                    ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(pair.Key, out int frameIndex),
                    Is.True,
                    $"Missing native Journey icon frame for {pair.Key}.");
                Assert.That(frameIndex, Is.EqualTo(pair.Value), $"Unexpected Journey frame for {pair.Key}.");
            }
        }

        [Test]
        public void JourneyIcons_CoverEveryTopLevelRootExactlyOnce()
        {
            var frameIndices = new HashSet<int>();
            IReadOnlyList<ItemNavigationNodeDefinition> roots =
                ItemTaxonomyDefinitions.GetChildren(ItemNavigationNodeId.AllItems);

            Assert.That(roots.Count, Is.EqualTo(11));

            foreach (ItemNavigationNodeDefinition root in roots)
            {
                Assert.That(
                    ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(root.Id, out int frameIndex),
                    Is.True,
                    $"Missing native Journey icon frame for {root.Id}.");
                Assert.That(frameIndex, Is.InRange(0, 10));
                Assert.That(frameIndices.Add(frameIndex), Is.True, $"Journey frame {frameIndex} is duplicated.");
            }

            Assert.That(frameIndices.Count, Is.EqualTo(roots.Count));
            Assert.That(
                ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(ItemNavigationNodeId.AllItems, out _),
                Is.False);
        }

        [Test]
        public void NavigationIcons_CoverEveryNamedRefinement()
        {
            var rootIds = new HashSet<ItemNavigationNodeId>();

            foreach (ItemNavigationNodeDefinition root in ItemTaxonomyDefinitions.GetChildren(
                         ItemNavigationNodeId.AllItems))
            {
                rootIds.Add(root.Id);
            }

            foreach (ItemNavigationNodeDefinition node in ItemTaxonomyDefinitions.NavigationNodes)
            {
                if (node.Id == ItemNavigationNodeId.AllItems || rootIds.Contains(node.Id))
                    continue;

                Assert.That(
                    ItemTaxonomyIconDefinitions.TryGetNavigationItemId(node.Id, out int itemId),
                    Is.True,
                    $"Missing representative item icon for {node.Id}.");
                Assert.That(itemId, Is.GreaterThan(0), $"Representative item ID for {node.Id} must be positive.");
            }
        }

        [Test]
        public void NavigationIcons_DoNotReplaceTopLevelJourneyIcons()
        {
            Assert.That(
                ItemTaxonomyIconDefinitions.TryGetNavigationItemId(ItemNavigationNodeId.AllItems, out _),
                Is.False);

            foreach (ItemNavigationNodeDefinition root in ItemTaxonomyDefinitions.GetChildren(
                         ItemNavigationNodeId.AllItems))
            {
                Assert.Multiple(() =>
                {
                    Assert.That(
                        ItemTaxonomyIconDefinitions.TryGetNavigationItemId(root.Id, out _),
                        Is.False,
                        $"Top-level root {root.Id} must keep its native Journey icon.");
                    Assert.That(
                        ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(root.Id, out _),
                        Is.True,
                        $"Top-level root {root.Id} must have a native Journey icon frame.");
                });
            }
        }

        [Test]
        public void FacetIcons_CoverEveryExposedFacet()
        {
            foreach (ItemTaxonomyFacetDefinition facet in ItemTaxonomyDefinitions.Facets)
            {
                int itemId = ItemTaxonomyIconDefinitions.GetFacetItemId(facet.Id);

                Assert.That(itemId, Is.GreaterThan(0), $"Representative item ID for {facet.Id} must be positive.");
            }
        }

        [Test]
        public void UnknownNavigationNode_IsRejected()
        {
            var unknown = (ItemNavigationNodeId)int.MaxValue;

            Assert.Multiple(() =>
            {
                Assert.That(ItemTaxonomyIconDefinitions.TryGetJourneyFrameIndex(unknown, out _), Is.False);
                Assert.That(ItemTaxonomyIconDefinitions.TryGetNavigationItemId(unknown, out _), Is.False);
            });
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => ItemTaxonomyIconDefinitions.GetNavigationItemId(unknown)));
        }

        [Test]
        public void UnknownFacet_IsRejected()
        {
            var unknown = (ItemTaxonomyFacetId)(1 << 20);

            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => ItemTaxonomyIconDefinitions.GetFacetItemId(unknown)));
        }
    }
}