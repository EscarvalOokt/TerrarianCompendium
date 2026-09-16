using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Bestiary
{
    internal sealed class NpcLootRelation
    {
        private static readonly IReadOnlyList<NpcLootCondition> _emptyConditions =
            new ReadOnlyCollection<NpcLootCondition>(new List<NpcLootCondition>());

        public NpcLootRelation(
            int npcNetId,
            int itemId,
            int stackMin,
            int stackMax,
            float dropRate,
            IEnumerable<NpcLootCondition> conditions = null)
        {
            if (itemId <= 0)
                throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be greater than zero.");

            if (stackMin < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stackMin),
                    stackMin,
                    "Minimum stack must not be negative.");
            }

            if (stackMax < stackMin)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stackMax),
                    stackMax,
                    "Maximum stack must be greater than or equal to minimum stack.");
            }

            if (float.IsNaN(dropRate) || float.IsInfinity(dropRate) || dropRate < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dropRate),
                    dropRate,
                    "Drop rate must be a finite non-negative value.");
            }

            NpcNetId = npcNetId;
            ItemId = itemId;
            StackMin = stackMin;
            StackMax = stackMax;
            DropRate = dropRate;
            Conditions = CreateConditionSnapshot(conditions);
        }

        public int NpcNetId { get; }

        public int ItemId { get; }

        public int StackMin { get; }

        public int StackMax { get; }

        public float DropRate { get; }

        public IReadOnlyList<NpcLootCondition> Conditions { get; }

        private static IReadOnlyList<NpcLootCondition> CreateConditionSnapshot(IEnumerable<NpcLootCondition> conditions)
        {
            if (conditions == null)
                return _emptyConditions;

            var conditionSnapshot = new List<NpcLootCondition>();

            foreach (NpcLootCondition condition in conditions)
            {
                if (condition == null)
                {
                    throw new ArgumentException("Loot conditions must not contain null values.", nameof(conditions));
                }

                conditionSnapshot.Add(condition);
            }

            if (conditionSnapshot.Count == 0)
                return _emptyConditions;

            return new ReadOnlyCollection<NpcLootCondition>(conditionSnapshot);
        }
    }
}