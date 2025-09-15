#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 事件统计管理器实现 - 负责统计和调试信息的收集（仅编辑器模式）
    /// </summary>
    public class EventStatistics : IEventStatistics
    {
        #region 私有字段

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 消息统计信息，记录每个消息标签的调用次数
        /// Key: 消息标签, Value: 调用次数
        /// </summary>
        private readonly Dictionary<string, int> _messageStats = new Dictionary<string, int>();

        /// <summary>
        /// 错误历史记录，记录每个消息标签的错误信息
        /// Key: 消息标签, Value: 错误异常列表
        /// </summary>
        private readonly Dictionary<string, List<Exception>> _errorHistory = 
            new Dictionary<string, List<Exception>>();

        /// <summary>
        /// 错误历史记录的最大条数
        /// </summary>
        private int _maxErrorHistory = 10;

        /// <summary>
        /// 消息调用时间统计
        /// Key: 消息标签, Value: 调用时间列表
        /// </summary>
        private readonly Dictionary<string, List<DateTime>> _messageTimings = 
            new Dictionary<string, List<DateTime>>();

        /// <summary>
        /// 最大时间记录数量
        /// </summary>
        private const int MAX_TIMING_RECORDS = 100;

        /// <summary>
        /// 统计开始时间
        /// </summary>
        private readonly DateTime _startTime = DateTime.Now;

        #endregion

        #region IEventStatistics 实现

        /// <summary>
        /// 记录消息调用
        /// </summary>
        public void RecordMessage(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;

            lock (_lock)
            {
                // 更新调用计数
                if (_messageStats.ContainsKey(tag))
                    _messageStats[tag]++;
                else
                    _messageStats[tag] = 1;

                // 记录调用时间
                if (!_messageTimings.ContainsKey(tag))
                    _messageTimings[tag] = new List<DateTime>();

                var timings = _messageTimings[tag];
                timings.Add(DateTime.Now);

                // 限制时间记录数量
                if (timings.Count > MAX_TIMING_RECORDS)
                    timings.RemoveAt(0);
            }
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        public void RecordError(string tag, Exception exception)
        {
            if (string.IsNullOrEmpty(tag) || exception == null) return;

            lock (_lock)
            {
                if (!_errorHistory.ContainsKey(tag))
                    _errorHistory[tag] = new List<Exception>();

                var errors = _errorHistory[tag];
                errors.Add(exception);

                // 保持错误历史在合理范围内
                if (errors.Count > _maxErrorHistory)
                    errors.RemoveAt(0);
            }

            // 同时输出到Unity控制台
            Debug.LogError($"[EventStatistics] Tag: {tag}, Error: {exception}");
        }

        /// <summary>
        /// 获取消息统计
        /// </summary>
        public Dictionary<string, int> GetMessageStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_messageStats);
            }
        }

        /// <summary>
        /// 获取错误历史
        /// </summary>
        public Dictionary<string, List<Exception>> GetErrorHistory()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, List<Exception>>();
                foreach (var kvp in _errorHistory)
                {
                    result[kvp.Key] = new List<Exception>(kvp.Value);
                }
                return result;
            }
        }

        /// <summary>
        /// 清空所有统计信息
        /// </summary>
        public void ClearStats()
        {
            lock (_lock)
            {
                _messageStats.Clear();
                _errorHistory.Clear();
                _messageTimings.Clear();
            }

            Debug.Log("[EventStatistics] 统计信息已清空");
        }

        /// <summary>
        /// 设置错误历史记录的最大数量
        /// </summary>
        public void SetMaxErrorHistory(int maxCount)
        {
            if (maxCount <= 0)
                throw new ArgumentException("最大错误历史数量必须大于0", nameof(maxCount));

            lock (_lock)
            {
                _maxErrorHistory = maxCount;

                // 如果当前记录超过新的限制，进行裁剪
                foreach (var errors in _errorHistory.Values)
                {
                    while (errors.Count > _maxErrorHistory)
                    {
                        errors.RemoveAt(0);
                    }
                }
            }
        }

        #endregion

        #region 扩展统计功能

        /// <summary>
        /// 获取详细的统计报告
        /// </summary>
        /// <returns>统计报告字符串</returns>
        public string GetDetailedReport()
        {
            lock (_lock)
            {
                var report = "=== Event System Statistics Report ===\n";
                report += $"Statistics Period: {_startTime:yyyy-MM-dd HH:mm:ss} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                report += $"Total Message Types: {_messageStats.Count}\n";
                report += $"Total Error Types: {_errorHistory.Count}\n\n";

                // 最活跃的消息
                report += "Top 10 Most Active Messages:\n";
                var sortedMessages = new List<KeyValuePair<string, int>>(_messageStats);
                sortedMessages.Sort((a, b) => b.Value.CompareTo(a.Value));

                for (int i = 0; i < Math.Min(10, sortedMessages.Count); i++)
                {
                    var kvp = sortedMessages[i];
                    report += $"  {i + 1}. {kvp.Key}: {kvp.Value} calls\n";
                }

                // 错误统计
                if (_errorHistory.Count > 0)
                {
                    report += "\nError Summary:\n";
                    foreach (var kvp in _errorHistory)
                    {
                        report += $"  {kvp.Key}: {kvp.Value.Count} errors\n";
                    }
                }

                return report;
            }
        }

        /// <summary>
        /// 获取消息调用频率分析
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>频率分析结果</returns>
        public MessageFrequencyAnalysis GetMessageFrequency(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return new MessageFrequencyAnalysis();

            lock (_lock)
            {
                if (!_messageTimings.TryGetValue(tag, out var timings) || timings.Count == 0)
                    return new MessageFrequencyAnalysis();

                var analysis = new MessageFrequencyAnalysis
                {
                    Tag = tag,
                    TotalCalls = timings.Count,
                    FirstCall = timings[0],
                    LastCall = timings[timings.Count - 1]
                };

                if (timings.Count > 1)
                {
                    var intervals = new List<TimeSpan>();
                    for (int i = 1; i < timings.Count; i++)
                    {
                        intervals.Add(timings[i] - timings[i - 1]);
                    }

                    var totalInterval = intervals.Select(ts => ts.TotalMilliseconds).Sum();
                    analysis.AverageInterval = TimeSpan.FromMilliseconds(totalInterval / intervals.Count);

                    analysis.MinInterval = TimeSpan.FromMilliseconds(intervals.Select(ts => ts.TotalMilliseconds).Min());
                    analysis.MaxInterval = TimeSpan.FromMilliseconds(intervals.Select(ts => ts.TotalMilliseconds).Max());
                }

                return analysis;
            }
        }

        /// <summary>
        /// 获取错误模式分析
        /// </summary>
        /// <returns>错误模式分析结果</returns>
        public ErrorPatternAnalysis GetErrorPatterns()
        {
            lock (_lock)
            {
                var analysis = new ErrorPatternAnalysis();
                var exceptionTypes = new Dictionary<string, int>();
                var errorsByHour = new Dictionary<int, int>();

                foreach (var kvp in _errorHistory)
                {
                    foreach (var exception in kvp.Value)
                    {
                        // 统计异常类型
                        var exceptionType = exception.GetType().Name;
                        exceptionTypes[exceptionType] = exceptionTypes.TryGetValue(exceptionType, out var count) 
                            ? count + 1 : 1;

                        // 统计错误发生时间分布（按小时）
                        // 注意：这里简化处理，实际应该记录错误发生的具体时间
                        var hour = DateTime.Now.Hour;
                        errorsByHour[hour] = errorsByHour.TryGetValue(hour, out var hourCount) 
                            ? hourCount + 1 : 1;
                    }
                }

                analysis.ExceptionTypeCounts = exceptionTypes;
                analysis.ErrorsByHour = errorsByHour;
                analysis.TotalErrors = exceptionTypes.Values.Sum();

                return analysis;
            }
        }

        #endregion

        #region 数据结构

        /// <summary>
        /// 消息频率分析结果
        /// </summary>
        public struct MessageFrequencyAnalysis
        {
            public string Tag;
            public int TotalCalls;
            public DateTime FirstCall;
            public DateTime LastCall;
            public TimeSpan AverageInterval;
            public TimeSpan MinInterval;
            public TimeSpan MaxInterval;

            public override string ToString()
            {
                return $"Message Frequency Analysis for '{Tag}':\n" +
                       $"- Total Calls: {TotalCalls}\n" +
                       $"- Time Range: {FirstCall:HH:mm:ss} - {LastCall:HH:mm:ss}\n" +
                       $"- Average Interval: {AverageInterval.TotalMilliseconds:F2}ms\n" +
                       $"- Min Interval: {MinInterval.TotalMilliseconds:F2}ms\n" +
                       $"- Max Interval: {MaxInterval.TotalMilliseconds:F2}ms";
            }
        }

        /// <summary>
        /// 错误模式分析结果
        /// </summary>
        public struct ErrorPatternAnalysis
        {
            public Dictionary<string, int> ExceptionTypeCounts;
            public Dictionary<int, int> ErrorsByHour;
            public int TotalErrors;

            public override string ToString()
            {
                var result = $"Error Pattern Analysis:\n";
                result += $"- Total Errors: {TotalErrors}\n";

                if (ExceptionTypeCounts?.Count > 0)
                {
                    result += "- Exception Types:\n";
                    foreach (var kvp in ExceptionTypeCounts)
                    {
                        result += $"  * {kvp.Key}: {kvp.Value}\n";
                    }
                }

                return result;
            }
        }

        #endregion
    }
}
#endif