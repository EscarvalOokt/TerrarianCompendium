using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Journey;

namespace TerrarianCompendium.Tests.Journey
{
    [TestFixture]
    public sealed class JourneyResearchStateTests
    {
        [Test]
        public void NewState_WithResearchableDefinition_StartsUnresearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.IsResearchable(1), Is.True);
            Assert.That(state.IsFullyResearched(1), Is.False);
            Assert.That(state.IsUnresearched(1), Is.True);
        }

        [Test]
        public void UnknownItem_IsNotResearchableOrUnresearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.IsResearchable(2), Is.False);
            Assert.That(state.IsFullyResearched(2), Is.False);
            Assert.That(state.IsUnresearched(2), Is.False);
        }

        [Test]
        public void PartialProgress_RemainsUnresearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 3))), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.False);
            Assert.That(state.IsUnresearched(1), Is.True);
        }

        [Test]
        public void ProgressAtRequiredAmount_IsFullyResearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 5))), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.True);
            Assert.That(state.IsUnresearched(1), Is.False);
        }

        [Test]
        public void ProgressAboveRequiredAmount_IsFullyResearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 7))), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.True);
            Assert.That(state.IsUnresearched(1), Is.False);
        }

        [Test]
        public void SharedCanonicalIdentity_UsesSameProgressForAllDefinitions()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 5, hasSharedResearchIdentity: true));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 5))), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.True);
            Assert.That(state.IsFullyResearched(2), Is.True);
            Assert.That(state.IsUnresearched(1), Is.False);
            Assert.That(state.IsUnresearched(2), Is.False);
        }

        [Test]
        public void SharedCanonicalIdentity_UsesPerItemRequiredAmount()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 10, hasSharedResearchIdentity: true));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 5))), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.True);
            Assert.That(state.IsUnresearched(1), Is.False);
            Assert.That(state.IsFullyResearched(2), Is.False);
            Assert.That(state.IsUnresearched(2), Is.True);
        }

        [Test]
        public void PositiveProgress_WithUnambiguousIdentity_IsIncludedInFoundCandidates()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 1))), Is.True);

            Assert.That(state.CreateFoundCandidateItemIdSnapshot(), Is.EqualTo([1]));
        }

        [Test]
        public void ZeroProgress_WithUnambiguousIdentity_IsNotIncludedInFoundCandidates()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.CreateFoundCandidateItemIdSnapshot(), Is.Empty);
        }

        [Test]
        public void PositiveProgress_WithSharedIdentity_IsNotIncludedInFoundCandidates()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 5, hasSharedResearchIdentity: true));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 5))), Is.True);

            Assert.That(state.CreateFoundCandidateItemIdSnapshot(), Is.Empty);
        }

        [Test]
        public void FullyResearchedSharedIdentity_StillIsNotIncludedInFoundCandidates()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 1, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 1, hasSharedResearchIdentity: true));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 1))), Is.True);
            Assert.That(state.IsFullyResearched(1), Is.True);
            Assert.That(state.IsFullyResearched(2), Is.True);

            Assert.That(state.CreateFoundCandidateItemIdSnapshot(), Is.Empty);
        }

        [Test]
        public void ReplaceProgressSnapshot_WithIdenticalProgress_DoesNotAdvanceRevision()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 2))), Is.True);
            long revision = state.Revision;

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 2))), Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void ReplaceProgressSnapshot_WithChangedProgress_AdvancesRevision()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 2))), Is.True);
            long revision = state.Revision;

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 3))), Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void ReplaceProgressSnapshot_WhenProgressDisappears_ReturnsItemToUnresearched()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 1))), Is.True);
            Assert.That(state.IsFullyResearched(1), Is.True);

            Assert.That(state.ReplaceProgressSnapshot(Progress()), Is.True);

            Assert.That(state.IsFullyResearched(1), Is.False);
            Assert.That(state.IsUnresearched(1), Is.True);
            Assert.That(state.CreateFoundCandidateItemIdSnapshot(), Is.Empty);
        }

        [Test]
        public void ReplaceProgressSnapshot_IgnoresUnknownCanonicalItems()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(999, 5))), Is.False);
            Assert.That(state.Revision, Is.EqualTo(0));
            Assert.That(state.IsUnresearched(1), Is.True);
        }

        [Test]
        public void CreateProgressSnapshot_NewState_IsEmpty()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));

            Assert.That(state.CreateProgressSnapshot(), Is.Empty);
        }

        [Test]
        public void CreateProgressSnapshot_SortsByCanonicalItemId()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 30, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(2, 10, 5, hasSharedResearchIdentity: false),
                new JourneyResearchDefinition(3, 20, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(30, 3), Pair(10, 1), Pair(20, 2))), Is.True);

            Assert.That(state.CreateProgressSnapshot(), Is.EqualTo([Pair(10, 1), Pair(20, 2), Pair(30, 3)]));
        }

        [Test]
        public void CreateProgressSnapshot_ContainsOnlyNormalizedPositiveKnownProgress()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: false));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 2), Pair(20, 7), Pair(30, 0))), Is.True);

            Assert.That(state.CreateProgressSnapshot(), Is.EqualTo([Pair(10, 2)]));
        }

        [Test]
        public void CreateProgressSnapshot_PreservesSharedCanonicalIdentity()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 10, hasSharedResearchIdentity: true));

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 4))), Is.True);

            Assert.That(state.CreateProgressSnapshot(), Is.EqualTo([Pair(10, 4)]));
        }

        [Test]
        public void CreateProgressSnapshot_IsDetachedFromSubsequentStateChanges()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 2))), Is.True);

            IReadOnlyList<KeyValuePair<int, int>> snapshot = state.CreateProgressSnapshot();

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 4))), Is.True);
            Assert.That(snapshot, Is.EqualTo([Pair(1, 2)]));
            Assert.That(state.CreateProgressSnapshot(), Is.EqualTo([Pair(1, 4)]));
        }

        [Test]
        public void CreateProgressSnapshot_IsReadOnly()
        {
            JourneyResearchState state =
                CreateState(new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 2))), Is.True);

            IReadOnlyList<KeyValuePair<int, int>> snapshot = state.CreateProgressSnapshot();
            var mutableView = (IList<KeyValuePair<int, int>>)snapshot;

            Assert.That(mutableView.IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(Pair(1, 3)));
        }

        private static JourneyResearchState CreateState(params JourneyResearchDefinition[] definitions)
        {
            return new JourneyResearchState(definitions);
        }

        private static IEnumerable<KeyValuePair<int, int>> Progress(params KeyValuePair<int, int>[] entries)
        {
            return entries;
        }

        private static KeyValuePair<int, int> Pair(int itemId, int amount)
        {
            return new KeyValuePair<int, int>(itemId, amount);
        }
    }
}