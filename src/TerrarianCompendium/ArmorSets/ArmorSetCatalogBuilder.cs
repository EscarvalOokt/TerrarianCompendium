using System;
using System.Collections.Generic;

namespace TerrarianCompendium.ArmorSets
{
    internal sealed class ArmorSetCatalogSourceEntry
    {
        public ArmorSetCatalogSourceEntry(
            string bonusIdentity,
            string bonusTextKey,
            ArmorSetPrimaryPart primaryPart,
            ArmorSetVariant variant)
        {
            if (string.IsNullOrWhiteSpace(bonusIdentity))
                throw new ArgumentException("Bonus identity must not be empty or whitespace.", nameof(bonusIdentity));

            if (string.IsNullOrWhiteSpace(bonusTextKey))
                throw new ArgumentException("Bonus text key must not be empty or whitespace.", nameof(bonusTextKey));

            if (!Enum.IsDefined(typeof(ArmorSetPrimaryPart), primaryPart))
                throw new ArgumentOutOfRangeException(
                    nameof(primaryPart),
                    primaryPart,
                    "Unsupported primary armor-set part.");

            BonusIdentity = bonusIdentity;
            BonusTextKey = bonusTextKey;
            PrimaryPart = primaryPart;
            Variant = variant;
        }

        public string BonusIdentity { get; }

        public string BonusTextKey { get; }

        public ArmorSetPrimaryPart PrimaryPart { get; }

        public ArmorSetVariant Variant { get; }
    }

    internal sealed class ArmorSetCatalogBuilder
    {
        public ArmorSetCatalog Build(IEnumerable<ArmorSetCatalogSourceEntry> sourceEntries)
        {
            if (sourceEntries == null)
                throw new ArgumentNullException(nameof(sourceEntries));

            var sources = new List<SourceWithOrder>();
            var seenVariantsByBonus = new Dictionary<string, HashSet<ArmorSetVariant>>(StringComparer.Ordinal);
            var order = 0;

            foreach (ArmorSetCatalogSourceEntry source in sourceEntries)
            {
                if (source == null)
                    throw new ArgumentException("Source entries must not contain null values.", nameof(sourceEntries));

                if (!seenVariantsByBonus.TryGetValue(source.BonusIdentity, out HashSet<ArmorSetVariant> seenVariants))
                {
                    seenVariants = new HashSet<ArmorSetVariant>();
                    seenVariantsByBonus.Add(source.BonusIdentity, seenVariants);
                }

                if (!seenVariants.Add(source.Variant))
                {
                    throw new ArgumentException(
                        $"Duplicate armor-set variant detected for bonus identity '{source.BonusIdentity}'.",
                        nameof(sourceEntries));
                }

                sources.Add(new SourceWithOrder(source, order++));
            }

            var groupsByBonus = new Dictionary<string, List<SourceWithOrder>>(StringComparer.Ordinal);
            var bonusOrder = new List<string>();

            foreach (SourceWithOrder source in sources)
            {
                if (!groupsByBonus.TryGetValue(source.Entry.BonusIdentity, out List<SourceWithOrder> group))
                {
                    group = new List<SourceWithOrder>();
                    groupsByBonus.Add(source.Entry.BonusIdentity, group);
                    bonusOrder.Add(source.Entry.BonusIdentity);
                }

                group.Add(source);
            }

            var components = new List<Component>();

            foreach (string bonusIdentity in bonusOrder)
            {
                List<SourceWithOrder> bonusEntries = groupsByBonus[bonusIdentity];
                ValidateBonusMetadata(bonusEntries);
                BuildComponents(bonusEntries, components);
            }

            components.Sort((left, right) => left.FirstOrder.CompareTo(right.FirstOrder));

            var catalogEntries = new List<ArmorSetCatalogEntry>(components.Count);

            for (var index = 0; index < components.Count; index++)
            {
                Component component = components[index];
                var variants = new List<ArmorSetVariant>(component.Entries.Count);

                component.Entries.Sort((left, right) => left.Order.CompareTo(right.Order));

                foreach (SourceWithOrder source in component.Entries)
                    variants.Add(source.Entry.Variant);

                ArmorSetCatalogSourceEntry first = component.Entries[0].Entry;
                int representativeItemId = SelectRepresentativeItem(first.PrimaryPart, variants);

                catalogEntries.Add(
                    new ArmorSetCatalogEntry(index + 1, first.BonusTextKey, representativeItemId, variants));
            }

            return ArmorSetCatalog.Create(catalogEntries);
        }

