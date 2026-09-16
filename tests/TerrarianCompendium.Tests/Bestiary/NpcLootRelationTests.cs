using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Bestiary;

namespace TerrarianCompendium.Tests.Bestiary
{
    [TestFixture]
    public sealed class NpcLootRelationTests
    {
        [Test]
        public void Constructor_WithValidValues_PreservesMetadata()
        {
            var relation = new NpcLootRelation(-5, 100, 2, 7, 0.25f);

            Assert.That(relation.NpcNetId, Is.EqualTo(-5));
            Assert.That(relation.ItemId, Is.EqualTo(100));
            Assert.That(relation.StackMin, Is.EqualTo(2));
            Assert.That(relation.StackMax, Is.EqualTo(7));
            Assert.That(relation.DropRate, Is.EqualTo(0.25f));
            Assert.That(relation.Conditions, Is.Empty);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Constructor_WithNonPositiveItemId_Throws(int itemId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, itemId, 1, 1, 0.5f); }));
        }

        [Test]
        public void Constructor_WithNegativeMinimumStack_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, 100, -1, 1, 0.5f); }));
        }

        [Test]
        public void Constructor_WithMaximumStackBelowMinimum_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, 100, 2, 1, 0.5f); }));
        }

        [Test]
        public void Constructor_WithNegativeDropRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, 100, 1, 1, -0.01f); }));
        }

        [Test]
        public void Constructor_WithNaNDropRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, 100, 1, 1, float.NaN); }));
        }

        [Test]
        public void Constructor_WithInfiniteDropRate_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (Action)(() => { _ = new NpcLootRelation(10, 100, 1, 1, float.PositiveInfinity); }));
        }

        [Test]
        public void Conditions_AreCopiedAndExposedReadOnly()
        {
            var source = new List<NpcLootCondition>();
            var relation = new NpcLootRelation(10, 100, 1, 1, 0.5f, source);

            source.Add(null);

            Assert.That(relation.Conditions, Is.Empty);

            var exposed = (IList<NpcLootCondition>)relation.Conditions;
            Assert.Throws<NotSupportedException>((Action)(() => { exposed.Add(null); }));
        }

        [Test]
        public void SeparateRelations_ForSameNpcAndItem_AreAllowed()
        {
            var first = new NpcLootRelation(10, 100, 1, 1, 0.25f);
            var second = new NpcLootRelation(10, 100, 1, 1, 0.5f);

            Assert.That(first.NpcNetId, Is.EqualTo(second.NpcNetId));
            Assert.That(first.ItemId, Is.EqualTo(second.ItemId));
            Assert.That(first.DropRate, Is.Not.EqualTo(second.DropRate));
        }
    }
}