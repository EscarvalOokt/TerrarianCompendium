using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Journey;
using TerrarianCompendium.UI.Vanilla;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class ItemResearchBadgePresentationTests
    {
        [Test]
        public void WithoutResearchState_BadgeIsHidden()
        {
            var presentation = new ItemResearchBadgePresentation(null);

            presentation.Bind(1);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.Synchronize(), Is.False);
                Assert.That(presentation.IsVisible, Is.False);
            });
        }

        [Test]
        public void ResearchableItemWithoutProgress_BadgeIsHidden()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(1);
            presentation.Synchronize();

            Assert.That(presentation.IsVisible, Is.False);
        }

        [Test]
        public void PartiallyResearchedItem_BadgeIsHidden()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 3))), Is.True);
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(1);
            presentation.Synchronize();

            Assert.That(presentation.IsVisible, Is.False);
        }

        [Test]
        public void FullyResearchedItem_BadgeIsVisible()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 5))), Is.True);
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(1);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.Synchronize(), Is.True);
                Assert.That(presentation.IsVisible, Is.True);
            });
        }

        [Test]
        public void NonResearchableItem_BadgeIsHidden()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 5, hasSharedResearchIdentity: false));
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(2);
            presentation.Synchronize();

            Assert.That(presentation.IsVisible, Is.False);
        }

        [Test]
        public void SharedCanonicalIdentity_UsesSharedProgressForBadge()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 5, hasSharedResearchIdentity: true));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 5))), Is.True);
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(1);
            presentation.Synchronize();
            Assert.That(presentation.IsVisible, Is.True);

            presentation.Bind(2);
            presentation.Synchronize();
            Assert.That(presentation.IsVisible, Is.True);
        }

        [Test]
        public void SharedCanonicalIdentity_UsesPerItemRequiredAmountForBadge()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 10, 5, hasSharedResearchIdentity: true),
                new JourneyResearchDefinition(2, 10, 10, hasSharedResearchIdentity: true));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(10, 5))), Is.True);
            var presentation = new ItemResearchBadgePresentation(state);

            presentation.Bind(1);
            presentation.Synchronize();
            Assert.That(presentation.IsVisible, Is.True);

            presentation.Bind(2);
            presentation.Synchronize();
            Assert.That(presentation.IsVisible, Is.False);
        }

        [Test]
        public void Synchronize_AfterResearchRevisionChanges_UpdatesBadgeVisibility()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));
            var presentation = new ItemResearchBadgePresentation(state);
            presentation.Bind(1);
            presentation.Synchronize();
            Assert.That(presentation.IsVisible, Is.False);

            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 1))), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(presentation.Synchronize(), Is.True);
                Assert.That(presentation.IsVisible, Is.True);
            });
        }

        [Test]
        public void Synchronize_WithoutRevisionChange_DoesNotReportPresentationChange()
        {
            JourneyResearchState state = CreateState(
                new JourneyResearchDefinition(1, 1, 1, hasSharedResearchIdentity: false));
            Assert.That(state.ReplaceProgressSnapshot(Progress(Pair(1, 1))), Is.True);
            var presentation = new ItemResearchBadgePresentation(state);
            presentation.Bind(1);
            Assert.That(presentation.Synchronize(), Is.True);

            Assert.That(presentation.Synchronize(), Is.False);
            Assert.That(presentation.IsVisible, Is.True);
        }

        private static JourneyResearchState CreateState(params JourneyResearchDefinition[] definitions)
        {
            return new JourneyResearchState(definitions);
        }

        private static KeyValuePair<int, int>[] Progress(params KeyValuePair<int, int>[] progress)
        {
            return progress;
        }

        private static KeyValuePair<int, int> Pair(int itemId, int progress)
        {
            return new KeyValuePair<int, int>(itemId, progress);
        }
    }
}