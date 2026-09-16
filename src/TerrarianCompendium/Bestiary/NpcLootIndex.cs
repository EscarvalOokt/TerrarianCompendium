using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcLootIndex
    {
        private static readonly IReadOnlyList<NpcLootRelation> _emptyRelations =
            new ReadOnlyCollection<NpcLootRelation>(new List<NpcLootRelation>());

        private readonly Dictionary<int, IReadOnlyList<NpcLootRelation>> _dropsByNpcNetId;
        private readonly Dictionary<int, IReadOnlyList<NpcLootRelation>> _npcSourcesByItemId;

        private NpcLootIndex(
            Dictionary<int, IReadOnlyList<NpcLootRelation>> dropsByNpcNetId,
            Dictionary<int, IReadOnlyList<NpcLootRelation>> npcSourcesByItemId)
        {
            _dropsByNpcNetId = dropsByNpcNetId;
            _npcSourcesByItemId = npcSourcesByItemId;
        }

        public static NpcLootIndex Create(NpcCatalog catalog, IEnumerable<NpcLootRelation> relations)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            var dropsByNpcNetId = new Dictionary<int, List<NpcLootRelation>>();
            var npcSourcesByItemId = new Dictionary<int, List<NpcLootRelation>>();

            foreach (NpcLootRelation relation in relations)
            {
                if (relation == null)
                    throw new ArgumentException("Loot relations must not contain null values.", nameof(relations));

                if (!catalog.Contains(relation.NpcNetId))
                {
                    throw new ArgumentException(
                        $"Loot relation references NPC net ID {relation.NpcNetId}, which is not present in the catalog.",
                        nameof(relations));
                }

                AddRelation(dropsByNpcNetId, relation.NpcNetId, relation);
                AddRelation(npcSourcesByItemId, relation.ItemId, relation);
            }

            return new NpcLootIndex(Freeze(dropsByNpcNetId), Freeze(npcSourcesByItemId));
        }

        public IReadOnlyList<NpcLootRelation> GetDropsForNpc(int npcNetId)
        {
            return _dropsByNpcNetId.TryGetValue(npcNetId, out IReadOnlyList<NpcLootRelation> relations)
                ? relations
                : _emptyRelations;
        }

        public IReadOnlyList<NpcLootRelation> GetNpcSourcesForItem(int itemId)
        {
            return _npcSourcesByItemId.TryGetValue(itemId, out IReadOnlyList<NpcLootRelation> relations)
                ? relations
                : _emptyRelations;
        }

        public bool HasNpcSourceForItem(int itemId)
        {
            return _npcSourcesByItemId.ContainsKey(itemId);
        }

        private static void AddRelation(
            Dictionary<int, List<NpcLootRelation>> relationsById,
            int id,
            NpcLootRelation relation)
        {
            if (!relationsById.TryGetValue(id, out List<NpcLootRelation> relations))
            {
                relations = new List<NpcLootRelation>();
                relationsById.Add(id, relations);
            }

            relations.Add(relation);
        }

        private static Dictionary<int, IReadOnlyList<NpcLootRelation>> Freeze(
            Dictionary<int, List<NpcLootRelation>> relationsById)
        {
            var frozen = new Dictionary<int, IReadOnlyList<NpcLootRelation>>(relationsById.Count);

            foreach (KeyValuePair<int, List<NpcLootRelation>> pair in relationsById)
            {
                frozen.Add(pair.Key, new ReadOnlyCollection<NpcLootRelation>(new List<NpcLootRelation>(pair.Value)));
            }

            return frozen;
        }
    }
}