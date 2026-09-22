using System;
using System.Collections.Generic;
using NUnit.Framework;
using TerrarianCompendium.UI;

namespace TerrarianCompendium.Tests.UI
{
    [TestFixture]
    public sealed class DeferredLoadQueueTests
    {
        [Test]
        public void Enqueue_DoesNotStartLoadingBeforeUpdate()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded);
            var resource = new Resource();

            queue.Enqueue(resource);

            Assert.That(loaded, Is.Empty);
            Assert.That(resource.IsLoaded, Is.False);

            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([resource]));
            Assert.That(resource.IsLoaded, Is.True);
        }

        [Test]
        public void Enqueue_SharedResourceRequestedBySeveralIcons_LoadsOnce()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded);
            var shared = new Resource();
            var other = new Resource();

            queue.Enqueue(shared);
            queue.Enqueue(other);
            queue.Enqueue(shared);
            queue.Enqueue(shared);
            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([shared, other]));
        }

        [Test]
        public void ProcessPending_LoadCountLimit_DefersRemainingResources()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded, maxLoadsPerUpdate: 2);
            var first = new Resource();
            var second = new Resource();
            var third = new Resource();
            queue.Enqueue(first);
            queue.Enqueue(second);
            queue.Enqueue(third);

            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([first, second]));
            Assert.That(third.IsLoaded, Is.False);

            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([first, second, third]));
        }

        [TestCase(4)]
        [TestCase(10)]
        public void ProcessPending_TimeBudgetReached_DoesNotStartAnotherLoad(long loadDuration)
        {
            var loaded = new List<Resource>();
            long now = 0;
            DeferredLoadQueue<Resource> queue = CreateQueue(
                loaded,
                getTimestamp: () => now,
                afterLoad: () => now += loadDuration);
            var first = new Resource();
            var second = new Resource();
            queue.Enqueue(first);
            queue.Enqueue(second);

            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([first]));
            Assert.That(second.IsLoaded, Is.False);

            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([first, second]));
        }

        [Test]
        public void ProcessPending_LoadedByAnotherConsumer_DoesNotReloadOrSpendLoadQuota()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded, maxLoadsPerUpdate: 1);
            var shared = new Resource();
            var pending = new Resource();
            queue.Enqueue(shared);
            queue.Enqueue(pending);

            shared.IsLoaded = true;
            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([pending]));
        }

        [Test]
        public void Enqueue_NullOrAlreadyLoadedResource_DoesNotInvokeLoader()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded);

            queue.Enqueue(null);
            queue.Enqueue(new Resource { IsLoaded = true });
            queue.ProcessPending();

            Assert.That(loaded, Is.Empty);
        }

        [Test]
        public void Clear_DiscardsPendingWorkAndAllowsCachedResourceToBeRequestedAgain()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded);
            var resource = new Resource();
            queue.Enqueue(resource);

            queue.Clear();
            queue.ProcessPending();

            Assert.That(loaded, Is.Empty);
            Assert.That(resource.IsLoaded, Is.False);

            queue.Enqueue(resource);
            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([resource]));
        }

        [Test]
        public void Enqueue_ResourceInvalidatedAfterLoading_CanLoadAgain()
        {
            var loaded = new List<Resource>();
            DeferredLoadQueue<Resource> queue = CreateQueue(loaded);
            var resource = new Resource();
            queue.Enqueue(resource);
            queue.ProcessPending();

            resource.IsLoaded = false;
            queue.Enqueue(resource);
            queue.ProcessPending();

            Assert.That(loaded, Is.EqualTo([resource, resource]));
        }

        private static DeferredLoadQueue<Resource> CreateQueue(
            List<Resource> loaded,
            int maxLoadsPerUpdate = 4,
            Func<long> getTimestamp = null,
            Action afterLoad = null)
        {
            return new DeferredLoadQueue<Resource>(
                resource => !resource.IsLoaded,
                resource =>
                {
                    resource.IsLoaded = true;
                    loaded.Add(resource);
                    afterLoad?.Invoke();
                },
                getTimestamp ?? (() => 0),
                maxLoadsPerUpdate,
                timeBudgetTicks: 4);
        }

        private sealed class Resource
        {
            public bool IsLoaded { get; set; }
        }
    }
}