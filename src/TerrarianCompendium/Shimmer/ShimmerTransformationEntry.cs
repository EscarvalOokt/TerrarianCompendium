using System;

namespace TerrarianCompendium.Shimmer
{
    internal sealed class ShimmerTransformationEntry
    {
        private ShimmerTransformationEntry(
            int inputItemId,
            ShimmerTransformationKind kind,
            int? directResultItemId,
            bool hasDynamicDirectResult,
            int? baseRecipeRuntimeIndex,
            int? crimsonRecipeRuntimeIndex,
            int? corruptionRecipeRuntimeIndex)
        {
            if (inputItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inputItemId),
                    inputItemId,
                    "Input item ID must be greater than zero.");
            }

            InputItemId = inputItemId;
            Kind = kind;
            DirectResultItemId = directResultItemId;
            HasDynamicDirectResult = hasDynamicDirectResult;
            BaseRecipeRuntimeIndex = baseRecipeRuntimeIndex;
            CrimsonRecipeRuntimeIndex = crimsonRecipeRuntimeIndex;
            CorruptionRecipeRuntimeIndex = corruptionRecipeRuntimeIndex;
        }

        public int InputItemId { get; }

        public ShimmerTransformationKind Kind { get; }

        public int? DirectResultItemId { get; }

        public bool HasDynamicDirectResult { get; }

        public int? BaseRecipeRuntimeIndex { get; }

        public int? CrimsonRecipeRuntimeIndex { get; }

        public int? CorruptionRecipeRuntimeIndex { get; }

        public bool IsDirect => Kind == ShimmerTransformationKind.Direct;

        public bool IsDecraft => Kind == ShimmerTransformationKind.Decraft;

        public static ShimmerTransformationEntry ForDirect(int inputItemId, int resultItemId)
        {
            if (resultItemId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resultItemId),
                    resultItemId,
                    "Direct Shimmer result item ID must be greater than zero.");
            }

            return new ShimmerTransformationEntry(
                inputItemId,
                ShimmerTransformationKind.Direct,
                resultItemId,
                hasDynamicDirectResult: false,
                baseRecipeRuntimeIndex: null,
                crimsonRecipeRuntimeIndex: null,
                corruptionRecipeRuntimeIndex: null);
        }

        public static ShimmerTransformationEntry ForDynamicDirect(int inputItemId)
        {
            return new ShimmerTransformationEntry(
                inputItemId,
                ShimmerTransformationKind.Direct,
                directResultItemId: null,
                hasDynamicDirectResult: true,
                baseRecipeRuntimeIndex: null,
                crimsonRecipeRuntimeIndex: null,
                corruptionRecipeRuntimeIndex: null);
        }

        public static ShimmerTransformationEntry ForDecraft(
            int inputItemId,
            int baseRecipeRuntimeIndex,
            int? crimsonRecipeRuntimeIndex = null,
            int? corruptionRecipeRuntimeIndex = null)
        {
            ValidateRecipeRuntimeIndex(baseRecipeRuntimeIndex, nameof(baseRecipeRuntimeIndex));
            ValidateOptionalRecipeRuntimeIndex(crimsonRecipeRuntimeIndex, nameof(crimsonRecipeRuntimeIndex));
            ValidateOptionalRecipeRuntimeIndex(corruptionRecipeRuntimeIndex, nameof(corruptionRecipeRuntimeIndex));

            return new ShimmerTransformationEntry(
                inputItemId,
                ShimmerTransformationKind.Decraft,
                directResultItemId: null,
                hasDynamicDirectResult: false,
                baseRecipeRuntimeIndex,
                crimsonRecipeRuntimeIndex,
                corruptionRecipeRuntimeIndex);
        }

        public int SelectDecraftingRecipeRuntimeIndex(bool worldIsCrimson)
        {
            if (!IsDecraft || !BaseRecipeRuntimeIndex.HasValue)
                throw new InvalidOperationException("Only decrafting entries define a recipe runtime index.");

            if (worldIsCrimson && CrimsonRecipeRuntimeIndex.HasValue)
                return CrimsonRecipeRuntimeIndex.Value;

            if (!worldIsCrimson && CorruptionRecipeRuntimeIndex.HasValue)
                return CorruptionRecipeRuntimeIndex.Value;

            return BaseRecipeRuntimeIndex.Value;
        }

        private static void ValidateRecipeRuntimeIndex(int runtimeIndex, string parameterName)
        {
            if (runtimeIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    runtimeIndex,
                    "Runtime recipe index must not be negative.");
            }
        }

        private static void ValidateOptionalRecipeRuntimeIndex(int? runtimeIndex, string parameterName)
        {
            if (runtimeIndex.HasValue)
                ValidateRecipeRuntimeIndex(runtimeIndex.Value, parameterName);
        }
    }
}