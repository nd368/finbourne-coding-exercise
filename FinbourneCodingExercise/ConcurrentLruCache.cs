using System;
using System.Collections.Generic;

namespace FinbourneCodingExercise
{
    public class ConcurrentLruCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Action<KeyValuePair<TKey, TValue>> _evictionCallback;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _lruLinkedList;
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _keyToLinkedListNodeMap;
        private readonly object _cacheLock = new object();

        public ConcurrentLruCache(int capacity, Action<KeyValuePair<TKey, TValue>> evictionCallback = null)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "capacity is less than 0");

            _capacity = capacity;
            _evictionCallback = evictionCallback;
            _lruLinkedList = new LinkedList<KeyValuePair<TKey, TValue>>();
            _keyToLinkedListNodeMap = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
        }

        public void Put(TKey key, TValue value)
        {
            KeyValuePair<TKey, TValue>? evictedCacheEntry = null;
            lock (_cacheLock)
            {
                if (_keyToLinkedListNodeMap.Remove(key, out var node))
                {
                    _lruLinkedList.Remove(node);
                }
                var cacheEntry = new KeyValuePair<TKey, TValue>(key, value);
                var newNode = _lruLinkedList.AddFirst(cacheEntry);
                _keyToLinkedListNodeMap[key] = newNode;

                if (_keyToLinkedListNodeMap.Count > _capacity)
                {
                    var lruNode = _lruLinkedList.Last;
                    _lruLinkedList.RemoveLast();
                    _keyToLinkedListNodeMap.Remove(lruNode.Value.Key);
                    evictedCacheEntry = lruNode.Value;
                }
            }
            if (_evictionCallback != null && evictedCacheEntry.HasValue) _evictionCallback.Invoke(evictedCacheEntry.Value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_cacheLock)
            {
                if (_keyToLinkedListNodeMap.TryGetValue(key, out var cachedNode))
                {
                    _lruLinkedList.Remove(cachedNode);
                    _lruLinkedList.AddFirst(cachedNode);
                    value = cachedNode.Value.Value;
                    return true;
                }
                value = default;
                return false;
            }
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            lock (_cacheLock)
            {
                if (_keyToLinkedListNodeMap.Remove(key, out var cachedNode))
                {
                    _lruLinkedList.Remove(cachedNode);
                    value = cachedNode.Value.Value;
                    return true;
                }
                value = default;
                return false;
            }
        }

        public void Clear()
        {
            lock (_cacheLock)
            {
                _keyToLinkedListNodeMap.Clear();
                _lruLinkedList.Clear();
            }
        }

        public int Count
        {
            get
            {
                lock (_cacheLock)
                {
                    return _keyToLinkedListNodeMap.Count;
                }
            }
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            lock (_cacheLock)
            {
                var array = new KeyValuePair<TKey, TValue>[_keyToLinkedListNodeMap.Count];
                var index = 0;
                foreach (var entry in _keyToLinkedListNodeMap)
                {
                    array[index++] = entry.Value.Value;
                }
                return array;
            }
        }
    }
}
