using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TerrarianCompendium.Catalog;
using TerrarianCompendium.Checklist;
using TerrarianCompendium.Journey;
using TerrarianCompendium.Shimmer;

namespace TerrarianCompendium.Details
{
    internal sealed class ShimmerDetailsItemReference(int itemId, string name, bool isFound, int stack)
    {
        public int ItemId { get; } = itemId > 0
            ? itemId
            : throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Item ID must be positive.");

        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

        public bool IsFound { get; } = isFound;

        public int Stack { get; } = stack > 0
            ? stack
            : throw new ArgumentOutOfRangeException(nameof(stack), stack, "Item stack must be positive.");
    }

    internal sealed class ShimmerDetailsVariantProjection
    {
        public ShimmerDetailsVariantProjection(
            ShimmerTransformationKind kind,
            ShimmerDetailsItemReference input,
            IEnumerable<ShimmerDetailsItemReference> outputs,
            bool isProgressionLocked,
            ShimmerProgressionRequirement progressionRequirement,
            ShimmerWorldCondition worldCondition,
            int? moonPhase,
            int? decraftingRecipeRuntimeIndex,
            bool isAlchemy)
        {
            if (kind != ShimmerTransformationKind.Direct && kind != ShimmerTransformationKind.Decraft)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Shimmer transformation kind.");
            }

            if (outputs == null)
                throw new ArgumentNullException(nameof(outputs));

            var copiedOutputs = new List<ShimmerDetailsItemReference>();
            foreach (ShimmerDetailsItemReference output in outputs)
            {
                if (output == null)
                    throw new ArgumentException("Output references must not contain null values.", nameof(outputs));

                copiedOutputs.Add(output);
            }

            if (copiedOutputs.Count == 0)
                throw new ArgumentException("Shimmer detail variants require at least one output.", nameof(outputs));

            Kind = kind;
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Outputs = new ReadOnlyCollection<ShimmerDetailsItemReference>(copiedOutputs);
            IsProgressionLocked = isProgressionLocked;
            ProgressionRequirement = progressionRequirement;
            WorldCondition = worldCondition;
            MoonPhase = moonPhase;
            DecraftingRecipeRuntimeIndex = decraftingRecipeRuntimeIndex;
            IsAlchemy = isAlchemy;
        }

        public ShimmerTransformationKind Kind { get; }

        public ShimmerDetailsItemReference Input { get; }

        public IReadOnlyList<ShimmerDetailsItemReference> Outputs { get; }

        public bool IsProgressionLocked { get; }

        public ShimmerProgressionRequirement ProgressionRequirement { get; }

        public ShimmerWorldCondition WorldCondition { get; }

        public int? MoonPhase { get; }

        public int? DecraftingRecipeRuntimeIndex { get; }

        public bool IsAlchemy { get; }

        public bool IsDirect => Kind == ShimmerTransformationKind.Direct;

