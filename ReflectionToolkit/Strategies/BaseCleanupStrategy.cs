using System;
using System.Collections.Generic;
using ReflectionToolkit.Interfaces;
using UnityEngine;

namespace ReflectionToolkit.Strategies
{
    /// <summary>
    /// 清理策略基础抽象类 - 提供通用的清理策略实现基础
    /// </summary>
    public abstract class BaseCleanupStrategy : ICleanupStrategy
    {
        #region 属性

        /// <summary>策略名称</summary>
        public abstract string StrategyName { get; }

        /// <summary>优先级（数值越大优先级越高）</summary>
        public virtual int Priority { get; protected set; } = 100;

        /// <summary>是否启用</summary>
        public bool IsEnabled { get; set; } = true;

        #endregion

        #region 保护字段

        /// <summary>策略配置</summary>
        protected ICleanupConfiguration _configuration;

        /// <summary>上次执行时间</summary>
        protected DateTime _lastExecutionTime = DateTime.MinValue;

        /// <summary>累计清理项数</summary>
        protected long _totalCleanedItems = 0;

        /// <summary>累计执行耗时</summary>
        protected long _totalExecutionTime = 0;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数，初始化默认配置
        /// </summary>
        protected BaseCleanupStrategy()
        {
            _configuration = CreateDefaultConfiguration();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 执行清理主入口，包含通用统计与异常处理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>清理结果</returns>
        public virtual CleanupResult ExecuteCleanup(CleanupContext context)
        {
            if (!IsEnabled || context?.TargetModule == null)
            {
                return CleanupResult.Failed(StrategyName, "策略未启用或上下文无效");
            }

            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = OnExecuteCleanup(context);

                stopwatch.Stop();
                _lastExecutionTime = startTime;
                _totalCleanedItems += result.CleanedItemsCount;
                _totalExecutionTime += stopwatch.ElapsedMilliseconds;

                // 添加策略执行统计信息
                if (result.ExtendedResults == null)
                    result.ExtendedResults = new Dictionary<string, object>();

                result.ExtendedResults["StrategyPriority"] = Priority;
                result.ExtendedResults["LastExecutionTime"] = _lastExecutionTime;
                result.ExtendedResults["TotalCleanedItems"] = _totalCleanedItems;
                result.ExtendedResults["TotalExecutionTime"] = _totalExecutionTime;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Debug.LogError($"清理策略 {StrategyName} 执行失败: {ex.Message}");
                return CleanupResult.Failed(StrategyName, ex.Message);
            }
        }

        /// <summary>
        /// 判断是否应执行清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        public virtual bool ShouldCleanup(CleanupContext context)
        {
            if (!IsEnabled || context?.TargetModule == null)
                return false;

            return OnShouldCleanup(context);
        }

        /// <summary>
        /// 获取当前策略配置
        /// </summary>
        /// <returns>配置对象</returns>
        public virtual ICleanupConfiguration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// 设置策略配置
        /// </summary>
        /// <param name="configuration">配置对象</param>
        public virtual void SetConfiguration(ICleanupConfiguration configuration)
        {
            _configuration = configuration ?? CreateDefaultConfiguration();
        }

        #endregion

        #region 抽象方法 - 子类实现

        /// <summary>
        /// 子类实现：执行具体清理逻辑
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>清理结果</returns>
        protected abstract CleanupResult OnExecuteCleanup(CleanupContext context);


        /// <summary>
        /// 子类实现：判断是否应清理
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>是否应清理</returns>
        protected abstract bool OnShouldCleanup(CleanupContext context);


        /// <summary>
        /// 子类实现：创建默认配置
        /// </summary>
        /// <returns>配置对象</returns>
        protected abstract ICleanupConfiguration CreateDefaultConfiguration();

        #endregion

        #region 保护辅助方法

        /// <summary>
        /// 获取配置参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>参数值</returns>
        protected T GetConfigParameter<T>(string key, T defaultValue = default(T))
        {
            if (_configuration == null)
                return defaultValue;
            return _configuration.GetParameter<T>(key, defaultValue);
        }


        /// <summary>
        /// 设置配置参数
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数名</param>
        /// <param name="value">参数值</param>
        protected void SetConfigParameter<T>(string key, T value)
        {
            _configuration?.SetParameter(key, value);
        }


        /// <summary>
        /// 检查是否满足最小间隔要求
        /// </summary>
        /// <param name="minimumInterval">最小间隔</param>
        /// <returns>是否满足</returns>
        protected bool CheckMinimumInterval(TimeSpan minimumInterval)
        {
            return DateTime.UtcNow - _lastExecutionTime >= minimumInterval;
        }


        /// <summary>
        /// 计算清理强度系数（0.0-1.0）
        /// </summary>
        /// <param name="context">清理上下文</param>
        /// <returns>强度系数</returns>
        protected float CalculateIntensityFactor(CleanupContext context)
        {
            return Math.Min(1.0f, context.CleanupIntensity / 4.0f);
        }


        /// <summary>
        /// 根据内存压力调整阈值
        /// </summary>
        /// <param name="baseThreshold">基础阈值</param>
        /// <param name="memoryPressure">内存压力</param>
        /// <returns>调整后阈值</returns>
        protected float AdjustThresholdByMemoryPressure(float baseThreshold, int memoryPressure)
        {
            // 内存压力越高，阈值越低（更容易触发清理）
            var pressureFactor = memoryPressure / 100.0f;
            return baseThreshold * (1.0f - pressureFactor * 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// 清理配置基础实现
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

        public T GetParameter<T>(string key, T defaultValue = default(T))
        {
            if (Parameters.TryGetValue(key, out var value))
            {
                try
                {
                    if (value is T directValue)
                        return directValue;

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