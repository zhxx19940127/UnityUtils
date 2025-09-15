using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Interceptors
{
    /// <summary>
    /// 频率限制拦截器 - 防止消息发送频率过快，避免系统过载
    /// 支持全局频率限制和针对特定消息的个性化频率控制
    /// </summary>
    public class RateLimitInterceptor : IMessageInterceptor
    {
        #region 配置参数
        
        /// <summary>
        /// 默认最小时间间隔（毫秒）
        /// </summary>
        private readonly TimeSpan _defaultMinInterval;
        
        /// <summary>
        /// 特定消息的时间间隔配置
        /// </summary>
        private readonly Dictionary<string, TimeSpan> _customIntervals;
        
        /// <summary>
        /// 消息最后调用时间记录
        /// </summary>
        private readonly Dictionary<string, DateTime> _lastCallTime;
        
        /// <summary>
        /// 消息调用次数统计
        /// </summary>
        private readonly Dictionary<string, int> _callCounts;
        
        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();
        
        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数 - 使用默认配置
        /// </summary>
        /// <param name="defaultIntervalMs">默认最小间隔（毫秒），默认100ms</param>
        public RateLimitInterceptor(int defaultIntervalMs = 100)
        {
            _defaultMinInterval = TimeSpan.FromMilliseconds(defaultIntervalMs);
            _customIntervals = new Dictionary<string, TimeSpan>();
            _lastCallTime = new Dictionary<string, DateTime>();
            _callCounts = new Dictionary<string, int>();
            
            // 设置常见消息的默认频率限制
            SetupDefaultRateRules();
        }
        
        /// <summary>
        /// 构造函数 - 使用自定义配置
        /// </summary>
        /// <param name="defaultIntervalMs">默认最小间隔（毫秒）</param>
        /// <param name="customRules">自定义频率规则</param>
        public RateLimitInterceptor(int defaultIntervalMs, Dictionary<string, int> customRules) 
            : this(defaultIntervalMs)
        {
            if (customRules != null)
            {
                foreach (var rule in customRules)
                {
                    SetCustomInterval(rule.Key, rule.Value);
                }
            }
        }
        
        #endregion
        
        #region 配置方法
        
        /// <summary>
        /// 设置特定消息的频率限制
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        /// <param name="intervalMs">最小时间间隔（毫秒）</param>
        public void SetCustomInterval(string messageTag, int intervalMs)
        {
            if (string.IsNullOrEmpty(messageTag) || intervalMs < 0)
                return;
                
            lock (_lock)
            {
                _customIntervals[messageTag] = TimeSpan.FromMilliseconds(intervalMs);
            }
            
            if (EnableVerboseLogging)
            {
                Debug.Log($"[RateLimitInterceptor] 设置消息 '{messageTag}' 的频率限制为 {intervalMs}ms");
            }
        }
        
        /// <summary>
        /// 移除特定消息的频率限制
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveCustomInterval(string messageTag)
        {
            if (string.IsNullOrEmpty(messageTag))
                return;
                
            lock (_lock)
            {
                _customIntervals.Remove(messageTag);
                _lastCallTime.Remove(messageTag);
                _callCounts.Remove(messageTag);
            }
            
            if (EnableVerboseLogging)
            {
                Debug.Log($"[RateLimitInterceptor] 移除消息 '{messageTag}' 的频率限制");
            }
        }
        
        /// <summary>
        /// 清除所有记录
        /// </summary>
        public void ClearAllRecords()
        {
            lock (_lock)
            {
                _lastCallTime.Clear();
                _callCounts.Clear();
            }
            
            Debug.Log("[RateLimitInterceptor] 已清除所有频率记录");
        }
        
        #endregion
        
        #region IMessageInterceptor 实现
        
        /// <summary>
        /// 判断是否应该处理该消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>true表示继续处理，false表示拦截</returns>
        public bool ShouldProcess(string tag, object[] parameters)
        {
            if (string.IsNullOrEmpty(tag))
                return true;
                
            try
            {
                lock (_lock)
                {
                    var now = DateTime.Now;
                    
                    // 获取该消息的频率限制
                    var minInterval = GetMessageInterval(tag);
                    
                    // 检查上次调用时间
                    if (_lastCallTime.TryGetValue(tag, out var lastTime))
                    {
                        var timeSinceLastCall = now - lastTime;
                        
                        if (timeSinceLastCall < minInterval)
                        {
                            // 频率过快，拦截消息
                            var remainingTime = minInterval - timeSinceLastCall;
                            
                            if (EnableVerboseLogging)
                            {
                                Debug.LogWarning($"[RateLimitInterceptor] 消息频率过快，已拦截: '{tag}' " +
                                               $"(距离上次调用: {timeSinceLastCall.TotalMilliseconds:F1}ms, " +
                                               $"最小间隔: {minInterval.TotalMilliseconds}ms, " +
                                               $"还需等待: {remainingTime.TotalMilliseconds:F1}ms)");
                            }
                            
                            return false;
                        }
                    }
                    
                    // 更新调用记录
                    _lastCallTime[tag] = now;
                    
                    // 更新调用计数
                    if (_callCounts.TryGetValue(tag, out var count))
                    {
                        _callCounts[tag] = count + 1;
                    }
                    else
                    {
                        _callCounts[tag] = 1;
                    }
                    
                    if (EnableVerboseLogging)
                    {
                        Debug.Log($"[RateLimitInterceptor] 消息通过频率检查: '{tag}' (第{_callCounts[tag]}次调用)");
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RateLimitInterceptor] 频率检查过程中发生异常: {ex.Message}");
                return true; // 异常情况下允许消息通过
            }
        }
        
        #endregion
        
        #region 统计和调试
        
        /// <summary>
        /// 获取消息调用统计
        /// </summary>
        /// <returns>消息调用次数字典</returns>
        public Dictionary<string, int> GetCallStatistics()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_callCounts);
            }
        }
        
        /// <summary>
        /// 获取当前所有频率限制规则
        /// </summary>
        /// <returns>频率限制规则字典</returns>
        public Dictionary<string, double> GetRateRules()
        {
            lock (_lock)
            {
                var rules = new Dictionary<string, double>();
                
                foreach (var rule in _customIntervals)
                {
                    rules[rule.Key] = rule.Value.TotalMilliseconds;
                }
                
                return rules;
            }
        }
        
        /// <summary>
        /// 打印频率统计报告
        /// </summary>
        public void PrintStatisticsReport()
        {
            lock (_lock)
            {
                Debug.Log("=== RateLimitInterceptor 统计报告 ===");
                Debug.Log($"默认频率限制: {_defaultMinInterval.TotalMilliseconds}ms");
                Debug.Log($"自定义规则数量: {_customIntervals.Count}");
                Debug.Log($"已记录消息数量: {_callCounts.Count}");
                
                if (_callCounts.Count > 0)
                {
                    Debug.Log("--- 消息调用统计 ---");
                    foreach (var stat in _callCounts)
                    {
                        var interval = GetMessageInterval(stat.Key).TotalMilliseconds;
                        Debug.Log($"  {stat.Key}: {stat.Value}次调用, 频率限制: {interval}ms");
                    }
                }
            }
        }
        
        #endregion
        
        #region 私有方法
        
        /// <summary>
        /// 获取指定消息的时间间隔
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        /// <returns>时间间隔</returns>
        private TimeSpan GetMessageInterval(string messageTag)
        {
            return _customIntervals.TryGetValue(messageTag, out var customInterval) 
                ? customInterval 
                : _defaultMinInterval;
        }
        
        /// <summary>
        /// 设置默认的频率限制规则
        /// </summary>
        private void SetupDefaultRateRules()
        {
            // UI相关消息 - 较短间隔
            SetCustomInterval("UI_Click", 50);
            SetCustomInterval("UI_Hover", 16); // 60fps
            SetCustomInterval("UI_Input", 50);
            
            // 网络相关消息 - 中等间隔
            SetCustomInterval("NetworkRequest", 200);
            SetCustomInterval("SendChatMessage", 1000); // 1秒
            SetCustomInterval("SendHeartbeat", 5000); // 5秒
            
            // 数据保存相关 - 较长间隔
            SetCustomInterval("SaveData", 1000);
            SetCustomInterval("SaveConfig", 2000);
            SetCustomInterval("AutoSave", 10000); // 10秒
            
            // 音频相关消息 - 短间隔
            SetCustomInterval("PlaySound", 10);
            SetCustomInterval("PlayMusic", 100);
            
            // 游戏逻辑相关 - 中等间隔
            SetCustomInterval("UpdateScore", 100);
            SetCustomInterval("CheckAchievement", 500);
            SetCustomInterval("SyncPlayerState", 1000);
        }
        
        #endregion
    }
}