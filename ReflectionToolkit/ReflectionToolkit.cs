using System;
using ReflectionToolkit.Core;
using ReflectionToolkit.Interfaces;
using ReflectionToolkit.Modules;
using ReflectionToolkit.Strategies;
using UnityEngine;

namespace ReflectionToolkit
{
    /// <summary>
    /// 反射工具包主入口：统一管理所有缓存模块、全局策略、统计与便捷操作。
    /// 支持自动初始化、模块/策略注册、全局清理、统计报告、事件订阅等。
    /// </summary>
    public static class ReflectionToolkit
    {
        #region 私有字段

        /// <summary>全局缓存管理器实例</summary>
        private static IReflectionCacheManager _manager;

        /// <summary>全局锁，保护初始化等操作</summary>
        private static readonly object _lockObject = new object();

        /// <summary>是否已初始化</summary>
        private static bool _isInitialized = false;

        #endregion

        #region 公共属性

        /// <summary>
        /// 获取全局缓存管理器实例（自动延迟初始化）。
        /// </summary>
        public static IReflectionCacheManager Manager
        {
            get
            {
                if (_manager == null)
                {
                    lock (_lockObject)
                    {
                        if (_manager == null)
                        {
                            Initialize();
                        }
                    }
                }

                return _manager;
            }
        }

        /// <summary>
        /// 是否已初始化。
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化（默认配置）。
        /// </summary>
        public static void Initialize()
        {
            Initialize(new DefaultReflectionCacheConfiguration());
        }

