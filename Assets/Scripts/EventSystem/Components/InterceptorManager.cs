using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 拦截器管理器实现 - 负责消息拦截器的管理和执行
    /// 支持链式拦截和优先级控制
    /// </summary>
    public class InterceptorManager : IInterceptorManager
    {
        #region 私有字段

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 拦截器列表，按优先级排序
        /// </summary>
        private readonly List<InterceptorWrapper> _interceptors = new List<InterceptorWrapper>();

        /// <summary>
        /// 拦截统计信息
        /// </summary>
        private readonly Dictionary<string, InterceptorStats> _interceptorStats =
            new Dictionary<string, InterceptorStats>();

        #endregion

        #region 属性

        /// <summary>
        /// 获取拦截器数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _interceptors.Count;
                }
            }
        }

        #endregion

        #region IInterceptorManager 实现

        /// <summary>
        /// 添加拦截器
        /// </summary>
        public void AddInterceptor(IMessageInterceptor interceptor)
        {
            if (interceptor == null)
                throw new ArgumentNullException(nameof(interceptor));

            lock (_lock)
            {
                // 检查是否已存在
                foreach (var existingWrapper in _interceptors)
                {
                    if (ReferenceEquals(existingWrapper.Interceptor, interceptor))
                    {
                        Debug.LogWarning($"[InterceptorManager] 拦截器已存在: {interceptor.GetType().Name}");
                        return;
                    }
                }

                var wrapper = new InterceptorWrapper(interceptor);
                _interceptors.Add(wrapper);

                // 按优先级排序（优先级高的先执行）
                _interceptors.Sort((a, b) => b.Priority.CompareTo(a.Priority));

                // 初始化统计信息
                var interceptorName = interceptor.GetType().Name;
                if (!_interceptorStats.ContainsKey(interceptorName))
                {
                    _interceptorStats[interceptorName] = new InterceptorStats(interceptorName);
                }

#if UNITY_EDITOR
                Debug.Log($"[InterceptorManager] 添加拦截器: {interceptorName} (优先级: {wrapper.Priority})");
#endif
            }
        }

        /// <summary>
        /// 移除拦截器
        /// </summary>
        public void RemoveInterceptor(IMessageInterceptor interceptor)
        {
            if (interceptor == null) return;

            lock (_lock)
            {
                for (int i = _interceptors.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(_interceptors[i].Interceptor, interceptor))
                    {
                        var wrapper = _interceptors[i];
                        _interceptors.RemoveAt(i);

#if UNITY_EDITOR
                        Debug.Log($"[InterceptorManager] 移除拦截器: {wrapper.Interceptor.GetType().Name}");
#endif
                        return;
                    }
                }

                Debug.LogWarning($"[InterceptorManager] 未找到要移除的拦截器: {interceptor.GetType().Name}");
            }
        }

        /// <summary>
        /// 检查消息是否应该被处理
        /// </summary>
        public bool ShouldProcessMessage(string tag, object[] parameters)
        {
            if (string.IsNullOrEmpty(tag))
                return true;

            lock (_lock)
            {
                if (_interceptors.Count == 0)
                    return true;

                // 按优先级顺序执行拦截器
                foreach (var wrapper in _interceptors)
                {
                    try
                    {
                        var startTime = DateTime.Now;
                        bool shouldProcess = wrapper.Interceptor.ShouldProcess(tag, parameters);
                        var duration = DateTime.Now - startTime;

                        // 更新统计信息
                        var interceptorName = wrapper.Interceptor.GetType().Name;
                        if (_interceptorStats.TryGetValue(interceptorName, out var stats))
                        {
                            stats.RecordCall(duration, shouldProcess);
                        }

                        // 如果任何一个拦截器返回false，则停止处理
                        if (!shouldProcess)
                        {
#if UNITY_EDITOR
                            Debug.Log($"[InterceptorManager] 消息被拦截 - Tag: {tag}, Interceptor: {interceptorName}");
#endif
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        var interceptorName = wrapper.Interceptor.GetType().Name;
                        Debug.LogError($"[InterceptorManager] 拦截器执行出错 - {interceptorName}: {ex}");

                        // 更新错误统计
                        if (_interceptorStats.TryGetValue(interceptorName, out var stats))
                        {
                            stats.RecordError(ex);
                        }

                        // 拦截器出错时，默认允许消息通过
                        continue;
                    }
                }

                return true; // 所有拦截器都允许通过
            }
        }

        /// <summary>
        /// 获取所有拦截器
        /// </summary>
        public IReadOnlyList<IMessageInterceptor> GetInterceptors()
        {
            lock (_lock)
            {
                var result = new List<IMessageInterceptor>(_interceptors.Count);
                foreach (var wrapper in _interceptors)
                {
                    result.Add(wrapper.Interceptor);
                }

                return new ReadOnlyCollection<IMessageInterceptor>(result);
            }
        }

        /// <summary>
        /// 清除所有拦截器
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                int count = _interceptors.Count;
                _interceptors.Clear();
                _interceptorStats.Clear();

#if UNITY_EDITOR
                Debug.Log($"[InterceptorManager] 清除了 {count} 个拦截器");
#endif
            }
        }

        #endregion

        #region 调试和统计

