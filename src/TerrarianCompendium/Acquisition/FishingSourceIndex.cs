using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Acquisition
{
    internal enum FishingSourceConditionKind
    {
        HardMode,
        EarlyMode,
        InLava,
        InHoney,
        CanFishInLava,
        Dungeon,
        Beach,
        Hallow,
        GlowingMushrooms,
        TrueDesert,
        TrueSnow,
        Remix,
        Height1,
        Height1And2,
        HeightAbove1,
        HeightAboveAnd1,
        HeightUnder2,
        HeightAbove2,
        Height0,
        Height2,
        Height3,
        UnderRockLayer,
        Corruption,
        Crimson,
        Jungle,
        Snow,
        Desert,
        HallowDesert,
        OriginalOcean,
        RemixOcean,
        Ocean,
        BloodMoon,
        AnglerQuest,
        Junk,
        Crate,
        Water1000,
        DidNotUseCombatBook
    }

    internal sealed class FishingSourceVariant(
        IEnumerable<FishingSourceConditionKind> conditions,
        IEnumerable<FishingSourceConditionKind> excludedConditions) : IEquatable<FishingSourceVariant>
    {
        public FishingSourceVariant(IEnumerable<FishingSourceConditionKind> conditions) : this(
            conditions,
            Array.Empty<FishingSourceConditionKind>())
        {
        }

        public IReadOnlyList<FishingSourceConditionKind> Conditions { get; } =
            Normalize(conditions, nameof(conditions));

        public IReadOnlyList<FishingSourceConditionKind> ExcludedConditions { get; } =
            Normalize(excludedConditions, nameof(excludedConditions));

        public bool Equals(FishingSourceVariant other)
        {
            if (ReferenceEquals(this, other))
                return true;

            return other != null &&
                   AreEqual(Conditions, other.Conditions) &&
                   AreEqual(ExcludedConditions, other.ExcludedConditions);
        }

        public override bool Equals(object obj)
        {
            return obj is FishingSourceVariant other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;

                for (var index = 0; index < Conditions.Count; index++)
                    hash = hash * 31 + (int)Conditions[index];

                hash = hash * 31 + 397;

                for (var index = 0; index < ExcludedConditions.Count; index++)
                    hash = hash * 31 + (int)ExcludedConditions[index];

                return hash;
            }
        }

        private static IReadOnlyList<FishingSourceConditionKind> Normalize(
            IEnumerable<FishingSourceConditionKind> conditions,
            string parameterName)
        {
            if (conditions == null)
                throw new ArgumentNullException(parameterName);

            var unique = new HashSet<FishingSourceConditionKind>(conditions);
            var ordered = new List<FishingSourceConditionKind>(unique);
            ordered.Sort();
            return new ReadOnlyCollection<FishingSourceConditionKind>(ordered);
        }

        private static bool AreEqual(
            IReadOnlyList<FishingSourceConditionKind> left,
            IReadOnlyList<FishingSourceConditionKind> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var index = 0; index < left.Count; index++)
            {
                if (left[index] != right[index])
                    return false;
            }

            return true;
        }
    }

    internal static class FishingSourceVariantComposer
    {
        public static bool TryCompose(
            FishingSourceVariant baseVariant,
            IEnumerable<FishingSourceConditionKind> requiredConditions,
            out FishingSourceVariant result)
        {
            if (baseVariant == null)
                throw new ArgumentNullException(nameof(baseVariant));
            if (requiredConditions == null)
                throw new ArgumentNullException(nameof(requiredConditions));

            var conditions = new HashSet<FishingSourceConditionKind>(baseVariant.Conditions);
            conditions.UnionWith(requiredConditions);

            foreach (FishingSourceConditionKind excludedCondition in baseVariant.ExcludedConditions)
            {
                if (conditions.Contains(excludedCondition))
                {
                    result = null;
                    return false;
                }
            }

            result = new FishingSourceVariant(conditions, baseVariant.ExcludedConditions);
            return true;
        }

        public static IReadOnlyList<FishingSourceVariant> ApplyStopper(
            IReadOnlyList<FishingSourceVariant> reachabilityAlternatives,
            IReadOnlyList<FishingSourceConditionKind> stopperConditions)
        {
            if (reachabilityAlternatives == null)
                throw new ArgumentNullException(nameof(reachabilityAlternatives));
            if (stopperConditions == null)
                throw new ArgumentNullException(nameof(stopperConditions));

            if (reachabilityAlternatives.Count == 0 || stopperConditions.Count == 0)
                return Array.Empty<FishingSourceVariant>();

            var result = new List<FishingSourceVariant>();

            foreach (FishingSourceVariant alternative in reachabilityAlternatives)
            {
                if (alternative == null)
                    continue;

                if (ContainsAny(alternative.ExcludedConditions, stopperConditions))
                {
                    AddUnique(result, alternative);
                    continue;
                }

                foreach (FishingSourceConditionKind stopperCondition in stopperConditions)
                {
                    if (Contains(alternative.Conditions, stopperCondition))
                        continue;

                    var excludedConditions = new List<FishingSourceConditionKind>(alternative.ExcludedConditions)
                    {
                        stopperCondition
                    };

                    AddUnique(result, new FishingSourceVariant(alternative.Conditions, excludedConditions));
                }
            }

            return result.Count == 0
                ? Array.Empty<FishingSourceVariant>()
                : new ReadOnlyCollection<FishingSourceVariant>(result);
        }

        private static bool ContainsAny(
            IReadOnlyList<FishingSourceConditionKind> left,
            IReadOnlyList<FishingSourceConditionKind> right)
        {
            for (var leftIndex = 0; leftIndex < left.Count; leftIndex++)
            {
                if (Contains(right, left[leftIndex]))
                    return true;
            }

            return false;
        }

        private static bool Contains(
            IReadOnlyList<FishingSourceConditionKind> conditions,
            FishingSourceConditionKind condition)
        {
            for (var index = 0; index < conditions.Count; index++)
            {
                if (conditions[index] == condition)
                    return true;
            }

            return false;
        }

        private static void AddUnique(List<FishingSourceVariant> variants, FishingSourceVariant candidate)
        {
            for (var index = 0; index < variants.Count; index++)
            {
                if (variants[index].Equals(candidate))
                    return;
            }

            variants.Add(candidate);
        }
    }

    internal readonly struct FishingSourceRelation(int targetItemId, FishingSourceVariant variant)
    {
        public int TargetItemId { get; } = targetItemId;

        public FishingSourceVariant Variant { get; } = variant ?? throw new ArgumentNullException(nameof(variant));
    }

    internal sealed class FishingSourceIndex
    {
        private static readonly IReadOnlyList<FishingSourceVariant> _emptyVariants =
            Array.Empty<FishingSourceVariant>();

        private readonly Dictionary<int, IReadOnlyList<FishingSourceVariant>> _variantsByItemId;

        public FishingSourceIndex(IEnumerable<int> itemIds) : this(CreateUnqualifiedRelations(itemIds))
        {
        }

        public FishingSourceIndex(IEnumerable<FishingSourceRelation> relations)
        {
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            var mutable = new Dictionary<int, List<FishingSourceVariant>>();

            foreach (FishingSourceRelation relation in relations)
            {
                if (relation.TargetItemId <= 0)
                    continue;

                if (!mutable.TryGetValue(relation.TargetItemId, out List<FishingSourceVariant> variants))
                {
                    variants = new List<FishingSourceVariant>();
                    mutable.Add(relation.TargetItemId, variants);
                }

                variants.Add(relation.Variant);
            }

            _variantsByItemId = new Dictionary<int, IReadOnlyList<FishingSourceVariant>>(mutable.Count);
            var itemIds = new List<int>(mutable.Keys);
            itemIds.Sort();
            ItemIds = new ReadOnlyCollection<int>(itemIds);

            foreach (int itemId in itemIds)
            {
                _variantsByItemId.Add(
                    itemId,
                    new ReadOnlyCollection<FishingSourceVariant>(new List<FishingSourceVariant>(mutable[itemId])));
            }
        }

        public IReadOnlyList<int> ItemIds { get; }

        public bool ContainsItem(int itemId)
        {
            return itemId > 0 && _variantsByItemId.ContainsKey(itemId);
        }

        public IReadOnlyList<FishingSourceVariant> GetVariantsForItem(int itemId)
        {
            return itemId > 0 && _variantsByItemId.TryGetValue(itemId, out IReadOnlyList<FishingSourceVariant> variants)
                ? variants
                : _emptyVariants;
        }

        private static IEnumerable<FishingSourceRelation> CreateUnqualifiedRelations(IEnumerable<int> itemIds)
        {
            if (itemIds == null)
                throw new ArgumentNullException(nameof(itemIds));

            var variant = new FishingSourceVariant(Array.Empty<FishingSourceConditionKind>());
            var seenItemIds = new HashSet<int>();

            foreach (int itemId in itemIds)
            {
                if (itemId > 0 && seenItemIds.Add(itemId))
                    yield return new FishingSourceRelation(itemId, variant);
            }
        }
    }
}