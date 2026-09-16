using System;

namespace TerrarianCompendium.ArmorSets
{
    internal readonly struct ArmorSetVariant : IEquatable<ArmorSetVariant>
    {
        public ArmorSetVariant(int headItemId, int bodyItemId, int legItemId)
        {
            if (headItemId < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(headItemId),
                    headItemId,
                    "Head item ID must not be negative.");

            if (bodyItemId < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(bodyItemId),
                    bodyItemId,
                    "Body item ID must not be negative.");

            if (legItemId < 0)
                throw new ArgumentOutOfRangeException(
                    nameof(legItemId),
                    legItemId,
                    "Leg item ID must not be negative.");

            if (headItemId == 0 && bodyItemId == 0 && legItemId == 0)
                throw new ArgumentException("Armor-set variant must contain at least one item.");

            HeadItemId = headItemId;
            BodyItemId = bodyItemId;
            LegItemId = legItemId;
        }

        public int HeadItemId { get; }

        public int BodyItemId { get; }

        public int LegItemId { get; }

        public bool ContainsItem(int itemId)
        {
            return itemId > 0 && (HeadItemId == itemId || BodyItemId == itemId || LegItemId == itemId);
        }

        public bool Equals(ArmorSetVariant other)
        {
            return HeadItemId == other.HeadItemId && BodyItemId == other.BodyItemId && LegItemId == other.LegItemId;
        }

        public override bool Equals(object obj)
        {
            return obj is ArmorSetVariant other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = HeadItemId;
                hashCode = (hashCode * 397) ^ BodyItemId;
                hashCode = (hashCode * 397) ^ LegItemId;
                return hashCode;
            }
        }

        public static bool operator ==(ArmorSetVariant left, ArmorSetVariant right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ArmorSetVariant left, ArmorSetVariant right)
        {
            return !left.Equals(right);
        }
    }
}