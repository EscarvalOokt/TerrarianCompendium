using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Shimmer
{
    internal sealed class ShimmerTransformationIndex
    {
        private static readonly IReadOnlyList<ShimmerTransformationVariant> _emptyVariants =
            new ReadOnlyCollection<ShimmerTransformationVariant>(new List<ShimmerTransformationVariant>());

        private readonly Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>> _producingByItemId;
        private readonly Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>> _usingByItemId;

        private ShimmerTransformationIndex(
            IReadOnlyList<ShimmerTransformationVariant> variants,
            Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>> producingByItemId,
            Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>> usingByItemId)
        {
            Variants = variants;
            _producingByItemId = producingByItemId;
            _usingByItemId = usingByItemId;
        }

        public IReadOnlyList<ShimmerTransformationVariant> Variants { get; }

        public static ShimmerTransformationIndex Create(IEnumerable<ShimmerTransformationVariant> variants)
        {
            if (variants == null)
                throw new ArgumentNullException(nameof(variants));

            var copiedVariants = new List<ShimmerTransformationVariant>();
            var producing = new Dictionary<int, List<ShimmerTransformationVariant>>();
            var usingRelations = new Dictionary<int, List<ShimmerTransformationVariant>>();

            foreach (ShimmerTransformationVariant variant in variants)
            {
                if (variant == null)
                    throw new ArgumentException("Shimmer variants must not contain null values.", nameof(variants));

                copiedVariants.Add(variant);
                AddRelation(usingRelations, variant.InputItemId, variant);

                var indexedOutputIds = new HashSet<int>();
                foreach (ShimmerTransformationOutput output in variant.Outputs)
                {
                    if (indexedOutputIds.Add(output.ItemId))
                        AddRelation(producing, output.ItemId, variant);
                }
            }

            return new ShimmerTransformationIndex(
                new ReadOnlyCollection<ShimmerTransformationVariant>(copiedVariants),
                Freeze(producing),
                Freeze(usingRelations));
        }

        public IReadOnlyList<ShimmerTransformationVariant> GetProducing(int resultItemId)
        {
            return _producingByItemId.TryGetValue(
                resultItemId,
                out IReadOnlyList<ShimmerTransformationVariant> variants)
                ? variants
                : _emptyVariants;
        }

        public IReadOnlyList<ShimmerTransformationVariant> GetUsing(int inputItemId)
        {
            return _usingByItemId.TryGetValue(inputItemId, out IReadOnlyList<ShimmerTransformationVariant> variants)
                ? variants
                : _emptyVariants;
        }

        public bool HasProducing(int itemId)
        {
            return _producingByItemId.ContainsKey(itemId);
        }

        public bool HasUsing(int itemId)
        {
            return _usingByItemId.ContainsKey(itemId);
        }

        public bool HasRelation(int itemId)
        {
            return HasProducing(itemId) || HasUsing(itemId);
        }

        private static void AddRelation(
            Dictionary<int, List<ShimmerTransformationVariant>> relations,
            int itemId,
            ShimmerTransformationVariant variant)
        {
            if (!relations.TryGetValue(itemId, out List<ShimmerTransformationVariant> variants))
            {
                variants = new List<ShimmerTransformationVariant>();
                relations.Add(itemId, variants);
            }

            variants.Add(variant);
        }

        private static Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>> Freeze(
            Dictionary<int, List<ShimmerTransformationVariant>> relations)
        {
            var frozen = new Dictionary<int, IReadOnlyList<ShimmerTransformationVariant>>(relations.Count);

            foreach (KeyValuePair<int, List<ShimmerTransformationVariant>> relation in relations)
            {
                frozen.Add(
                    relation.Key,
                    new ReadOnlyCollection<ShimmerTransformationVariant>(
                        new List<ShimmerTransformationVariant>(relation.Value)));
            }

            return frozen;
        }
    }
}