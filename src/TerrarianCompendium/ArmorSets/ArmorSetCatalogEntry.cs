using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.ArmorSets
{
    internal enum ArmorSetPrimaryPart
    {
        None,
        Head,
        Body,
        Legs
    }

    internal sealed class ArmorSetCatalogEntry
    {
        public ArmorSetCatalogEntry(
            int id,
            string bonusTextKey,
            int representativeItemId,
            IEnumerable<ArmorSetVariant> variants)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Armor-set ID must be greater than zero.");

            if (string.IsNullOrWhiteSpace(bonusTextKey))
                throw new ArgumentException("Bonus text key must not be empty or whitespace.", nameof(bonusTextKey));

            if (representativeItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(representativeItemId),
                    representativeItemId,
                    "Representative item ID must be greater than zero.");
            }

            if (variants == null)
                throw new ArgumentNullException(nameof(variants));

            var copiedVariants = new List<ArmorSetVariant>();
            var seenVariants = new HashSet<ArmorSetVariant>();
            var representativeFound = false;

            foreach (ArmorSetVariant variant in variants)
            {
                if (!seenVariants.Add(variant))
                    throw new ArgumentException("Armor-set variants must not contain duplicates.", nameof(variants));

                copiedVariants.Add(variant);

                if (variant.ContainsItem(representativeItemId))
                    representativeFound = true;
            }

            if (copiedVariants.Count == 0)
                throw new ArgumentException("Armor-set entry must contain at least one variant.", nameof(variants));

            if (!representativeFound)
            {
                throw new ArgumentException(
                    "Representative item must be present in at least one armor-set variant.",
                    nameof(representativeItemId));
            }

            Id = id;
            BonusTextKey = bonusTextKey;
            RepresentativeItemId = representativeItemId;
            Variants = new ReadOnlyCollection<ArmorSetVariant>(copiedVariants);
        }

        public int Id { get; }

        public string BonusTextKey { get; }

        public int RepresentativeItemId { get; }

        public IReadOnlyList<ArmorSetVariant> Variants { get; }
    }
}