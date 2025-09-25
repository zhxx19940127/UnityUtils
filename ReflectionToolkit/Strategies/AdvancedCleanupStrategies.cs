using System;
using ReflectionToolkit.Interfaces;

namespace ReflectionToolkit.Strategies
{
    /// <summary>
    /// 内存压力清理策略 - 基于系统内存压力的智能清理
    /// </summary>
    /// <summary>
    /// 内存压力清理策略，基于系统内存压力的智能清理。
    /// </summary>
    public class MemoryPressureCleanupStrategy : BaseCleanupStrategy
    {
        #region 属性

        /// <summary>策略名称</summary>
        public override string StrategyName => "MemoryPressure";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，设置优先级
        /// </summary>
        public MemoryPressureCleanupStrategy()
        {
            Priority = 300; // 最高优先级
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 执行内存压力清理逻辑
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
                var memoryPressure = context.SystemMemoryPressure;

                // 根据内存压力确定清理强度
                var cleanupIntensity = DetermineCleanupIntensity(memoryPressure);
                var cleanupRatio = CalculateCleanupRatio(memoryPressure);

                int cleanedItems = 0;
                long freedMemory = 0;

                // 执行清理
                if (cleanupIntensity > 0)
                {
                    // 可能需要多轮清理
                    var maxRounds = GetConfigParameter("MaxCleanupRounds", 3);
                    for (int round = 0; round < maxRounds && round < cleanupIntensity; round++)
                    {
                        var roundCleaned = module.SmartCleanup();
                        cleanedItems += roundCleaned;

                        if (roundCleaned == 0) break; // 没有更多可清理的项目
                    }

                    // 估算释放的内存
                    var avgItemSize = stats.TotalItems > 0 ? stats.MemoryUsage / stats.TotalItems : 150;
                    freedMemory = cleanedItems * avgItemSize;
                }

                stopwatch.Stop();

                var result = CleanupResult.Success(
                    StrategyName,
                    cleanedItems,
                    freedMemory,
                    stopwatch.ElapsedMilliseconds
                );

                // 添加内存压力特定的统计信息
                result.ExtendedResults["MemoryPressure"] = memoryPressure;
                result.ExtendedResults["CleanupIntensity"] = cleanupIntensity;
                result.ExtendedResults["CleanupRatio"] = cleanupRatio;
                result.ExtendedResults["CleanupRounds"] =
                    Math.Min(cleanupIntensity, GetConfigParameter("MaxCleanupRounds", 3));

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行内存压力清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected override bool OnShouldCleanup(CleanupContext context)
        {
            if (context?.TargetModule == null)
                return false;

            var memoryPressure = context.SystemMemoryPressure;
            var pressureThreshold = GetConfigParameter("MemoryPressureThreshold", 60);

            // 检查最小间隔（内存压力高时减少间隔）
            var baseInterval = GetConfigParameter("BaseIntervalMinutes", 2);
            var adjustedInterval = Math.Max(0.5, baseInterval * (100 - memoryPressure) / 100.0);
            var minInterval = TimeSpan.FromMinutes(adjustedInterval);

            if (!CheckMinimumInterval(minInterval))
                return false;

            // 内存压力超过阈值就应该清理
            return memoryPressure >= pressureThreshold;
        }

        /// <summary>
        /// 创建内存压力清理默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected override ICleanupConfiguration CreateDefaultConfiguration()
        {
            var config = new CleanupConfiguration("MemoryPressure");

            config.SetParameter("MemoryPressureThreshold", 60); // 内存压力阈值60%
            config.SetParameter("BaseIntervalMinutes", 2); // 基础清理间隔2分钟
            config.SetParameter("MaxCleanupRounds", 3); // 最大清理轮数
            config.SetParameter("EnableAdaptiveInterval", true); // 启用自适应间隔
            config.SetParameter("EmergencyThreshold", 90); // 紧急清理阈值90%

            return config;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 根据内存压力确定清理强度（1-4级）
        /// </summary>
        /// <summary>
        /// 根据内存压力确定清理强度（1-4级）
        /// </summary>
        /// <param name="memoryPressure">内存压力</param>
        /// <returns>清理强度</returns>
        private int DetermineCleanupIntensity(int memoryPressure)
        {
            var emergencyThreshold = GetConfigParameter("EmergencyThreshold", 90);

            if (memoryPressure >= emergencyThreshold)
                return 4; // 紧急清理
            if (memoryPressure >= 80)
                return 3; // 积极清理
            if (memoryPressure >= 70)
                return 2; // 正常清理
            if (memoryPressure >= 60)
                return 1; // 轻量清理

            return 0; // 不清理
        }

        /// <summary>
        /// 计算清理比例
        /// </summary>
        /// <summary>
        /// 计算清理比例（内存压力越高，比例越大）
        /// </summary>
        /// <param name="memoryPressure">内存压力</param>
        /// <returns>清理比例</returns>
        private float CalculateCleanupRatio(int memoryPressure)
        {
            // 内存压力越高，清理比例越大
            return Math.Min(0.8f, (memoryPressure - 50) / 50.0f * 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// 定时清理策略 - 基于固定时间间隔的清理
    /// </summary>
    /// <summary>
    /// 定时清理策略，基于固定时间间隔的清理。
    /// </summary>
    public class ScheduledCleanupStrategy : BaseCleanupStrategy
    {
        #region 属性

        /// <summary>策略名称</summary>
        public override string StrategyName => "Scheduled";

        #endregion

        #region 私有字段

        /// <summary>下次调度时间</summary>
        private DateTime _nextScheduledTime = DateTime.MinValue;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，设置优先级
        /// </summary>
        public ScheduledCleanupStrategy()
        {
            Priority = 50; // 低优先级
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 执行定时清理逻辑
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

                // 执行轻量级清理
                var cleanedItems = module.SmartCleanup();

                // 计算下次调度时间
                var interval = TimeSpan.FromMinutes(GetConfigParameter("ScheduleIntervalMinutes", 30));
                _nextScheduledTime = DateTime.UtcNow + interval;

                // 估算释放的内存
                var avgItemSize = stats.TotalItems > 0 ? stats.MemoryUsage / stats.TotalItems : 100;
                var freedMemory = cleanedItems * avgItemSize;

                stopwatch.Stop();

                var result = CleanupResult.Success(
                    StrategyName,
                    cleanedItems,
                    freedMemory,
                    stopwatch.ElapsedMilliseconds
                );

                // 添加调度特定的统计信息
                result.ExtendedResults["NextScheduledTime"] = _nextScheduledTime;
                result.ExtendedResults["ScheduleInterval"] = interval.TotalMinutes;
                result.ExtendedResults["IsRegularSchedule"] = true;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行定时清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected override bool OnShouldCleanup(CleanupContext context)
        {
            if (context?.TargetModule == null)
                return false;

            // 检查是否启用定时清理
            if (!GetConfigParameter("EnableScheduledCleanup", true))
                return false;

            // 初始化下次调度时间
            if (_nextScheduledTime == DateTime.MinValue)
            {
                var interval = TimeSpan.FromMinutes(GetConfigParameter("ScheduleIntervalMinutes", 30));
                _nextScheduledTime = DateTime.UtcNow + interval;
                return false;
            }

            // 检查是否到了调度时间
            var currentTime = DateTime.UtcNow;
            if (currentTime >= _nextScheduledTime)
            {
                return true;
            }

            // 如果是强制清理，也执行
            return context.CleanupIntensity >= (int)CleanupIntensity.Force;
        }

        /// <summary>
        /// 创建定时清理默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected override ICleanupConfiguration CreateDefaultConfiguration()
        {
            var config = new CleanupConfiguration("Scheduled");

            config.SetParameter("ScheduleIntervalMinutes", 30); // 调度间隔30分钟
            config.SetParameter("EnableScheduledCleanup", true); // 启用定时清理
            config.SetParameter("CleanupAtStartup", false); // 启动时不清理

            return config;
        }

        #endregion
    }

    /// <summary>
    /// 自适应清理策略 - 根据缓存性能自动调整清理策略
    /// </summary>
    /// <summary>
    /// 自适应清理策略，根据缓存性能自动调整清理强度。
    /// </summary>
    public class AdaptiveCleanupStrategy : BaseCleanupStrategy
    {
        #region 属性

        public override string StrategyName => "Adaptive";

        #endregion

        #region 私有字段

        /// <summary>上次命中率</summary>
        private float _previousHitRate = 0f;

        /// <summary>性能趋势（-1:下降, 0:稳定, 1:上升）</summary>
        private int _performanceTrend = 0;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，设置优先级
        /// </summary>
        public AdaptiveCleanupStrategy()
        {
            Priority = 175; // 中高优先级
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 执行自适应清理逻辑
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

                // 分析性能趋势
                AnalyzePerformanceTrend(stats.HitRate);

                // 根据趋势调整清理策略
                var cleanupIntensity = DetermineAdaptiveIntensity(stats);
                var cleanedItems = 0;

                if (cleanupIntensity > 0)
                {
                    for (int i = 0; i < cleanupIntensity; i++)
                    {
                        var roundCleaned = module.SmartCleanup();
                        cleanedItems += roundCleaned;
                        if (roundCleaned == 0) break;
                    }
                }

                // 估算释放的内存
                var avgItemSize = stats.TotalItems > 0 ? stats.MemoryUsage / stats.TotalItems : 120;
                var freedMemory = cleanedItems * avgItemSize;

                stopwatch.Stop();

                var result = CleanupResult.Success(
                    StrategyName,
                    cleanedItems,
                    freedMemory,
                    stopwatch.ElapsedMilliseconds
                );

                // 添加自适应特定的统计信息
                result.ExtendedResults["PerformanceTrend"] = _performanceTrend;
                result.ExtendedResults["PreviousHitRate"] = _previousHitRate;
                result.ExtendedResults["CurrentHitRate"] = stats.HitRate;
                result.ExtendedResults["AdaptiveIntensity"] = cleanupIntensity;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行自适应清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected override bool OnShouldCleanup(CleanupContext context)
        {
            if (context?.TargetModule == null)
                return false;

            var stats = context.CurrentStats;
            var minInterval = TimeSpan.FromMinutes(GetConfigParameter("MinIntervalMinutes", 8));

            if (!CheckMinimumInterval(minInterval))
                return false;

            // 分析当前性能
            AnalyzePerformanceTrend(stats.HitRate);

            // 性能下降时需要清理
            if (_performanceTrend < 0)
                return true;

            // 命中率过低时需要清理
            if (stats.HitRate < GetConfigParameter("MinHitRate", 0.6f))
                return true;

            // 平均使用频率过低时需要清理
            if (stats.AverageUsageFrequency < GetConfigParameter("MinUsageFrequency", 3.0f))
                return true;

            return false;
        }

        /// <summary>
        /// 创建自适应清理默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected override ICleanupConfiguration CreateDefaultConfiguration()
        {
            var config = new CleanupConfiguration("Adaptive");

            config.SetParameter("MinIntervalMinutes", 8); // 最小间隔8分钟
            config.SetParameter("MinHitRate", 0.6f); // 最小命中率60%
            config.SetParameter("MinUsageFrequency", 3.0f); // 最小使用频率
            config.SetParameter("TrendSensitivity", 0.05f); // 趋势敏感度5%
            config.SetParameter("MaxAdaptiveIntensity", 3); // 最大自适应强度

            return config;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 分析性能趋势
        /// </summary>
        /// <summary>
        /// 分析缓存命中率趋势
        /// </summary>
        /// <param name="currentHitRate">当前命中率</param>
        private void AnalyzePerformanceTrend(float currentHitRate)
        {
            if (_previousHitRate == 0f)
            {
                _previousHitRate = currentHitRate;
                _performanceTrend = 0;
                return;
            }

            var sensitivity = GetConfigParameter("TrendSensitivity", 0.05f);
            var difference = currentHitRate - _previousHitRate;

            if (Math.Abs(difference) < sensitivity)
            {
                _performanceTrend = 0; // 稳定
            }
            else if (difference > 0)
            {
                _performanceTrend = 1; // 上升
            }
            else
            {
                _performanceTrend = -1; // 下降
            }

            _previousHitRate = currentHitRate;
        }

        /// <summary>
        /// 确定自适应清理强度
        /// </summary>
        /// <summary>
        /// 确定自适应清理强度
        /// </summary>
        /// <param name="stats">缓存统计</param>
        /// <returns>清理强度</returns>
        private int DetermineAdaptiveIntensity(CacheStatistics stats)
        {
            var maxIntensity = GetConfigParameter("MaxAdaptiveIntensity", 3);

            // 性能下降时增加清理强度
            if (_performanceTrend < 0)
            {
                return Math.Min(maxIntensity, 2);
            }

            // 命中率很低时积极清理
            if (stats.HitRate < 0.5f)
            {
                return maxIntensity;
            }

            // 使用频率低时轻度清理
            if (stats.AverageUsageFrequency < 2.0f)
            {
                return 1;
            }

            return 0;
        }

        #endregion
    }
}