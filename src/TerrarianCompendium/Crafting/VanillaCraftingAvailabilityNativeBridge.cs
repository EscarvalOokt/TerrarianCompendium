using System;
using System.Reflection;
using Terraria;

namespace TerrarianCompendium.Crafting
{
    internal static class VanillaCraftingAvailabilityNativeBridge
    {
        private static MethodInfo _collectItemsToCraftWithFromMethod;
        private static bool _collectorResolutionAttempted;

        public static VanillaCraftingAvailabilityNativeExecutionResult Execute(Player player, Action calculation)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            MethodInfo collector = ResolveCollectItemsToCraftWithFrom();

            if (collector == null)
                return VanillaCraftingAvailabilityNativeExecutionResult.CollectorUnavailable();

            var adjacencySnapshot = new AdjacencySnapshot(player);
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

            if (failure != null)
                return VanillaCraftingAvailabilityNativeExecutionResult.Failed(failure);

            if (!adjacencyRestored)
                return VanillaCraftingAvailabilityNativeExecutionResult.AdjacencyRestoreFailed();

            return VanillaCraftingAvailabilityNativeExecutionResult.Ready();
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
            Exception exception)
        {
            Status = status;
            Exception = exception;
        }

        public VanillaCraftingAvailabilityNativeExecutionStatus Status { get; }

        public Exception Exception { get; }

        public bool CollectorAvailable =>
            Status != VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable;

        public bool Succeeded => Status == VanillaCraftingAvailabilityNativeExecutionStatus.Ready;

        public static VanillaCraftingAvailabilityNativeExecutionResult Ready()
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.Ready,
                null);
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult CollectorUnavailable()
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.CollectorUnavailable,
                null);
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult Failed(Exception exception)
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.Failed,
                exception ?? throw new ArgumentNullException(nameof(exception)));
        }

        public static VanillaCraftingAvailabilityNativeExecutionResult AdjacencyRestoreFailed()
        {
            return new VanillaCraftingAvailabilityNativeExecutionResult(
                VanillaCraftingAvailabilityNativeExecutionStatus.AdjacencyRestoreFailed,
                null);
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