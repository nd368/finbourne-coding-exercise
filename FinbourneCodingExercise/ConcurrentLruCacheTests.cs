using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FinbourneCodingExercise
{
    [TestFixture]
    public class ConcurrentLruCacheTests
    {
        [Test]
        public void CachePut()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            lruCache.Put(1, 2);
            Assert.AreEqual(1, lruCache.Count);
            Assert.AreEqual(1, lruCache.ToArray()[0].Key);
            Assert.AreEqual(2, lruCache.ToArray()[0].Value);
        }

        [Test]
        public void CacheGet()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            Assert.False(lruCache.TryGetValue(2, out var value));
            Assert.True(lruCache.TryGetValue(1, out value));
            Assert.AreEqual(1, value);
        }

        [Test]
        public void CacheRemove()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            Assert.False(lruCache.TryRemove(2, out var value));
            Assert.True(lruCache.TryRemove(1, out value));
            Assert.AreEqual(1, value);
            Assert.AreEqual(0, lruCache.Count);
        }

        [Test]
        public void CacheEviction()
        {
            var evictedEntries = new List<KeyValuePair<int, int>>();
            var lruCache = new ConcurrentLruCache<int, int>(10, entry =>
            {
                evictedEntries.Add(entry);
            });

            // perform deterministic put/get/remove operations, then check items evicted in expected order
            for (var i = 1; i <= 10; i++) lruCache.Put(i, i);
            lruCache.TryGetValue(1, out _);
            lruCache.TryGetValue(2, out _);
            lruCache.TryGetValue(3, out _);
            lruCache.TryRemove(4, out _);
            lruCache.TryRemove(5, out _);
            for (var i = 11; i <= 15; i++) lruCache.Put(i, i);

            Assert.AreEqual(3, evictedEntries.Count);
            Assert.AreEqual(6, evictedEntries[0].Key);
            Assert.AreEqual(7, evictedEntries[1].Key);
            Assert.AreEqual(8, evictedEntries[2].Key);
        }

        [Test]
        public void CacheClear()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            lruCache.Put(2, 2);
            lruCache.Put(3, 3);
            lruCache.Clear();
            Assert.AreEqual(0, lruCache.Count);
        }

        [Test]
        public void CacheCount()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            lruCache.Put(2, 2);
            lruCache.Put(3, 3);
            Assert.AreEqual(3, lruCache.Count);
        }

        [Test]
        public void CacheToArray()
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            lruCache.Put(1, 1);
            lruCache.Put(2, 2);
            var array = lruCache.ToArray();
            Assert.AreEqual(2, array.Length);
            Assert.AreEqual(1, array[0].Key);
            Assert.AreEqual(1, array[0].Value);
            Assert.AreEqual(2, array[1].Key);
            Assert.AreEqual(2, array[1].Value);
        }

        [Explicit, TestCase(100)]
        public void BenchmarkPerformance(int numberOfThreads)
        {
            var lruCache = new ConcurrentLruCache<int, int>(10);
            var stopwatch = Stopwatch.StartNew();
            Parallel.For(0, numberOfThreads, new ParallelOptions { MaxDegreeOfParallelism = numberOfThreads }, x =>
            {
                for (var i = 0; i < 100; i++)
                {
                    lruCache.Put(i, i);
                }
            });
            Console.WriteLine($"{stopwatch.ElapsedMilliseconds} ms");
        }
    }
}
