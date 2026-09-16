using System;
using System.Collections.Generic;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Crafting
{
    internal sealed class CraftingAvailabilityState(RecipeCatalog recipeCatalog)
    {
        private readonly HashSet<int> _availableRecipeIndices = new();

        private readonly RecipeCatalog _recipeCatalog =
            recipeCatalog ?? throw new ArgumentNullException(nameof(recipeCatalog));

        private long _revision;

        public int Count => _availableRecipeIndices.Count;

        public long Revision => _revision;

        public bool IsCraftable(int runtimeRecipeIndex)
        {
            return _availableRecipeIndices.Contains(runtimeRecipeIndex);
        }

        public bool ReplaceSnapshot(IEnumerable<int> runtimeRecipeIndices)
        {
            if (runtimeRecipeIndices == null)
                throw new ArgumentNullException(nameof(runtimeRecipeIndices));

            var normalizedRecipeIndices = new HashSet<int>();

            foreach (int runtimeRecipeIndex in runtimeRecipeIndices)
            {
                if (_recipeCatalog.Contains(runtimeRecipeIndex))
                    normalizedRecipeIndices.Add(runtimeRecipeIndex);
            }

            if (_availableRecipeIndices.SetEquals(normalizedRecipeIndices))
                return false;

            _availableRecipeIndices.Clear();
            _availableRecipeIndices.UnionWith(normalizedRecipeIndices);
            _revision++;

            return true;
        }

        public IReadOnlyList<int> CreateAvailableRecipeIndexSnapshot()
        {
            var runtimeRecipeIndices = new List<int>(_availableRecipeIndices);
            runtimeRecipeIndices.Sort();

            return runtimeRecipeIndices.AsReadOnly();
        }
    }
}