using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TerrarianCompendium.Shimmer
{
    internal enum ShimmerWorldCondition
    {
        Any,
        Crimson,
        Corruption
    }

    internal sealed class ShimmerTransformationOutput(int itemId, int stack)
    {
        public int ItemId { get; } = itemId > 0
            ? itemId
            : throw new ArgumentOutOfRangeException(nameof(itemId), itemId, "Output item ID must be positive.");

        public int Stack { get; } = stack > 0
            ? stack
            : throw new ArgumentOutOfRangeException(nameof(stack), stack, "Output stack must be positive.");
    }

    internal sealed class ShimmerTransformationVariant
    {
        public ShimmerTransformationVariant(
            int inputItemId,
            ShimmerTransformationKind kind,
            int inputStack,
            IEnumerable<ShimmerTransformationOutput> outputs,
            ShimmerProgressionRequirement progressionRequirement = ShimmerProgressionRequirement.None,
            ShimmerWorldCondition worldCondition = ShimmerWorldCondition.Any,
            int? moonPhase = null,
            int? decraftingRecipeRuntimeIndex = null,
            bool isAlchemy = false)
        {
            if (inputItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputItemId),
                    inputItemId,
                    "Input item ID must be positive.");
            }

            if (kind != ShimmerTransformationKind.Direct && kind != ShimmerTransformationKind.Decraft)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Shimmer transformation kind.");
            }

            if (inputStack <= 0)
                throw new ArgumentOutOfRangeException(nameof(inputStack), inputStack, "Input stack must be positive.");

            if (outputs == null)
                throw new ArgumentNullException(nameof(outputs));

            if (worldCondition != ShimmerWorldCondition.Any &&
                worldCondition != ShimmerWorldCondition.Crimson &&
                worldCondition != ShimmerWorldCondition.Corruption)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(worldCondition),
                    worldCondition,
                    "Unsupported Shimmer world condition.");
            }

            if (moonPhase.HasValue && (moonPhase.Value < 0 || moonPhase.Value > 7))
                throw new ArgumentOutOfRangeException(
                    nameof(moonPhase),
                    moonPhase,
                    "Moon phase must be between 0 and 7.");

            if (decraftingRecipeRuntimeIndex is < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(decraftingRecipeRuntimeIndex),
                    decraftingRecipeRuntimeIndex,
                    "Runtime recipe index must not be negative.");
            }

            if (kind == ShimmerTransformationKind.Direct && decraftingRecipeRuntimeIndex.HasValue)
            {
                throw new ArgumentException(
                    "Direct Shimmer transformations must not define a decrafting recipe index.",
                    nameof(decraftingRecipeRuntimeIndex));
            }

            if (kind == ShimmerTransformationKind.Decraft && !decraftingRecipeRuntimeIndex.HasValue)
            {
                throw new ArgumentException(
                    "Decrafting Shimmer transformations require a recipe runtime index.",
                    nameof(decraftingRecipeRuntimeIndex));
            }

            if (kind == ShimmerTransformationKind.Decraft && moonPhase.HasValue)
            {
                throw new ArgumentException(
                    "Decrafting Shimmer transformations do not use a moon-phase result condition.",
                    nameof(moonPhase));
            }

            var copiedOutputs = new List<ShimmerTransformationOutput>();
            foreach (ShimmerTransformationOutput output in outputs)
            {
                if (output == null)
                    throw new ArgumentException("Shimmer outputs must not contain null values.", nameof(outputs));

                copiedOutputs.Add(output);
            }

            if (copiedOutputs.Count == 0)
                throw new ArgumentException(
                    "Shimmer transformation variants require at least one output.",
                    nameof(outputs));

            InputItemId = inputItemId;
            Kind = kind;
            InputStack = inputStack;
            Outputs = new ReadOnlyCollection<ShimmerTransformationOutput>(copiedOutputs);
            ProgressionRequirement = progressionRequirement;
            WorldCondition = worldCondition;
            MoonPhase = moonPhase;
            DecraftingRecipeRuntimeIndex = decraftingRecipeRuntimeIndex;
            IsAlchemy = isAlchemy;
        }

        public int InputItemId { get; }

        public ShimmerTransformationKind Kind { get; }

        public int InputStack { get; }

        public IReadOnlyList<ShimmerTransformationOutput> Outputs { get; }

        public ShimmerProgressionRequirement ProgressionRequirement { get; }

        public ShimmerWorldCondition WorldCondition { get; }

        public int? MoonPhase { get; }

        public int? DecraftingRecipeRuntimeIndex { get; }

        public bool IsAlchemy { get; }

        public bool IsDirect => Kind == ShimmerTransformationKind.Direct;

        public bool IsDecraft => Kind == ShimmerTransformationKind.Decraft;

        public bool Produces(int itemId)
        {
            foreach (ShimmerTransformationOutput output in Outputs)
            {
                if (output.ItemId == itemId)
                    return true;
            }

            return false;
        }
    }
}