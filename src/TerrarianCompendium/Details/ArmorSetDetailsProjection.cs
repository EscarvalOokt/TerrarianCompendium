using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Details
{
    internal sealed class ArmorSetDetailsItemReference(int itemId, string name, bool isFound)
    {
        public int ItemId { get; } = itemId;

        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

        public bool IsFound { get; } = isFound;
    }

    internal sealed class ArmorSetDetailsVariant
    {
        public ArmorSetDetailsVariant(
            ArmorSetDetailsItemReference head,
            ArmorSetDetailsItemReference body,
            ArmorSetDetailsItemReference legs)
        {
            if (head == null && body == null && legs == null)
                throw new ArgumentException("Armor-set details variant must contain at least one item.");

            Head = head;
            Body = body;
            Legs = legs;
        }

        public ArmorSetDetailsItemReference Head { get; }

        public ArmorSetDetailsItemReference Body { get; }

        public ArmorSetDetailsItemReference Legs { get; }
    }

    internal sealed class ArmorSetDetailsProjection
    {
        public ArmorSetDetailsProjection(
            int armorSetId,
            int representativeItemId,
            string bonusDescription,
            IEnumerable<ArmorSetDetailsVariant> variants)
        {
            if (armorSetId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(armorSetId),
                    armorSetId,
                    "Armor-set ID must be greater than zero.");

            if (representativeItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(representativeItemId),
                    representativeItemId,
                    "Representative item ID must be greater than zero.");
            }

            if (variants == null)
                throw new ArgumentNullException(nameof(variants));

            var copiedVariants = new List<ArmorSetDetailsVariant>();

            foreach (ArmorSetDetailsVariant variant in variants)
            {
                if (variant == null)
                    throw new ArgumentException(
                        "Armor-set detail variants must not contain null values.",
                        nameof(variants));

                copiedVariants.Add(variant);
            }

            if (copiedVariants.Count == 0)
                throw new ArgumentException("Armor-set details must contain at least one variant.", nameof(variants));

            ArmorSetId = armorSetId;
            RepresentativeItemId = representativeItemId;
            BonusDescription = bonusDescription ?? string.Empty;
            Variants = new ReadOnlyCollection<ArmorSetDetailsVariant>(copiedVariants);
            HeadVariants = BuildDistinctReferences(copiedVariants, variant => variant.Head);
            BodyVariants = BuildDistinctReferences(copiedVariants, variant => variant.Body);
            LegVariants = BuildDistinctReferences(copiedVariants, variant => variant.Legs);
            UsesRoleVariantPresentation = FormsCartesianProduct(copiedVariants);
        }

        public int ArmorSetId { get; }

        public int RepresentativeItemId { get; }

        public string BonusDescription { get; }

        public IReadOnlyList<ArmorSetDetailsVariant> Variants { get; }

        public IReadOnlyList<ArmorSetDetailsItemReference> HeadVariants { get; }

        public IReadOnlyList<ArmorSetDetailsItemReference> BodyVariants { get; }

        public IReadOnlyList<ArmorSetDetailsItemReference> LegVariants { get; }

        public bool UsesRoleVariantPresentation { get; }

        private static IReadOnlyList<ArmorSetDetailsItemReference> BuildDistinctReferences(
            IReadOnlyList<ArmorSetDetailsVariant> variants,
            Func<ArmorSetDetailsVariant, ArmorSetDetailsItemReference> selector)
        {
            var references = new List<ArmorSetDetailsItemReference>();
            var seenItemIds = new HashSet<int>();

            foreach (ArmorSetDetailsVariant variant in variants)
            {
                ArmorSetDetailsItemReference reference = selector(variant);

                if (reference != null && seenItemIds.Add(reference.ItemId))
                    references.Add(reference);
            }

            return new ReadOnlyCollection<ArmorSetDetailsItemReference>(references);
        }

        private static bool FormsCartesianProduct(IReadOnlyList<ArmorSetDetailsVariant> variants)
        {
            List<int> headIds = GetDistinctPartIds(variants, variant => variant.Head);
            List<int> bodyIds = GetDistinctPartIds(variants, variant => variant.Body);
            List<int> legIds = GetDistinctPartIds(variants, variant => variant.Legs);
            long expectedVariantCount = (long)headIds.Count * bodyIds.Count * legIds.Count;

            if (expectedVariantCount != variants.Count)
                return false;

            foreach (int headItemId in headIds)
            {
                foreach (int bodyItemId in bodyIds)
                {
                    foreach (int legItemId in legIds)
                    {
                        if (!ContainsVariant(variants, headItemId, bodyItemId, legItemId))
                            return false;
                    }
                }
            }

            return true;
        }

        private static List<int> GetDistinctPartIds(
            IReadOnlyList<ArmorSetDetailsVariant> variants,
            Func<ArmorSetDetailsVariant, ArmorSetDetailsItemReference> selector)
        {
            var itemIds = new List<int>();
            var seenItemIds = new HashSet<int>();

            foreach (ArmorSetDetailsVariant variant in variants)
            {
                ArmorSetDetailsItemReference reference = selector(variant);
                int itemId = reference?.ItemId ?? 0;

                if (seenItemIds.Add(itemId))
                    itemIds.Add(itemId);
            }

            return itemIds;
        }

        private static bool ContainsVariant(
            IReadOnlyList<ArmorSetDetailsVariant> variants,
            int headItemId,
            int bodyItemId,
            int legItemId)
        {
            foreach (ArmorSetDetailsVariant variant in variants)
            {
                if ((variant.Head?.ItemId ?? 0) == headItemId &&
                    (variant.Body?.ItemId ?? 0) == bodyItemId &&
                    (variant.Legs?.ItemId ?? 0) == legItemId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}