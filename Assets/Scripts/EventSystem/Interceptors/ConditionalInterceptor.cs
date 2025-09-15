using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Interceptors
{
    /// <summary>
    /// 条件拦截器 - 支持动态启用/禁用和基于游戏状态的条件拦截
    /// 提供灵活的拦截条件配置，可根据游戏状态、时间、用户设置等进行动态拦截
    /// </summary>
    public class ConditionalInterceptor : IMessageInterceptor
    {
        #region 配置和状态

        /// <summary>
        /// 拦截器是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 游戏状态提供者
        /// </summary>
        private IGameStateProvider _gameStateProvider;

        /// <summary>
        /// 自定义条件函数字典
        /// </summary>
        private readonly Dictionary<string, Func<object[], bool>> _customConditions;

        /// <summary>
        /// 基于时间的拦截配置
        /// </summary>
        private readonly Dictionary<string, TimeBasedRule> _timeBasedRules;

        /// <summary>
        /// 被拦截的消息标签（黑名单）
        /// </summary>
        private readonly HashSet<string> _blockedMessages;

        /// <summary>
        /// 允许通过的消息标签（白名单）
        /// </summary>
        private readonly HashSet<string> _allowedMessages;

        /// <summary>
        /// 是否使用白名单模式（true：只允许白名单消息，false：阻止黑名单消息）
        /// </summary>
        public bool UseWhitelistMode { get; set; } = false;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="gameStateProvider">游戏状态提供者</param>
        public ConditionalInterceptor(IGameStateProvider gameStateProvider = null)
        {
            _gameStateProvider = gameStateProvider ?? new DefaultGameStateProvider();
            _customConditions = new Dictionary<string, Func<object[], bool>>();
            _timeBasedRules = new Dictionary<string, TimeBasedRule>();
            _blockedMessages = new HashSet<string>();
            _allowedMessages = new HashSet<string>();

            SetupDefaultConditions();
        }

        #endregion

        #region 配置方法

        /// <summary>
        /// 设置游戏状态提供者
        /// </summary>
        /// <param name="provider">状态提供者</param>
        public void SetGameStateProvider(IGameStateProvider provider)
        {
            _gameStateProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log("[ConditionalInterceptor] 游戏状态提供者已更新");
        }

        /// <summary>
        /// 添加自定义条件
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        /// <param name="condition">条件函数（返回true表示拦截）</param>
        public void AddCustomCondition(string messageTag, Func<object[], bool> condition)
        {
            if (string.IsNullOrEmpty(messageTag) || condition == null)
                return;

            _customConditions[messageTag] = condition;

            if (EnableVerboseLogging)
            {
                Debug.Log($"[ConditionalInterceptor] 添加自定义条件: {messageTag}");
            }
        }

        /// <summary>
        /// 移除自定义条件
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveCustomCondition(string messageTag)
        {
            if (string.IsNullOrEmpty(messageTag))
                return;

            _customConditions.Remove(messageTag);

            if (EnableVerboseLogging)
            {
                Debug.Log($"[ConditionalInterceptor] 移除自定义条件: {messageTag}");
            }
        }

        /// <summary>
        /// 添加基于时间的拦截规则
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        /// <param name="startHour">开始小时（0-23）</param>
        /// <param name="endHour">结束小时（0-23）</param>
        /// <param name="blockDuringTimeRange">是否在时间范围内拦截（true：范围内拦截，false：范围外拦截）</param>
        public void AddTimeBasedRule(string messageTag, int startHour, int endHour, bool blockDuringTimeRange = true)
        {
            if (string.IsNullOrEmpty(messageTag) || startHour < 0 || startHour > 23 || endHour < 0 || endHour > 23)
                return;

            _timeBasedRules[messageTag] = new TimeBasedRule
            {
                StartHour = startHour,
                EndHour = endHour,
                BlockDuringTimeRange = blockDuringTimeRange
            };

            if (EnableVerboseLogging)
            {
                var action = blockDuringTimeRange ? "拦截" : "允许";
                Debug.Log($"[ConditionalInterceptor] 添加时间规则: {messageTag} 在 {startHour}:00-{endHour}:00 期间{action}");
            }
        }

        /// <summary>
        /// 添加到黑名单
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void AddToBlocklist(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _blockedMessages.Add(messageTag);

                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 添加到黑名单: {messageTag}");
                }
            }
        }

        /// <summary>
        /// 添加到白名单
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void AddToAllowlist(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _allowedMessages.Add(messageTag);

                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 添加到白名单: {messageTag}");
                }
            }
        }

        /// <summary>
        /// 从黑名单移除
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveFromBlocklist(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _blockedMessages.Remove(messageTag);
            }
        }

        /// <summary>
        /// 从白名单移除
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveFromAllowlist(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _allowedMessages.Remove(messageTag);
            }
        }

        /// <summary>
        /// 清除所有配置
        /// </summary>
        public void ClearAllRules()
        {
            _customConditions.Clear();
            _timeBasedRules.Clear();
            _blockedMessages.Clear();
            _allowedMessages.Clear();

            Debug.Log("[ConditionalInterceptor] 已清除所有拦截规则");
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
            try
            {
                // 检查拦截器是否启用
                if (!IsEnabled)
                {
                    if (EnableVerboseLogging)
                    {
                        Debug.Log($"[ConditionalInterceptor] 拦截器已禁用，拦截消息: {tag}");
                    }

                    return false;
                }

                // 白名单模式检查
                if (UseWhitelistMode)
                {
                    if (!_allowedMessages.Contains(tag))
                    {
                        if (EnableVerboseLogging)
                        {
                            Debug.Log($"[ConditionalInterceptor] 白名单模式：消息不在允许列表中，拦截: {tag}");
                        }

                        return false;
                    }
                }
                else
                {
                    // 黑名单模式检查
                    if (_blockedMessages.Contains(tag))
                    {
                        if (EnableVerboseLogging)
                        {
                            Debug.Log($"[ConditionalInterceptor] 黑名单模式：消息在拦截列表中，拦截: {tag}");
                        }

                        return false;
                    }
                }

                // 游戏状态检查
                if (!CheckGameStateConditions(tag))
                {
                    return false;
                }

                // 时间规则检查
                if (!CheckTimeBasedRules(tag))
                {
                    return false;
                }

                // 自定义条件检查
                if (!CheckCustomConditions(tag, parameters))
                {
                    return false;
                }

                // 所有条件检查通过
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 条件检查通过，允许消息: {tag}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionalInterceptor] 条件检查过程中发生异常: {ex.Message}");
                return true; // 异常情况下允许消息通过
            }
        }

        #endregion

        #region 条件检查方法

        /// <summary>
        /// 检查游戏状态条件
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否通过检查</returns>
        private bool CheckGameStateConditions(string tag)
        {
            // 游戏暂停时拦截UI消息
            if (tag.StartsWith("UI") && _gameStateProvider.IsPaused())
            {
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 游戏暂停时拦截UI消息: {tag}");
                }

                return false;
            }

            // 加载状态时拦截游戏逻辑消息
            if (tag.StartsWith("Game") && _gameStateProvider.IsLoading())
            {
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 加载状态时拦截游戏消息: {tag}");
                }

                return false;
            }

            // 网络断开时拦截网络相关消息
            if (tag.StartsWith("Network") && !_gameStateProvider.IsNetworkConnected())
            {
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 网络断开时拦截网络消息: {tag}");
                }

                return false;
            }

            // 低电量模式时拦截高耗能操作
            if (_gameStateProvider.IsLowPowerMode() && IsHighPowerConsumptionMessage(tag))
            {
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 低电量模式时拦截高耗能消息: {tag}");
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// 检查基于时间的规则
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否通过检查</returns>
        private bool CheckTimeBasedRules(string tag)
        {
            if (!_timeBasedRules.TryGetValue(tag, out var rule))
                return true;

            var currentHour = DateTime.Now.Hour;
            bool isInTimeRange;

            if (rule.StartHour <= rule.EndHour)
            {
                // 正常时间范围（如 9-17）
                isInTimeRange = currentHour >= rule.StartHour && currentHour <= rule.EndHour;
            }
            else
            {
                // 跨日时间范围（如 22-6）
                isInTimeRange = currentHour >= rule.StartHour || currentHour <= rule.EndHour;
            }

            bool shouldBlock = rule.BlockDuringTimeRange ? isInTimeRange : !isInTimeRange;

            if (shouldBlock && EnableVerboseLogging)
            {
                Debug.Log($"[ConditionalInterceptor] 时间规则拦截消息: {tag} (当前时间: {currentHour}:xx)");
            }

            return !shouldBlock;
        }

        /// <summary>
        /// 检查自定义条件
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>是否通过检查</returns>
        private bool CheckCustomConditions(string tag, object[] parameters)
        {
            if (!_customConditions.TryGetValue(tag, out var condition))
                return true;

            try
            {
                bool shouldBlock = condition(parameters);

                if (shouldBlock && EnableVerboseLogging)
                {
                    Debug.Log($"[ConditionalInterceptor] 自定义条件拦截消息: {tag}");
                }

                return !shouldBlock;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ConditionalInterceptor] 自定义条件执行异常: {ex.Message}");
                return true; // 条件执行异常时允许通过
            }
        }

        /// <summary>
        /// 检查是否为高耗能消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否为高耗能消息</returns>
        private bool IsHighPowerConsumptionMessage(string tag)
        {
            return tag.StartsWith("Render") ||
                   tag.StartsWith("Physics") ||
                   tag.StartsWith("Animation") ||
                   tag.StartsWith("Audio") ||
                   tag.Contains("Effect") ||
                   tag.Contains("Particle");
        }

        #endregion

        #region 默认配置

        /// <summary>
        /// 设置默认条件
        /// </summary>
        private void SetupDefaultConditions()
        {
            // 添加一些常用的时间规则示例
            // 夜间模式：23:00-06:00 禁用音效
            AddTimeBasedRule("PlaySound", 23, 6, true);
            AddTimeBasedRule("PlayMusic", 23, 6, true);

            // 工作时间：09:00-17:00 允许通知
            AddTimeBasedRule("ShowNotification", 9, 17, false);

            // 添加一些默认的自定义条件
            AddCustomCondition("HighFrequencyUpdate", (parameters) =>
            {
                // 如果帧率过低，拦截高频更新消息
                return _gameStateProvider.GetFrameRate() < 30;
            });

            AddCustomCondition("MemoryIntensiveOperation", (parameters) =>
            {
                // 如果内存使用率过高，拦截内存密集型操作
                return _gameStateProvider.GetMemoryUsage() > 0.8f;
            });
        }

        #endregion

        #region 调试和统计

        /// <summary>
        /// 打印配置报告
        /// </summary>
        public void PrintConfigurationReport()
        {
            Debug.Log("=== ConditionalInterceptor 配置报告 ===");
            Debug.Log($"拦截器状态: {(IsEnabled ? "启用" : "禁用")}");
            Debug.Log($"模式: {(UseWhitelistMode ? "白名单" : "黑名单")}");
            Debug.Log($"自定义条件数量: {_customConditions.Count}");
            Debug.Log($"时间规则数量: {_timeBasedRules.Count}");
            Debug.Log($"黑名单消息数量: {_blockedMessages.Count}");
            Debug.Log($"白名单消息数量: {_allowedMessages.Count}");

            var gameState = _gameStateProvider.GetCurrentState();
            Debug.Log($"当前游戏状态: {gameState}");
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 基于时间的规则配置
        /// </summary>
        private class TimeBasedRule
        {
            public int StartHour { get; set; }
            public int EndHour { get; set; }
            public bool BlockDuringTimeRange { get; set; }
        }

        #endregion
    }

    #region 游戏状态提供者接口和默认实现

    /// <summary>
    /// 游戏状态提供者接口
    /// </summary>
    public interface IGameStateProvider
    {
        /// <summary>
        /// 游戏是否暂停
        /// </summary>
        bool IsPaused();

        /// <summary>
        /// 游戏是否在加载中
        /// </summary>
        bool IsLoading();

        /// <summary>
        /// 网络是否连接
        /// </summary>
        bool IsNetworkConnected();

        /// <summary>
        /// 是否为低电量模式
        /// </summary>
        bool IsLowPowerMode();

        /// <summary>
        /// 获取当前帧率
        /// </summary>
        float GetFrameRate();

        /// <summary>
        /// 获取内存使用率（0-1）
        /// </summary>
        float GetMemoryUsage();

        /// <summary>
        /// 获取当前游戏状态描述
        /// </summary>
        string GetCurrentState();
    }

    /// <summary>
    /// 默认游戏状态提供者实现
    /// </summary>
    public class DefaultGameStateProvider : IGameStateProvider
    {
        public bool IsPaused() => Time.timeScale == 0f;

        public bool IsLoading() => false; // 默认不在加载状态

        public bool IsNetworkConnected() => Application.internetReachability != NetworkReachability.NotReachable;

        public bool IsLowPowerMode() => SystemInfo.batteryLevel < 0.2f && SystemInfo.batteryLevel > 0f;

        public float GetFrameRate() => 1f / Time.deltaTime;

        public float GetMemoryUsage()
        {
            // 简单的内存使用率估算
            var usedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            var totalMemory = SystemInfo.systemMemorySize * 1024L * 1024L; // MB to bytes
            return (float)usedMemory / totalMemory;
        }

        public string GetCurrentState()
        {
            var states = new List<string>();

            if (IsPaused()) states.Add("暂停");
            if (IsLoading()) states.Add("加载中");
            if (!IsNetworkConnected()) states.Add("网络断开");
            if (IsLowPowerMode()) states.Add("低电量");

            return states.Count > 0 ? string.Join(", ", states) : "正常";
        }
    }

    #endregion
}