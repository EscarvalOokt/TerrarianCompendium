using System;
using System.Reflection;
using Terraria;

namespace TerrarianCompendium.Crafting
{
    internal static class VanillaCraftingAvailabilityNativeBridge
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private static MethodInfo _collectItemsToCraftWithFromMethod;
        private static bool _collectorResolutionAttempted;

        public static VanillaCraftingAvailabilityNativeExecutionResult Execute(Player player, Action calculation)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            var adjacencySnapshot = new AdjacencySnapshot(player);
            MethodInfo collector = ResolveCollectItemsToCraftWithFrom();

            if (collector == null)
            {
                return VanillaCraftingAvailabilityNativeExecutionResult.CollectorUnavailable(
                    adjacencySnapshot.Fingerprint,
                    ComputeAdjacencyFingerprint(player));
            }

            Exception failure = null;
            bool adjacencyRestored;

            try
            {
                player.AdjTiles();
                collector.Invoke(null, [player]);
                calculation();
            }
            catch (Exception exception)
            {
                failure = UnwrapInvocationException(exception);
            }
            finally
            {
                adjacencyRestored = adjacencySnapshot.Restore(player);
            }

            ulong adjacencyAfterFingerprint = ComputeAdjacencyFingerprint(player);

            if (failure != null)
            {
                return VanillaCraftingAvailabilityNativeExecutionResult.Failed(
                    failure,
                    adjacencySnapshot.Fingerprint,
                    adjacencyAfterFingerprint,
                    adjacencyRestored);
            }

            if (!adjacencyRestored)
            {
                return VanillaCraftingAvailabilityNativeExecutionResult.AdjacencyRestoreFailed(
                    adjacencySnapshot.Fingerprint,
                    adjacencyAfterFingerprint);
            }

