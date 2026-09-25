using System;
using NUnit.Framework;
using TerrarianCompendium.Filtering;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Tests.Shimmer
{
    [TestFixture]
    public sealed class ShimmerFilterStateTests
    {
        [Test]
        public void Defaults_AreNeutralAndInactive()
        {
            var state = new ShimmerFilterState();

            Assert.Multiple(() =>
            {
                Assert.That(state.Kind, Is.Null);
                Assert.That(state.Progression, Is.Null);
                Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.All));
                Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
                Assert.That(state.ActiveFilterCount, Is.Zero);
                Assert.That(state.IsActive, Is.False);
                Assert.That(state.UsesChecklistState, Is.False);
                Assert.That(state.UsesJourneyResearchState, Is.False);
                Assert.That(state.Revision, Is.Zero);
            });
        }

        [Test]
        public void ActiveFilterCount_TracksFourIndependentGroups()
        {
            var state = new ShimmerFilterState
            {
                Kind = ShimmerTransformationKind.Decraft,
                Progression = ShimmerProgressionFilter.Locked,
                CompletionFilter = ChecklistCompletionFilter.Missing,
                ResearchFilter = ChecklistResearchFilter.Unresearched
            };

            Assert.Multiple(() =>
            {
                Assert.That(state.ActiveFilterCount, Is.EqualTo(4));
                Assert.That(state.UsesChecklistState, Is.True);
                Assert.That(state.UsesJourneyResearchState, Is.True);
            });
        }

        [Test]
        public void SettingSameCriterion_DoesNotAdvanceRevision()
        {
            var state = new ShimmerFilterState { Kind = ShimmerTransformationKind.Direct };
            long revision = state.Revision;

            state.Kind = ShimmerTransformationKind.Direct;

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void Clear_RestoresAllNeutralCriteriaWithSingleRevisionChange()
        {
            var state = new ShimmerFilterState
            {
                Kind = ShimmerTransformationKind.Direct,
                Progression = ShimmerProgressionFilter.Unlocked,
                CompletionFilter = ChecklistCompletionFilter.Found,
                ResearchFilter = ChecklistResearchFilter.Researched
            };
            long revision = state.Revision;

            state.Clear();

            Assert.Multiple(() =>
            {
                Assert.That(state.Kind, Is.Null);
                Assert.That(state.Progression, Is.Null);
                Assert.That(state.CompletionFilter, Is.EqualTo(ChecklistCompletionFilter.All));
                Assert.That(state.ResearchFilter, Is.EqualTo(ChecklistResearchFilter.All));
                Assert.That(state.ActiveFilterCount, Is.Zero);
                Assert.That(state.Revision, Is.EqualTo(revision + 1));
            });
        }

        [Test]
        public void Clear_WhenAlreadyNeutral_DoesNotChangeRevision()
        {
            var state = new ShimmerFilterState();

            state.Clear();

            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void InvalidEnumValuesThrow()
        {
            var state = new ShimmerFilterState();

            Assert.Multiple(() =>
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => state.Kind = (ShimmerTransformationKind)999);
                Assert.Throws<ArgumentOutOfRangeException>(() => state.Progression = (ShimmerProgressionFilter)999);
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                    state.CompletionFilter = (ChecklistCompletionFilter)999);
                Assert.Throws<ArgumentOutOfRangeException>(() => state.ResearchFilter = (ChecklistResearchFilter)999);
            });
        }
    }
}