        private static void ValidateBonusMetadata(IReadOnlyList<SourceWithOrder> entries)
        {
            if (entries.Count == 0)
                return;

            ArmorSetCatalogSourceEntry first = entries[0].Entry;

            for (var index = 1; index < entries.Count; index++)
            {
                ArmorSetCatalogSourceEntry current = entries[index].Entry;

                if (!string.Equals(first.BonusTextKey, current.BonusTextKey, StringComparison.Ordinal) ||
                    first.PrimaryPart != current.PrimaryPart)
                {
                    throw new ArgumentException(
                        $"Bonus identity '{first.BonusIdentity}' contains inconsistent metadata.");
                }
            }
        }

        private static void BuildComponents(IReadOnlyList<SourceWithOrder> entries, List<Component> output)
        {
            var visited = new bool[entries.Count];

            for (var startIndex = 0; startIndex < entries.Count; startIndex++)
            {
                if (visited[startIndex])
                    continue;

                var queue = new Queue<int>();
                var componentEntries = new List<SourceWithOrder>();
                int firstOrder = int.MaxValue;
                visited[startIndex] = true;
                queue.Enqueue(startIndex);

                while (queue.Count > 0)
                {
                    int currentIndex = queue.Dequeue();
                    SourceWithOrder current = entries[currentIndex];
                    componentEntries.Add(current);
                    firstOrder = Math.Min(firstOrder, current.Order);

                    for (var candidateIndex = 0; candidateIndex < entries.Count; candidateIndex++)
                    {
                        if (visited[candidateIndex])
                            continue;

                        if (!AreStructurallyAdjacent(current.Entry.Variant, entries[candidateIndex].Entry.Variant))
                            continue;

                        visited[candidateIndex] = true;
                        queue.Enqueue(candidateIndex);
                    }
                }

                output.Add(new Component(firstOrder, componentEntries));
            }
        }

        private static bool AreStructurallyAdjacent(ArmorSetVariant left, ArmorSetVariant right)
        {
            var differences = 0;

            if (left.HeadItemId != right.HeadItemId)
                differences++;
            if (left.BodyItemId != right.BodyItemId)
                differences++;
            if (left.LegItemId != right.LegItemId)
                differences++;

            return differences == 1;
        }

        private static int SelectRepresentativeItem(
            ArmorSetPrimaryPart primaryPart,
            IReadOnlyList<ArmorSetVariant> variants)
        {
            ArmorSetVariant first = variants[0];
            int primaryItemId = GetPart(first, primaryPart);

            if (primaryItemId > 0)
                return primaryItemId;
            if (first.HeadItemId > 0)
                return first.HeadItemId;
            if (first.BodyItemId > 0)
                return first.BodyItemId;
            if (first.LegItemId > 0)
                return first.LegItemId;

            throw new InvalidOperationException("Armor-set variant does not contain a representative item.");
        }

        private static int GetPart(ArmorSetVariant variant, ArmorSetPrimaryPart part)
        {
            return part switch
            {
                ArmorSetPrimaryPart.Head => variant.HeadItemId,
                ArmorSetPrimaryPart.Body => variant.BodyItemId,
                ArmorSetPrimaryPart.Legs => variant.LegItemId,
                _ => 0
            };
        }

        private sealed class SourceWithOrder(ArmorSetCatalogSourceEntry entry, int order)
        {
            public ArmorSetCatalogSourceEntry Entry { get; } = entry;

            public int Order { get; } = order;
        }

        private sealed class Component(int firstOrder, List<SourceWithOrder> entries)
        {
            public int FirstOrder { get; } = firstOrder;

            public List<SourceWithOrder> Entries { get; } = entries;
        }
    }
}