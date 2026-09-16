using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Recipes
{
    internal sealed class RecipeFavoriteState(RecipePersistentKeyIndex persistentKeyIndex)
    {
        private readonly HashSet<RecipePersistentKey> _favoriteKeys = new();

        private readonly RecipePersistentKeyIndex _persistentKeyIndex =
            persistentKeyIndex ?? throw new ArgumentNullException(nameof(persistentKeyIndex));

        private long _revision;

        public int FavoriteCount => _favoriteKeys.Count;

        public long Revision => _revision;

        public bool IsFavorite(int runtimeIndex)
        {
            return _persistentKeyIndex.TryGetKey(runtimeIndex, out RecipePersistentKey key) &&
                   _favoriteKeys.Contains(key);
        }

        public bool Toggle(int runtimeIndex)
        {
            if (!_persistentKeyIndex.TryGetKey(runtimeIndex, out RecipePersistentKey key))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runtimeIndex),
                    runtimeIndex,
                    "Runtime recipe index is not present in the persistent recipe key index.");
            }

            bool isFavorite;

            if (_favoriteKeys.Remove(key))
            {
                isFavorite = false;
            }
            else
            {
                _favoriteKeys.Add(key);
                isFavorite = true;
            }

            _revision++;
            return isFavorite;
        }

        public void ReplacePersistentKeySnapshot(IEnumerable<RecipePersistentKey> persistentKeys)
        {
            if (persistentKeys == null)
                throw new ArgumentNullException(nameof(persistentKeys));

            var replacement = new HashSet<RecipePersistentKey>();

            foreach (RecipePersistentKey key in persistentKeys)
            {
                if (key == null)
                {
                    throw new ArgumentException(
                        "Favorite recipe key snapshot must not contain null values.",
                        nameof(persistentKeys));
                }

                replacement.Add(key);
            }

            if (_favoriteKeys.SetEquals(replacement))
                return;

            _favoriteKeys.Clear();

            foreach (RecipePersistentKey key in replacement)
                _favoriteKeys.Add(key);

            _revision++;
        }

        public IReadOnlyList<RecipePersistentKey> CreatePersistentKeySnapshot()
        {
            var keys = new List<RecipePersistentKey>(_favoriteKeys);
            keys.Sort(CompareKeys);

            return new ReadOnlyCollection<RecipePersistentKey>(keys);
        }

        private static int CompareKeys(RecipePersistentKey left, RecipePersistentKey right)
        {
            return string.Compare(left.Value, right.Value, StringComparison.Ordinal);
        }
    }
}