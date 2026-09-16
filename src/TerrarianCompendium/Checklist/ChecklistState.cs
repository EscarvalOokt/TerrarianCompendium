using System;
using System.Collections.Generic;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Checklist
{
    internal sealed class ChecklistState(ItemCatalog catalog)
    {
        private readonly ItemCatalog _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        private readonly HashSet<int> _foundItemIds = new();
        private long _revision;

        public int FoundCount => _foundItemIds.Count;

        public int TotalCount => _catalog.Count;

        public double CompletionRatio => TotalCount == 0 ? 0.0 : (double)FoundCount / TotalCount;

        public long Revision => _revision;

        public bool IsFound(int itemId)
        {
            return _foundItemIds.Contains(itemId);
        }

        public bool MarkFound(int itemId)
        {
            if (!_catalog.Contains(itemId))
                return false;

            if (!_foundItemIds.Add(itemId))
                return false;

            _revision++;

            return true;
        }

        public bool Clear()
        {
            if (_foundItemIds.Count == 0)
                return false;

            _foundItemIds.Clear();
            _revision++;

            return true;
        }

        public IReadOnlyList<int> CreateFoundItemIdSnapshot()
        {
            var foundItemIds = new List<int>(_foundItemIds);
            foundItemIds.Sort();

            return foundItemIds.AsReadOnly();
        }
    }
}