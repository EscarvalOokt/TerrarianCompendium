using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class VanillaNpcCatalogBuilder(ILogger logger)
    {
        public NpcCatalog Build()
        {
            BestiaryDatabase database = Main.BestiaryDB;

            if (database == null)
                throw new InvalidOperationException("Terraria Bestiary database is not initialized.");

            List<BestiaryEntry> databaseEntries = database.Entries;

            if (databaseEntries == null)
                throw new InvalidOperationException("Terraria Bestiary database has no finalized entry collection.");

            var entries = new List<NpcCatalogEntry>();

            for (var bestiaryOrder = 0; bestiaryOrder < databaseEntries.Count; bestiaryOrder++)
            {
                BestiaryEntry bestiaryEntry = databaseEntries[bestiaryOrder];

                if (bestiaryEntry == null)
                {
                    throw new InvalidOperationException(
                        $"Terraria Bestiary entry at order {bestiaryOrder} is not initialized.");
                }

                if (!VanillaBestiaryNativeBridge.TryGetNpcNetId(bestiaryEntry, out int netId))
                    continue;

                if (!ContentSamples.NpcBestiaryCreditIdsByNpcNetIds.TryGetValue(netId, out string bestiaryCreditId) ||
                    string.IsNullOrWhiteSpace(bestiaryCreditId))
                {
                    throw new InvalidOperationException(
                        $"Terraria Bestiary entry for NPC net ID {netId} has no Bestiary credit identity.");
                }

                entries.Add(new NpcCatalogEntry(netId, bestiaryOrder));
            }

            var catalog = NpcCatalog.Create(entries);

            logger?.Info(
                $"Vanilla NPC catalog built: {catalog.Count} NPC-backed entries from " +
                $"BestiaryDatabase.Entries={databaseEntries.Count}.");

            return catalog;
        }
    }
}