using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using TerrariaModder.Core.Logging;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class VanillaNpcLootIndexBuilder(ILogger logger)
    {
        public NpcLootIndex Build(NpcCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            BestiaryDatabase database = Main.BestiaryDB;

            if (database == null)
                throw new InvalidOperationException("Terraria Bestiary database is not initialized.");

            List<BestiaryEntry> databaseEntries = database.Entries;

            if (databaseEntries == null)
                throw new InvalidOperationException("Terraria Bestiary database has no finalized entry collection.");

            var relations = new List<NpcLootRelation>();
            var npcNetIdsWithLoot = new HashSet<int>();
            var itemIds = new HashSet<int>();

            for (var bestiaryOrder = 0; bestiaryOrder < databaseEntries.Count; bestiaryOrder++)
            {
                BestiaryEntry bestiaryEntry = databaseEntries[bestiaryOrder];

                if (bestiaryEntry == null)
                {
                    throw new InvalidOperationException(
                        $"Terraria Bestiary entry at order {bestiaryOrder} is not initialized.");
                }

                if (!VanillaBestiaryNativeBridge.TryGetNpcNetId(bestiaryEntry, out int npcNetId))
                    continue;

                if (!catalog.Contains(npcNetId))
                {
                    throw new InvalidOperationException(
                        $"Finalized Terraria Bestiary entry references NPC net ID {npcNetId}, " +
                        "which is not present in the NPC catalog.");
                }

                if (bestiaryEntry.Info == null)
                    throw new InvalidOperationException("Terraria Bestiary entry has no info collection.");

                for (var infoIndex = 0; infoIndex < bestiaryEntry.Info.Count; infoIndex++)
                {
                    if (bestiaryEntry.Info[infoIndex] is not ItemDropBestiaryInfoElement dropElement)
                        continue;

                    DropRateInfo dropRateInfo = VanillaBestiaryNativeBridge.GetDropRateInfo(dropElement);
                    NpcLootRelation relation = CreateRelation(npcNetId, dropRateInfo);

                    relations.Add(relation);
                    npcNetIdsWithLoot.Add(npcNetId);
                    itemIds.Add(relation.ItemId);
                }
            }

            var index = NpcLootIndex.Create(catalog, relations);

            logger?.Info(
                $"Vanilla NPC loot index built: {relations.Count} relations, " +
                $"{npcNetIdsWithLoot.Count} NPCs with loot, {itemIds.Count} distinct items.");

            return index;
        }

        private static NpcLootRelation CreateRelation(int npcNetId, DropRateInfo dropRateInfo)
        {
            List<NpcLootCondition> conditions = null;

            if (dropRateInfo.conditions is { Count: > 0 })
            {
                conditions = new List<NpcLootCondition>(dropRateInfo.conditions.Count);

                for (var conditionIndex = 0; conditionIndex < dropRateInfo.conditions.Count; conditionIndex++)
                {
                    IItemDropRuleCondition condition = dropRateInfo.conditions[conditionIndex];

                    if (condition == null)
                    {
                        throw new InvalidOperationException(
                            $"Terraria Bestiary loot relation for NPC net ID {npcNetId} and item ID " +
                            $"{dropRateInfo.itemId} contains a null drop condition.");
                    }

                    conditions.Add(new NpcLootCondition(condition));
                }
            }

            return new NpcLootRelation(
                npcNetId,
                dropRateInfo.itemId,
                dropRateInfo.stackMin,
                dropRateInfo.stackMax,
                dropRateInfo.dropRate,
                conditions);
        }
    }
}