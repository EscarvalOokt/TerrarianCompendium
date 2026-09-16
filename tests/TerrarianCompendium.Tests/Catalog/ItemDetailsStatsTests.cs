using NUnit.Framework;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Tests.Catalog
{
    [TestFixture]
    public sealed class ItemDetailsStatsTests
    {
        [Test]
        public void ProjectBaseItem_MeleeWeapon_ProjectsBaseCombatStatsIncludingZeroKnockback()
        {
            ItemDetailsStats stats = Project(damage: 20, melee: true, knockback: 0f, crit: 0, useTime: 18, useStyle: 1);

            Assert.Multiple(() =>
            {
                Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.Melee));
                Assert.That(stats.Knockback, Is.EqualTo(0f));
                Assert.That(stats.BaseCriticalHitChance, Is.EqualTo(4));
                Assert.That(stats.UseTimeTicks, Is.EqualTo(18));
                Assert.That(stats.TagDamage, Is.Null);
            });
        }

        [TestCase(false, true, false, (int)ItemDetailsDamageType.Ranged)]
        [TestCase(false, false, true, (int)ItemDetailsDamageType.Magic)]
        public void ProjectBaseItem_RangedOrMagicWeapon_ProjectsDamageTypeAndBaseCrit(
            bool melee,
            bool ranged,
            bool magic,
            int expectedDamageType)
        {
            ItemDetailsStats stats = Project(
                damage: 30,
                melee: melee,
                ranged: ranged,
                magic: magic,
                crit: 5,
                useTime: 20,
                useStyle: 5);

            Assert.That(stats.DamageType, Is.EqualTo((ItemDetailsDamageType)expectedDamageType));
            Assert.That(stats.BaseCriticalHitChance, Is.EqualTo(9));
        }

        [Test]
        public void ProjectBaseItem_SummonWeapon_DoesNotProjectOrdinaryCrit()
        {
            ItemDetailsStats stats = Project(damage: 25, summon: true, crit: 12, useTime: 24, useStyle: 1);

            Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.Summon));
            Assert.That(stats.BaseCriticalHitChance, Is.Null);
        }

        [Test]
        public void ProjectBaseItem_GenericDamagingItem_DoesNotProjectOrdinaryCrit()
        {
            ItemDetailsStats stats = Project(damage: 15, crit: 7, useTime: 30, useStyle: 1);

            Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.Generic));
            Assert.That(stats.BaseCriticalHitChance, Is.Null);
        }

        [TestCase(true, true, true, true, (int)ItemDetailsDamageType.Melee)]
        [TestCase(false, true, true, true, (int)ItemDetailsDamageType.Ranged)]
        [TestCase(false, false, true, true, (int)ItemDetailsDamageType.Magic)]
        [TestCase(false, false, false, true, (int)ItemDetailsDamageType.Summon)]
        public void ProjectBaseItem_OverlappingFlags_UsesVanillaDamageTypePrecedence(
            bool melee,
            bool ranged,
            bool magic,
            bool summon,
            int expectedDamageType)
        {
            ItemDetailsStats stats = Project(
                damage: 20,
                melee: melee,
                ranged: ranged,
                magic: magic,
                summon: summon,
                useTime: 20,
                useStyle: 1);

            Assert.That(stats.DamageType, Is.EqualTo((ItemDetailsDamageType)expectedDamageType));
        }

        [Test]
        public void ProjectBaseItem_NonDamagingItem_DoesNotProjectCombatOrUseStats()
        {
            ItemDetailsStats stats = Project(
                damage: 0,
                melee: true,
                ranged: true,
                magic: true,
                summon: true,
                knockback: 8f,
                crit: 9,
                useTime: 10,
                useStyle: 1);

            Assert.Multiple(() =>
            {
                Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.None));
                Assert.That(stats.Knockback, Is.Null);
                Assert.That(stats.BaseCriticalHitChance, Is.Null);
                Assert.That(stats.UseTimeTicks, Is.Null);
            });
        }

        [Test]
        public void ProjectBaseItem_DamagingAmmoLikeInput_KeepsDamageStatsButOmitsUseTime()
        {
            ItemDetailsStats stats = Project(
                damage: 12,
                ranged: true,
                knockback: 2.25f,
                crit: 0,
                useTime: 15,
                useStyle: 0);

            Assert.Multiple(() =>
            {
                Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.Ranged));
                Assert.That(stats.Knockback, Is.EqualTo(2.25f));
                Assert.That(stats.BaseCriticalHitChance, Is.EqualTo(4));
                Assert.That(stats.UseTimeTicks, Is.Null);
            });
        }

        [Test]
        public void ProjectBaseItem_DamagingToolLikeInput_IsNotExcludedFromCombatStats()
        {
            ItemDetailsStats stats = Project(damage: 16, melee: true, knockback: 5f, crit: 2, useTime: 12, useStyle: 1);

            Assert.Multiple(() =>
            {
                Assert.That(stats.DamageType, Is.EqualTo(ItemDetailsDamageType.Melee));
                Assert.That(stats.Knockback, Is.EqualTo(5f));
                Assert.That(stats.BaseCriticalHitChance, Is.EqualTo(6));
                Assert.That(stats.UseTimeTicks, Is.EqualTo(12));
            });
        }

        [Test]
        public void ProjectBaseItem_PositiveTagDamage_ProjectsStandaloneValue()
        {
            ItemDetailsStats stats = Project(damage: 20, summon: true, useTime: 30, useStyle: 1, tagDamage: 15);

            Assert.That(stats.TagDamage, Is.EqualTo(15));
        }

        [Test]
        public void ProjectBaseItem_ZeroTagDamage_DoesNotProjectTagDamage()
        {
            ItemDetailsStats stats = Project(damage: 20, summon: true, useTime: 30, useStyle: 1, tagDamage: 0);

            Assert.That(stats.TagDamage, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ProjectBaseItem_NonPositiveUseTime_DoesNotProjectUseTime(int useTime)
        {
            ItemDetailsStats stats = Project(damage: 20, melee: true, useTime: useTime, useStyle: 1);

            Assert.That(stats.UseTimeTicks, Is.Null);
        }

        private static ItemDetailsStats Project(
            int damage,
            bool melee = false,
            bool ranged = false,
            bool magic = false,
            bool summon = false,
            float knockback = 0f,
            int crit = 0,
            int useTime = 0,
            int useStyle = 0,
            int tagDamage = 0)
        {
            return ItemDetailsStatsProjector.ProjectBaseItem(
                damage,
                melee,
                ranged,
                magic,
                summon,
                knockback,
                crit,
                useTime,
                useStyle,
                tagDamage);
        }
    }
}