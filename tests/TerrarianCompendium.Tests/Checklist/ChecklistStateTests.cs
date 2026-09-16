using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Tests.Checklist
{
    [TestFixture]
    public sealed class ChecklistStateTests
    {
        [Test]
        public void Constructor_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { _ = new ChecklistState(null); }));
        }

        [Test]
        public void NewState_HasEmptyProgress()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3);
            var state = new ChecklistState(catalog);

            Assert.That(state.FoundCount, Is.EqualTo(0));
            Assert.That(state.TotalCount, Is.EqualTo(3));
            Assert.That(state.CompletionRatio, Is.EqualTo(0.0));
        }

        [Test]
        public void NewState_RevisionStartsAtZero()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3);
            var state = new ChecklistState(catalog);

            Assert.That(state.Revision, Is.EqualTo(0));
        }

        [Test]
        public void IsFound_WithUnmarkedCatalogItem_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            Assert.That(state.IsFound(1), Is.False);
        }

        [Test]
        public void IsFound_WithUnknownItem_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            Assert.That(state.IsFound(999), Is.False);
        }

        [Test]
        public void MarkFound_WithCatalogItem_MarksItem()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            bool marked = state.MarkFound(1);

            Assert.That(marked, Is.True);
            Assert.That(state.IsFound(1), Is.True);
            Assert.That(state.FoundCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkFound_WithCatalogItem_IncrementsRevision()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);

            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void MarkFound_WithDuplicateItem_IsIdempotent()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            bool firstMark = state.MarkFound(1);
            bool secondMark = state.MarkFound(1);

            Assert.That(firstMark, Is.True);
            Assert.That(secondMark, Is.False);
            Assert.That(state.IsFound(1), Is.True);
            Assert.That(state.FoundCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkFound_WithDuplicateItem_DoesNotIncrementRevision()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);

            long revision = state.Revision;

            Assert.That(state.MarkFound(1), Is.False);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void MarkFound_WithUnknownItem_DoesNotChangeState()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            bool marked = state.MarkFound(999);

            Assert.That(marked, Is.False);
            Assert.That(state.IsFound(999), Is.False);
            Assert.That(state.FoundCount, Is.EqualTo(0));
            Assert.That(state.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void MarkFound_WithUnknownItem_DoesNotIncrementRevision()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(999), Is.False);

            Assert.That(state.Revision, Is.EqualTo(0));
        }

        [Test]
        public void MarkFound_WithZeroItemId_DoesNotChangeState()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            bool marked = state.MarkFound(0);

            Assert.That(marked, Is.False);
            Assert.That(state.IsFound(0), Is.False);
            Assert.That(state.FoundCount, Is.EqualTo(0));
        }

        [Test]
        public void CompletionRatio_AfterPartialDiscovery_IsCalculatedFromCatalog()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3, 4);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            Assert.That(state.FoundCount, Is.EqualTo(2));
            Assert.That(state.TotalCount, Is.EqualTo(4));
            Assert.That(state.CompletionRatio, Is.EqualTo(0.5));
        }

        [Test]
        public void CompletionRatio_WhenAllItemsFound_ReturnsOne()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(2), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            Assert.That(state.FoundCount, Is.EqualTo(state.TotalCount));
            Assert.That(state.CompletionRatio, Is.EqualTo(1.0));
        }

        [Test]
        public void CompletionRatio_WithEmptyCatalog_ReturnsZero()
        {
            var catalog = ItemCatalog.Create(Array.Empty<ItemCatalogEntry>());
            var state = new ChecklistState(catalog);

            Assert.That(state.FoundCount, Is.EqualTo(0));
            Assert.That(state.TotalCount, Is.EqualTo(0));
            Assert.That(state.CompletionRatio, Is.EqualTo(0.0));
        }

        [Test]
        public void Clear_WithFoundItems_ClearsProgress()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            bool cleared = state.Clear();

            Assert.That(cleared, Is.True);
            Assert.That(state.FoundCount, Is.EqualTo(0));
            Assert.That(state.IsFound(1), Is.False);
            Assert.That(state.IsFound(3), Is.False);
            Assert.That(state.TotalCount, Is.EqualTo(3));
            Assert.That(state.CompletionRatio, Is.EqualTo(0.0));
        }

        [Test]
        public void Clear_WithFoundItems_IncrementsRevision()
        {
            ItemCatalog catalog = CreateCatalog(1);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);

            long revision = state.Revision;

            Assert.That(state.Clear(), Is.True);
            Assert.That(state.Revision, Is.EqualTo(revision + 1));
        }

        [Test]
        public void Clear_WithEmptyState_ReturnsFalse()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            bool cleared = state.Clear();

            Assert.That(cleared, Is.False);
            Assert.That(state.FoundCount, Is.EqualTo(0));
            Assert.That(state.TotalCount, Is.EqualTo(2));
        }

        [Test]
        public void Clear_WithEmptyState_DoesNotIncrementRevision()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            Assert.That(state.Clear(), Is.False);

            Assert.That(state.Revision, Is.EqualTo(0));
        }

        [Test]
        public void CreateFoundItemIdSnapshot_WithEmptyState_ReturnsEmptySnapshot()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            IReadOnlyList<int> snapshot = state.CreateFoundItemIdSnapshot();

            Assert.That(snapshot, Is.Empty);
        }

        [Test]
        public void CreateFoundItemIdSnapshot_WithFoundItems_ReturnsIdsInAscendingOrder()
        {
            ItemCatalog catalog = CreateCatalog(1, 2, 3, 4);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(4), Is.True);
            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(3), Is.True);

            IReadOnlyList<int> snapshot = state.CreateFoundItemIdSnapshot();

            Assert.That(snapshot, Is.EqualTo(new[] { 1, 3, 4 }));
        }

        [Test]
        public void CreateFoundItemIdSnapshot_IsIndependentFromLaterStateChanges()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);

            IReadOnlyList<int> snapshot = state.CreateFoundItemIdSnapshot();

            Assert.That(state.MarkFound(2), Is.True);

            Assert.That(snapshot, Is.EqualTo(new[] { 1 }));
            Assert.That(state.CreateFoundItemIdSnapshot(), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void CreateFoundItemIdSnapshot_DuplicateDiscoveryDoesNotCreateDuplicates()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(2), Is.True);
            Assert.That(state.MarkFound(2), Is.False);

            IReadOnlyList<int> snapshot = state.CreateFoundItemIdSnapshot();

            Assert.That(snapshot, Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void CreateFoundItemIdSnapshot_UnknownItemsAreNotIncluded()
        {
            ItemCatalog catalog = CreateCatalog(1, 2);
            var state = new ChecklistState(catalog);

            Assert.That(state.MarkFound(1), Is.True);
            Assert.That(state.MarkFound(999), Is.False);

            IReadOnlyList<int> snapshot = state.CreateFoundItemIdSnapshot();

            Assert.That(snapshot, Is.EqualTo(new[] { 1 }));
        }

        private static ItemCatalog CreateCatalog(params int[] itemIds)
        {
            var entries = new ItemCatalogEntry[itemIds.Length];

            for (var index = 0; index < itemIds.Length; index++)
            {
                int itemId = itemIds[index];
                entries[index] = new ItemCatalogEntry(itemId, $"Item {itemId}");
            }

            return ItemCatalog.Create(entries);
        }
    }
}