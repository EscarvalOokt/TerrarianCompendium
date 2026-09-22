using System;

namespace TerrarianCompendium.Bestiary
{
    internal enum BestiaryFilterCriterionKind
    {
        All,
        Native,
        HasDrops,
        HasMissingDrops,
        HasUnresearchedDrops
    }

    internal readonly struct BestiaryFilterCriterion : IEquatable<BestiaryFilterCriterion>
    {
        private BestiaryFilterCriterion(BestiaryFilterCriterionKind kind, int nativeFilterId)
        {
            Kind = kind;
            NativeFilterId = nativeFilterId;
        }

        public BestiaryFilterCriterionKind Kind { get; }

        public int NativeFilterId { get; }

        public bool IsAll => Kind == BestiaryFilterCriterionKind.All;

        public static BestiaryFilterCriterion All => default;

        public static BestiaryFilterCriterion HasDrops => new(BestiaryFilterCriterionKind.HasDrops, 0);

        public static BestiaryFilterCriterion HasMissingDrops => new(BestiaryFilterCriterionKind.HasMissingDrops, 0);

        public static BestiaryFilterCriterion HasUnresearchedDrops =>
            new(BestiaryFilterCriterionKind.HasUnresearchedDrops, 0);

        public static BestiaryFilterCriterion ForNative(int filterId)
        {
            if (filterId < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(filterId),
                    filterId,
                    "Bestiary filter ID must not be negative.");

            return new BestiaryFilterCriterion(BestiaryFilterCriterionKind.Native, filterId);
        }

        public bool Equals(BestiaryFilterCriterion other)
        {
            return Kind == other.Kind && NativeFilterId == other.NativeFilterId;
        }

        public override bool Equals(object obj)
        {
            return obj is BestiaryFilterCriterion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Kind * 397) ^ NativeFilterId;
            }
        }

        public static bool operator ==(BestiaryFilterCriterion left, BestiaryFilterCriterion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BestiaryFilterCriterion left, BestiaryFilterCriterion right)
        {
            return !left.Equals(right);
        }
    }
}