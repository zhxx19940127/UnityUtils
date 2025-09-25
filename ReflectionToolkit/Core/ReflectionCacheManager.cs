using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReflectionToolkit.Interfaces;
using UnityEngine;

namespace ReflectionToolkit.Core
{
    /// <summary>
    /// 反射缓存管理器 - 统一管理所有缓存模块和清理策略的核心类
    /// </summary>
    /// <summary>
    /// 反射缓存管理器：负责所有缓存模块的注册、全局清理策略的调度与执行、性能统计与自动清理。
    /// 支持多模块并发、策略优先级分派、自动定时清理、全局统计聚合。
    /// </summary>
    public class ReflectionCacheManager : IReflectionCacheManager
    {
        #region 私有字段

        // 所有已注册的缓存模块，按类型唯一索引。线程安全。
        private readonly ConcurrentDictionary<Type, ICacheModule> _modules =
            new ConcurrentDictionary<Type, ICacheModule>();

        // 所有已注册的全局清理策略，按名称唯一索引。线程安全。
        private readonly ConcurrentDictionary<string, ICleanupStrategy> _cleanupStrategies =
            new ConcurrentDictionary<string, ICleanupStrategy>();

        // 全局锁，保护注册/注销等批量操作。
        private readonly object _lockObject = new object();

        // 全局配置（可热切换）。
        private IReflectionCacheConfiguration _configuration;

        //TODO  自动清理定时器。 自行实现吧
        private object _autoCleanupTimer;

        // 是否已销毁。
        private bool _isDisposed;

        // 性能监控
        // 性能计数器（如累计清理次数、累计释放内存等）。
        private readonly ConcurrentDictionary<string, long> _performanceCounters =
            new ConcurrentDictionary<string, long>();

        // 上一次全局清理时间。
        private DateTime _lastGlobalCleanup = DateTime.MinValue;

        #endregion

        #region 事件

        // 事件：模块统计信息变更。
        public event Action<string, CacheStatistics> OnModuleStatisticsChanged;

        // 事件：全局清理完成。
        public event Action<GlobalCleanupResult> OnGlobalCleanupCompleted;

        // 事件：模块注册/注销。
        public event Action<string> OnModuleRegistered;
        public event Action<string> OnModuleUnregistered;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数：使用默认配置。
        /// </summary>
        public ReflectionCacheManager()
        {
            _configuration = new DefaultReflectionCacheConfiguration();
            Initialize();
        }

        /// <summary>
        /// 构造函数：使用自定义配置。
        /// </summary>
        public ReflectionCacheManager(IReflectionCacheConfiguration configuration)
        {
            _configuration = configuration ?? new DefaultReflectionCacheConfiguration();
            Initialize();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 注册缓存模块。类型唯一，重复注册会覆盖。
        /// </summary>
        public void RegisterModule<T>(T module) where T : ICacheModule
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            lock (_lockObject)
            {
                var moduleType = typeof(T);

                if (_modules.ContainsKey(moduleType))
                {
                    Debug.LogWarning($"模块 {moduleType.Name} 已存在，将替换现有模块");
                }

                // 初始化模块
                module.Initialize();

                // 订阅事件
                module.OnCacheHitRateChanged += OnModuleHitRateChanged;
                module.OnCacheCleanupCompleted += OnModuleCleanupCompleted;

                _modules[moduleType] = module;

                Debug.Log($"注册缓存模块: {module.ModuleName}");
                OnModuleRegistered?.Invoke(module.ModuleName);
            }
        }

        /// <summary>
        /// 注销缓存模块。
        /// </summary>
        public void UnregisterModule<T>() where T : ICacheModule
        {
            lock (_lockObject)
            {
                var moduleType = typeof(T);

                if (_modules.TryRemove(moduleType, out var module))
                {
                    // 取消订阅事件
                    module.OnCacheHitRateChanged -= OnModuleHitRateChanged;
                    module.OnCacheCleanupCompleted -= OnModuleCleanupCompleted;

                    // 销毁模块
                    module.Dispose();

                    Debug.Log($"注销缓存模块: {module.ModuleName}");
                    OnModuleUnregistered?.Invoke(module.ModuleName);
                }
            }
        }

        /// <summary>
        /// 获取指定类型的缓存模块。
        /// </summary>
        public T GetModule<T>() where T : class, ICacheModule
        {
            var moduleType = typeof(T);
            return _modules.TryGetValue(moduleType, out var module) ? module as T : null;
        }

        /// <summary>
        /// 注册全局清理策略。名称唯一，重复注册会覆盖。
        /// </summary>
        public void RegisterCleanupStrategy(ICleanupStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            lock (_lockObject)
            {
                _cleanupStrategies[strategy.StrategyName] = strategy;
                Debug.Log($"注册清理策略: {strategy.StrategyName}");
            }
        }

