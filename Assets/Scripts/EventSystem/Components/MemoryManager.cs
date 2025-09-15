using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 内存管理器实现 - 负责弱引用和对象池的管理
    /// 提供内存优化和自动清理功能
    /// </summary>
    public class MemoryManager : IMemoryManager
    {
        #region 私有字段

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 对象池锁
        /// </summary>
        private readonly object _poolLock = new object();

        /// <summary>
        /// 弱引用缓存，避免重复创建相同对象的弱引用
        /// Key: 对象的哈希码, Value: 弱引用
        /// </summary>
        private readonly Dictionary<int, WeakReference<object>> _weakReferenceCache =
            new Dictionary<int, WeakReference<object>>();

        /// <summary>
        /// 弱引用比较器实例
        /// </summary>
        private readonly WeakReferenceComparer _comparer = new WeakReferenceComparer();

        /// <summary>
        /// 各类型对象池的字典
        /// Key: 类型, Value: 对象池栈
        /// </summary>
        private readonly Dictionary<Type, object> _objectPools = new Dictionary<Type, object>();

        /// <summary>
        /// 对象池的最大容量限制
        /// </summary>
        private const int MAX_POOL_SIZE = 100;

        /// <summary>
        /// 弱引用清理的阈值
        /// </summary>
        private const int CLEANUP_THRESHOLD = 50;

        /// <summary>
        /// 上次清理的时间
        /// </summary>
        private DateTime _lastCleanupTime = DateTime.Now;

        /// <summary>
        /// 清理间隔（分钟）
        /// </summary>
        private const int CLEANUP_INTERVAL_MINUTES = 5;

        #endregion

        #region IMemoryManager 实现

        /// <summary>
        /// 创建或获取对象的弱引用
        /// </summary>
        public WeakReference<object> GetOrCreateWeakReference(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            var hashCode = instance.GetHashCode();

            lock (_lock)
            {
                // 检查缓存中是否已存在
                if (_weakReferenceCache.TryGetValue(hashCode, out var existing))
                {
                    if (existing.TryGetTarget(out var target) && ReferenceEquals(target, instance))
                    {
                        return existing;
                    }
                    else
                    {
                        // 弱引用已失效，移除并创建新的
                        _weakReferenceCache.Remove(hashCode);
                    }
                }

                // 创建新的弱引用
                var newWeakRef = new WeakReference<object>(instance);
                _weakReferenceCache[hashCode] = newWeakRef;

                // 定期清理检查
                CheckPeriodicCleanup();

                return newWeakRef;
            }
        }

        /// <summary>
        /// 清理无效的弱引用
        /// </summary>
        public int CleanupDeadReferences()
        {
            int cleanedCount = 0;

            lock (_lock)
            {
                var deadKeys = new List<int>();

                foreach (var kvp in _weakReferenceCache)
                {
                    if (!kvp.Value.TryGetTarget(out var _))
                    {
                        deadKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in deadKeys)
                {
                    _weakReferenceCache.Remove(key);
                    cleanedCount++;
                }

                _lastCleanupTime = DateTime.Now;
            }

#if UNITY_EDITOR
            if (cleanedCount > 0)
            {
                Debug.Log($"[MemoryManager] 清理了 {cleanedCount} 个无效弱引用");
            }
#endif

            return cleanedCount;
        }

        /// <summary>
        /// 获取活跃的弱引用数量
        /// </summary>
        public int GetActiveReferenceCount()
        {
            lock (_lock)
            {
                int activeCount = 0;
                foreach (var weakRef in _weakReferenceCache.Values)
                {
                    if (weakRef.TryGetTarget(out var _))
                    {
                        activeCount++;
                    }
                }
                return activeCount;
            }
        }

        /// <summary>
        /// 从对象池获取列表
        /// </summary>
        public List<T> GetPooledList<T>()
        {
            var type = typeof(List<T>);

            lock (_poolLock)
            {
                if (_objectPools.TryGetValue(type, out var poolObj) && poolObj is Stack<List<T>> pool)
                {
                    if (pool.Count > 0)
                    {
                        var list = pool.Pop();
                        list.Clear();
                        return list;
                    }
                }
            }

            return new List<T>();
        }

        /// <summary>
        /// 归还列表到对象池
        /// </summary>
        public void ReturnPooledList<T>(List<T> list)
        {
            if (list == null) return;

            var type = typeof(List<T>);

            lock (_poolLock)
            {
                if (!_objectPools.TryGetValue(type, out var poolObj))
                {
                    poolObj = new Stack<List<T>>();
                    _objectPools[type] = poolObj;
                }

                if (poolObj is Stack<List<T>> pool && pool.Count < MAX_POOL_SIZE)
                {
                    list.Clear();
                    pool.Push(list);
                }
            }
        }

        /// <summary>
        /// 清理对象池
        /// </summary>
        public void ClearPools()
        {
            lock (_poolLock)
            {
                _objectPools.Clear();
            }

#if UNITY_EDITOR
            Debug.Log("[MemoryManager] 已清理所有对象池");
#endif
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查是否需要定期清理
        /// </summary>
        private void CheckPeriodicCleanup()
        {
            // 检查时间间隔
            if ((DateTime.Now - _lastCleanupTime).TotalMinutes >= CLEANUP_INTERVAL_MINUTES)
            {
                CleanupDeadReferences();
                return;
            }

            // 检查数量阈值
            if (_weakReferenceCache.Count >= CLEANUP_THRESHOLD)
            {
                CleanupDeadReferences();
            }
        }

        #endregion

        #region 调试和诊断方法

#if UNITY_EDITOR
        /// <summary>
        /// 获取内存管理器统计信息（仅编辑器模式）
        /// </summary>
        /// <returns>统计信息</returns>
        public MemoryStats GetMemoryStats()
        {
            lock (_lock)
            lock (_poolLock)
            {
                int totalPooledObjects = 0;
                var poolDetails = new Dictionary<string, int>();

                foreach (var kvp in _objectPools)
                {
                    var typeName = kvp.Key.Name;
                    var count = 0;

                    // 通过反射获取池的大小
                    if (kvp.Value is System.Collections.ICollection collection)
                    {
                        count = collection.Count;
                        totalPooledObjects += count;
                    }

                    poolDetails[typeName] = count;
                }

                return new MemoryStats
                {
                    WeakReferenceCount = _weakReferenceCache.Count,
                    ActiveReferenceCount = GetActiveReferenceCount(),
                    TotalPooledObjects = totalPooledObjects,
                    PoolDetails = poolDetails,
                    LastCleanupTime = _lastCleanupTime
                };
            }
        }

        /// <summary>
        /// 强制执行全面的内存清理（仅编辑器模式）
        /// </summary>
        public void ForceFullCleanup()
        {
            var cleanedRefs = CleanupDeadReferences();
            ClearPools();

            Debug.Log($"[MemoryManager] 强制清理完成 - 清理了 {cleanedRefs} 个弱引用，清空了所有对象池");
        }

        /// <summary>
        /// 内存统计信息结构
        /// </summary>
        public struct MemoryStats
        {
            public int WeakReferenceCount;
            public int ActiveReferenceCount;
            public int TotalPooledObjects;
            public Dictionary<string, int> PoolDetails;
            public DateTime LastCleanupTime;

            public override string ToString()
            {
                var poolInfo = string.Join(", ", PoolDetails.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                return $"MemoryManager Stats:\n" +
                       $"- Weak References: {WeakReferenceCount} (Active: {ActiveReferenceCount})\n" +
                       $"- Pooled Objects: {TotalPooledObjects}\n" +
                       $"- Pool Details: {poolInfo}\n" +
                       $"- Last Cleanup: {LastCleanupTime:yyyy-MM-dd HH:mm:ss}";
            }
        }
#endif

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            lock (_poolLock)
            {
                _weakReferenceCache.Clear();
                _objectPools.Clear();
            }

#if UNITY_EDITOR
            Debug.Log("[MemoryManager] 已释放所有资源");
#endif
        }

        #endregion
    }
}