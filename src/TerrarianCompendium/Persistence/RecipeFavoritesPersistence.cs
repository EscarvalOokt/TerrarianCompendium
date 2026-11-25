using System;
using TerrarianCompendium.Recipes;

namespace TerrarianCompendium.Persistence
{
    internal sealed class RecipeFavoritesPersistence
    {
        private static readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(5);
        private readonly RecipeFavoriteState _favorites;
        private readonly Func<string> _resolvePath;
        private readonly RecipeFavoritesStore _store;
        private bool _loaded;
        private DateTime _nextAttempt;
        private string _path;
        private bool _preserveBackup;
        private bool _requiresRewrite;
        private long _savedRevision;
        private bool _unsupportedVersion;

        public RecipeFavoritesPersistence(
            RecipeFavoritesStore store,
            RecipeFavoriteState favorites,
            Func<string> resolvePath)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _favorites = favorites ?? throw new ArgumentNullException(nameof(favorites));
            _resolvePath = resolvePath ?? throw new ArgumentNullException(nameof(resolvePath));
            _favorites.SetEditingAvailable(false);
        }

        public void Update(DateTime now, bool forceRetry = false)
        {
            if (_unsupportedVersion || (!forceRetry && now < _nextAttempt))
                return;

            if (!_loaded)
            {
                _path ??= _resolvePath();
                if (_path == null)
                {
                    _nextAttempt = now + _retryInterval;
                    return;
                }

                RecipeFavoritesLoadResult result = _store.Load(_path);
                if (result.Status == RecipeFavoritesLoadStatus.Failed)
                {
                    _nextAttempt = now + _retryInterval;
                    return;
                }

                if (result.Status == RecipeFavoritesLoadStatus.UnsupportedVersion)
                {
                    _unsupportedVersion = true;
                    return;
                }

                _favorites.ReplacePersistentKeySnapshot(result.FavoriteRecipeKeys);
                _favorites.SetEditingAvailable(true);
                _savedRevision = _favorites.Revision;
                _loaded = true;
                _requiresRewrite = result.RequiresRewrite;
                _preserveBackup = result.Status == RecipeFavoritesLoadStatus.RecoveredFromBackup;
            }

            if (!_requiresRewrite && _savedRevision == _favorites.Revision)
                return;

            if (!_store.Save(_path, _favorites.CreatePersistentKeySnapshot(), _preserveBackup))
            {
                _nextAttempt = now + _retryInterval;
                return;
            }

            _nextAttempt = DateTime.MinValue;
            _savedRevision = _favorites.Revision;
            _requiresRewrite = false;
            _preserveBackup = false;
        }
    }
}