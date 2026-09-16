using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipePersistentKeyIndex
    {
        private static readonly IReadOnlyList<RecipeCatalogEntry> _emptyRecipes =
            new ReadOnlyCollection<RecipeCatalogEntry>(new List<RecipeCatalogEntry>());

        private readonly Dictionary<int, RecipePersistentKey> _keysByRuntimeIndex;
        private readonly Dictionary<RecipePersistentKey, IReadOnlyList<RecipeCatalogEntry>> _recipesByKey;

        private RecipePersistentKeyIndex(
            IReadOnlyList<RecipePersistentKey> keys,
            Dictionary<int, RecipePersistentKey> keysByRuntimeIndex,
            Dictionary<RecipePersistentKey, IReadOnlyList<RecipeCatalogEntry>> recipesByKey)
        {
            Keys = keys;
            _keysByRuntimeIndex = keysByRuntimeIndex;
            _recipesByKey = recipesByKey;
        }

        public IReadOnlyList<RecipePersistentKey> Keys { get; }

        public int Count => _keysByRuntimeIndex.Count;

        public int UniqueKeyCount => _recipesByKey.Count;

        public static RecipePersistentKeyIndex Create(RecipeCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            var keysByRuntimeIndex = new Dictionary<int, RecipePersistentKey>(catalog.Count);
            var mutableRecipesByKey = new Dictionary<RecipePersistentKey, List<RecipeCatalogEntry>>();

            foreach (RecipeCatalogEntry recipe in catalog.Recipes)
            {
                var key = RecipePersistentKey.Create(recipe);
                keysByRuntimeIndex.Add(recipe.RuntimeIndex, key);

                if (!mutableRecipesByKey.TryGetValue(key, out List<RecipeCatalogEntry> recipes))
                {
                    recipes = new List<RecipeCatalogEntry>();
                    mutableRecipesByKey.Add(key, recipes);
                }

                recipes.Add(recipe);
            }

            var keys = new List<RecipePersistentKey>(mutableRecipesByKey.Keys);
            keys.Sort(CompareKeys);

            var recipesByKey = new Dictionary<RecipePersistentKey, IReadOnlyList<RecipeCatalogEntry>>(
                mutableRecipesByKey.Count);

            foreach (KeyValuePair<RecipePersistentKey, List<RecipeCatalogEntry>> pair in mutableRecipesByKey)
            {
                var recipes = new List<RecipeCatalogEntry>(pair.Value);
                recipes.Sort(CompareRecipesByRuntimeIndex);
                recipesByKey.Add(pair.Key, new ReadOnlyCollection<RecipeCatalogEntry>(recipes));
            }

            return new RecipePersistentKeyIndex(
                new ReadOnlyCollection<RecipePersistentKey>(keys),
                keysByRuntimeIndex,
                recipesByKey);
        }

        public bool TryGetKey(int runtimeIndex, out RecipePersistentKey key)
        {
            return _keysByRuntimeIndex.TryGetValue(runtimeIndex, out key);
        }

        public IReadOnlyList<RecipeCatalogEntry> GetRecipes(RecipePersistentKey key)
        {
            if (key == null)
                return _emptyRecipes;

            return _recipesByKey.TryGetValue(key, out IReadOnlyList<RecipeCatalogEntry> recipes)
                ? recipes
                : _emptyRecipes;
        }

        private static int CompareKeys(RecipePersistentKey left, RecipePersistentKey right)
        {
            return string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        }

        private static int CompareRecipesByRuntimeIndex(RecipeCatalogEntry left, RecipeCatalogEntry right)
        {
            return left.RuntimeIndex.CompareTo(right.RuntimeIndex);
        }
    }
}