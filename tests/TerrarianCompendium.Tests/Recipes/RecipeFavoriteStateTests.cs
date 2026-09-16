using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipeFavoriteStateTests
    {
        [Test]
        public void Constructor_WithNullPersistentKeyIndex_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { _ = new RecipeFavoriteState(null); }));
        }

        [Test]
        public void NewState_IsEmptyWithZeroRevision()
        {
            RecipeCatalog recipeCatalog = CreateCatalog(CreateEntry(0, 100));
            RecipeFavoriteState state = CreateState(recipeCatalog);

            Assert.That(state.FavoriteCount, Is.Zero);
            Assert.That(state.Revision, Is.Zero);
            Assert.That(state.IsFavorite(0), Is.False);
            Assert.That(state.CreatePersistentKeySnapshot(), Is.Empty);
        }

        [Test]
        public void Toggle_AddsAndRemovesPersistentIdentity()
        {
            RecipeCatalog recipeCatalog = CreateCatalog(CreateEntry(7, 100));
            RecipeFavoriteState state = CreateState(recipeCatalog);

            bool added = state.Toggle(7);

            Assert.That(added, Is.True);
            Assert.That(state.IsFavorite(7), Is.True);
            Assert.That(state.FavoriteCount, Is.EqualTo(1));
            Assert.That(state.Revision, Is.EqualTo(1));

            bool removed = state.Toggle(7);

            Assert.That(removed, Is.False);
            Assert.That(state.IsFavorite(7), Is.False);
            Assert.That(state.FavoriteCount, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(2));
        }

        [Test]
        public void Toggle_WithUnknownRuntimeIndex_ThrowsWithoutChangingState()
        {
            RecipeCatalog recipeCatalog = CreateCatalog(CreateEntry(7, 100));
            RecipeFavoriteState state = CreateState(recipeCatalog);

            Assert.Throws<ArgumentOutOfRangeException>((Action)(() => { state.Toggle(8); }));
            Assert.That(state.FavoriteCount, Is.Zero);
            Assert.That(state.Revision, Is.Zero);
        }

        [Test]
        public void DuplicateRuntimeRecipesWithSamePersistentKey_ShareFavoriteState()
        {
            RecipeCatalogEntry first = CreateEntry(3, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(20, 100, CreateItemIngredient(10));
            RecipeCatalog recipeCatalog = CreateCatalog(first, second);
            RecipeFavoriteState state = CreateState(recipeCatalog);

            Assert.That(state.Toggle(3), Is.True);

            Assert.That(state.IsFavorite(3), Is.True);
            Assert.That(state.IsFavorite(20), Is.True);
            Assert.That(state.FavoriteCount, Is.EqualTo(1));

            Assert.That(state.Toggle(20), Is.False);
            Assert.That(state.IsFavorite(3), Is.False);
            Assert.That(state.IsFavorite(20), Is.False);
        }

        [Test]
        public void ReplacePersistentKeySnapshot_DeduplicatesAndPreservesOrphanedKeys()
        {
            RecipeCatalogEntry currentRecipe = CreateEntry(0, 100);
            RecipeCatalog recipeCatalog = CreateCatalog(currentRecipe);
            var currentKey = RecipePersistentKey.Create(currentRecipe);
            var orphanedKey = RecipePersistentKey.Create(CreateEntry(1, 200));
            RecipeFavoriteState state = CreateState(recipeCatalog);

            state.ReplacePersistentKeySnapshot([orphanedKey, currentKey, orphanedKey]);

            Assert.That(state.FavoriteCount, Is.EqualTo(2));
            Assert.That(state.IsFavorite(0), Is.True);
            Assert.That(
                GetValues(state.CreatePersistentKeySnapshot()),
                Is.EquivalentTo([currentKey.Value, orphanedKey.Value]));
        }

        [Test]
        public void ReplacePersistentKeySnapshot_WithEquivalentSet_DoesNotChangeRevision()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100);
            RecipeCatalogEntry second = CreateEntry(1, 200);
            RecipeCatalog recipeCatalog = CreateCatalog(first, second);
            var firstKey = RecipePersistentKey.Create(first);
            var secondKey = RecipePersistentKey.Create(second);
            RecipeFavoriteState state = CreateState(recipeCatalog);

            state.ReplacePersistentKeySnapshot([firstKey, secondKey]);
            long revision = state.Revision;

            state.ReplacePersistentKeySnapshot([secondKey, firstKey, secondKey]);

            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void ReplacePersistentKeySnapshot_WithDifferentSet_IncrementsRevisionOnce()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100);
            RecipeCatalogEntry second = CreateEntry(1, 200);
            RecipeCatalog recipeCatalog = CreateCatalog(first, second);
            var firstKey = RecipePersistentKey.Create(first);
            var secondKey = RecipePersistentKey.Create(second);
            RecipeFavoriteState state = CreateState(recipeCatalog);

            state.ReplacePersistentKeySnapshot([firstKey]);
            long revision = state.Revision;

            state.ReplacePersistentKeySnapshot([secondKey]);

            Assert.That(state.Revision, Is.EqualTo(revision + 1));
            Assert.That(state.IsFavorite(0), Is.False);
            Assert.That(state.IsFavorite(1), Is.True);
        }

        [Test]
        public void CreatePersistentKeySnapshot_ReturnsCanonicalOrdinalOrderAndIsReadOnly()
        {
            RecipeCatalogEntry third = CreateEntry(2, 300);
            RecipeCatalogEntry first = CreateEntry(0, 100);
            RecipeCatalogEntry second = CreateEntry(1, 200);
            RecipeCatalog recipeCatalog = CreateCatalog(third, first, second);
            RecipeFavoriteState state = CreateState(recipeCatalog);

            state.Toggle(2);
            state.Toggle(0);
            state.Toggle(1);

            IReadOnlyList<RecipePersistentKey> snapshot = state.CreatePersistentKeySnapshot();
            string[] values = GetValues(snapshot);
            var sorted = (string[])values.Clone();
            Array.Sort(sorted, StringComparer.Ordinal);

            Assert.That(values, Is.EqualTo(sorted));
            Assert.Throws<NotSupportedException>(
                (Action)(() => { ((IList<RecipePersistentKey>)snapshot).Add(snapshot[0]); }));
        }

        [Test]
        public void ReplacePersistentKeySnapshot_WithNullCollection_Throws()
        {
            RecipeFavoriteState state = CreateState(CreateCatalog(CreateEntry(0, 100)));

            Assert.Throws<ArgumentNullException>((Action)(() => { state.ReplacePersistentKeySnapshot(null); }));
        }

        [Test]
        public void ReplacePersistentKeySnapshot_WithNullKey_Throws()
        {
            RecipeFavoriteState state = CreateState(CreateCatalog(CreateEntry(0, 100)));

            Assert.Throws<ArgumentException>((Action)(() => { state.ReplacePersistentKeySnapshot([null]); }));
        }

        private static RecipeFavoriteState CreateState(RecipeCatalog recipeCatalog)
        {
            return new RecipeFavoriteState(RecipePersistentKeyIndex.Create(recipeCatalog));
        }

        private static RecipeCatalog CreateCatalog(params RecipeCatalogEntry[] entries)
        {
            return RecipeCatalog.Create(entries);
        }

        private static RecipeCatalogEntry CreateEntry(
            int runtimeIndex,
            int resultItemId,
            params RecipeIngredient[] ingredients)
        {
            return new RecipeCatalogEntry(
                runtimeIndex,
                resultItemId,
                1,
                ingredients ?? [],
                new RecipeEnvironmentRequirements(null, false, false, false, false, false, false, false),
                isAlchemy: false);
        }

        private static RecipeIngredient CreateItemIngredient(int itemId)
        {
            return new RecipeIngredient(itemId, 1, RecipeIngredientRequirement.ForItem(itemId));
        }

        private static string[] GetValues(IReadOnlyList<RecipePersistentKey> keys)
        {
            var values = new string[keys.Count];

            for (var index = 0; index < keys.Count; index++)
                values[index] = keys[index].Value;

            return values;
        }
    }
}