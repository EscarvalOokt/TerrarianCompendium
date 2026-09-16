using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.Localization;
using Terraria.UI;
using TerrarianCompendium.Localization;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class VanillaBestiaryFilterOption
    {
        private readonly IBestiaryEntryFilter _filter;

        public VanillaBestiaryFilterOption(int id, IBestiaryEntryFilter filter)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Filter ID must not be negative.");

            Id = id;
            _filter = filter ?? throw new ArgumentNullException(nameof(filter));
            DisplayNameKey = _filter.GetDisplayNameKey() ?? string.Empty;
        }

        public int Id { get; }

        public string DisplayNameKey { get; }

        public string GetDisplayName(CompendiumLocalization localization)
        {
            if (localization == null)
                throw new ArgumentNullException(nameof(localization));

            return string.IsNullOrEmpty(DisplayNameKey)
                ? localization.Format(CompendiumTextKeys.Bestiary.NativeFilterFallback, Id)
                : Language.GetTextValue(DisplayNameKey);
        }

        public UIElement CreateImage()
        {
            return _filter.GetImage();
        }

        internal bool Matches(BestiaryEntry entry)
        {
            return _filter.FitsFilter(entry);
        }
    }

    internal sealed class VanillaBestiaryFilterCatalog
    {
        private readonly Dictionary<string, VanillaBestiaryFilterOption> _optionsByDisplayNameKey;
        private readonly Dictionary<int, VanillaBestiaryFilterOption> _optionsById;

        public VanillaBestiaryFilterCatalog(NpcCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            BestiaryDatabase database = Main.BestiaryDB;

            if (database == null)
                throw new InvalidOperationException("Terraria Bestiary database is not initialized.");

            List<IBestiaryEntryFilter> filters = database.Filters;

            if (filters == null)
                throw new InvalidOperationException("Terraria Bestiary database has no registered filter collection.");

            var options = new List<VanillaBestiaryFilterOption>();
            _optionsById = new Dictionary<int, VanillaBestiaryFilterOption>();
            _optionsByDisplayNameKey = new Dictionary<string, VanillaBestiaryFilterOption>(StringComparer.Ordinal);

            for (var index = 0; index < filters.Count; index++)
            {
                IBestiaryEntryFilter filter = filters[index];

                if (filter == null || filter is Filters.ByUnlockState)
                    continue;

                if (!MatchesAnyCatalogEntry(catalog, filter))
                    continue;

                var option = new VanillaBestiaryFilterOption(index, filter);
                options.Add(option);
                _optionsById.Add(index, option);

                if (!string.IsNullOrWhiteSpace(option.DisplayNameKey) &&
                    !_optionsByDisplayNameKey.ContainsKey(option.DisplayNameKey))
                {
                    _optionsByDisplayNameKey.Add(option.DisplayNameKey, option);
                }
            }

            Options = new ReadOnlyCollection<VanillaBestiaryFilterOption>(options);

            var optionIds = new List<int>(options.Count);

            foreach (VanillaBestiaryFilterOption option in options)
                optionIds.Add(option.Id);

            OptionIds = new ReadOnlyCollection<int>(optionIds);
        }

        public IReadOnlyList<VanillaBestiaryFilterOption> Options { get; }

        public IReadOnlyList<int> OptionIds { get; }

        public bool MatchesAny(NpcCatalogEntry catalogEntry, IReadOnlyCollection<int> activeFilterIds)
        {
            if (catalogEntry == null)
                throw new ArgumentNullException(nameof(catalogEntry));

            if (activeFilterIds == null)
                throw new ArgumentNullException(nameof(activeFilterIds));

            if (activeFilterIds.Count == 0)
                return true;

            BestiaryEntry entry = VanillaBestiaryNativeBridge.GetBestiaryEntry(catalogEntry);

            foreach (int filterId in activeFilterIds)
            {
                if (!_optionsById.TryGetValue(filterId, out VanillaBestiaryFilterOption option))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(activeFilterIds),
                        filterId,
                        "Unknown Bestiary native filter ID.");
                }

                if (option.Matches(entry))
                    return true;
            }

            return false;
        }

        public bool TryGetOptionByDisplayNameKey(string displayNameKey, out VanillaBestiaryFilterOption option)
        {
            if (string.IsNullOrWhiteSpace(displayNameKey))
            {
                option = null;
                return false;
            }

            return _optionsByDisplayNameKey.TryGetValue(displayNameKey, out option);
        }

        private static bool MatchesAnyCatalogEntry(NpcCatalog catalog, IBestiaryEntryFilter filter)
        {
            for (var index = 0; index < catalog.Count; index++)
            {
                BestiaryEntry entry = VanillaBestiaryNativeBridge.GetBestiaryEntry(catalog.Entries[index]);

                if (filter.FitsFilter(entry))
                    return true;
            }

            return false;
        }
    }
}