#if UNITY_EDITOR
        /// <summary>
        /// 获取拦截器统计信息（仅编辑器模式）
        /// </summary>
        /// <returns>统计信息字典</returns>
        public Dictionary<string, InterceptorStats> GetStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, InterceptorStats>(_interceptorStats);
            }
        }

        /// <summary>
        /// 重置统计信息（仅编辑器模式）
        /// </summary>
        public void ResetStats()
        {
            lock (_lock)
            {
                foreach (var stats in _interceptorStats.Values)
                {
                    stats.Reset();
                }
            }

            Debug.Log("[InterceptorManager] 统计信息已重置");
        }

        /// <summary>
        /// 获取拦截器管理器状态信息（仅编辑器模式）
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetStatusInfo()
        {
            lock (_lock)
            {
                var info = $"InterceptorManager Status:\n";
                info += $"- Total Interceptors: {_interceptors.Count}\n";

                if (_interceptors.Count > 0)
                {
                    info += "- Interceptors (by priority):\n";
                    foreach (var wrapper in _interceptors)
                    {
                        var name = wrapper.Interceptor.GetType().Name;
                        info += $"  * {name} (Priority: {wrapper.Priority})\n";
                    }
                }

                return info;
            }
        }

        /// <summary>
        /// 验证拦截器链的完整性（仅编辑器模式）
        /// </summary>
        /// <returns>验证结果</returns>
        public (bool isValid, List<string> issues) ValidateInterceptorChain()
        {
            var issues = new List<string>();
            bool isValid = true;

            lock (_lock)
            {
                // 检查重复的拦截器类型
                var typeCount = new Dictionary<Type, int>();
                foreach (var wrapper in _interceptors)
                {
                    var type = wrapper.Interceptor.GetType();
                    typeCount[type] = typeCount.TryGetValue(type, out var count) ? count + 1 : 1;
                }

                foreach (var kvp in typeCount)
                {
                    if (kvp.Value > 1)
                    {
                        issues.Add($"检测到重复的拦截器类型: {kvp.Key.Name} (数量: {kvp.Value})");
                        isValid = false;
                    }
                }

                // 检查优先级冲突
                var priorityGroups = new Dictionary<int, List<string>>();
                foreach (var wrapper in _interceptors)
                {
                    var priority = wrapper.Priority;
                    var name = wrapper.Interceptor.GetType().Name;

                    if (!priorityGroups.ContainsKey(priority))
                        priorityGroups[priority] = new List<string>();

                    priorityGroups[priority].Add(name);
                }

                foreach (var kvp in priorityGroups)
                {
                    if (kvp.Value.Count > 1)
                    {
                        issues.Add($"优先级冲突 ({kvp.Key}): {string.Join(", ", kvp.Value)}");
                        // 优先级冲突不算致命错误，只是警告
                    }
                }
            }

            return (isValid, issues);
        }
