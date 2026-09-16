using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Crafting;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Crafting
{
    [TestFixture]
    public sealed class CraftingAvailabilityStateTests
    {
        [Test]
        public void Constructor_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { _ = new CraftingAvailabilityState(null); }));
        }

        [Test]
        public void InitialState_IsEmptyWithZeroRevision()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));

            Assert.That(state.Count, Is.EqualTo(0));
            Assert.That(state.Revision, Is.EqualTo(0));
            Assert.That(state.IsCraftable(0), Is.False);
            Assert.That(state.CreateAvailableRecipeIndexSnapshot(), Is.Empty);
        }

        [Test]
        public void ReplaceSnapshot_WithFirstNonEmptySnapshot_UpdatesState()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));

            bool changed = state.ReplaceSnapshot(new[] { 2, 0 });

            Assert.That(changed, Is.True);
            Assert.That(state.Count, Is.EqualTo(2));
            Assert.That(state.Revision, Is.EqualTo(1));
            Assert.That(state.IsCraftable(0), Is.True);
            Assert.That(state.IsCraftable(2), Is.True);
        }

        [Test]
        public void ReplaceSnapshot_WithIdenticalSnapshot_DoesNotChangeRevision()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));
            state.ReplaceSnapshot(new[] { 0, 2 });

            bool changed = state.ReplaceSnapshot(new[] { 0, 2 });

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceSnapshot_WithSameSetInDifferentOrder_DoesNotChangeRevision()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2, 5));
            state.ReplaceSnapshot(new[] { 0, 2, 5 });

            bool changed = state.ReplaceSnapshot(new[] { 5, 0, 2 });

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceSnapshot_WithDuplicates_NormalizesSet()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));

            state.ReplaceSnapshot(new[] { 0, 0, 2, 2 });
            bool changed = state.ReplaceSnapshot(new[] { 2, 0 });

            Assert.That(changed, Is.False);
            Assert.That(state.Count, Is.EqualTo(2));
            Assert.That(state.Revision, Is.EqualTo(1));
        }

        [Test]
        public void ReplaceSnapshot_WhenRecipeIsAdded_IncrementsRevisionOnce()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));
            state.ReplaceSnapshot(new[] { 0 });

            bool changed = state.ReplaceSnapshot(new[] { 0, 2 });

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(2));
            Assert.That(state.IsCraftable(2), Is.True);
        }

        [Test]
        public void ReplaceSnapshot_WhenRecipeIsRemoved_IncrementsRevisionOnce()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));
            state.ReplaceSnapshot(new[] { 0, 2 });

            bool changed = state.ReplaceSnapshot(new[] { 2 });

            Assert.That(changed, Is.True);
            Assert.That(state.Revision, Is.EqualTo(2));
            Assert.That(state.IsCraftable(0), Is.False);
            Assert.That(state.IsCraftable(2), Is.True);
        }

        [Test]
        public void ReplaceSnapshot_WithEmptySnapshot_ClearsNonEmptyStateOnce()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0));
            state.ReplaceSnapshot(new[] { 0 });

            bool changed = state.ReplaceSnapshot(Array.Empty<int>());
            bool changedAgain = state.ReplaceSnapshot(Array.Empty<int>());

            Assert.That(changed, Is.True);
            Assert.That(changedAgain, Is.False);
            Assert.That(state.Count, Is.EqualTo(0));
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void ReplaceSnapshot_IgnoresUnknownRuntimeIndices()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2));

            bool changed = state.ReplaceSnapshot(new[] { 0, 1, 2, 999 });

            Assert.That(changed, Is.True);
            Assert.That(state.Count, Is.EqualTo(2));
            Assert.That(state.IsCraftable(0), Is.True);
            Assert.That(state.IsCraftable(1), Is.False);
            Assert.That(state.IsCraftable(2), Is.True);
            Assert.That(state.IsCraftable(999), Is.False);
        }

        [Test]
        public void ReplaceSnapshot_WithOnlyUnknownRuntimeIndices_DoesNotChangeEmptyState()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0));

            bool changed = state.ReplaceSnapshot(new[] { 1, 999 });

            Assert.That(changed, Is.False);
            Assert.That(state.Revision, Is.EqualTo(0));
        }

        [Test]
        public void ReplaceSnapshot_WithNullSnapshot_Throws()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0));

            Assert.Throws<ArgumentNullException>((Action)(() => { state.ReplaceSnapshot(null); }));
        }

        [Test]
        public void CreateAvailableRecipeIndexSnapshot_ReturnsSortedReadOnlySnapshot()
        {
            var state = new CraftingAvailabilityState(CreateCatalog(0, 2, 5));
            state.ReplaceSnapshot(new[] { 5, 0, 2 });

            IReadOnlyList<int> snapshot = state.CreateAvailableRecipeIndexSnapshot();
            var mutableView = (IList<int>)snapshot;

            Assert.That(snapshot, Is.EqualTo(new[] { 0, 2, 5 }));
            Assert.Throws<NotSupportedException>((Action)(() => { mutableView.Add(7); }));
        }

        private static RecipeCatalog CreateCatalog(params int[] runtimeIndices)
        {
            var entries = new List<RecipeCatalogEntry>();

            foreach (int runtimeIndex in runtimeIndices)
            {
                entries.Add(
                    new RecipeCatalogEntry(
                        runtimeIndex,
                        runtimeIndex + 1,
                        1,
                        Array.Empty<RecipeIngredient>(),
                        new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                        isAlchemy: false));
            }

            return RecipeCatalog.Create(entries);
        }
    }
}