            return VanillaCraftingAvailabilityNativeExecutionResult.Ready(
                adjacencySnapshot.Fingerprint,
                adjacencyAfterFingerprint);
        }

        private static MethodInfo ResolveCollectItemsToCraftWithFrom()
        {
            if (_collectorResolutionAttempted)
                return _collectItemsToCraftWithFromMethod;

            _collectorResolutionAttempted = true;

            MethodInfo method = typeof(Recipe).GetMethod(
                "CollectItemsToCraftWithFrom",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                binder: null,
                types: [typeof(Player)],
                modifiers: null);

            if (method == null || method.ReturnType != typeof(void))
                return null;

            _collectItemsToCraftWithFromMethod = method;

            return _collectItemsToCraftWithFromMethod;
        }

        private static Exception UnwrapInvocationException(Exception exception)
        {
            if (exception is TargetInvocationException { InnerException: not null } targetInvocationException)
                return targetInvocationException.InnerException;

            return exception;
        }

        private static ulong ComputeAdjacencyFingerprint(Player player)
        {
            if (player == null)
                return AddInt(FnvOffsetBasis, -1);

            ulong hash = FnvOffsetBasis;
            bool[] adjTile = player.adjTile;
            int tileCount = adjTile?.Length ?? 0;

            hash = AddInt(hash, tileCount);

            if (adjTile != null)
            {
                for (var i = 0; i < adjTile.Length; i++)
                    hash = AddBool(hash, adjTile[i]);
            }

            hash = AddBool(hash, player.adjWaterSource);
            hash = AddBool(hash, player.adjHoney);
            hash = AddBool(hash, player.adjLava);
            hash = AddBool(hash, player.oldAdjWaterSource);
            hash = AddBool(hash, player.oldAdjHoney);
            hash = AddBool(hash, player.oldAdjLava);
            hash = AddBool(hash, player.alchemyTable);

            return hash;
        }

        private static ulong AddBool(ulong hash, bool value)
        {
            return AddInt(hash, value ? 1 : 0);
        }

        private static ulong AddInt(ulong hash, int value)
        {
            unchecked
            {
                var data = (uint)value;

                hash ^= (byte)data;
                hash *= FnvPrime;
                hash ^= (byte)(data >> 8);
                hash *= FnvPrime;
                hash ^= (byte)(data >> 16);
                hash *= FnvPrime;
                hash ^= (byte)(data >> 24);
                hash *= FnvPrime;
            }

            return hash;
        }

        private readonly struct AdjacencySnapshot(Player player)
        {
            private readonly bool[] _adjTile = (bool[])player?.adjTile?.Clone();
            private readonly bool _adjWaterSource = player?.adjWaterSource ?? false;
            private readonly bool _adjHoney = player?.adjHoney ?? false;
            private readonly bool _adjLava = player?.adjLava ?? false;
            private readonly bool _oldAdjWaterSource = player?.oldAdjWaterSource ?? false;
            private readonly bool _oldAdjHoney = player?.oldAdjHoney ?? false;
            private readonly bool _oldAdjLava = player?.oldAdjLava ?? false;
            private readonly bool _alchemyTable = player?.alchemyTable ?? false;

            public ulong Fingerprint { get; } = ComputeAdjacencyFingerprint(player);

            public bool Restore(Player player)
            {
                if (player == null)
                    return false;

                bool tileStateRestored;

                if (_adjTile == null)
                {
                    tileStateRestored = player.adjTile == null;
                }
                else
                {
                    tileStateRestored = player.adjTile != null && player.adjTile.Length == _adjTile.Length;

                    if (tileStateRestored)
                        Array.Copy(_adjTile, player.adjTile, _adjTile.Length);
                }

                player.adjWaterSource = _adjWaterSource;
                player.adjHoney = _adjHoney;
                player.adjLava = _adjLava;
                player.oldAdjWaterSource = _oldAdjWaterSource;
                player.oldAdjHoney = _oldAdjHoney;
                player.oldAdjLava = _oldAdjLava;
                player.alchemyTable = _alchemyTable;

                return tileStateRestored && Matches(player);
            }

            private bool Matches(Player player)
            {
                if (player == null)
                    return false;

                if (player.adjWaterSource != _adjWaterSource ||
                    player.adjHoney != _adjHoney ||
                    player.adjLava != _adjLava ||
                    player.oldAdjWaterSource != _oldAdjWaterSource ||
                    player.oldAdjHoney != _oldAdjHoney ||
                    player.oldAdjLava != _oldAdjLava ||
                    player.alchemyTable != _alchemyTable)
                {
                    return false;
                }

                if (_adjTile == null)
                    return player.adjTile == null;

                if (player.adjTile == null || player.adjTile.Length != _adjTile.Length)
                    return false;

                for (var i = 0; i < _adjTile.Length; i++)
                {
                    if (player.adjTile[i] != _adjTile[i])
                        return false;
                }

                return true;
            }
        }
    }

    internal sealed class VanillaCraftingAvailabilityNativeExecutionResult
    {
        private VanillaCraftingAvailabilityNativeExecutionResult(
            VanillaCraftingAvailabilityNativeExecutionStatus status,
            Exception exception,
            ulong adjacencyBeforeFingerprint,
            ulong adjacencyAfterFingerprint,
            bool adjacencyRestored)
        {
            Status = status;
            Exception = exception;
            AdjacencyBeforeFingerprint = adjacencyBeforeFingerprint;
            AdjacencyAfterFingerprint = adjacencyAfterFingerprint;
            AdjacencyRestored = adjacencyRestored;
        }

        public VanillaCraftingAvailabilityNativeExecutionStatus Status { get; }

        public Exception Exception { get; }

        public ulong AdjacencyBeforeFingerprint { get; }

        public ulong AdjacencyAfterFingerprint { get; }

        public bool AdjacencyRestored { get; }

        public bool CollectorAvailable =>
            Status != VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable;

        public bool Succeeded => Status == VanillaCraftingAvailabilityNativeExecutionStatus.Ready;

        public static VanillaCraftingAvailabilityNativeExecutionResult Ready(
            ulong adjacencyBeforeFingerprint,
            ulong adjacencyAfterFingerprint)
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.Ready,
                null,
                adjacencyBeforeFingerprint,
                adjacencyAfterFingerprint,
                adjacencyRestored: true);
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult CollectorUnavailable(
            ulong adjacencyBeforeFingerprint,
            ulong adjacencyAfterFingerprint)
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable,
                null,
                adjacencyBeforeFingerprint,
                adjacencyAfterFingerprint,
                adjacencyRestored: true);
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult Failed(
            Exception exception,
            ulong adjacencyBeforeFingerprint,
            ulong adjacencyAfterFingerprint,
            bool adjacencyRestored)
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.Failed,
                exception ?? throw new ArgumentNullException(nameof(exception)),
                adjacencyBeforeFingerprint,
                adjacencyAfterFingerprint,
                adjacencyRestored);
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult AdjacencyRestoreFailed(
            ulong adjacencyBeforeFingerprint,
            ulong adjacencyAfterFingerprint)
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.AdjacencyRestoreFailed,
                null,
                adjacencyBeforeFingerprint,
                adjacencyAfterFingerprint,
                adjacencyRestored: false);
        }
    }

    internal enum VanillaCraftingAvailabilityNativeExecutionStatus
    {
        Ready,
        CollectorUnavailable,
        Failed,
        AdjacencyRestoreFailed
    }
}