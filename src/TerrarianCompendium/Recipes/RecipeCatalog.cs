using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeCatalog
    {
        private readonly IReadOnlyList<RecipeCatalogEntry> _recipes;
        private readonly Dictionary<int, RecipeCatalogEntry> _recipesByRuntimeIndex;

        private RecipeCatalog(
            IReadOnlyList<RecipeCatalogEntry> recipes,
            Dictionary<int, RecipeCatalogEntry> recipesByRuntimeIndex)
        {
            _recipes = recipes;
            _recipesByRuntimeIndex = recipesByRuntimeIndex;
        }

        public IReadOnlyList<RecipeCatalogEntry> Recipes => _recipes;

        public int Count => _recipes.Count;

        public static RecipeCatalog Create(IEnumerable<RecipeCatalogEntry> entries)
        {
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            var recipes = new List<RecipeCatalogEntry>();
            var recipesByRuntimeIndex = new Dictionary<int, RecipeCatalogEntry>();

            foreach (RecipeCatalogEntry entry in entries)
            {
                if (entry == null)
                    throw new ArgumentException("Catalog entries must not contain null values.", nameof(entries));

                if (recipesByRuntimeIndex.ContainsKey(entry.RuntimeIndex))
                {
                    throw new ArgumentException(
                        $"Catalog contains duplicate runtime recipe index {entry.RuntimeIndex}.",
                        nameof(entries));
                }

                recipes.Add(entry);
                recipesByRuntimeIndex.Add(entry.RuntimeIndex, entry);
            }

            recipes.Sort(CompareByRuntimeIndex);

            return new RecipeCatalog(new ReadOnlyCollection<RecipeCatalogEntry>(recipes), recipesByRuntimeIndex);
        }

        public bool Contains(int runtimeIndex)
        {
            return _recipesByRuntimeIndex.ContainsKey(runtimeIndex);
        }

        public bool TryGet(int runtimeIndex, out RecipeCatalogEntry entry)
        {
            return _recipesByRuntimeIndex.TryGetValue(runtimeIndex, out entry);
        }

        private static int CompareByRuntimeIndex(RecipeCatalogEntry left, RecipeCatalogEntry right)
        {
            return left.RuntimeIndex.CompareTo(right.RuntimeIndex);
        }
    }
}