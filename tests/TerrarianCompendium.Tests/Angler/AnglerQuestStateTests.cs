using System;
using NUnit.Framework;
using TerrarianCompendium.Angler;

namespace TerrarianCompendium.Tests.Angler
{
    [TestFixture]
    public sealed class AnglerQuestStateTests
    {
        [Test]
        public void InitialState_HasNoSnapshotAndZeroRevision()
        {
            var state = new AnglerQuestState();

            Assert.Multiple(() =>
            {
                Assert.That(state.HasSnapshot, Is.False);
                Assert.That(state.Revision, Is.EqualTo(0));
                Assert.That(state.TryGetSnapshot(out _), Is.False);
            });
        }

        [Test]
        public void ReplaceSnapshot_FirstSnapshotUpdatesStateAndRevision()
        {
            var state = new AnglerQuestState();
            AnglerQuestSnapshot snapshot = CreateSnapshot();

            bool changed = state.ReplaceSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.True);
                Assert.That(state.HasSnapshot, Is.True);
                Assert.That(state.Revision, Is.EqualTo(1));
                Assert.That(state.TryGetSnapshot(out AnglerQuestSnapshot actual), Is.True);
                Assert.That(actual, Is.EqualTo(snapshot));
            });
        }

        [Test]
        public void ReplaceSnapshot_IdenticalSnapshotDoesNotAdvanceRevision()
        {
            var state = new AnglerQuestState();
            AnglerQuestSnapshot snapshot = CreateSnapshot();
            state.ReplaceSnapshot(snapshot);

            bool changed = state.ReplaceSnapshot(snapshot);

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.False);
                Assert.That(state.Revision, Is.EqualTo(1));
            });
        }

        [Test]
        public void ReplaceSnapshot_AnyProjectedInputChangeAdvancesRevision()
        {
            var state = new AnglerQuestState();
            state.ReplaceSnapshot(CreateSnapshot());

            Assert.That(state.ReplaceSnapshot(CreateSnapshot(questItemId: 2451)), Is.True);
            Assert.That(state.ReplaceSnapshot(CreateSnapshot(questItemId: 2451, finishedToday: true)), Is.True);
            Assert.That(
                state.ReplaceSnapshot(CreateSnapshot(questItemId: 2451, finishedToday: true, questsFinished: 13)),
                Is.True);
            Assert.That(
                state.ReplaceSnapshot(
                    CreateSnapshot(questItemId: 2451, finishedToday: true, questsFinished: 13, isHardMode: true)),
                Is.True);
            Assert.That(
                state.ReplaceSnapshot(
                    CreateSnapshot(
                        questItemId: 2451,
                        finishedToday: true,
                        questsFinished: 13,
                        isHardMode: true,
                        isExpertMode: true)),
                Is.True);
            Assert.That(
                state.ReplaceSnapshot(
                    CreateSnapshot(
                        questItemId: 2451,
                        finishedToday: true,
                        questsFinished: 13,
                        isHardMode: true,
                        isExpertMode: true,
                        ownedRewardComponents: AnglerRewardOwnership.FishingBobber)),
                Is.True);

            Assert.That(state.Revision, Is.EqualTo(7));
        }

        [Test]
        public void NewState_DoesNotInheritPreviousSessionSnapshot()
        {
            var previousSessionState = new AnglerQuestState();
            previousSessionState.ReplaceSnapshot(CreateSnapshot(questsFinished: 25));

            var newSessionState = new AnglerQuestState();

            Assert.Multiple(() =>
            {
                Assert.That(previousSessionState.HasSnapshot, Is.True);
                Assert.That(newSessionState.HasSnapshot, Is.False);
                Assert.That(newSessionState.Revision, Is.EqualTo(0));
            });
        }

        [Test]
        public void Snapshot_InvalidQuestItemId_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new AnglerQuestSnapshot(
                0,
                false,
                0,
                false,
                false,
                AnglerRewardOwnership.None));
        }

        [Test]
        public void Snapshot_NegativeQuestCount_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new AnglerQuestSnapshot(
                2450,
                false,
                -1,
                false,
                false,
                AnglerRewardOwnership.None));
        }

        [Test]
        public void Snapshot_UnknownOwnershipFlag_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ = new AnglerQuestSnapshot(
                2450,
                false,
                0,
                false,
                false,
                (AnglerRewardOwnership)(1 << 20)));
        }

        private static AnglerQuestSnapshot CreateSnapshot(
            int questItemId = 2450,
            bool finishedToday = false,
            int questsFinished = 12,
            bool isHardMode = false,
            bool isExpertMode = false,
            AnglerRewardOwnership ownedRewardComponents = AnglerRewardOwnership.None)
        {
            return new AnglerQuestSnapshot(
                questItemId,
                finishedToday,
                questsFinished,
                isHardMode,
                isExpertMode,
                ownedRewardComponents);
        }
    }
}