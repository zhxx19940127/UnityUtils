using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 事件分发器实现 - 负责消息的分发和执行
    /// 支持优先级排序、异常处理、性能统计等功能
    /// </summary>
    public class EventDispatcher : IEventDispatcher
    {
        #region 私有字段

        /// <summary>
        /// 内存管理器引用
        /// </summary>
        private readonly IMemoryManager _memoryManager;

#if UNITY_EDITOR
        /// <summary>
        /// 统计管理器引用（仅编辑器模式）
        /// </summary>
        private readonly IEventStatistics _statistics;
#endif

        #endregion

        #region 属性

        /// <summary>
        /// 是否启用优先级排序
        /// </summary>
        public bool EnablePrioritySorting { get; set; } = true;

        /// <summary>
        /// 是否启用异常处理
        /// </summary>
        public bool EnableExceptionHandling { get; set; } = true;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryManager">内存管理器</param>
        /// <param name="statistics">统计管理器（仅编辑器模式）</param>
        public EventDispatcher(IMemoryManager memoryManager
#if UNITY_EDITOR
            , IEventStatistics statistics = null
#endif
        )
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
#if UNITY_EDITOR
            _statistics = statistics;
#endif
        }

        #endregion

        #region IEventDispatcher 实现

        /// <summary>
        /// 分发消息到指定事件列表
        /// </summary>
        public void DispatchMessage(string tag, List<MessageEvent> events, object[] parameters)
        {
            if (string.IsNullOrEmpty(tag) || events == null || events.Count == 0)
                return;

            var executeEvents = _memoryManager.GetPooledList<MessageEvent>();
            try
            {
                // 过滤和验证事件
                foreach (var messageEvent in events)
                {
                    if (messageEvent.Tag != tag) continue;
                    
                    // 验证实例是否仍然有效
                    if (messageEvent.Instance == null) continue;
                    
                    executeEvents.Add(messageEvent);
                }

                if (executeEvents.Count == 0) return;

                // 按优先级排序执行（优先级高的先执行）
                if (EnablePrioritySorting)
                {
                    executeEvents.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }

                // 执行事件
                foreach (var messageEvent in executeEvents)
                {
                    ExecuteEvent(messageEvent, parameters, tag);
                }
            }
            finally
            {
                _memoryManager.ReturnPooledList(executeEvents);
            }
        }

        /// <summary>
        /// 分发消息并收集返回值
        /// </summary>
        public List<TResult> DispatchMessageWithResult<TResult>(string tag, List<MessageEvent> events, object[] parameters)
        {
            var results = new List<TResult>();
            
            if (string.IsNullOrEmpty(tag) || events == null || events.Count == 0)
                return results;

            var executeEvents = _memoryManager.GetPooledList<MessageEvent>();
            try
            {
                // 过滤和验证事件（只处理有返回值的事件）
                foreach (var messageEvent in events)
                {
                    if (messageEvent.Tag != tag) continue;
                    if (messageEvent.Instance == null) continue;
                    if (!messageEvent.HasReturnValue) continue;
                    
                    executeEvents.Add(messageEvent);
                }

                if (executeEvents.Count == 0) return results;

                // 按优先级排序执行
                if (EnablePrioritySorting)
                {
                    executeEvents.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                }

                // 执行事件并收集返回值
                foreach (var messageEvent in executeEvents)
                {
                    var result = ExecuteEventWithResult(messageEvent, parameters, tag);
                    if (result is TResult typedResult)
                    {
                        results.Add(typedResult);
                    }
                }
            }
            finally
            {
                _memoryManager.ReturnPooledList(executeEvents);
            }

            return results;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 执行单个事件（无返回值）
        /// </summary>
        /// <param name="messageEvent">要执行的事件</param>
        /// <param name="parameters">事件参数</param>
        /// <param name="tag">消息标签</param>
        private void ExecuteEvent(MessageEvent messageEvent, object[] parameters, string tag)
        {
            try
            {
                messageEvent.Invoke(parameters);
                
#if UNITY_EDITOR
                // 记录统计信息
                _statistics?.RecordMessage(tag);
#endif
            }
            catch (Exception ex)
            {
                HandleEventException(ex, messageEvent, tag);
            }
        }

        /// <summary>
        /// 执行单个事件（有返回值）
        /// </summary>
        /// <param name="messageEvent">要执行的事件</param>
        /// <param name="parameters">事件参数</param>
        /// <param name="tag">消息标签</param>
        /// <returns>事件执行结果</returns>
        private object ExecuteEventWithResult(MessageEvent messageEvent, object[] parameters, string tag)
        {
            try
            {
                var result = messageEvent.InvokeWithResult(parameters);
                
#if UNITY_EDITOR
                // 记录统计信息
                _statistics?.RecordMessage(tag);
#endif
                
                return result;
            }
            catch (Exception ex)
            {
                HandleEventException(ex, messageEvent, tag);
                return null;
            }
        }

        /// <summary>
        /// 处理事件执行异常
        /// </summary>
        /// <param name="ex">异常对象</param>
        /// <param name="messageEvent">出错的事件</param>
        /// <param name="tag">消息标签</param>
        private void HandleEventException(Exception ex, MessageEvent messageEvent, string tag)
        {
            if (EnableExceptionHandling)
            {
                // 记录错误日志
                Debug.LogError($"[EventDispatcher] 执行事件时出错 - Tag: {tag}, " +
                              $"Instance: {messageEvent.Instance?.GetType().Name}, " +
                              $"Error: {ex.Message}");

#if UNITY_EDITOR
                // 记录错误统计
                _statistics?.RecordError(tag, ex);
#endif
            }
            else
            {
                // 如果不启用异常处理，重新抛出异常
                throw ex;
            }
        }

        #endregion

        #region 调试和诊断方法

#if UNITY_EDITOR
        /// <summary>
        /// 获取分发器配置信息（仅编辑器模式）
        /// </summary>
        /// <returns>配置信息字符串</returns>
        public string GetDispatcherInfo()
        {
            return $"EventDispatcher Configuration:\n" +
                   $"- Priority Sorting: {EnablePrioritySorting}\n" +
                   $"- Exception Handling: {EnableExceptionHandling}";
        }

        /// <summary>
        /// 验证事件列表的完整性（仅编辑器模式）
        /// </summary>
        /// <param name="events">要验证的事件列表</param>
        /// <returns>验证结果</returns>
        public (int validCount, int invalidCount, List<string> issues) ValidateEvents(List<MessageEvent> events)
        {
            int validCount = 0;
            int invalidCount = 0;
            var issues = new List<string>();

            foreach (var messageEvent in events)
            {
                if (messageEvent.Instance == null)
                {
                    invalidCount++;
                    issues.Add($"事件实例为空: Tag={messageEvent.Tag}");
                }
                else if (string.IsNullOrEmpty(messageEvent.Tag))
                {
                    invalidCount++;
                    issues.Add($"事件标签为空: Instance={messageEvent.Instance.GetType().Name}");
                }
                else
                {
                    validCount++;
                }
            }

            return (validCount, invalidCount, issues);
        }
#endif

        #endregion
    }
}