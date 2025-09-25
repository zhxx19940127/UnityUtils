using System;
using System.Collections.Generic;
using System.Threading;
using ReflectionToolkit.Interfaces;
using UnityEngine;

namespace ReflectionToolkit.Core
{
    /// <summary>
    /// 缓存模块基础抽象类 - 提供通用的缓存模块实现基础
    /// </summary>
    /// <summary>
    /// 缓存模块基础抽象类：为所有具体缓存模块（如类型缓存、委托缓存等）提供统一的生命周期、统计、清理、命中率等通用实现。
    /// 支持命中/未命中统计、智能清理、事件通知、并发安全。
    /// </summary>
    public abstract class BaseCacheModule : ICacheModule
    {
        #region 属性

        /// <summary>模块名称（子类实现）</summary>
        public abstract string ModuleName { get; }

        /// <summary>模块优先级（影响全局清理顺序）</summary>
        public virtual int Priority { get; protected set; } = 100;

        /// <summary>是否启用（可动态切换）</summary>
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region 事件

        /// <summary>命中率变化事件（每100次请求触发）</summary>
        public event Action<string, float> OnCacheHitRateChanged;

        /// <summary>清理完成事件（每次清理后触发）</summary>
        public event Action<string, int> OnCacheCleanupCompleted;

        #endregion

        #region 保护字段

        /// <summary>命中次数</summary>
        protected long _hitCount;

        /// <summary>未命中次数</summary>
        protected long _missCount;

        /// <summary>上次清理时间</summary>
        protected DateTime _lastCleanupTime = DateTime.MinValue;

        /// <summary>是否已初始化</summary>
        protected bool _isInitialized;

        /// <summary>是否已销毁</summary>
        protected bool _isDisposed;

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化模块（只执行一次）。
        /// </summary>
        public virtual void Initialize()
        {
            if (_isInitialized) return;

            OnInitialize();
            _isInitialized = true;
            //Debug.Log($"缓存模块 {ModuleName} 初始化完成");
        }