#endif

        #endregion

        #region 内部类型

        /// <summary>
        /// 拦截器包装类，包含拦截器和其元数据
        /// </summary>
        private class InterceptorWrapper
        {
            public IMessageInterceptor Interceptor { get; }
            public int Priority { get; }
            public DateTime AddedTime { get; }

            public InterceptorWrapper(IMessageInterceptor interceptor)
            {
                Interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
                AddedTime = DateTime.Now;

                // 尝试获取优先级（如果拦截器实现了优先级接口）
                if (interceptor is IPriorityInterceptor priorityInterceptor)
                {
                    Priority = priorityInterceptor.Priority;
                }
                else
                {
                    Priority = 0; // 默认优先级
                }
            }
        }

        /// <summary>
        /// 拦截器统计信息类
        /// </summary>
        public class InterceptorStats
        {
            public string Name { get; }
            public int TotalCalls { get; private set; }
            public int AllowedCalls { get; private set; }
            public int BlockedCalls { get; private set; }
            public int ErrorCount { get; private set; }
            public TimeSpan TotalExecutionTime { get; private set; }

            public TimeSpan AverageExecutionTime =>
                TotalCalls > 0 ? new TimeSpan(TotalExecutionTime.Ticks / TotalCalls) : TimeSpan.Zero;

            private readonly List<Exception> _recentErrors = new List<Exception>();
            private const int MAX_RECENT_ERRORS = 5;

            public InterceptorStats(string name)
            {
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }

            /// <summary>
            /// 记录拦截器调用
            /// </summary>
            /// <param name="executionTime">执行时间</param>
            /// <param name="allowed">是否允许通过</param>
            public void RecordCall(TimeSpan executionTime, bool allowed)
            {
                TotalCalls++;
                TotalExecutionTime = TotalExecutionTime.Add(executionTime);

                if (allowed)
                    AllowedCalls++;
                else
                    BlockedCalls++;
            }

            /// <summary>
            /// 记录错误
            /// </summary>
            /// <param name="exception">异常信息</param>
            public void RecordError(Exception exception)
            {
                ErrorCount++;

                if (_recentErrors.Count >= MAX_RECENT_ERRORS)
                    _recentErrors.RemoveAt(0);

                _recentErrors.Add(exception);
            }

            /// <summary>
            /// 重置统计信息
            /// </summary>
            public void Reset()
            {
                TotalCalls = 0;
                AllowedCalls = 0;
                BlockedCalls = 0;
                ErrorCount = 0;
                TotalExecutionTime = TimeSpan.Zero;
                _recentErrors.Clear();
            }

            /// <summary>
            /// 获取最近的错误列表
            /// </summary>
            /// <returns>最近的错误列表</returns>
            public IReadOnlyList<Exception> GetRecentErrors()
            {
                return new ReadOnlyCollection<Exception>(_recentErrors);
            }

            /// <summary>
            /// 转换为字符串
            /// </summary>
            public override string ToString()
            {
                var blockRate = TotalCalls > 0 ? (double)BlockedCalls / TotalCalls * 100 : 0;
                return $"{Name}: Calls={TotalCalls}, Blocked={BlockedCalls}({blockRate:F1}%), " +
                       $"Errors={ErrorCount}, AvgTime={AverageExecutionTime.TotalMilliseconds:F2}ms";
            }
        }

        /// <summary>
        /// 优先级拦截器接口，用于支持拦截器优先级
        /// </summary>
        public interface IPriorityInterceptor
        {
            /// <summary>
            /// 拦截器优先级（数值越高优先级越高）
            /// </summary>
            int Priority { get; }
        }

        #endregion
    }
}