        /// <summary>
        /// 注销全局清理策略。
        /// </summary>
        public void UnregisterCleanupStrategy(string strategyName)
        {
            if (string.IsNullOrEmpty(strategyName))
                return;

            lock (_lockObject)
            {
                if (_cleanupStrategies.TryRemove(strategyName, out _))
                {
                    Debug.Log($"注销清理策略: {strategyName}");
                }
            }
        }

        /// <summary>
        /// 执行全局清理：遍历所有模块，按策略优先级依次尝试所有启用策略。
        /// 支持并发，聚合所有模块的清理结果。
        /// </summary>
        public GlobalCleanupResult ExecuteGlobalCleanup(CleanupIntensity intensity = CleanupIntensity.Normal)
        {
            var startTime = DateTime.UtcNow;
            var result = new GlobalCleanupResult
            {
                Intensity = intensity,
                StartTime = startTime,
                ModuleResults = new Dictionary<string, CleanupResult>()
            };

            try
            {
                Debug.Log($"开始全局清理，强度: {intensity}");

                var activeModules = GetActiveModules();
                var enabledStrategies = GetEnabledStrategies();

                // 并行执行模块清理
                var tasks = activeModules.Select(module => Task.Run(() =>
                    ExecuteModuleCleanup(module, enabledStrategies, intensity))).ToArray();

                Task.WaitAll(tasks);

                // 汇总结果
                foreach (var task in tasks)
                {
                    var moduleResult = task.Result;
                    if (moduleResult.HasValue)
                    {
                        result.ModuleResults[moduleResult.Value.Key] = moduleResult.Value.Value;
                        result.TotalCleanedItems += moduleResult.Value.Value.CleanedItemsCount;
                        result.TotalFreedMemory += moduleResult.Value.Value.FreedMemoryBytes;
                        result.TotalExecutionTime += moduleResult.Value.Value.ExecutionTimeMs;
                    }
                }

                _lastGlobalCleanup = DateTime.UtcNow;

                Debug.Log($"全局清理完成: {result}");
                OnGlobalCleanupCompleted?.Invoke(result);

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"全局清理失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取全局缓存统计信息（聚合所有模块）。
        /// </summary>
        public GlobalCacheStatistics GetGlobalStatistics()
        {
            var stats = new GlobalCacheStatistics
            {
                ModuleStatistics = new Dictionary<string, CacheStatistics>(),
                LastUpdateTime = DateTime.UtcNow
            };

            var activeModules = GetActiveModules();

            foreach (var module in activeModules)
            {
                try
                {
                    var moduleStats = module.GetStatistics();
                    stats.ModuleStatistics[module.ModuleName] = moduleStats;

                    stats.TotalCacheItems += moduleStats.TotalItems;
                    stats.TotalMemoryUsage += moduleStats.MemoryUsage;

                    // 触发模块统计更新事件（提供订阅方实时感知能力）
                    try
                    {
                        OnModuleStatisticsChanged?.Invoke(module.ModuleName, moduleStats);
                    }
                    catch (Exception evtEx)
                    {
                        Debug.LogWarning($"模块 {module.ModuleName} 统计事件回调异常: {evtEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"获取模块 {module.ModuleName} 统计信息失败: {ex.Message}");
                }
            }

            stats.ActiveModuleCount = activeModules.Count;
            stats.GlobalHitRate = CalculateGlobalHitRate(stats.ModuleStatistics);
            stats.SystemMemoryPressure = DetermineMemoryPressure(stats.TotalMemoryUsage);

            return stats;
        }

        /// <summary>
        /// 清空所有缓存模块。
        /// </summary>
        public void ClearAllCaches()
        {
            Debug.Log("清空所有缓存");

            var activeModules = GetActiveModules();

            Parallel.ForEach(activeModules, module =>
            {
                try
                {
                    module.ClearCache();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"清空模块 {module.ModuleName} 缓存失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 获取所有已注册的缓存模块。
        /// </summary>
        public IReadOnlyList<ICacheModule> GetAllModules()
        {
            return _modules.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 获取所有已注册的全局清理策略。
        /// </summary>
        public IReadOnlyList<ICleanupStrategy> GetAllStrategies()
        {
            return _cleanupStrategies.Values.ToList().AsReadOnly();
        }

        /// <summary>
        /// 设置全局配置（支持热切换）。
        /// </summary>
        public void SetGlobalConfiguration(IReflectionCacheConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // 重新启动自动清理
            if (_autoCleanupTimer != null)
            {
                StopAutoCleanup();
                StartAutoCleanup(_configuration.AutoCleanupInterval);
            }
        }

        /// <summary>
        /// 启动自动定时清理。
        /// </summary>
        public void StartAutoCleanup(TimeSpan interval)
        {
            StopAutoCleanup();

            //TODO  实现定时器功能
            // 这里假设有一个 Timer 类可以注册定时任务
            //_autoCleanupTimer = Timer.Register((float)interval.TotalSeconds, AutoCleanupCallback, null, true);
            Debug.Log($"启动自动清理，间隔: {interval}");
        }

        /// <summary>
        /// 停止自动定时清理。
        /// </summary>
        public void StopAutoCleanup()
        {
            //TODO  定时器取消
            //_autoCleanupTimer?.Clear();
            _autoCleanupTimer = null;
            Debug.Log("停止自动清理");
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化性能计数器等。
        /// </summary>
        private void Initialize()
        {
            // 初始化性能计数器
            _performanceCounters["全局清理次数"] = 0;
            _performanceCounters["累计清理项数"] = 0;
            _performanceCounters["累计释放内存"] = 0;

            Debug.Log("反射缓存管理器初始化完成");
        }

        /// <summary>
        /// 获取所有启用的缓存模块，按优先级降序。
        /// </summary>
        private List<ICacheModule> GetActiveModules()
        {
            return _modules.Values.Where(m => m.IsEnabled).OrderByDescending(m => m.Priority).ToList();
        }

        /// <summary>
        /// 获取所有启用的全局清理策略，按优先级降序。
        /// </summary>
        private List<ICleanupStrategy> GetEnabledStrategies()
        {
            return _cleanupStrategies.Values.Where(s => s.IsEnabled).OrderByDescending(s => s.Priority).ToList();
        }

        /// <summary>
        /// 对单个模块依次尝试所有启用策略，聚合所有执行结果。
        /// </summary>
        private KeyValuePair<string, CleanupResult>? ExecuteModuleCleanup(
            ICacheModule module,
            List<ICleanupStrategy> strategies,
            CleanupIntensity intensity)
        {
            try
            {
                // 基础上下文（策略执行前可动态刷新部分字段）
                var baseStats = module.GetStatistics();
                var aggregateResult = new CleanupResult
                {
                    WasExecuted = false,
                    CleanedItemsCount = 0,
                    FreedMemoryBytes = 0,
                    ExecutionTimeMs = 0,
                    StrategyName = "Aggregated",
                    ExtendedResults = new Dictionary<string, object>()
                };

                int executedStrategies = 0;
                foreach (var strategy in strategies)
                {
                    CleanupContext context = new CleanupContext
                    {
                        TargetModule = module,
                        CurrentStats = baseStats, // 可根据需要做增量刷新
                        CleanupIntensity = (int)intensity,
                        SystemMemoryPressure = GetSystemMemoryPressure()
                    };

                    bool should;
                    try
                    {
                        should = strategy.ShouldCleanup(context);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"策略 {strategy.StrategyName} ShouldCleanup 异常: {ex.Message}");
                        continue;
                    }

                    if (!should) continue;

                    CleanupResult r;
                    try
                    {
                        r = strategy.ExecuteCleanup(context);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"策略 {strategy.StrategyName} ExecuteCleanup 异常: {ex.Message}");
                        continue;
                    }

                    if (r.WasExecuted)
                    {
                        aggregateResult.WasExecuted = true;
                        aggregateResult.CleanedItemsCount += r.CleanedItemsCount;
                        aggregateResult.FreedMemoryBytes += r.FreedMemoryBytes;
                        aggregateResult.ExecutionTimeMs += r.ExecutionTimeMs;
                        executedStrategies++;

                        // 记录子策略结果（避免键冲突加入前缀）
                        if (r.ExtendedResults != null)
                        {
                            foreach (var kv in r.ExtendedResults)
                            {
                                aggregateResult.ExtendedResults[$"{strategy.StrategyName}.{kv.Key}"] = kv.Value;
                            }
                        }

                        aggregateResult.ExtendedResults[$"{strategy.StrategyName}.CleanedItems"] = r.CleanedItemsCount;
                        if (!string.IsNullOrEmpty(r.ErrorMessage))
                            aggregateResult.ExtendedResults[$"{strategy.StrategyName}.Error"] = r.ErrorMessage;
                    }
                }

                if (!aggregateResult.WasExecuted) return null; // 无策略执行

                aggregateResult.ExtendedResults["ExecutedStrategyCount"] = executedStrategies;
                return new KeyValuePair<string, CleanupResult>(module.ModuleName, aggregateResult);
            }
            catch (Exception ex)
            {
                Debug.LogError($"模块 {module.ModuleName} 清理失败: {ex.Message}");
                return new KeyValuePair<string, CleanupResult>(module.ModuleName,
                    CleanupResult.Failed("Error", ex.Message));
            }
        }

        /// <summary>
        /// 计算全局命中率（所有模块累计）。
        /// </summary>
        private float CalculateGlobalHitRate(Dictionary<string, CacheStatistics> moduleStats)
        {
            long totalHits = 0;
            long totalRequests = 0;

            foreach (var stats in moduleStats.Values)
            {
                totalHits += stats.HitCount;
                totalRequests += stats.HitCount + stats.MissCount;
            }

            return totalRequests > 0 ? (float)totalHits / totalRequests : 0f;
        }

        /// <summary>
        /// 根据总内存使用量判断全局内存压力级别。
        /// </summary>
        private string DetermineMemoryPressure(long totalMemoryUsage)
        {
            var thresholdMB = _configuration.MemoryPressureThreshold;
            var usageMB = totalMemoryUsage / (1024 * 1024);

            if (usageMB > thresholdMB * 0.8) return "高";
            if (usageMB > thresholdMB * 0.5) return "中";
            if (usageMB > thresholdMB * 0.2) return "低";
            return "正常";
        }

        /// <summary>
        /// 获取系统内存压力（0-100）。
        /// </summary>
        private int GetSystemMemoryPressure()
        {
            // 简化的内存压力计算，实际实现可以更复杂
            var stats = GetGlobalStatistics();
            var usageMB = stats.TotalMemoryUsage / (1024 * 1024);
            var thresholdMB = _configuration.MemoryPressureThreshold;

            return Math.Min(100, (int)((double)usageMB / thresholdMB * 100));
        }

        /// <summary>
        /// 自动清理定时器回调。
        /// </summary>
        private void AutoCleanupCallback()
        {
            try
            {
                var intensity = DetermineAutoCleanupIntensity();
                ExecuteGlobalCleanup(intensity);
                IncrementCounter("全局清理次数", 1);
            }
            catch (Exception ex)
            {
                Debug.LogError($"自动清理失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据内存压力等指标自适应决定清理强度。
        /// </summary>
        private CleanupIntensity DetermineAutoCleanupIntensity()
        {
            var memoryPressure = GetSystemMemoryPressure();

            if (memoryPressure > 80) return CleanupIntensity.Aggressive;
            if (memoryPressure > 60) return CleanupIntensity.Normal;
            return CleanupIntensity.Light;
        }

        /// <summary>
        /// 模块命中率变化事件回调。
        /// </summary>
        private void OnModuleHitRateChanged(string moduleName, float hitRate)
        {
            // 可以在这里实现命中率变化的响应逻辑
            if (_configuration.EnablePerformanceMonitoring)
            {
                Debug.Log($"模块 {moduleName} 命中率变化: {hitRate:P2}");
            }
        }

        /// <summary>
        /// 模块清理完成事件回调。
        /// </summary>
        private void OnModuleCleanupCompleted(string moduleName, int cleanedItems)
        {
            IncrementCounter("累计清理项数", cleanedItems);

            if (_configuration.EnablePerformanceMonitoring)
            {
                Debug.Log($"模块 {moduleName} 清理完成: {cleanedItems} 项");
            }
        }

        /// <summary>
        /// 安全递增性能计数器（兼容较低 C# 版本，避免对索引器取 ref）
        /// </summary>
        /// <summary>
        /// 安全递增性能计数器（兼容较低 C# 版本，避免对索引器取 ref）。
        /// </summary>
        private void IncrementCounter(string key, long delta)
        {
            _performanceCounters.AddOrUpdate(key, delta, (k, oldValue) => oldValue + delta);
        }

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放所有资源，销毁所有模块与策略。
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            StopAutoCleanup();

            // 销毁所有模块
            foreach (var module in _modules.Values)
            {
                try
                {
                    module.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"销毁模块失败: {ex.Message}");
                }
            }

            _modules.Clear();
            _cleanupStrategies.Clear();
            _performanceCounters.Clear();

            _isDisposed = true;
            Debug.Log("反射缓存管理器已销毁");
        }

        #endregion
    }

    /// <summary>
    /// 默认反射缓存配置
    /// </summary>
    public class DefaultReflectionCacheConfiguration : IReflectionCacheConfiguration
    {
        public TimeSpan AutoCleanupInterval { get; set; } = TimeSpan.FromMinutes(10);
        public int MemoryPressureThreshold { get; set; } = 100; // 100MB
        public int MaxCacheItems { get; set; } = 10000;
        public CleanupIntensity DefaultCleanupIntensity { get; set; } = CleanupIntensity.Normal;
        public bool EnableStatistics { get; set; } = true;
        public bool EnablePerformanceMonitoring { get; set; } = true;

        public Dictionary<string, Dictionary<string, object>> ModuleConfigurations { get; set; } =
            new Dictionary<string, Dictionary<string, object>>();
    }
}