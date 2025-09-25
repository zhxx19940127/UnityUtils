using System;
using ReflectionToolkit.Interfaces;

namespace ReflectionToolkit.Strategies
{
    /// <summary>
    /// LRU（最近最少使用）清理策略，基于时间的缓存清理。
    /// </summary>
    public class LRUCleanupStrategy : BaseCleanupStrategy
    {
        #region 属性

        /// <summary>策略名称</summary>
        public override string StrategyName => "LRU";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，设置优先级
        /// </summary>
        public LRUCleanupStrategy()
        {
            Priority = 200; // 高优先级
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 执行LRU清理逻辑
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>清理结果</returns>
        protected override CleanupResult OnExecuteCleanup(CleanupContext context)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var module = context.TargetModule;
                var stats = context.CurrentStats;

                // 获取配置参数
                var maxAge = TimeSpan.FromMinutes(GetConfigParameter("MaxAgeMinutes", 60));
                var maxItems = GetConfigParameter("MaxItems", 1000);
                var cleanupRatio = GetConfigParameter("CleanupRatio", 0.25f); // 清理25%

                // 根据清理强度调整参数
                var intensityFactor = CalculateIntensityFactor(context);
                maxAge = TimeSpan.FromTicks((long)(maxAge.Ticks * (1.0f - intensityFactor * 0.5f)));
                cleanupRatio = Math.Min(1.0f, cleanupRatio * (1.0f + intensityFactor));

                // 根据内存压力调整
                if (context.SystemMemoryPressure > 70)
                {
                    maxAge = TimeSpan.FromTicks(maxAge.Ticks / 2);
                    cleanupRatio *= 1.5f;
                }

                int cleanedItems = 0;
                long freedMemory = 0;

                // 执行基于时间的清理
                if (stats.TotalItems > maxItems || ShouldCleanupByAge(maxAge))
                {
                    cleanedItems = module.SmartCleanup();

                    // 估算释放的内存
                    var avgItemSize = stats.TotalItems > 0 ? stats.MemoryUsage / stats.TotalItems : 100;
                    freedMemory = cleanedItems * avgItemSize;
                }

                stopwatch.Stop();

                var result = CleanupResult.Success(
                    StrategyName,
                    cleanedItems,
                    freedMemory,
                    stopwatch.ElapsedMilliseconds
                );

                // 添加LRU特定的统计信息
                result.ExtendedResults["MaxAgeMinutes"] = maxAge.TotalMinutes;
                result.ExtendedResults["CleanupRatio"] = cleanupRatio;
                result.ExtendedResults["IntensityFactor"] = intensityFactor;
                result.ExtendedResults["MemoryPressureAdjustment"] = context.SystemMemoryPressure > 70;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行LRU清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected override bool OnShouldCleanup(CleanupContext context)
        {
            if (context?.TargetModule == null)
                return false;

            var stats = context.CurrentStats;
            var maxAge = TimeSpan.FromMinutes(GetConfigParameter("MaxAgeMinutes", 60));
            var maxItems = GetConfigParameter("MaxItems", 1000);
            var memoryThreshold = GetConfigParameter("MemoryThresholdMB", 50) * 1024 * 1024;

            // 检查最小间隔
            var minInterval = TimeSpan.FromMinutes(GetConfigParameter("MinIntervalMinutes", 5));
            if (!CheckMinimumInterval(minInterval))
                return false;

            // 检查是否需要清理的条件
            // 1. 项目数量超过阈值
            bool shouldCleanup = stats.TotalItems > maxItems;

            // 2. 内存使用超过阈值
            if (stats.MemoryUsage > memoryThreshold)
            {
                shouldCleanup = true;
            }

            // 3. 基于时间的清理
            if (ShouldCleanupByAge(maxAge))
            {
                shouldCleanup = true;
            }

            // 4. 系统内存压力
            if (context.SystemMemoryPressure > 80)
            {
                shouldCleanup = true;
            }

            // 5. 强制清理模式
            if (context.CleanupIntensity >= (int)CleanupIntensity.Force)
            {
                shouldCleanup = true;
            }

            return shouldCleanup;
        }

        /// <summary>
        /// 创建LRU默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected override ICleanupConfiguration CreateDefaultConfiguration()
        {
            var config = new CleanupConfiguration("LRU");

            // 默认配置参数
            config.SetParameter("MaxAgeMinutes", 60); // 最大缓存时间60分钟
            config.SetParameter("MaxItems", 1000); // 最大项目数1000
            config.SetParameter("CleanupRatio", 0.25f); // 清理25%的项目
            config.SetParameter("MemoryThresholdMB", 50); // 内存阈值50MB
            config.SetParameter("MinIntervalMinutes", 5); // 最小清理间隔5分钟
            config.SetParameter("EnableTimeBasedCleanup", true); // 启用基于时间的清理

            return config;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 检查是否应基于时间进行清理
        /// </summary>
        /// <param name="maxAge">最大缓存时长</param>
        /// <returns>是否应清理</returns>
        private bool ShouldCleanupByAge(TimeSpan maxAge)
        {
            if (!GetConfigParameter("EnableTimeBasedCleanup", true))
                return false;

            // 如果上次清理时间超过最大年龄的一半，就应该清理
            return DateTime.UtcNow - _lastExecutionTime > TimeSpan.FromTicks(maxAge.Ticks / 2);
        }

        #endregion
    }


    /// <summary>
    /// 基于使用频率的清理策略，清理低频使用的缓存项。
    /// </summary>
    public class UsageBasedCleanupStrategy : BaseCleanupStrategy
    {
        #region 属性

        /// <summary>策略名称</summary>
        public override string StrategyName => "UsageBased";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，设置优先级
        /// </summary>
        public UsageBasedCleanupStrategy()
        {
            Priority = 150; // 中等优先级
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 执行基于使用频率的清理逻辑
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>清理结果</returns>
        protected override CleanupResult OnExecuteCleanup(CleanupContext context)
        {
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var module = context.TargetModule;
                var stats = context.CurrentStats;

                // 获取配置参数
                var minUsageThreshold = GetConfigParameter("MinUsageThreshold", 5);
                var lowUsageRatio = GetConfigParameter("LowUsageRatio", 0.3f); // 低使用率30%
                var cleanupRatio = GetConfigParameter("CleanupRatio", 0.2f); // 清理20%

                // 根据清理强度和内存压力调整
                var intensityFactor = CalculateIntensityFactor(context);
                minUsageThreshold = (int)(minUsageThreshold * (1.0f - intensityFactor * 0.3f));
                cleanupRatio = Math.Min(1.0f, cleanupRatio * (1.0f + intensityFactor * 0.5f));

                if (context.SystemMemoryPressure > 70)
                {
                    minUsageThreshold = Math.Max(1, minUsageThreshold / 2);
                    cleanupRatio *= 1.3f;
                }

                int cleanedItems = 0;
                long freedMemory = 0;

                // 检查是否需要基于使用频率清理
                if (ShouldCleanupByUsage(stats, minUsageThreshold, lowUsageRatio))
                {
                    cleanedItems = module.SmartCleanup();

                    // 估算释放的内存
                    var avgItemSize = stats.TotalItems > 0 ? stats.MemoryUsage / stats.TotalItems : 100;
                    freedMemory = cleanedItems * avgItemSize;
                }

                stopwatch.Stop();

                var result = CleanupResult.Success(
                    StrategyName,
                    cleanedItems,
                    freedMemory,
                    stopwatch.ElapsedMilliseconds
                );

                // 添加使用频率特定的统计信息
                result.ExtendedResults["MinUsageThreshold"] = minUsageThreshold;
                result.ExtendedResults["LowUsageRatio"] = lowUsageRatio;
                result.ExtendedResults["CleanupRatio"] = cleanupRatio;
                result.ExtendedResults["AverageUsageFrequency"] = stats.AverageUsageFrequency;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行基于使用频率的清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected override bool OnShouldCleanup(CleanupContext context)
        {
            if (context?.TargetModule == null)
                return false;

            var stats = context.CurrentStats;
            var minUsageThreshold = GetConfigParameter("MinUsageThreshold", 5);
            var lowUsageRatio = GetConfigParameter("LowUsageRatio", 0.3f);
            var hitRateThreshold = GetConfigParameter("HitRateThreshold", 0.7f);

            // 检查最小间隔
            var minInterval = TimeSpan.FromMinutes(GetConfigParameter("MinIntervalMinutes", 10));
            if (!CheckMinimumInterval(minInterval))
                return false;

            // 检查是否需要清理的条件
            // 1. 命中率低于阈值
            bool shouldCleanup = stats.HitRate < hitRateThreshold;


            // 2. 基于使用频率
            if (ShouldCleanupByUsage(stats, minUsageThreshold, lowUsageRatio))
            {
                shouldCleanup = true;
            }

            // 3. 系统内存压力
            if (context.SystemMemoryPressure > 75)
            {
                shouldCleanup = true;
            }

            // 4. 强制清理
            if (context.CleanupIntensity >= (int)CleanupIntensity.Aggressive)
            {
                shouldCleanup = true;
            }

            return shouldCleanup;
        }

        /// <summary>
        /// 创建基于使用频率的默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected override ICleanupConfiguration CreateDefaultConfiguration()
        {
            var config = new CleanupConfiguration("UsageBased");

            config.SetParameter("MinUsageThreshold", 5); // 最小使用次数阈值
            config.SetParameter("LowUsageRatio", 0.3f); // 低使用率30%
            config.SetParameter("CleanupRatio", 0.2f); // 清理20%
            config.SetParameter("HitRateThreshold", 0.7f); // 命中率阈值70%
            config.SetParameter("MinIntervalMinutes", 10); // 最小清理间隔10分钟
            config.SetParameter("EnableHitRateCheck", true); // 启用命中率检查

            return config;
        }

        #endregion

        #region 私有方法


        /// <summary>
        /// 检查是否应基于使用频率进行清理
        /// </summary>
        /// <param name="stats">缓存统计</param>
        /// <param name="minUsageThreshold">最小使用阈值</param>
        /// <param name="lowUsageRatio">低频率比例</param>
        /// <returns>是否应清理</returns>
        private bool ShouldCleanupByUsage(CacheStatistics stats, int minUsageThreshold, float lowUsageRatio)
        {
            // 如果平均使用频率很低，说明有很多低频使用的项目
            if (stats.AverageUsageFrequency < minUsageThreshold)
                return true;

            // 如果命中率低于阈值，说明缓存效果不好
            var hitRateThreshold = GetConfigParameter("HitRateThreshold", 0.7f);
            if (GetConfigParameter("EnableHitRateCheck", true) && stats.HitRate < hitRateThreshold)
                return true;

            return false;
        }

        #endregion
    }
}