        /// <summary>
        /// 初始化（自定义配置）。
        /// </summary>
        public static void Initialize(IReflectionCacheConfiguration configuration)
        {
            if (_isInitialized) return;

            lock (_lockObject)
            {
                if (_isInitialized) return;

                try
                {
                    // 创建管理器
                    _manager = new ReflectionCacheManager(configuration);

                    // 注册默认模块
                    RegisterDefaultModules();

                    // 注册默认清理策略
                    RegisterDefaultCleanupStrategies();

                    // 启动自动清理
                    if (configuration.AutoCleanupInterval > TimeSpan.Zero)
                    {
                        _manager.StartAutoCleanup(configuration.AutoCleanupInterval);
                    }

                    _isInitialized = true;
                    UnityEngine.Debug.Log("反射工具包初始化完成");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"反射工具包初始化失败: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// 注册所有默认缓存模块（类型、委托、属性索引、网络等）。
        /// </summary>
        private static void RegisterDefaultModules()
        {
            // 注册基础缓存模块
            _manager.RegisterModule(new TypeCacheModule());
            _manager.RegisterModule(new DelegateCacheModule());


            UnityEngine.Debug.Log("默认缓存模块注册完成");
        }

        /// <summary>
        /// 注册所有默认全局清理策略。
        /// </summary>
        private static void RegisterDefaultCleanupStrategies()
        {
            // 注册基础清理策略
            _manager.RegisterCleanupStrategy(new LRUCleanupStrategy());
            _manager.RegisterCleanupStrategy(new UsageBasedCleanupStrategy());
            _manager.RegisterCleanupStrategy(new MemoryPressureCleanupStrategy());
            _manager.RegisterCleanupStrategy(new ScheduledCleanupStrategy());
            _manager.RegisterCleanupStrategy(new AdaptiveCleanupStrategy());

            UnityEngine.Debug.Log("默认清理策略注册完成");
        }

        #endregion

        #region 模块访问方法

        /// <summary>
        /// 获取指定类型的缓存模块。
        /// </summary>
        public static T GetModule<T>() where T : class, ICacheModule
        {
            return Manager?.GetModule<T>();
        }

        /// <summary>
        /// 注册缓存模块。
        /// </summary>
        public static void RegisterModule<T>(T module) where T : ICacheModule
        {
            Manager?.RegisterModule(module);
        }

        /// <summary>
        /// 注销缓存模块。
        /// </summary>
        public static void UnregisterModule<T>() where T : ICacheModule
        {
            Manager?.UnregisterModule<T>();
        }

        #endregion

        #region 清理策略方法

        /// <summary>
        /// 注册全局清理策略。
        /// </summary>
        public static void RegisterCleanupStrategy(ICleanupStrategy strategy)
        {
            Manager?.RegisterCleanupStrategy(strategy);
        }

        /// <summary>
        /// 注销全局清理策略。
        /// </summary>
        public static void UnregisterCleanupStrategy(string strategyName)
        {
            Manager?.UnregisterCleanupStrategy(strategyName);
        }

        /// <summary>
        /// 执行全局清理。
        /// </summary>
        public static GlobalCleanupResult ExecuteGlobalCleanup(CleanupIntensity intensity = CleanupIntensity.Normal)
        {
            return Manager?.ExecuteGlobalCleanup(intensity) ?? default;
        }

        #endregion

        #region 统计和监控方法

        /// <summary>
        /// 获取全局缓存统计信息。
        /// </summary>
        public static GlobalCacheStatistics GetGlobalStatistics()
        {
            return Manager?.GetGlobalStatistics() ?? default;
        }

        /// <summary>
        /// 获取详细缓存报告（含各模块统计、扩展信息）。
        /// </summary>
        public static string GetDetailedCacheReport()
        {
            if (!_isInitialized || _manager == null)
                return "反射工具包未初始化";

            var stats = GetGlobalStatistics();
            var report = $"=== 反射工具包缓存报告 ===\n";
            report += $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
            report += $"活跃模块数: {stats.ActiveModuleCount}\n";
            report += $"总缓存项: {stats.TotalCacheItems}\n";
            report += $"总内存使用: {stats.TotalMemoryUsage / 1024:F2} KB\n";
            report += $"全局命中率: {stats.GlobalHitRate:P2}\n";
            report += $"系统内存压力: {stats.SystemMemoryPressure}\n\n";

            report += "=== 各模块详细统计 ===\n";
            foreach (var kvp in stats.ModuleStatistics)
            {
                var moduleName = kvp.Key;
                var moduleStats = kvp.Value;

                report += $"\n【{moduleName}】\n";
                report += $"  缓存项数: {moduleStats.TotalItems}\n";
                report += $"  命中次数: {moduleStats.HitCount}\n";
                report += $"  未命中次数: {moduleStats.MissCount}\n";
                report += $"  命中率: {moduleStats.HitRate:P2}\n";
                report += $"  内存使用: {moduleStats.MemoryUsage / 1024:F2} KB\n";
                report += $"  平均使用频率: {moduleStats.AverageUsageFrequency:F2}\n";
                report += $"  内存压力级别: {moduleStats.MemoryPressureLevel}\n";
                report += $"  最后清理时间: {moduleStats.LastCleanupTime:yyyy-MM-dd HH:mm:ss}\n";

                if (moduleStats.ExtendedStats != null && moduleStats.ExtendedStats.Count > 0)
                {
                    report += "  扩展统计:\n";
                    foreach (var extKvp in moduleStats.ExtendedStats)
                    {
                        report += $"    {extKvp.Key}: {extKvp.Value}\n";
                    }
                }
            }

            return report;
        }

        /// <summary>
        /// 获取性能监控信息（模块/策略注册、优先级等）。
        /// </summary>
        public static string GetPerformanceMonitorInfo()
        {
            if (!_isInitialized)
                return "反射工具包未初始化";

            var modules = _manager.GetAllModules();
            var strategies = _manager.GetAllStrategies();

            var info = $"=== 反射工具包性能监控 ===\n";
            info += $"监控时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n";

            info += $"已注册模块数量: {modules.Count}\n";
            foreach (var module in modules)
            {
                info += $"  - {module.ModuleName} (优先级: {module.Priority}, 启用: {module.IsEnabled})\n";
            }

            info += $"\n已注册策略数量: {strategies.Count}\n";
            foreach (var strategy in strategies)
            {
                info += $"  - {strategy.StrategyName} (优先级: {strategy.Priority}, 启用: {strategy.IsEnabled})\n";
            }

            return info;
        }

        #endregion

        #region 配置管理方法

        /// <summary>
        /// 设置全局配置。
        /// </summary>
        public static void SetGlobalConfiguration(IReflectionCacheConfiguration configuration)
        {
            Manager?.SetGlobalConfiguration(configuration);
        }

        /// <summary>
        /// 启动自动清理。
        /// </summary>
        public static void StartAutoCleanup(TimeSpan interval)
        {
            Manager?.StartAutoCleanup(interval);
        }

        /// <summary>
        /// 停止自动清理。
        /// </summary>
        public static void StopAutoCleanup()
        {
            Manager?.StopAutoCleanup();
        }

        #endregion

        #region 便捷访问方法

        /// <summary>
        /// 清空所有缓存。
        /// </summary>
        public static void ClearAllCaches()
        {
            Manager?.ClearAllCaches();
        }

        /// <summary>
        /// 预热所有缓存。
        /// </summary>
        public static void WarmupAllCaches()
        {
            var modules = Manager?.GetAllModules();
            if (modules != null)
            {
                foreach (var module in modules)
                {
                    try
                    {
                        module.WarmupCache();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"模块 {module.ModuleName} 预热失败: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 获取内存使用摘要。
        /// </summary>
        public static string GetMemoryUsageSummary()
        {
            var stats = GetGlobalStatistics();
            var totalMB = stats.TotalMemoryUsage / (1024.0 * 1024.0);

            return $"反射工具包内存使用: {totalMB:F2} MB " +
                   $"({stats.TotalCacheItems} 个缓存项, " +
                   $"{stats.ActiveModuleCount} 个活跃模块)";
        }

        #endregion

        #region 事件订阅方法

        /// <summary>
        /// 订阅全局清理完成事件。
        /// </summary>
        public static void SubscribeToGlobalCleanup(Action<GlobalCleanupResult> handler)
        {
            if (Manager != null)
            {
                Manager.OnGlobalCleanupCompleted += handler;
            }
        }

        /// <summary>
        /// 取消订阅全局清理完成事件。
        /// </summary>
        public static void UnsubscribeFromGlobalCleanup(Action<GlobalCleanupResult> handler)
        {
            if (Manager != null)
            {
                Manager.OnGlobalCleanupCompleted -= handler;
            }
        }

        #endregion

        #region 销毁方法

        /// <summary>
        /// 销毁反射工具包及所有资源。
        /// </summary>
        public static void Dispose()
        {
            if (!_isInitialized) return;

            lock (_lockObject)
            {
                if (!_isInitialized) return;

                try
                {
                    _manager?.Dispose();
                    _manager = null;
                    _isInitialized = false;

                    UnityEngine.Debug.Log("反射工具包已销毁");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"销毁反射工具包失败: {ex.Message}");
                }
            }
        }

        #endregion
    }
}