        public bool IsDecraft => Kind == ShimmerTransformationKind.Decraft;
    }

    internal sealed class ShimmerDetailsProjection
    {
        public ShimmerDetailsProjection(
            ShimmerDetailsItemReference result,
            int totalVariantCount,
            IEnumerable<ShimmerDetailsVariantProjection> matchingVariants)
        {
            if (totalVariantCount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(totalVariantCount),
                    totalVariantCount,
                    "Total Shimmer variant count must not be negative.");
            }

            if (matchingVariants == null)
                throw new ArgumentNullException(nameof(matchingVariants));

            var copiedVariants = new List<ShimmerDetailsVariantProjection>();
            foreach (ShimmerDetailsVariantProjection variant in matchingVariants)
            {
                if (variant == null)
                    throw new ArgumentException(
                        "Shimmer variants must not contain null values.",
                        nameof(matchingVariants));

                copiedVariants.Add(variant);
            }

            Result = result ?? throw new ArgumentNullException(nameof(result));
            TotalVariantCount = totalVariantCount;
            MatchingVariants = new ReadOnlyCollection<ShimmerDetailsVariantProjection>(copiedVariants);
        }

        public ShimmerDetailsItemReference Result { get; }

        public int TotalVariantCount { get; }

        public IReadOnlyList<ShimmerDetailsVariantProjection> MatchingVariants { get; }

        public int MatchingVariantCount => MatchingVariants.Count;

        public bool HasProducingVariants => TotalVariantCount > 0;
    }

    internal sealed class ShimmerDetailsModel(
        ShimmerTransformationIndex shimmerIndex,
        ItemCatalog itemCatalog,
        ItemTextIndex itemTextIndex,
        ChecklistState checklistState,
        JourneyResearchState journeyResearchState,
        ShimmerFilterState filterState,
        Func<ShimmerTransformationVariant, bool> isProgressionLocked,
        Func<ShimmerRuntimeContextKey> runtimeContextProvider)
    {
        private readonly ChecklistState _checklistState =
            checklistState ?? throw new ArgumentNullException(nameof(checklistState));

        private readonly ShimmerFilterState _filterState =
            filterState ?? throw new ArgumentNullException(nameof(filterState));

        private readonly Func<ShimmerTransformationVariant, bool> _isProgressionLocked =
            isProgressionLocked ?? throw new ArgumentNullException(nameof(isProgressionLocked));

        private readonly ItemCatalog _itemCatalog = itemCatalog ?? throw new ArgumentNullException(nameof(itemCatalog));

        private readonly ItemTextIndex _itemTextIndex =
            itemTextIndex ?? throw new ArgumentNullException(nameof(itemTextIndex));

        private readonly Func<ShimmerRuntimeContextKey> _runtimeContextProvider = runtimeContextProvider ??
            throw new ArgumentNullException(nameof(runtimeContextProvider));

        private readonly ShimmerTransformationIndex _shimmerIndex =
            shimmerIndex ?? throw new ArgumentNullException(nameof(shimmerIndex));

        private long _cachedChecklistRevision = -1;
        private long _cachedFilterRevision = -1;
        private int _cachedItemId;
        private long _cachedItemTextRevision = -1;
        private ShimmerDetailsProjection _cachedProjection;
        private long _cachedResearchRevision = -1;
        private ShimmerRuntimeContextKey _cachedRuntimeContext;
        private bool _hasCachedRuntimeContext;

        public bool TryGetProjection(int itemId, out ShimmerDetailsProjection projection)
        {
            if (!_itemCatalog.Contains(itemId) || !_shimmerIndex.HasRelation(itemId))
            {
                projection = null;
                return false;
            }

            long checklistRevision = _checklistState.Revision;
            long itemTextRevision = _itemTextIndex.Revision;
            long filterRevision = _filterState.Revision;
            long researchRevision = _filterState.UsesJourneyResearchState
                ? journeyResearchState?.Revision ?? -1
                : _cachedResearchRevision;
            ShimmerRuntimeContextKey runtimeContext = _runtimeContextProvider();

            if (_cachedProjection != null &&
                _cachedItemId == itemId &&
                _cachedChecklistRevision == checklistRevision &&
                _cachedItemTextRevision == itemTextRevision &&
                _cachedFilterRevision == filterRevision &&
                _cachedResearchRevision == researchRevision &&
                _hasCachedRuntimeContext &&
                _cachedRuntimeContext == runtimeContext)
            {
                projection = _cachedProjection;
                return true;
            }

            IReadOnlyList<ShimmerTransformationVariant> producingVariants = _shimmerIndex.GetProducing(itemId);
            var matchingVariants = new List<ShimmerDetailsVariantProjection>();
            bool resultMatches = ShimmerFilterMatcher.MatchesResult(
                itemId,
                _filterState,
                _checklistState,
                journeyResearchState);

            if (resultMatches)
            {
                foreach (ShimmerTransformationVariant variant in producingVariants)
                {
                    if (!ShimmerFilterMatcher.MatchesVariant(variant, _filterState, _isProgressionLocked))
                        continue;

                    matchingVariants.Add(CreateVariantProjection(variant));
                }
            }

            projection = new ShimmerDetailsProjection(
                CreateItemReference(itemId, 1),
                producingVariants.Count,
                matchingVariants);

            _cachedItemId = itemId;
            _cachedProjection = projection;
            _cachedChecklistRevision = checklistRevision;
            _cachedItemTextRevision = itemTextRevision;
            _cachedFilterRevision = filterRevision;
            _cachedResearchRevision = researchRevision;
            _cachedRuntimeContext = runtimeContext;
            _hasCachedRuntimeContext = true;
            return true;
        }

        private ShimmerDetailsVariantProjection CreateVariantProjection(ShimmerTransformationVariant variant)
        {
            var outputs = new List<ShimmerDetailsItemReference>(variant.Outputs.Count);

            foreach (ShimmerTransformationOutput output in variant.Outputs)
            {
                if (!_itemCatalog.Contains(output.ItemId))
                {
                    throw new InvalidOperationException(
                        $"Shimmer output references catalog-missing item ID {output.ItemId}.");
                }

                outputs.Add(CreateItemReference(output.ItemId, output.Stack));
            }

            return new ShimmerDetailsVariantProjection(
                variant.Kind,
                CreateItemReference(variant.InputItemId, variant.InputStack),
                outputs,
                _isProgressionLocked(variant),
                variant.ProgressionRequirement,
                variant.WorldCondition,
                variant.MoonPhase,
                variant.DecraftingRecipeRuntimeIndex,
                variant.IsAlchemy);
        }

        private ShimmerDetailsItemReference CreateItemReference(int itemId, int stack)
        {
            return new ShimmerDetailsItemReference(
                itemId,
                _itemTextIndex.GetName(itemId),
                _checklistState.IsFound(itemId),
                stack);
        }
    }
}