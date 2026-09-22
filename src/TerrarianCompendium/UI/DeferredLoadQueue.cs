using System;
using System.Collections.Generic;

namespace TerrarianCompendium.UI
{
    internal sealed class DeferredLoadQueue<T> where T : class
    {
        private readonly Func<long> _getTimestamp;
        private readonly Action<T> _load;
        private readonly int _maxLoadsPerUpdate;
        private readonly Func<T, bool> _needsLoad;
        private readonly Queue<T> _pending = new();
        private readonly HashSet<T> _queued = new();
        private readonly long _timeBudgetTicks;

        public DeferredLoadQueue(
            Func<T, bool> needsLoad,
            Action<T> load,
            Func<long> getTimestamp,
            int maxLoadsPerUpdate,
            long timeBudgetTicks)
        {
            _needsLoad = needsLoad ?? throw new ArgumentNullException(nameof(needsLoad));
            _load = load ?? throw new ArgumentNullException(nameof(load));
            _getTimestamp = getTimestamp ?? throw new ArgumentNullException(nameof(getTimestamp));

            if (maxLoadsPerUpdate <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxLoadsPerUpdate));

            if (timeBudgetTicks <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeBudgetTicks));

            _maxLoadsPerUpdate = maxLoadsPerUpdate;
            _timeBudgetTicks = timeBudgetTicks;
        }

        public void Enqueue(T resource)
        {
            if (resource != null && _needsLoad(resource) && _queued.Add(resource))
                _pending.Enqueue(resource);
        }

        public void ProcessPending()
        {
            long startedAt = _getTimestamp();
            var loadedCount = 0;

            while (_pending.Count > 0 && loadedCount < _maxLoadsPerUpdate)
            {
                if (loadedCount > 0 && _getTimestamp() - startedAt >= _timeBudgetTicks)
                    break;

                T resource = _pending.Dequeue();
                _queued.Remove(resource);

                if (!_needsLoad(resource))
                    continue;

                _load(resource);
                loadedCount++;
            }
        }

        public void Clear()
        {
            _pending.Clear();
            _queued.Clear();
        }
    }
}