        /// <summary>
        /// 获取当前统计信息（命中/未命中/内存/扩展等）。
        /// </summary>
        public virtual CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalItems = GetCacheItemCount(),
                HitCount = Interlocked.Read(ref _hitCount),
                MissCount = Interlocked.Read(ref _missCount),
                MemoryUsage = GetMemoryUsage(),
                LastCleanupTime = _lastCleanupTime,
                AverageUsageFrequency = CalculateAverageUsageFrequency(),
                MemoryPressureLevel = DetermineMemoryPressureLevel(),
                ExtendedStats = GetExtendedStatistics()
            };
        }

        /// <summary>
        /// 清空所有缓存内容并重置统计。
        /// </summary>
        public virtual void ClearCache()
        {
            if (_isDisposed) return;

            OnClearCache();
            ResetStatistics();
            Debug.Log($"缓存模块 {ModuleName} 已清空");
        }

        /// <summary>
        /// 智能清理入口：调用 OnSmartCleanup 并更新统计、触发事件。
        /// </summary>
        public virtual int SmartCleanup()
        {
            if (_isDisposed) return 0;
            int cleanedCount = 0;
            try
            {
                cleanedCount = OnSmartCleanup();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"模块 {ModuleName} OnSmartCleanup 异常: {ex.Message}");
            }

            _lastCleanupTime = DateTime.UtcNow;
            OnCacheCleanupCompleted?.Invoke(ModuleName, cleanedCount);
            Debug.Log($"缓存模块 {ModuleName} 智能清理完成(统一全局策略后仅执行模块自定义逻辑)，清理了 {cleanedCount} 项");
            return cleanedCount;
        }

        /// <summary>
        /// 预热缓存（可选，子类可重写）。
        /// </summary>
        public virtual void WarmupCache()
        {
            if (_isDisposed) return;

            OnWarmupCache();
            Debug.Log($"缓存模块 {ModuleName} 预热完成");
        }

        /// <summary>
        /// 获取当前内存使用量（字节，子类实现）。
        /// </summary>
        public abstract long GetMemoryUsage();

        /// <summary>
        /// 销毁模块，释放所有资源。
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed) return;

            OnDispose();
            _isDisposed = true;
            Debug.Log($"缓存模块 {ModuleName} 已销毁");
        }

        #endregion

        #region 保护方法 - 子类重写

        /// <summary>
        /// 模块初始化时调用
        /// </summary>
        /// <summary>
        /// 子类初始化钩子。
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// 清空缓存时调用
        /// </summary>
        /// <summary>
        /// 子类清空缓存钩子。
        /// </summary>
        protected abstract void OnClearCache();

        /// <summary>
        /// 智能清理时调用
        /// </summary>
        /// <returns>清理的项目数量</returns>
        /// <summary>
        /// 子类智能清理钩子（返回清理条数）。
        /// </summary>
        protected abstract int OnSmartCleanup();

        /// <summary>
        /// 预热缓存时调用
        /// </summary>
        /// <summary>
        /// 子类预热钩子。
        /// </summary>
        protected virtual void OnWarmupCache()
        {
        }

        /// <summary>
        /// 销毁模块时调用
        /// </summary>
        /// <summary>
        /// 子类销毁钩子。
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        /// <summary>
        /// 获取缓存项目总数
        /// </summary>
        /// <summary>
        /// 获取缓存项总数（子类实现）。
        /// </summary>
        protected abstract int GetCacheItemCount();

        /// <summary>
        /// 获取扩展统计信息
        /// </summary>
        /// <summary>
        /// 获取扩展统计信息（子类可扩展）。
        /// </summary>
        protected virtual Dictionary<string, object> GetExtendedStatistics()
        {
            return new Dictionary<string, object>();
        }

        #endregion

        #region 保护辅助方法

        /// <summary>
        /// 记录缓存命中
        /// </summary>
        /// <summary>
        /// 记录一次缓存命中。
        /// </summary>
        protected void RecordCacheHit()
        {
            Interlocked.Increment(ref _hitCount);
            UpdateHitRate();
        }

        /// <summary>
        /// 记录缓存未命中
        /// </summary>
        /// <summary>
        /// 记录一次缓存未命中。
        /// </summary>
        protected void RecordCacheMiss()
        {
            Interlocked.Increment(ref _missCount);
            UpdateHitRate();
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        /// <summary>
        /// 重置命中/未命中统计。
        /// </summary>
        protected void ResetStatistics()
        {
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
        }

        /// <summary>
        /// 计算平均使用频率
        /// </summary>
        /// <summary>
        /// 计算平均使用频率（每分钟请求数）。
        /// </summary>
        protected virtual float CalculateAverageUsageFrequency()
        {
            var totalRequests = _hitCount + _missCount;
            if (totalRequests == 0) return 0f;

            var moduleAge = DateTime.UtcNow -
                            (_lastCleanupTime == DateTime.MinValue ? DateTime.UtcNow : _lastCleanupTime);
            return moduleAge.TotalMinutes > 0 ? (float)(totalRequests / moduleAge.TotalMinutes) : 0f;
        }

        /// <summary>
        /// 确定内存压力级别
        /// </summary>
        /// <summary>
        /// 估算内存压力级别（高/中/低/正常）。
        /// </summary>
        protected virtual string DetermineMemoryPressureLevel()
        {
            var memoryUsage = GetMemoryUsage();
            var memoryMB = memoryUsage / (1024 * 1024);

            if (memoryMB > 50) return "高";
            if (memoryMB > 20) return "中";
            if (memoryMB > 5) return "低";
            return "正常";
        }

        /// <summary>
        /// 更新命中率并触发事件
        /// </summary>
        /// <summary>
        /// 更新命中率并在每100次请求时触发事件。
        /// </summary>
        private void UpdateHitRate()
        {
            var totalRequests = _hitCount + _missCount;
            if (totalRequests > 0 && totalRequests % 100 == 0) // 每100次请求更新一次
            {
                var hitRate = (float)_hitCount / totalRequests;
                OnCacheHitRateChanged?.Invoke(ModuleName, hitRate);
            }
        }

        #endregion

        // 模块级驱逐策略 API 已移除
    }

    /// <summary>
    /// 清理策略配置实现
    /// </summary>
    public class CleanupConfiguration : ICleanupConfiguration
    {
        public string Name { get; private set; }
        public Dictionary<string, object> Parameters { get; private set; }

        public CleanupConfiguration(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Parameters = new Dictionary<string, object>();
        }

        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        public void SetParameter<T>(string key, T value)
        {
            Parameters[key] = value;
        }
    }
}