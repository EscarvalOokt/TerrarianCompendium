using System;
using NUnit.Framework;
using TerrarianCompendium.ArmorSets;

namespace TerrarianCompendium.Tests.ArmorSets
{
    [TestFixture]
    public sealed class ArmorSetCatalogBuilderTests
    {
        [Test]
        public void Build_SameBonusWithAlternativeHead_GroupsIntoOneLogicalEntry()
        {
            ArmorSetCatalog catalog = Build(
                Source("palladium", "ArmorSetBonus.Palladium", 1, 10, 20),
                Source("palladium", "ArmorSetBonus.Palladium", 2, 10, 20),
                Source("palladium", "ArmorSetBonus.Palladium", 3, 10, 20));

            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[0].Variants, Has.Count.EqualTo(3));
            Assert.That(catalog.Entries[0].RepresentativeItemId, Is.EqualTo(1));
        }

        [Test]
        public void Build_SameBodyAndLegsWithDifferentBonusIdentities_RemainSeparate()
        {
            ArmorSetCatalog catalog = Build(
                Source("caster", "ArmorSetBonus.Caster", 1, 10, 20, ArmorSetPrimaryPart.Head),
                Source("melee", "ArmorSetBonus.Melee", 2, 10, 20, ArmorSetPrimaryPart.Head),
                Source("ranged", "ArmorSetBonus.Ranged", 3, 10, 20, ArmorSetPrimaryPart.Head));

            Assert.That(catalog.Entries, Has.Count.EqualTo(3));
            Assert.That(catalog.Entries[0].RepresentativeItemId, Is.EqualTo(1));
            Assert.That(catalog.Entries[1].RepresentativeItemId, Is.EqualTo(2));
            Assert.That(catalog.Entries[2].RepresentativeItemId, Is.EqualTo(3));
        }

        [Test]
        public void Build_SameBonusWithDisconnectedFamilies_RemainSeparate()
        {
            ArmorSetCatalog catalog = Build(
                Source("wood", "ArmorSetBonus.Wood", 1, 2, 3),
                Source("wood", "ArmorSetBonus.Wood", 10, 11, 12));

            Assert.That(catalog.Entries, Has.Count.EqualTo(2));
            Assert.That(catalog.Entries[0].Variants, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[1].Variants, Has.Count.EqualTo(1));
        }

        [Test]
        public void Build_StructuralChain_FormsOneConnectedComponent()
        {
            ArmorSetCatalog catalog = Build(
                Source("chain", "ArmorSetBonus.Chain", 1, 10, 20),
                Source("chain", "ArmorSetBonus.Chain", 2, 10, 20),
                Source("chain", "ArmorSetBonus.Chain", 2, 11, 20));

            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[0].Variants, Has.Count.EqualTo(3));
        }

        [Test]
        public void Build_DiagonalVariants_DoNotInventCartesianCombinations()
        {
            ArmorSetCatalog catalog = Build(
                Source("diagonal", "ArmorSetBonus.Diagonal", 1, 10, 20),
                Source("diagonal", "ArmorSetBonus.Diagonal", 2, 11, 20));

            Assert.That(catalog.Entries, Has.Count.EqualTo(2));
            Assert.That(catalog.Entries[0].Variants, Is.EqualTo([new ArmorSetVariant(1, 10, 20)]));
            Assert.That(catalog.Entries[1].Variants, Is.EqualTo([new ArmorSetVariant(2, 11, 20)]));
        }

        [Test]
        public void Build_TwoPartVariant_PreservesMissingLegsSentinel()
        {
            ArmorSetCatalog catalog = Build(Source("wizard", "ArmorSetBonus.Wizard", 1, 10, 0));

            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[0].Variants[0].LegItemId, Is.Zero);
        }

        [Test]
        public void Build_AssignsIdsByFirstSourceAppearanceAcrossBonusGroups()
        {
            ArmorSetCatalog catalog = Build(
                Source("first", "ArmorSetBonus.First", 1, 2, 3),
                Source("second", "ArmorSetBonus.Second", 4, 5, 6),
                Source("first", "ArmorSetBonus.First", 10, 11, 12));

            Assert.That(catalog.Entries, Has.Count.EqualTo(3));
            Assert.That(catalog.Entries[0].Id, Is.EqualTo(1));
            Assert.That(catalog.Entries[0].BonusTextKey, Is.EqualTo("ArmorSetBonus.First"));
            Assert.That(catalog.Entries[1].Id, Is.EqualTo(2));
            Assert.That(catalog.Entries[1].BonusTextKey, Is.EqualTo("ArmorSetBonus.Second"));
            Assert.That(catalog.Entries[2].Id, Is.EqualTo(3));
            Assert.That(catalog.Entries[2].BonusTextKey, Is.EqualTo("ArmorSetBonus.First"));
        }

        [Test]
        public void Build_PrimaryPart_WinsRepresentativeSelection()
        {
            ArmorSetCatalog catalog = Build(
                Source("body", "ArmorSetBonus.Body", 1, 10, 20, ArmorSetPrimaryPart.Body),
                Source("body", "ArmorSetBonus.Body", 2, 10, 20, ArmorSetPrimaryPart.Body));

            Assert.That(catalog.Entries[0].RepresentativeItemId, Is.EqualTo(10));
        }

        [Test]
        public void Build_PrimaryPartUsesFirstVariantEvenWhenPartDiffersAcrossVariants()
        {
            ArmorSetCatalog catalog = Build(
                Source("body", "ArmorSetBonus.Body", 1, 10, 20, ArmorSetPrimaryPart.Body),
                Source("body", "ArmorSetBonus.Body", 1, 11, 20, ArmorSetPrimaryPart.Body));

            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[0].RepresentativeItemId, Is.EqualTo(10));
        }

        [Test]
        public void Build_ComponentWithoutCommonPart_FallsBackToFirstVariantHead()
        {
            ArmorSetCatalog catalog = Build(
                Source("chain", "ArmorSetBonus.Chain", 1, 10, 20),
                Source("chain", "ArmorSetBonus.Chain", 2, 10, 20),
                Source("chain", "ArmorSetBonus.Chain", 2, 11, 20),
                Source("chain", "ArmorSetBonus.Chain", 2, 11, 21));

            Assert.That(catalog.Entries, Has.Count.EqualTo(1));
            Assert.That(catalog.Entries[0].RepresentativeItemId, Is.EqualTo(1));
        }

        [Test]
        public void Build_DuplicateVariantWithinBonusIdentity_Throws()
        {
            Assert.Throws<ArgumentException>(() => Build(
                Source("duplicate", "ArmorSetBonus.Duplicate", 1, 2, 3),
                Source("duplicate", "ArmorSetBonus.Duplicate", 1, 2, 3)));
        }

        private static ArmorSetCatalog Build(params ArmorSetCatalogSourceEntry[] entries)
        {
            return new ArmorSetCatalogBuilder().Build(entries);
        }

        private static ArmorSetCatalogSourceEntry Source(
            string bonusIdentity,
            string bonusTextKey,
            int head,
            int body,
            int legs,
            ArmorSetPrimaryPart primaryPart = ArmorSetPrimaryPart.None)
        {
            return new ArmorSetCatalogSourceEntry(
                bonusIdentity,
                bonusTextKey,
                primaryPart,
                new ArmorSetVariant(head, body, legs));
        }
    }
}