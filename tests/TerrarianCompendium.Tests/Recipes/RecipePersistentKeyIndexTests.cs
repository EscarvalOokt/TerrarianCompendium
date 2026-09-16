using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Tests.Recipes
{
    [TestFixture]
    public sealed class RecipePersistentKeyIndexTests
    {
        [Test]
        public void Create_WithNullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>((Action)(() => { RecipePersistentKeyIndex.Create(null); }));
        }

        [Test]
        public void Create_WithEmptyCatalog_ReturnsEmptyIndex()
        {
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([]));

            Assert.That(index.Count, Is.Zero);
            Assert.That(index.UniqueKeyCount, Is.Zero);
            Assert.That(index.Keys, Is.Empty);
        }

        [Test]
        public void TryGetKey_ReturnsKeyForRuntimeRecipe()
        {
            RecipeCatalogEntry recipe = CreateEntry(7, 100);
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([recipe]));

            bool found = index.TryGetKey(7, out RecipePersistentKey key);

            Assert.That(found, Is.True);
            Assert.That(key, Is.EqualTo(RecipePersistentKey.Create(recipe)));
        }

        [Test]
        public void TryGetKey_WithMissingRuntimeRecipe_ReturnsFalse()
        {
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([CreateEntry(7, 100)]));

            bool found = index.TryGetKey(8, out RecipePersistentKey key);

            Assert.That(found, Is.False);
            Assert.That(key, Is.Null);
        }

        [Test]
        public void GetRecipes_ReturnsCatalogEntryForPersistentKey()
        {
            RecipeCatalogEntry recipe = CreateEntry(7, 100);
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([recipe]));
            var key = RecipePersistentKey.Create(recipe);

            IReadOnlyList<RecipeCatalogEntry> recipes = index.GetRecipes(key);

            Assert.That(recipes, Is.EqualTo([recipe]));
            Assert.That(recipes[0], Is.SameAs(recipe));
        }

        [Test]
        public void GetRecipes_WithParsedPersistentValue_ResolvesCurrentRecipe()
        {
            RecipeCatalogEntry recipe = CreateEntry(7, 100, CreateItemIngredient(10));
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([recipe]));
            RecipePersistentKey currentKey = GetKey(index, recipe.RuntimeIndex);
            bool parsed = RecipePersistentKey.TryParse(currentKey.Value, out RecipePersistentKey persistedKey);

            Assert.That(parsed, Is.True);
            Assert.That(index.GetRecipes(persistedKey), Is.EqualTo([recipe]));
        }

        [Test]
        public void GetRecipes_WithNullKey_ReturnsEmptyResult()
        {
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([CreateEntry(7, 100)]));

            Assert.That(index.GetRecipes(null), Is.Empty);
        }

        [Test]
        public void DuplicatePersistentKeys_AreGroupedWithoutRuntimeIndexDisambiguation()
        {
            RecipeCatalogEntry first = CreateEntry(20, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(3, 100, CreateItemIngredient(10));
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([first, second]));

            RecipePersistentKey firstKey = GetKey(index, first.RuntimeIndex);
            RecipePersistentKey secondKey = GetKey(index, second.RuntimeIndex);
            IReadOnlyList<RecipeCatalogEntry> recipes = index.GetRecipes(firstKey);

            Assert.That(secondKey, Is.EqualTo(firstKey));
            Assert.That(index.Count, Is.EqualTo(2));
            Assert.That(index.UniqueKeyCount, Is.EqualTo(1));
            Assert.That(GetRuntimeIndices(recipes), Is.EqualTo([3, 20]));
        }

        [Test]
        public void DifferentPersistentDescriptors_RemainSeparate()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(1, 101, CreateItemIngredient(10));
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([first, second]));

            RecipePersistentKey firstKey = GetKey(index, first.RuntimeIndex);
            RecipePersistentKey secondKey = GetKey(index, second.RuntimeIndex);

            Assert.That(secondKey, Is.Not.EqualTo(firstKey));
            Assert.That(index.UniqueKeyCount, Is.EqualTo(2));
            Assert.That(index.GetRecipes(firstKey), Is.EqualTo([first]));
            Assert.That(index.GetRecipes(secondKey), Is.EqualTo([second]));
        }

        [Test]
        public void CatalogInputOrder_DoesNotChangeGeneratedKeys()
        {
            RecipeCatalogEntry third = CreateEntry(2, 300, CreateItemIngredient(30));
            RecipeCatalogEntry first = CreateEntry(0, 100, CreateItemIngredient(10));
            RecipeCatalogEntry second = CreateEntry(1, 200, CreateItemIngredient(20));
            var unordered = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([third, first, second]));
            var ordered = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([first, second, third]));

            Assert.That(GetKey(unordered, 0), Is.EqualTo(GetKey(ordered, 0)));
            Assert.That(GetKey(unordered, 1), Is.EqualTo(GetKey(ordered, 1)));
            Assert.That(GetKey(unordered, 2), Is.EqualTo(GetKey(ordered, 2)));
            Assert.That(GetKeyValues(unordered.Keys), Is.EqualTo(GetKeyValues(ordered.Keys)));
        }

        [Test]
        public void Keys_AreSortedByCanonicalValue()
        {
            var index = RecipePersistentKeyIndex.Create(
                RecipeCatalog.Create(
                [
                    CreateEntry(0, 300),
                    CreateEntry(1, 100),
                    CreateEntry(2, 200)
                ]));

            string[] keyValues = GetKeyValues(index.Keys);
            var sorted = (string[])keyValues.Clone();
            Array.Sort(sorted, StringComparer.Ordinal);

            Assert.That(keyValues, Is.EqualTo(sorted));
        }

        [Test]
        public void Keys_AndRecipeGroups_AreReadOnly()
        {
            RecipeCatalogEntry first = CreateEntry(0, 100);
            RecipeCatalogEntry second = CreateEntry(1, 100);
            var index = RecipePersistentKeyIndex.Create(RecipeCatalog.Create([first, second]));
            RecipePersistentKey key = GetKey(index, 0);
            var keys = (IList<RecipePersistentKey>)index.Keys;
            var recipes = (IList<RecipeCatalogEntry>)index.GetRecipes(key);

            Assert.Throws<NotSupportedException>((Action)(() => { keys.Add(key); }));
            Assert.Throws<NotSupportedException>((Action)(() => { recipes.Add(CreateEntry(2, 100)); }));
        }

        private static RecipePersistentKey GetKey(RecipePersistentKeyIndex index, int runtimeIndex)
        {
            bool found = index.TryGetKey(runtimeIndex, out RecipePersistentKey key);
            Assert.That(found, Is.True);

            return key;
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

        private static int[] GetRuntimeIndices(IReadOnlyList<RecipeCatalogEntry> recipes)
        {
            var runtimeIndices = new int[recipes.Count];

            for (var index = 0; index < recipes.Count; index++)
                runtimeIndices[index] = recipes[index].RuntimeIndex;

            return runtimeIndices;
        }

        private static string[] GetKeyValues(IReadOnlyList<RecipePersistentKey> keys)
        {
            var values = new string[keys.Count];

            for (var index = 0; index < keys.Count; index++)
                values[index] = keys[index].Value;

            return values;
        }
    }
}