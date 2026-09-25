using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class ShimmerFilterMatcherTests
    {
        [Test]
        public void CompletionFilter_AppliesToResultItem()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var checklist = new ChecklistState(catalog);
            ShimmerTransformationVariant variant = Direct(10, 1);
            var state = new ShimmerFilterState { CompletionFilter = ChecklistCompletionFilter.Missing };

            Assert.That(Matches(1, variant, state, checklist), Is.True);

            checklist.MarkFound(1);
            Assert.That(Matches(1, variant, state, checklist), Is.False);

            state.CompletionFilter = ChecklistCompletionFilter.Found;
            Assert.That(Matches(1, variant, state, checklist), Is.True);
        }

        [Test]
        public void ResearchFilter_AppliesToResultItemAndExcludesNonResearchableFromUnresearched()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 10);
            var checklist = new ChecklistState(catalog);
            JourneyResearchState research = CreateResearchState(1, amountNeeded: 5, progress: 2);
            ShimmerTransformationVariant variant = Direct(10, 1);
            var state = new ShimmerFilterState { ResearchFilter = ChecklistResearchFilter.Unresearched };

            Assert.That(Matches(1, variant, state, checklist, research), Is.True);
            Assert.That(Matches(2, Direct(10, 2), state, checklist, research), Is.False);

            research.ReplaceProgressSnapshot([new KeyValuePair<int, int>(1, 5)]);
            Assert.That(Matches(1, variant, state, checklist, research), Is.False);

            state.ResearchFilter = ChecklistResearchFilter.Researched;
            Assert.That(Matches(1, variant, state, checklist, research), Is.True);
        }

        [Test]
        public void KindAndProgressionFilters_MustMatchSameVariant()
        {
            ItemCatalog catalog = CreateCatalog(1, 10, 20);
            var checklist = new ChecklistState(catalog);
            var state = new ShimmerFilterState
            {
                Kind = ShimmerTransformationKind.Direct,
                Progression = ShimmerProgressionFilter.Locked
            };
            ShimmerTransformationVariant directUnlocked = Direct(10, 1);
            ShimmerTransformationVariant decraftLocked = Decraft(20, 1);

            bool anyMatch = new[] { directUnlocked, decraftLocked }.Any(variant => Matches(
                1,
                variant,
                state,
                checklist,
                isLocked: candidate => ReferenceEquals(candidate, decraftLocked)));

            Assert.That(anyMatch, Is.False);
        }

        [Test]
        public void NoSelectedCriteria_MatchesVariantWithoutConsultingProgressionProvider()
        {
            ItemCatalog catalog = CreateCatalog(1, 10);
            var checklist = new ChecklistState(catalog);
            var state = new ShimmerFilterState();
            var calls = new Counter();

            bool matches = Matches(
                1,
                Direct(10, 1),
                state,
                checklist,
                isLocked: _ =>
                {
                    calls.Value++;
                    return true;
                });

            Assert.That(matches, Is.True);
            Assert.That(calls.Value, Is.Zero);
        }

        private static bool Matches(
            int resultItemId,
            ShimmerTransformationVariant variant,
            ShimmerFilterState state,
            ChecklistState checklist,
            JourneyResearchState research = null,
            Func<ShimmerTransformationVariant, bool> isLocked = null)
        {
            return ShimmerFilterMatcher.MatchesResult(resultItemId, state, checklist, research) &&
                   ShimmerFilterMatcher.MatchesVariant(variant, state, isLocked ?? (_ => false));
        }

        private static ShimmerTransformationVariant Direct(int inputItemId, int resultItemId)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Direct,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)]);
        }

        private static ShimmerTransformationVariant Decraft(int inputItemId, int resultItemId)
        {
            return new ShimmerTransformationVariant(
                inputItemId,
                ShimmerTransformationKind.Decraft,
                1,
                [new ShimmerTransformationOutput(resultItemId, 1)],
                decraftingRecipeRuntimeIndex: 7);
        }

        private static ItemCatalog CreateCatalog(params int[] itemIds)
        {
            return ItemCatalog.Create(itemIds.Select(id => new ItemCatalogEntry(id, "Item " + id)));
        }

        private sealed class Counter
        {
            public int Value { get; set; }
        }

        private static JourneyResearchState CreateResearchState(int itemId, int amountNeeded, int progress)
        {
            var state = new JourneyResearchState(
                [new JourneyResearchDefinition(itemId, itemId, amountNeeded, hasSharedResearchIdentity: false)]);

            if (progress > 0)
                state.ReplaceProgressSnapshot([new KeyValuePair<int, int>(itemId, progress)]);

            return state;
        }
    }
}