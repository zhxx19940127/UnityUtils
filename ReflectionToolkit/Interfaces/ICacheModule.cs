using System;
using System.Collections.Generic;

namespace ReflectionToolkit.Interfaces
{
    /// <summary>
    /// 缓存模块基础接口，定义所有缓存模块的通用行为。
    /// </summary>
    public interface ICacheModule
    {
        /// <summary>
        /// 模块名称（用于日志和调试）
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// 模块优先级（数值越高，优先级越高）
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 模块是否启用
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>统计结构体</returns>
        CacheStatistics GetStatistics();

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 智能清理缓存（根据注册的清理策略）
        /// </summary>
        /// <returns>被清理的缓存项数量</returns>
        int SmartCleanup();

        /// <summary>
        /// 预热缓存（可选实现）
        /// </summary>
        void WarmupCache();

        /// <summary>
        /// 获取内存占用估算（字节）
        /// </summary>
        /// <returns>估算字节数</returns>
        long GetMemoryUsage();

        /// <summary>
        /// 模块初始化
        /// </summary>
        void Initialize();

        /// <summary>
        /// 模块销毁
        /// </summary>
        void Dispose();

        /// <summary>
        /// 缓存命中率统计事件
        /// </summary>
        event Action<string, float> OnCacheHitRateChanged;

        /// <summary>
        /// 缓存清理完成事件
        /// </summary>
        event Action<string, int> OnCacheCleanupCompleted;
    }

    /// <summary>
    /// 缓存统计信息结构
    /// </summary>
    public struct CacheStatistics
    {
        /// <summary>
        /// 缓存项总数
        /// </summary>
        public int TotalItems;

        /// <summary>
        /// 缓存命中次数
        /// </summary>
        public long HitCount;

        /// <summary>
        /// 缓存未命中次数
        /// </summary>
        public long MissCount;

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public float HitRate => HitCount + MissCount > 0 ? (float)HitCount / (HitCount + MissCount) : 0f;

        /// <summary>
        /// 内存占用（字节）
        /// </summary>
        public long MemoryUsage;

        /// <summary>
        /// 最后清理时间
        /// </summary>
        public DateTime LastCleanupTime;

        /// <summary>
        /// 平均使用频率
        /// </summary>
        public float AverageUsageFrequency;

        /// <summary>
        /// 内存压力级别
        /// </summary>
        public string MemoryPressureLevel;

        /// <summary>
        /// 额外统计信息（模块特定）
        /// </summary>
        public Dictionary<string, object> ExtendedStats;

        public override string ToString()
        {
            return $"缓存统计 - 总数: {TotalItems}, 命中率: {HitRate:P2}, " +
                   $"内存: {MemoryUsage / 1024}KB, 压力: {MemoryPressureLevel}";
        }
    }
}