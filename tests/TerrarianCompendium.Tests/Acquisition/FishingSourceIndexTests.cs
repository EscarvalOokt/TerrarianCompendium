using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Acquisition;

namespace TerrarianCompendium.Tests.Acquisition
{
    [TestFixture]
    public sealed class FishingSourceIndexTests
    {
        [Test]
        public void Constructor_WithItemIds_DeduplicatesSortsAndIgnoresInvalidIds()
        {
            var index = new FishingSourceIndex([5, 2, 5, 0, -1, 3]);

            Assert.That(index.ItemIds, Is.EqualTo([2, 3, 5]));
            Assert.That(index.ContainsItem(2), Is.True);
            Assert.That(index.ContainsItem(4), Is.False);
            Assert.That(index.ContainsItem(0), Is.False);
            Assert.That(index.GetVariantsForItem(2), Has.Count.EqualTo(1));
            Assert.That(index.GetVariantsForItem(2)[0].Conditions, Is.Empty);
            Assert.That(index.GetVariantsForItem(5), Has.Count.EqualTo(1));
        }

        [Test]
        public void Constructor_WithRelations_PreservesRawAlternativesAndNormalizesConditionsWithinEachVariant()
        {
            var index = new FishingSourceIndex(
            [
                new FishingSourceRelation(
                    5,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.Jungle,
                        FishingSourceConditionKind.HardMode,
                        FishingSourceConditionKind.Jungle
                    ])),
                new FishingSourceRelation(
                    5,
                    new FishingSourceVariant(
                    [
                        FishingSourceConditionKind.HardMode,
                        FishingSourceConditionKind.Jungle
                    ])),
                new FishingSourceRelation(5, new FishingSourceVariant([FishingSourceConditionKind.Jungle])),
                new FishingSourceRelation(2, new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>())),
                new FishingSourceRelation(0, new FishingSourceVariant([FishingSourceConditionKind.Ocean]))
            ]);

            Assert.That(index.ItemIds, Is.EqualTo([2, 5]));
            Assert.That(index.GetVariantsForItem(5), Has.Count.EqualTo(3));
            Assert.That(
                index.GetVariantsForItem(5)[0].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HardMode, FishingSourceConditionKind.Jungle]));
            Assert.That(
                index.GetVariantsForItem(5)[1].Conditions,
                Is.EqualTo([FishingSourceConditionKind.HardMode, FishingSourceConditionKind.Jungle]));
            Assert.That(index.GetVariantsForItem(5)[2].Conditions, Is.EqualTo([FishingSourceConditionKind.Jungle]));
            Assert.That(index.GetVariantsForItem(6), Is.Empty);
        }

        [Test]
        public void Constructor_WithRelations_PreservesAlternativeOrderPerItem()
        {
            var index = new FishingSourceIndex(
            [
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.Jungle])),
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.HardMode])),
                new FishingSourceRelation(1, new FishingSourceVariant([FishingSourceConditionKind.Ocean]))
            ]);

            Assert.That(index.GetVariantsForItem(1), Has.Count.EqualTo(3));
            Assert.That(index.GetVariantsForItem(1)[0].Conditions, Is.EqualTo([FishingSourceConditionKind.Jungle]));
            Assert.That(index.GetVariantsForItem(1)[1].Conditions, Is.EqualTo([FishingSourceConditionKind.HardMode]));
            Assert.That(index.GetVariantsForItem(1)[2].Conditions, Is.EqualTo([FishingSourceConditionKind.Ocean]));
        }

        [Test]
        public void FishingSourceVariant_NormalizesExcludedConditionsAndIncludesThemInIdentity()
        {
            var first = new FishingSourceVariant(
                [FishingSourceConditionKind.Jungle],
                [
                    FishingSourceConditionKind.Ocean,
                    FishingSourceConditionKind.InLava,
                    FishingSourceConditionKind.Ocean
                ]);
            var equal = new FishingSourceVariant(
                [FishingSourceConditionKind.Jungle],
                [FishingSourceConditionKind.InLava, FishingSourceConditionKind.Ocean]);
            var different = new FishingSourceVariant(
                [FishingSourceConditionKind.Jungle],
                [FishingSourceConditionKind.InLava]);

            Assert.That(
                first.ExcludedConditions,
                Is.EqualTo([FishingSourceConditionKind.InLava, FishingSourceConditionKind.Ocean]));
            Assert.That(first, Is.EqualTo(equal));
            Assert.That(first.GetHashCode(), Is.EqualTo(equal.GetHashCode()));
            Assert.That(first, Is.Not.EqualTo(different));
        }

        [TestCase("InLava")]
        [TestCase("InHoney")]
        [TestCase("Ocean")]
        public void Builder_StopperClassification_SourceReachabilityStopper_ReturnsTrue(string conditionFieldName)
        {
            bool result = VanillaFishingSourceIndexBuilder.ShouldApplyStopperToSourceReachability(conditionFieldName);

            Assert.That(result, Is.True);
        }

        [TestCase("AnyEnemies")]
        [TestCase("Junk")]
        [TestCase("Crate")]
        public void Builder_StopperClassification_ControlOnlyStopper_ReturnsFalse(string conditionFieldName)
        {
            bool result = VanillaFishingSourceIndexBuilder.ShouldApplyStopperToSourceReachability(conditionFieldName);

            Assert.That(result, Is.False);
        }

        [Test]
        public void Builder_StopperClassification_UnknownStopper_Throws()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                VanillaFishingSourceIndexBuilder.ShouldApplyStopperToSourceReachability("UnexpectedStopper"));

            Assert.That(
                // ReSharper disable once PossibleNullReferenceException
                exception.Message,
                Is.EqualTo(
                    "Terraria fishing stopper condition field 'UnexpectedStopper' has no classified source-reachability behavior."));
        }

        [Test]
        public void VariantComposer_TryCompose_RejectsRequiredConditionExcludedByReachability()
        {
            var reachability = new FishingSourceVariant(
                Array.Empty<FishingSourceConditionKind>(),
                [FishingSourceConditionKind.InLava]);

            bool composed = FishingSourceVariantComposer.TryCompose(
                reachability,
                [FishingSourceConditionKind.InLava],
                out FishingSourceVariant variant);

            Assert.That(composed, Is.False);
            Assert.That(variant, Is.Null);
        }

        [Test]
        public void VariantComposer_SequentialSingleConditionStoppers_AccumulateExclusions()
        {
            IReadOnlyList<FishingSourceVariant> reachability =
            [
                new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>())
            ];

            reachability = FishingSourceVariantComposer.ApplyStopper(reachability, [FishingSourceConditionKind.InLava]);
            reachability = FishingSourceVariantComposer.ApplyStopper(
                reachability,
                [FishingSourceConditionKind.InHoney]);
            reachability = FishingSourceVariantComposer.ApplyStopper(reachability, [FishingSourceConditionKind.Ocean]);

            Assert.That(reachability, Has.Count.EqualTo(1));
            Assert.That(
                reachability[0].ExcludedConditions,
                Is.EqualTo(
                [
                    FishingSourceConditionKind.InLava,
                    FishingSourceConditionKind.InHoney,
                    FishingSourceConditionKind.Ocean
                ]));
        }

        [Test]
        public void VariantComposer_ConjunctiveStopper_CreatesAlternativeNegations()
        {
            IReadOnlyList<FishingSourceVariant> reachability =
            [
                new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>())
            ];

            IReadOnlyList<FishingSourceVariant> result = FishingSourceVariantComposer.ApplyStopper(
                reachability,
                [FishingSourceConditionKind.HardMode, FishingSourceConditionKind.Jungle]);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].ExcludedConditions, Is.EqualTo([FishingSourceConditionKind.HardMode]));
            Assert.That(result[1].ExcludedConditions, Is.EqualTo([FishingSourceConditionKind.Jungle]));
        }

        [Test]
        public void VariantComposer_UnconditionalStopper_MakesLaterRulesUnreachable()
        {
            IReadOnlyList<FishingSourceVariant> reachability =
            [
                new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>())
            ];

            IReadOnlyList<FishingSourceVariant> result = FishingSourceVariantComposer.ApplyStopper(
                reachability,
                Array.Empty<FishingSourceConditionKind>());

            Assert.That(result, Is.Empty);
        }
    }
}