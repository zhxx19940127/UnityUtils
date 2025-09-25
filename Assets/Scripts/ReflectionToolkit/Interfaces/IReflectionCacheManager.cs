using System;
using System.Collections.Generic;

namespace ReflectionToolkit.Interfaces
{
    /// <summary>
    /// 反射缓存管理器接口，统一管理所有缓存模块和清理策略。
    /// </summary>
    public interface IReflectionCacheManager : IDisposable
    {
        /// <summary>
        /// 注册缓存模块
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <param name="module">模块实例</param>
        void RegisterModule<T>(T module) where T : ICacheModule;

        /// <summary>
        /// 注销缓存模块
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        void UnregisterModule<T>() where T : ICacheModule;

        /// <summary>
        /// 获取缓存模块
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <returns>模块实例</returns>
        T GetModule<T>() where T : class, ICacheModule;

        /// <summary>
        /// 注册清理策略
        /// </summary>
        /// <param name="strategy">策略实例</param>
        void RegisterCleanupStrategy(ICleanupStrategy strategy);

        /// <summary>
        /// 注销清理策略
        /// </summary>
        /// <param name="strategyName">策略名称</param>
        void UnregisterCleanupStrategy(string strategyName);

        /// <summary>
        /// 执行全局智能清理
        /// </summary>
        /// <param name="intensity">清理强度</param>
        /// <returns>全局清理结果</returns>
        GlobalCleanupResult ExecuteGlobalCleanup(CleanupIntensity intensity = CleanupIntensity.Normal);

        /// <summary>
        /// 获取全局缓存统计
        /// </summary>
        /// <returns>全局统计结构体</returns>
        GlobalCacheStatistics GetGlobalStatistics();

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        void ClearAllCaches();

        /// <summary>
        /// 获取所有注册的模块
        /// </summary>
        /// <returns>模块列表</returns>
        IReadOnlyList<ICacheModule> GetAllModules();

        /// <summary>
        /// 获取所有注册的清理策略
        /// </summary>
        /// <returns>策略列表</returns>
        IReadOnlyList<ICleanupStrategy> GetAllStrategies();

        /// <summary>
        /// 设置全局配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        void SetGlobalConfiguration(IReflectionCacheConfiguration configuration);

        /// <summary>
        /// 启动自动清理任务
        /// </summary>
        /// <param name="interval">清理间隔</param>
        void StartAutoCleanup(TimeSpan interval);

        /// <summary>
        /// 停止自动清理任务
        /// </summary>
        void StopAutoCleanup();

        /// <summary>
        /// 全局缓存事件
        /// </summary>
        event Action<string, CacheStatistics> OnModuleStatisticsChanged;

        /// <summary>全局清理完成事件</summary>
        event Action<GlobalCleanupResult> OnGlobalCleanupCompleted;

        /// <summary>模块注册事件</summary>
        event Action<string> OnModuleRegistered;

        /// <summary>模块注销事件</summary>
        event Action<string> OnModuleUnregistered;
    }

    /// <summary>
    /// 清理强度枚举
    /// </summary>
    public enum CleanupIntensity
    {
        /// <summary>
        /// 轻量清理 - 只清理明显过期的缓存
        /// </summary>
        Light = 1,

        /// <summary>
        /// 正常清理 - 平衡性能和内存
        /// </summary>
        Normal = 2,

        /// <summary>
        /// 积极清理 - 释放更多内存
        /// </summary>
        Aggressive = 3,

        /// <summary>
        /// 强制清理 - 清空大部分缓存
        /// </summary>
        Force = 4
    }

    /// <summary>
    /// 全局清理结果结构体
    /// </summary>
    public struct GlobalCleanupResult
    {
        /// <summary>
        /// 总清理项目数
        /// </summary>
        public int TotalCleanedItems;

        /// <summary>
        /// 总释放内存
        /// </summary>
        public long TotalFreedMemory;

        /// <summary>
        /// 总执行时间
        /// </summary>
        public long TotalExecutionTime;

        /// <summary>
        /// 各模块清理结果
        /// </summary>
        public Dictionary<string, CleanupResult> ModuleResults;

        /// <summary>
        /// 清理强度
        /// </summary>
        public CleanupIntensity Intensity;

        /// <summary>
        /// 清理开始时间
        /// </summary>
        public DateTime StartTime;

        public override string ToString()
        {
            return $"全局清理完成 - 清理项目: {TotalCleanedItems}, " +
                   $"释放内存: {TotalFreedMemory / 1024}KB, 耗时: {TotalExecutionTime}ms";
        }
    }

    /// <summary>
    /// 全局缓存统计结构体
    /// </summary>
    public struct GlobalCacheStatistics
    {
        /// <summary>
        /// 所有模块的统计信息
        /// </summary>
        public Dictionary<string, CacheStatistics> ModuleStatistics;

        /// <summary>
        /// 总缓存项目数
        /// </summary>
        public int TotalCacheItems;

        /// <summary>
        /// 总内存使用量
        /// </summary>
        public long TotalMemoryUsage;

        /// <summary>
        /// 全局命中率
        /// </summary>
        public float GlobalHitRate;

        /// <summary>
        /// 活跃模块数量
        /// </summary>
        public int ActiveModuleCount;

        /// <summary>
        /// 系统整体内存压力级别
        /// </summary>
        public string SystemMemoryPressure;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime;

        public override string ToString()
        {
            return $"全局缓存统计 - 模块数: {ActiveModuleCount}, 总项目: {TotalCacheItems}, " +
                   $"内存: {TotalMemoryUsage / 1024}KB, 命中率: {GlobalHitRate:P2}";
        }
    }

    /// <summary>
    /// 反射缓存配置接口
    /// </summary>
    public interface IReflectionCacheConfiguration
    {
        /// <summary>
        /// 自动清理间隔
        /// </summary>
        TimeSpan AutoCleanupInterval { get; set; }

        /// <summary>
        /// 内存压力阈值(MB)
        /// </summary>
        int MemoryPressureThreshold { get; set; }

        /// <summary>
        /// 最大缓存项目数
        /// </summary>
        int MaxCacheItems { get; set; }

        /// <summary>
        /// 默认清理强度
        /// </summary>
        CleanupIntensity DefaultCleanupIntensity { get; set; }

        /// <summary>
        /// 是否启用统计信息收集
        /// </summary>
        bool EnableStatistics { get; set; }

        /// <summary>
        /// 是否启用性能监控
        /// </summary>
        bool EnablePerformanceMonitoring { get; set; }

        /// <summary>
        /// 模块特定配置
        /// </summary>
        Dictionary<string, Dictionary<string, object>> ModuleConfigurations { get; set; }
    }
}