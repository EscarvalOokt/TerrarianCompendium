using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Catalog
{
    internal sealed class ItemTextIndex
    {
        private readonly HashSet<int> _itemIds;
        private IReadOnlyDictionary<int, string> _descriptions;
        private IReadOnlyDictionary<int, string> _names;

        public ItemTextIndex(ItemCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            _itemIds = new HashSet<int>();
            var descriptions = new Dictionary<int, string>(catalog.Count);
            var names = new Dictionary<int, string>(catalog.Count);

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                _itemIds.Add(entry.Id);
                names.Add(entry.Id, entry.Name);
                descriptions.Add(entry.Id, string.Empty);
            }

            _names = new ReadOnlyDictionary<int, string>(names);
            _descriptions = new ReadOnlyDictionary<int, string>(descriptions);
        }

        public string CultureName { get; private set; } = string.Empty;

        public long Revision { get; private set; }

        public string GetName(int itemId)
        {
            return _names.TryGetValue(itemId, out string name) ? name : string.Empty;
        }

        public string GetDescription(int itemId)
        {
            return _descriptions.TryGetValue(itemId, out string description) ? description : string.Empty;
        }

        public bool MatchesDescription(int itemId, string query)
        {
            if (string.IsNullOrEmpty(query))
                return false;

            if (!_descriptions.TryGetValue(itemId, out string description))
                return false;

            return description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void ReplaceSnapshot(
            string cultureName,
            IReadOnlyDictionary<int, string> names,
            IReadOnlyDictionary<int, string> descriptions)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
                throw new ArgumentException("Culture name must not be empty or whitespace.", nameof(cultureName));

            if (names == null)
                throw new ArgumentNullException(nameof(names));

            if (descriptions == null)
                throw new ArgumentNullException(nameof(descriptions));

            if (names.Count != _itemIds.Count)
            {
                throw new ArgumentException(
                    "Item name snapshot must contain exactly one entry for every catalog item.",
                    nameof(names));
            }

            if (descriptions.Count != _itemIds.Count)
            {
                throw new ArgumentException(
                    "Item description snapshot must contain exactly one entry for every catalog item.",
                    nameof(descriptions));
            }

            var nameSnapshot = new Dictionary<int, string>(_itemIds.Count);
            var descriptionSnapshot = new Dictionary<int, string>(_itemIds.Count);

            foreach (int itemId in _itemIds)
            {
                if (!names.TryGetValue(itemId, out string name))
                {
                    throw new ArgumentException(
                        $"Item name snapshot does not contain catalog item ID {itemId}.",
                        nameof(names));
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(
                        $"Item name snapshot contains empty or whitespace text for item ID {itemId}.",
                        nameof(names));
                }

                if (!descriptions.TryGetValue(itemId, out string description))
                {
                    throw new ArgumentException(
                        $"Item description snapshot does not contain catalog item ID {itemId}.",
                        nameof(descriptions));
                }

                if (description == null)
                {
                    throw new ArgumentException(
                        $"Item description snapshot contains null text for item ID {itemId}.",
                        nameof(descriptions));
                }

                nameSnapshot.Add(itemId, name);
                descriptionSnapshot.Add(itemId, description);
            }

            _names = new ReadOnlyDictionary<int, string>(nameSnapshot);
            _descriptions = new ReadOnlyDictionary<int, string>(descriptionSnapshot);
            CultureName = cultureName;
            Revision++;
        }
    }
}