using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Details;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Details
{
    [TestFixture]
    public sealed class ShimmerDetailsModelTests
    {
        [Test]
        public void TryGetProjection_ResultWithMultipleProducingVariants_ReturnsAllVariants()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 20);
            ShimmerTransformationVariant first = Direct(10, 1);
            ShimmerTransformationVariant second = Decraft(20, [new ShimmerTransformationOutput(1, 2)]);
            ShimmerDetailsModel model = CreateModel(catalog, [first, second]);

            Assert.That(model.TryGetProjection(1, out ShimmerDetailsProjection projection), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(projection.Result.ItemId, Is.EqualTo(1));
                Assert.That(projection.TotalVariantCount, Is.EqualTo(2));
                Assert.That(projection.MatchingVariantCount, Is.EqualTo(2));
                Assert.That(projection.MatchingVariants.Select(variant => variant.Input.ItemId), Is.EqualTo([10, 20]));
            });
        }

        [Test]
        public void TryGetProjection_InputOnlyContext_IsValidWithoutProducingVariants()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            ShimmerDetailsModel model = CreateModel(catalog, [Direct(10, 1)]);

            Assert.That(model.TryGetProjection(10, out ShimmerDetailsProjection projection), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(projection.HasProducingVariants, Is.False);
                Assert.That(projection.TotalVariantCount, Is.Zero);
                Assert.That(projection.MatchingVariants, Is.Empty);
            });
        }

        [Test]
        public void TryGetProjection_FilterStateRestrictsVariantsWithoutChangingTotalCount()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 20);
            var filters = new ShimmerFilterState { Kind = ShimmerTransformationKind.Direct };
            ShimmerDetailsModel model = CreateModel(
                catalog,
                [Direct(10, 1), Decraft(20, [new ShimmerTransformationOutput(1, 1)])],
                filters: filters);

            model.TryGetProjection(1, out ShimmerDetailsProjection projection);

            Assert.That(projection.TotalVariantCount, Is.EqualTo(2));
            Assert.That(projection.MatchingVariantCount, Is.EqualTo(1));
            Assert.That(projection.MatchingVariants[0].IsDirect, Is.True);
        }

        [Test]
        public void TryGetProjection_MoonAndWorldConditionsRemainStaticMetadata()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10, 20);
            ShimmerTransformationVariant moon = new(
                10,
                ShimmerTransformationKind.Direct,
                1,
                [new ShimmerTransformationOutput(1, 1)],
                moonPhase: 3);
            ShimmerTransformationVariant world = new(
                20,
                ShimmerTransformationKind.Decraft,
                2,
                [new ShimmerTransformationOutput(2, 1)],
                worldCondition: ShimmerWorldCondition.Crimson,
                decraftingRecipeRuntimeIndex: 8);
            ShimmerDetailsModel model = CreateModel(catalog, [moon, world]);

            model.TryGetProjection(1, out ShimmerDetailsProjection moonProjection);
            model.TryGetProjection(2, out ShimmerDetailsProjection worldProjection);

            Assert.That(moonProjection.MatchingVariants[0].MoonPhase, Is.EqualTo(3));
            Assert.That(worldProjection.MatchingVariants[0].WorldCondition, Is.EqualTo(ShimmerWorldCondition.Crimson));
        }

        [Test]
        public void TryGetProjection_ProgressionRequirementAndCurrentLockAreSeparate()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var runtime = new RuntimeState { Locked = true };
            ShimmerTransformationVariant variant = Direct(10, 1, ShimmerProgressionRequirement.PostMoonLord);
            ShimmerDetailsModel model = CreateModel(
                catalog,
                [variant],
                runtime: runtime,
                isLocked: _ => runtime.Locked);

            model.TryGetProjection(1, out ShimmerDetailsProjection before);
            runtime.Locked = false;
            runtime.Context = new ShimmerRuntimeContextKey(false, false, true, false, 0);
            model.TryGetProjection(1, out ShimmerDetailsProjection after);

            Assert.Multiple(() =>
            {
                Assert.That(
                    before.MatchingVariants[0].ProgressionRequirement,
                    Is.EqualTo(ShimmerProgressionRequirement.PostMoonLord));
                Assert.That(before.MatchingVariants[0].IsProgressionLocked, Is.True);
                Assert.That(
                    after.MatchingVariants[0].ProgressionRequirement,
                    Is.EqualTo(ShimmerProgressionRequirement.PostMoonLord));
                Assert.That(after.MatchingVariants[0].IsProgressionLocked, Is.False);
            });
        }

        [Test]
        public void TryGetProjection_MultiOutputDecraftShowsFullOutputSetAndRecipeMetadata()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10);
            ShimmerTransformationVariant variant = Decraft(
                10,
                [new ShimmerTransformationOutput(1, 3), new ShimmerTransformationOutput(2, 4)],
                runtimeIndex: 12,
                isAlchemy: true);
            ShimmerDetailsModel model = CreateModel(catalog, [variant]);

            model.TryGetProjection(1, out ShimmerDetailsProjection projection);
            ShimmerDetailsVariantProjection projectedVariant = projection.MatchingVariants[0];

            Assert.Multiple(() =>
            {
                Assert.That(projectedVariant.Outputs.Select(output => output.ItemId), Is.EqualTo([1, 2]));
                Assert.That(projectedVariant.Outputs.Select(output => output.Stack), Is.EqualTo([3, 4]));
                Assert.That(projectedVariant.DecraftingRecipeRuntimeIndex, Is.EqualTo(12));
                Assert.That(projectedVariant.IsAlchemy, Is.True);
            });
        }

        [Test]
        public void TryGetProjection_UnrelatedItemReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 99);
            ShimmerDetailsModel model = CreateModel(catalog, [Direct(10, 1)]);

            Assert.That(model.TryGetProjection(99, out _), Is.False);
        }

        private static ShimmerDetailsModel CreateModel(
            ItemCatalog catalog,
            IEnumerable<ShimmerTransformationVariant> variants,
            ShimmerFilterState filters = null,
            JourneyResearchState research = null,
            RuntimeState runtime = null,
            Func<ShimmerTransformationVariant, bool> isLocked = null)
        {
            runtime ??= new RuntimeState();
            return new ShimmerDetailsModel(
                ShimmerTransformationIndex.Create(variants),
                catalog,
                new ItemTextIndex(catalog),
                new ChecklistState(catalog),
                research,
                filters ?? new ShimmerFilterState(),
                isLocked ?? (_ => false),
                () => runtime.Context);
        }

        private static ShimmerTransformationVariant Direct(
            int inputItemId,
            int resultItemId,
            ShimmerProgressionRequirement progression = ShimmerProgressionRequirement.None)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Direct,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)],
                progression);
        }

        private static ShimmerTransformationVariant Decraft(
            int inputItemId,
            IEnumerable<ShimmerTransformationOutput> outputs,
            int runtimeIndex = 7,
            bool isAlchemy = false)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Decraft,
                2,
                outputs,
                decraftingRecipeRuntimeIndex: runtimeIndex,
                isAlchemy: isAlchemy);
        }

        private static ItemCatalog CreateCatalog(params int[] itemIds)
        {
            return ItemCatalog.Create(itemIds.Select(id => new ItemCatalogEntry(id, "Item " + id)));
        }

        private sealed class RuntimeState
        {
            public bool Locked { get; set; }

            public ShimmerRuntimeContextKey Context { get; set; }
        }
    }
}