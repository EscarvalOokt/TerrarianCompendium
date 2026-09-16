using System;
using TerrarianCompendium.Catalog;

namespace TerrarianCompendium.Filtering
{
    internal enum ChecklistTaxonomyFacetState
    {
        Off,
        Include,
        Exclude
    }

    internal readonly struct ChecklistTaxonomyFacetSelection : IEquatable<ChecklistTaxonomyFacetSelection>
    {
        private ChecklistTaxonomyFacetSelection(ItemTaxonomyFacetId includedFacets, ItemTaxonomyFacetId excludedFacets)
        {
            if (!ItemTaxonomyDefinitions.IsKnownFacetMask(includedFacets))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(includedFacets),
                    includedFacets,
                    "Unsupported included taxonomy facet mask.");
            }

            if (!ItemTaxonomyDefinitions.IsKnownFacetMask(excludedFacets))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(excludedFacets),
                    excludedFacets,
                    "Unsupported excluded taxonomy facet mask.");
            }

            if ((includedFacets & excludedFacets) != ItemTaxonomyFacetId.None)
            {
                throw new ArgumentException(
                    "Included and excluded taxonomy facet masks must not overlap.",
                    nameof(excludedFacets));
            }

            IncludedFacets = includedFacets;
            ExcludedFacets = excludedFacets;
        }

        public static ChecklistTaxonomyFacetSelection None => default;

        public ItemTaxonomyFacetId IncludedFacets { get; }

        public ItemTaxonomyFacetId ExcludedFacets { get; }

        public ChecklistTaxonomyFacetState GetState(ItemTaxonomyFacetId facetId)
        {
            ValidateFacetId(facetId);

            if ((IncludedFacets & facetId) == facetId)
                return ChecklistTaxonomyFacetState.Include;

            if ((ExcludedFacets & facetId) == facetId)
                return ChecklistTaxonomyFacetState.Exclude;

            return ChecklistTaxonomyFacetState.Off;
        }

        public ChecklistTaxonomyFacetSelection SetState(ItemTaxonomyFacetId facetId, ChecklistTaxonomyFacetState state)
        {
            ValidateFacetId(facetId);
            ValidateState(state);

            ItemTaxonomyFacetId includedFacets = IncludedFacets & ~facetId;
            ItemTaxonomyFacetId excludedFacets = ExcludedFacets & ~facetId;

            switch (state)
            {
                case ChecklistTaxonomyFacetState.Off:
                    break;

                case ChecklistTaxonomyFacetState.Include:
                    includedFacets |= facetId;
                    break;

                case ChecklistTaxonomyFacetState.Exclude:
                    excludedFacets |= facetId;
                    break;

                default:
                    throw new InvalidOperationException("Unsupported taxonomy facet state.");
            }

            return new ChecklistTaxonomyFacetSelection(includedFacets, excludedFacets);
        }

        public ChecklistTaxonomyFacetSelection Cycle(ItemTaxonomyFacetId facetId)
        {
            switch (GetState(facetId))
            {
                case ChecklistTaxonomyFacetState.Off:
                    return SetState(facetId, ChecklistTaxonomyFacetState.Include);

                case ChecklistTaxonomyFacetState.Include:
                    return SetState(facetId, ChecklistTaxonomyFacetState.Exclude);

                case ChecklistTaxonomyFacetState.Exclude:
                    return SetState(facetId, ChecklistTaxonomyFacetState.Off);

                default:
                    throw new InvalidOperationException("Unsupported taxonomy facet state.");
            }
        }

        public bool Equals(ChecklistTaxonomyFacetSelection other)
        {
            return IncludedFacets == other.IncludedFacets && ExcludedFacets == other.ExcludedFacets;
        }

        public override bool Equals(object obj)
        {
            return obj is ChecklistTaxonomyFacetSelection other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)IncludedFacets * 397) ^ (int)ExcludedFacets;
            }
        }

        public static bool operator ==(ChecklistTaxonomyFacetSelection left, ChecklistTaxonomyFacetSelection right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ChecklistTaxonomyFacetSelection left, ChecklistTaxonomyFacetSelection right)
        {
            return !left.Equals(right);
        }

        private static void ValidateFacetId(ItemTaxonomyFacetId facetId)
        {
            ItemTaxonomyDefinitions.GetFacet(facetId);
        }

        private static void ValidateState(ChecklistTaxonomyFacetState state)
        {
            if (state != ChecklistTaxonomyFacetState.Off &&
                state != ChecklistTaxonomyFacetState.Include &&
                state != ChecklistTaxonomyFacetState.Exclude)
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported taxonomy facet state.");
            }
        }
    }
}