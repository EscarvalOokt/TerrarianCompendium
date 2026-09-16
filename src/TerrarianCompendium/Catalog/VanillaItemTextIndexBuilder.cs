using System;
using System.Collections.Generic;
using Terraria;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Catalog
{
    internal sealed class VanillaItemTextIndexBuilder(ILogger logger)
    {
        public void Rebuild(ItemCatalog catalog, ItemTextIndex itemTextIndex, string cultureName)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (itemTextIndex == null)
                throw new ArgumentNullException(nameof(itemTextIndex));

            if (string.IsNullOrWhiteSpace(cultureName))
                throw new ArgumentException("Culture name must not be empty or whitespace.", nameof(cultureName));

            var names = new Dictionary<int, string>(catalog.Count);
            var descriptions = new Dictionary<int, string>(catalog.Count);
            var describedCount = 0;

            foreach (ItemCatalogEntry entry in catalog.Items)
            {
                var item = new Item();
                item.SetDefaults(entry.Id);

                if (item.type != entry.Id)
                {
                    throw new InvalidOperationException(
                        $"Item.SetDefaults({entry.Id}) produced item type {item.type}.");
                }

                string name = item.Name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new InvalidOperationException(
                        $"Item.SetDefaults({entry.Id}) produced an empty localized name.");
                }

                string description = BuildDescription(item);

                if (description.Length > 0)
                    describedCount++;

                names.Add(entry.Id, name);
                descriptions.Add(entry.Id, description);
            }

            itemTextIndex.ReplaceSnapshot(cultureName, names, descriptions);

            logger?.Info(
                $"Vanilla item text built: {names.Count} entries; " +
                $"withDescription={describedCount}; culture={cultureName}.");
        }

        private static string BuildDescription(Item item)
        {
            if (item.ToolTip == null || item.ToolTip.Lines <= 0)
                return string.Empty;

            var lines = new List<string>(item.ToolTip.Lines);

            for (var lineIndex = 0; lineIndex < item.ToolTip.Lines; lineIndex++)
            {
                string line = item.ToolTip.GetLine(lineIndex);

                if (!string.IsNullOrWhiteSpace(line))
                    lines.Add(line);
            }

            return lines.Count == 0 ? string.Empty : string.Join("\n", lines);
        }
    }
}