using System.Collections.Generic;

namespace ReflectionToolkit.Interfaces
{
    /// <summary>
    /// 清理策略接口，定义缓存清理的策略行为。
    /// </summary>
    public interface ICleanupStrategy
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        string StrategyName { get; }


        /// <summary>
        /// 策略优先级（数值越高，优先级越高）
        /// </summary>
        int Priority { get; }


        /// <summary>
        /// 策略是否启用
        /// </summary>
        bool IsEnabled { get; set; }


        /// <summary>
        /// 执行清理策略
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>清理结果</returns>
        CleanupResult ExecuteCleanup(CleanupContext context);


        /// <summary>
        /// 判断是否应该触发清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应该清理</returns>
        bool ShouldCleanup(CleanupContext context);


        /// <summary>
        /// 获取策略配置
        /// </summary>
        ICleanupConfiguration GetConfiguration();


        /// <summary>
        /// 设置策略配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        void SetConfiguration(ICleanupConfiguration configuration);
    }


    /// <summary>
    /// 清理上下文，包含清理所需的所有信息。
    /// </summary>
    public class CleanupContext
    {
        /// <summary>
        /// 目标缓存模块
        /// </summary>
        public ICacheModule TargetModule { get; set; }


        /// <summary>
        /// 当前缓存统计
        /// </summary>
        public CacheStatistics CurrentStats { get; set; }


        /// <summary>
        /// 系统内存压力级别 (0-100)
        /// </summary>
        public int SystemMemoryPressure { get; set; }


        /// <summary>
        /// 可用于清理决策的额外数据
        /// </summary>
        public Dictionary<string, object> ExtendedData { get; set; }


        /// <summary>
        /// 清理强度级别 (1=轻量, 2=中等, 3=积极)
        /// </summary>
        public int CleanupIntensity { get; set; }


        /// <summary>
        /// 最大允许清理项数量（0=无限制）
        /// </summary>
        public int MaxCleanupItems { get; set; }

        public CleanupContext()
        {
            ExtendedData = new Dictionary<string, object>();
            CleanupIntensity = 1;
            MaxCleanupItems = 0;
        }
    }

    /// <summary>
    /// 清理结果
    /// </summary>
    /// <summary>
    /// 清理结果结构体
    /// </summary>
    public struct CleanupResult
    {
        /// <summary>
        /// 是否执行了清理
        /// </summary>
        public bool WasExecuted;


        /// <summary>
        /// 清理的项目数量
        /// </summary>
        public int CleanedItemsCount;


        /// <summary>
        /// 释放的内存大小（字节）
        /// </summary>
        public long FreedMemoryBytes;


        /// <summary>
        /// 清理耗时（毫秒）
        /// </summary>
        public long ExecutionTimeMs;


        /// <summary>
        /// 清理策略名称
        /// </summary>
        public string StrategyName;


        /// <summary>
        /// 额外的清理信息
        /// </summary>
        public Dictionary<string, object> ExtendedResults;


        /// <summary>
        /// 错误信息（如果有）
        /// </summary>
        public string ErrorMessage;

        public static CleanupResult Success(string strategyName, int cleanedItems, long freedMemory, long executionTime)
        {
            return new CleanupResult
            {
                WasExecuted = true,
                CleanedItemsCount = cleanedItems,
                FreedMemoryBytes = freedMemory,
                ExecutionTimeMs = executionTime,
                StrategyName = strategyName,
                ExtendedResults = new Dictionary<string, object>()
            };
        }

        public static CleanupResult Failed(string strategyName, string errorMessage)
        {
            return new CleanupResult
            {
                WasExecuted = false,
                StrategyName = strategyName,
                ErrorMessage = errorMessage,
                ExtendedResults = new Dictionary<string, object>()
            };
        }
    }


    /// <summary>
    /// 清理策略配置接口
    /// </summary>
    public interface ICleanupConfiguration
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        string Name { get; }


        /// <summary>
        /// 配置参数
        /// </summary>
        Dictionary<string, object> Parameters { get; }


        /// <summary>
        /// 获取配置参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        T GetParameter<T>(string key, T defaultValue = default);


        /// <summary>
        /// 设置配置参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        void SetParameter<T>(string key, T value);
    }
}