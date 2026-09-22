using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class MerchantConditionIconPresentationTests
    {
        [TestCaseSource(nameof(AllConditionKinds))]
        public void Describe_AllKnownKinds_HaveConcreteVisual(int kindValue)
        {
            var kind = (MerchantSourceConditionKind)kindValue;
            MerchantSourceCondition condition = CreateRepresentativeCondition(kind);

            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(condition);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.PartCount, Is.GreaterThan(0));
                Assert.That(descriptor.GetPart(0).Kind, Is.Not.EqualTo(MerchantConditionVisualKind.Generic));
            });
        }

        [Test]
        public void Describe_UnknownKind_Throws()
        {
            var condition = new MerchantSourceCondition((MerchantSourceConditionKind)int.MaxValue);

            Assert.Throws<ArgumentOutOfRangeException>(() => MerchantConditionIconPresentation.Describe(condition));
        }

        [Test]
        public void Describe_NegatedGenericCondition_ReusesBaseVisualAndMarksNegation()
        {
            var positive = new MerchantSourceCondition(MerchantSourceConditionKind.BloodMoon);
            var negative = new MerchantSourceCondition(MerchantSourceConditionKind.BloodMoon, isNegated: true);

            MerchantConditionVisualDescriptor positiveDescriptor = MerchantConditionIconPresentation.Describe(positive);
            MerchantConditionVisualDescriptor negativeDescriptor = MerchantConditionIconPresentation.Describe(negative);

            Assert.Multiple(() =>
            {
                Assert.That(positiveDescriptor.IsNegated, Is.False);
                Assert.That(negativeDescriptor.IsNegated, Is.True);
                Assert.That(negativeDescriptor.PartCount, Is.EqualTo(positiveDescriptor.PartCount));
                Assert.That(negativeDescriptor.GetPart(0).Kind, Is.EqualTo(positiveDescriptor.GetPart(0).Kind));
                Assert.That(negativeDescriptor.GetPart(0).Value, Is.EqualTo(positiveDescriptor.GetPart(0).Value));
            });
        }

        [Test]
        public void Describe_DayTime_UsesNativeDayAndNightVisualsWithoutNegationMarker()
        {
            MerchantConditionVisualDescriptor day = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.DayTime));
            MerchantConditionVisualDescriptor night = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.DayTime, isNegated: true));

            Assert.Multiple(() =>
            {
                Assert.That(day.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BestiaryTag));
                Assert.That(day.GetPart(0).Value, Is.EqualTo(36));
                Assert.That(day.IsNegated, Is.False);
                Assert.That(night.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BestiaryTag));
                Assert.That(night.GetPart(0).Value, Is.EqualTo(37));
                Assert.That(night.IsNegated, Is.False);
            });
        }

        [Test]
        public void Describe_WorldSilverOreTier_UsesOreItemDomainAndResolvedAlternative()
        {
            MerchantConditionVisualDescriptor tungsten = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.WorldSilverOreTier, argument: 168));
            MerchantConditionVisualDescriptor silver = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.WorldSilverOreTier,
                    argument: 168,
                    isNegated: true));

            Assert.Multiple(() =>
            {
                Assert.That(tungsten.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(tungsten.GetPart(0).Value, Is.EqualTo(701));
                Assert.That(tungsten.IsNegated, Is.False);
                Assert.That(silver.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(silver.GetPart(0).Value, Is.EqualTo(14));
                Assert.That(silver.IsNegated, Is.False);
            });
        }

        [Test]
        public void Describe_PlayerHasItem_UsesArgumentItemVisual()
        {
            var condition = new MerchantSourceCondition(MerchantSourceConditionKind.PlayerHasItem, argument: 29);

            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(condition);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(descriptor.GetPart(0).Value, Is.EqualTo(29));
                Assert.That(descriptor.ArgumentText, Is.Empty);
            });
        }

        [Test]
        public void Describe_NpcPresent_UsesArgumentNpcVisual()
        {
            var condition = new MerchantSourceCondition(MerchantSourceConditionKind.NpcPresent, argument: 17);

            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(condition);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Npc));
                Assert.That(descriptor.GetPart(0).Value, Is.EqualTo(17));
                Assert.That(descriptor.ArgumentText, Is.Empty);
            });
        }

        [Test]
        public void Describe_CurrentCoinRequirement_UsesPlatinumDenominationAndActualComparison()
        {
            MerchantConditionVisualDescriptor positive = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.PlayerCoinValueAtLeast, argument: 1000000));
            MerchantConditionVisualDescriptor negative = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.PlayerCoinValueAtLeast,
                    argument: 1000000,
                    isNegated: true));

            Assert.Multiple(() =>
            {
                Assert.That(positive.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(positive.GetPart(0).Value, Is.EqualTo(74));
                Assert.That(positive.ArgumentText, Is.EqualTo(">=1"));
                Assert.That(positive.IsNegated, Is.False);
                Assert.That(negative.GetPart(0).Value, Is.EqualTo(74));
                Assert.That(negative.ArgumentText, Is.EqualTo("<1"));
                Assert.That(negative.IsNegated, Is.False);
            });
        }

        [Test]
        public void Describe_BestiaryCompletion_PreservesPercentageAndActualComparison()
        {
            MerchantConditionVisualDescriptor positive = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints,
                    argument: 1250));
            MerchantConditionVisualDescriptor negative = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints,
                    argument: 1250,
                    isNegated: true));

            Assert.Multiple(() =>
            {
                Assert.That(positive.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BestiaryCompletion));
                Assert.That(positive.ArgumentText, Is.EqualTo(">=12.5%"));
                Assert.That(positive.IsNegated, Is.False);
                Assert.That(negative.ArgumentText, Is.EqualTo("<12.5%"));
                Assert.That(negative.IsNegated, Is.False);
            });
        }

        [Test]
        public void Describe_ThresholdConditions_PreserveNegatedComparisonSemantics()
        {
            MerchantConditionVisualDescriptor life = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.PlayerLifeMaxAtLeast,
                    argument: 400,
                    isNegated: true));
            MerchantConditionVisualDescriptor mana = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.PlayerManaMaxAtLeast,
                    argument: 200,
                    isNegated: true));
            MerchantConditionVisualDescriptor atLeast = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.GolferScoreAtLeast,
                    argument: 1000,
                    isNegated: true));
            MerchantConditionVisualDescriptor greaterThan = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(
                    MerchantSourceConditionKind.GolferScoreGreaterThan,
                    argument: 1000,
                    isNegated: true));

            Assert.Multiple(() =>
            {
                Assert.That(life.ArgumentText, Is.EqualTo("<400"));
                Assert.That(life.IsNegated, Is.False);
                Assert.That(mana.ArgumentText, Is.EqualTo("<200"));
                Assert.That(mana.IsNegated, Is.False);
                Assert.That(atLeast.ArgumentText, Is.EqualTo("<1000"));
                Assert.That(atLeast.IsNegated, Is.False);
                Assert.That(greaterThan.ArgumentText, Is.EqualTo("<=1000"));
                Assert.That(greaterThan.IsNegated, Is.False);
            });
        }

        [Test]
        public void Describe_GolferThresholds_PreservePositiveComparisonSemantics()
        {
            MerchantConditionVisualDescriptor atLeast = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.GolferScoreAtLeast, argument: 1000));
            MerchantConditionVisualDescriptor greaterThan = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.GolferScoreGreaterThan, argument: 1000));

            Assert.Multiple(() =>
            {
                Assert.That(atLeast.ArgumentText, Is.EqualTo(">=1000"));
                Assert.That(greaterThan.ArgumentText, Is.EqualTo(">1000"));
            });
        }

        [Test]
        public void Describe_MoonPhase_UsesSextantWithoutRawPhaseIndex()
        {
            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.MoonPhase, argument: 3));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(descriptor.GetPart(0).Value, Is.EqualTo(3096));
                Assert.That(descriptor.ArgumentText, Is.Empty);
            });
        }

        [Test]
        public void Describe_FrostAndUnderworld_UseNativeBestiaryVisuals()
        {
            MerchantConditionVisualDescriptor frost = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.DownedFrost));
            MerchantConditionVisualDescriptor underworld = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.ZoneUnderworldHeight));

            Assert.Multiple(() =>
            {
                Assert.That(frost.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BestiaryTag));
                Assert.That(frost.GetPart(0).Value, Is.EqualTo(54));
                Assert.That(underworld.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BestiaryTag));
                Assert.That(underworld.GetPart(0).Value, Is.EqualTo(33));
            });
        }

        [Test]
        public void Describe_Boss2_UsesEaterAndBrainComposite()
        {
            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.DownedBoss2));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.PartCount, Is.EqualTo(2));
                Assert.That(descriptor.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.BossHead));
                Assert.That(descriptor.GetPart(0).Value, Is.EqualTo(13));
                Assert.That(descriptor.GetPart(1).Kind, Is.EqualTo(MerchantConditionVisualKind.BossHead));
                Assert.That(descriptor.GetPart(1).Value, Is.EqualTo(266));
            });
        }

        [Test]
        public void Describe_AnyMechanicalBoss_UsesAllConfirmedBossVisuals()
        {
            MerchantConditionVisualDescriptor descriptor = MerchantConditionIconPresentation.Describe(
                new MerchantSourceCondition(MerchantSourceConditionKind.DownedMechBossAny));

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.PartCount, Is.EqualTo(4));
                Assert.That(descriptor.GetPart(0).Value, Is.EqualTo(134));
                Assert.That(descriptor.GetPart(1).Value, Is.EqualTo(125));
                Assert.That(descriptor.GetPart(2).Value, Is.EqualTo(126));
                Assert.That(descriptor.GetPart(3).Value, Is.EqualTo(127));
            });
        }

        [Test]
        public void DescribeRestrictions_KeepRandomAndCapacityVisualsSeparate()
        {
            MerchantConditionVisualDescriptor random = MerchantConditionIconPresentation.DescribeRandomStock();
            MerchantConditionVisualDescriptor
                capacity = MerchantConditionIconPresentation.DescribeShopCapacityLimited();

            Assert.Multiple(() =>
            {
                Assert.That(random.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Npc));
                Assert.That(random.GetPart(0).Value, Is.EqualTo(368));
                Assert.That(capacity.GetPart(0).Kind, Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(capacity.GetPart(0).Value, Is.EqualTo(48));
            });
        }

        [Test]
        public void GetAsyncRequestKind_DispatchesOnlyItemAndNpcParts()
        {
            Assert.Multiple(() =>
            {
                Assert.That(
                    MerchantConditionIconPresentation.GetAsyncRequestKind(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.Item, 29)),
                    Is.EqualTo(MerchantConditionVisualKind.Item));
                Assert.That(
                    MerchantConditionIconPresentation.GetAsyncRequestKind(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.Npc, 17)),
                    Is.EqualTo(MerchantConditionVisualKind.Npc));
                Assert.That(
                    MerchantConditionIconPresentation.GetAsyncRequestKind(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.BossHead, 4)),
                    Is.Null);
                Assert.That(
                    MerchantConditionIconPresentation.GetAsyncRequestKind(
                        new MerchantConditionVisualPart(MerchantConditionVisualKind.Item)),
                    Is.Null);
            });
        }

        private static IEnumerable<int> AllConditionKinds()
        {
            foreach (MerchantSourceConditionKind kind in Enum.GetValues(typeof(MerchantSourceConditionKind)))
                yield return (int)kind;
        }

        private static MerchantSourceCondition CreateRepresentativeCondition(MerchantSourceConditionKind kind)
        {
            return kind switch
            {
                MerchantSourceConditionKind.WorldSilverOreTier => new MerchantSourceCondition(kind, argument: 168),
                MerchantSourceConditionKind.NpcPresent => new MerchantSourceCondition(kind, argument: 17),
                MerchantSourceConditionKind.PlayerHasItem => new MerchantSourceCondition(kind, argument: 29),
                MerchantSourceConditionKind.PlayerHasItemInAnyInventory => new MerchantSourceCondition(
                    kind,
                    argument: 29),
                MerchantSourceConditionKind.PlayerLifeMaxAtLeast => new MerchantSourceCondition(kind, argument: 120),
                MerchantSourceConditionKind.PlayerManaMaxAtLeast => new MerchantSourceCondition(kind, argument: 40),
                MerchantSourceConditionKind.PlayerCoinValueAtLeast => new MerchantSourceCondition(
                    kind,
                    argument: 1000000),
                MerchantSourceConditionKind.GolferScoreAtLeast => new MerchantSourceCondition(kind, argument: 1000),
                MerchantSourceConditionKind.GolferScoreGreaterThan => new MerchantSourceCondition(kind, argument: 1000),
                MerchantSourceConditionKind.BestiaryCompletionAtLeastBasisPoints => new MerchantSourceCondition(
                    kind,
                    argument: 1250),
                MerchantSourceConditionKind.MoonPhase => new MerchantSourceCondition(kind, argument: 3),
                _ => new MerchantSourceCondition(kind)
            };
        }
    }
}