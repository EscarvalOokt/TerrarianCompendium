using System;
using System.Collections.Generic;
using TerrarianCompendium.Checklist;

namespace TerrarianCompendium.Persistence
{
    internal sealed class ChecklistProgressPersistence(ChecklistProgressStore store)
    {
        private static readonly TimeSpan _checkpointInterval = TimeSpan.FromSeconds(30);
        private readonly Dictionary<string, CharacterProgress> _characters = new(StringComparer.OrdinalIgnoreCase);

        private readonly ChecklistProgressStore _store = store ?? throw new ArgumentNullException(nameof(store));
        private CharacterProgress _active;
        private ChecklistState _checklist;
        private DateTime _nextCheckpoint;
        private long _observedRevision;

        public void Open(string path, string legacyPath, ChecklistState checklist, DateTime now)
        {
            Close();
            _checklist = checklist ?? throw new ArgumentNullException(nameof(checklist));
            if (!_characters.TryGetValue(path, out _active))
            {
                _active = new CharacterProgress(path, legacyPath);
                _characters.Add(path, _active);
            }

            _observedRevision = -1;
            _active.NeedsLoad = true;
            _active.UnsupportedVersion = false;
            Synchronize(_active);
            // Detached records may already be due for a retry when switching characters.
            if (_characters.Count == 1)
                _nextCheckpoint = now + _checkpointInterval;
        }

        public void Update(DateTime now, bool force = false)
        {
            if (_characters.Count == 0 || (!force && now < _nextCheckpoint))
                return;

            _nextCheckpoint = now + _checkpointInterval;
            foreach (CharacterProgress character in new List<CharacterProgress>(_characters.Values))
            {
                Synchronize(character);
                if (character != _active && !character.HasPendingChanges)
                    _characters.Remove(character.Path);
            }
        }

        public void Close()
        {
            if (_active == null)
                return;

            Synchronize(_active);
            if (!_active.HasPendingChanges)
                _characters.Remove(_active.Path);

            _active = null;
            _checklist = null;
        }

        private void Synchronize(CharacterProgress character)
        {
            if (character.UnsupportedVersion)
                return;

            if (character == _active && _checklist.Revision != _observedRevision)
            {
                int previousCount = character.Ids.Count;
                character.Ids.UnionWith(_checklist.CreateFoundItemIdSnapshot());
                if (character.Ids.Count != previousCount)
                    character.Dirty = true;
                _observedRevision = _checklist.Revision;
            }

            if (character.NeedsLoad)
            {
                ChecklistProgressLoadResult loaded = _store.LoadWithLegacyFallback(
                    character.Path,
                    character.LegacyPath,
                    out bool legacy);

                if (loaded.Status == ChecklistProgressLoadStatus.UnsupportedVersion)
                {
                    character.UnsupportedVersion = true;
                    return;
                }

                if (loaded.Status == ChecklistProgressLoadStatus.Failed)
                    return;

                // The disk set includes positive IDs that this runtime's catalog may not recognize.
                var diskIds = new HashSet<int>(loaded.FoundItemIds);
                diskIds.RemoveWhere(id => id <= 0);
                character.Dirty = loaded.RequiresRewrite || legacy || !character.Ids.IsSubsetOf(diskIds);
                character.Ids.UnionWith(diskIds);
                character.NeedsLoad = false;
                character.PreserveBackup = loaded.Status == ChecklistProgressLoadStatus.RecoveredFromBackup;
                if (character == _active)
                {
                    foreach (int id in character.Ids)
                        _checklist.MarkFound(id);

                    _observedRevision = _checklist.Revision;
                }
            }

            if (!character.Dirty)
                return;

            if (_store.Save(character.Path, character.Ids, character.PreserveBackup))
            {
                character.Dirty = false;
                character.PreserveBackup = false;
            }
            else
            {
                // Revalidate the on-disk schema and merge any newer discoveries on retry.
                character.NeedsLoad = true;
            }
        }

        private sealed class CharacterProgress(string path, string legacyPath)
        {
            public string Path { get; } = path;
            public string LegacyPath { get; } = legacyPath;
            public HashSet<int> Ids { get; } = new();
            public bool NeedsLoad { get; set; } = true;
            public bool UnsupportedVersion { get; set; }
            public bool Dirty { get; set; }
            public bool PreserveBackup { get; set; }
            public bool HasPendingChanges => !UnsupportedVersion && Dirty;
        }
    }
}