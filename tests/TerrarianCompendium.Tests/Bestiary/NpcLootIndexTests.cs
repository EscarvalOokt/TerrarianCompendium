using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.Tests.Bestiary
{
    [TestFixture]
    public sealed class NpcLootIndexTests
    {
        [Test]
        public void Create_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { NpcLootIndex.Create(null, []); }));
        }

        [Test]
        public void Create_WithNullRelations_Throws()
        {
            NpcCatalog catalog = CreateCatalog();

            Assert.Throws<ArgumentNullException>((Action)(() => { NpcLootIndex.Create(catalog, null); }));
        }

        [Test]
        public void GetDropsForNpc_ReturnsAllRelationsForNpc()
        {
            NpcCatalog catalog = CreateCatalog();
            var first = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var second = new NpcLootRelation(20, 200, 1, 1, 0.5f);
            var third = new NpcLootRelation(10, 300, 2, 4, 0.75f);
            var index = NpcLootIndex.Create(catalog, [first, second, third]);

            Assert.That(index.GetDropsForNpc(10), Is.EqualTo([first, third]));
        }

        [Test]
        public void GetNpcSourcesForItem_ReturnsAllRelationsForItem()
        {
            NpcCatalog catalog = CreateCatalog();
            var first = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var second = new NpcLootRelation(20, 100, 1, 1, 0.5f);
            var third = new NpcLootRelation(10, 200, 1, 1, 0.75f);
            var index = NpcLootIndex.Create(catalog, [first, second, third]);

            Assert.That(index.GetNpcSourcesForItem(100), Is.EqualTo([first, second]));
            Assert.That(index.HasNpcSourceForItem(100), Is.True);
            Assert.That(index.HasNpcSourceForItem(300), Is.False);
        }

        [Test]
        public void Relations_AreSharedBetweenBothDirections()
        {
            NpcCatalog catalog = CreateCatalog();
            var relation = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var index = NpcLootIndex.Create(catalog, [relation]);

            Assert.That(index.GetDropsForNpc(10)[0], Is.SameAs(relation));
            Assert.That(index.GetNpcSourcesForItem(100)[0], Is.SameAs(relation));
        }

        [Test]
        public void Create_WithDuplicateNpcItemRows_PreservesEveryRelation()
        {
            NpcCatalog catalog = CreateCatalog();
            var first = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var second = new NpcLootRelation(10, 100, 1, 1, 0.5f);
            var index = NpcLootIndex.Create(catalog, [first, second]);

            Assert.That(index.GetDropsForNpc(10), Is.EqualTo([first, second]));
            Assert.That(index.GetNpcSourcesForItem(100), Is.EqualTo([first, second]));
        }

        [Test]
        public void Create_WithRelationForUnknownNpc_Throws()
        {
            NpcCatalog catalog = CreateCatalog();
            var relation = new NpcLootRelation(999, 100, 1, 1, 0.25f);

            Assert.Throws<ArgumentException>((Action)(() => { NpcLootIndex.Create(catalog, [relation]); }));
        }

        [Test]
        public void Queries_WithMissingIds_ReturnEmptyResults()
        {
            var index = NpcLootIndex.Create(CreateCatalog(), []);

            Assert.That(index.GetDropsForNpc(999), Is.Empty);
            Assert.That(index.GetNpcSourcesForItem(999), Is.Empty);
            Assert.That(index.HasNpcSourceForItem(999), Is.False);
        }

        [Test]
        public void Create_CopiesInputRelations()
        {
            NpcCatalog catalog = CreateCatalog();
            var relations = new List<NpcLootRelation>
            {
                new NpcLootRelation(10, 100, 1, 1, 0.25f)
            };

            var index = NpcLootIndex.Create(catalog, relations);
            relations.Add(new NpcLootRelation(10, 200, 1, 1, 0.5f));

            Assert.That(index.GetDropsForNpc(10), Has.Count.EqualTo(1));
            Assert.That(index.GetNpcSourcesForItem(200), Is.Empty);
        }

        [Test]
        public void RelationResults_AreReadOnly()
        {
            NpcCatalog catalog = CreateCatalog();
            var relation = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var index = NpcLootIndex.Create(catalog, [relation]);
            var drops = (IList<NpcLootRelation>)index.GetDropsForNpc(10);
            var sources = (IList<NpcLootRelation>)index.GetNpcSourcesForItem(100);

            Assert.Throws<NotSupportedException>((Action)(() => { drops.Add(relation); }));
            Assert.Throws<NotSupportedException>((Action)(() => { sources.Add(relation); }));
        }

        [Test]
        public void ItemRelation_DoesNotRequireItemCatalogMembership()
        {
            NpcCatalog catalog = CreateCatalog();
            var relation = new NpcLootRelation(10, int.MaxValue, 1, 1, 0.25f);

            var index = NpcLootIndex.Create(catalog, [relation]);

            Assert.That(index.GetNpcSourcesForItem(int.MaxValue), Is.EqualTo([relation]));
        }

        private static NpcCatalog CreateCatalog()
        {
            return NpcCatalog.Create(
            [
                new NpcCatalogEntry(10, 0),
                new NpcCatalogEntry(20, 1),
                new NpcCatalogEntry(-5, 2)
            ]);
        }
    }
}