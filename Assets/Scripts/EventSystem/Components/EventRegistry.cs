using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 事件注册管理器实现 - 负责事件的注册、注销和管理
    /// 使用弱引用避免内存泄漏，支持线程安全操作
    /// </summary>
    public class EventRegistry : IEventRegistry
    {
        #region 私有字段

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 静态类型缓存锁
        /// </summary>
        private static readonly object _staticLock = new object();

        /// <summary>
        /// 类型到事件方法映射的缓存
        /// Key: 对象类型, Value: 该类型中所有可注册的事件方法列表
        /// </summary>
        private static readonly Dictionary<Type, List<MessageEvent>> _classType2Methods =
            new Dictionary<Type, List<MessageEvent>>();

        /// <summary>
        /// 实例到事件方法的映射，使用弱引用防止内存泄漏
        /// Key: 对象的弱引用, Value: 该对象注册的所有事件方法
        /// </summary>
        private readonly Dictionary<WeakReference<object>, List<MessageEvent>> _subscribeInstance2Methods =
            new Dictionary<WeakReference<object>, List<MessageEvent>>(new WeakReferenceComparer());

        /// <summary>
        /// 标签到事件方法的映射，消息分发的核心数据结构
        /// Key: 消息标签, Value: 监听该标签的所有事件方法
        /// </summary>
        private readonly Dictionary<string, List<MessageEvent>> _subscribeTag2Methods =
            new Dictionary<string, List<MessageEvent>>();

        /// <summary>
        /// 内存管理器引用
        /// </summary>
        private readonly IMemoryManager _memoryManager;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memoryManager">内存管理器</param>
        public EventRegistry(IMemoryManager memoryManager)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        }

        #endregion

        #region IEventRegistry 实现

        /// <summary>
        /// 注册事件到指定标签
        /// </summary>
        public void RegisterEvent(MessageEvent messageEvent)
        {
            if (messageEvent == null)
                throw new ArgumentNullException(nameof(messageEvent));

            lock (_lock)
            {
                // 查找或创建弱引用key
                WeakReference<object> weakInstance = _memoryManager.GetOrCreateWeakReference(messageEvent.Instance);

                if (!_subscribeInstance2Methods.TryGetValue(weakInstance, out var instanceEvents))
                {
                    instanceEvents = new List<MessageEvent>();
                    _subscribeInstance2Methods[weakInstance] = instanceEvents;
                }

                instanceEvents.Add(messageEvent);

                if (!_subscribeTag2Methods.TryGetValue(messageEvent.Tag, out var tagEvents))
                {
                    tagEvents = new List<MessageEvent>();
                    _subscribeTag2Methods[messageEvent.Tag] = tagEvents;
                }

                tagEvents.Add(messageEvent);
            }
        }

        /// <summary>
        /// 从标签中注销事件
        /// </summary>
        public void UnregisterEvent(string tag, object instance)
        {
            if (string.IsNullOrEmpty(tag) || instance == null)
                return;

            lock (_lock)
            {
                // 从标签映射中移除
                if (_subscribeTag2Methods.TryGetValue(tag, out var tagEvents))
                {
                    for (var i = tagEvents.Count - 1; i >= 0; i--)
                    {
                        if (ReferenceEquals(tagEvents[i].Instance, instance))
                        {
                            tagEvents.RemoveAt(i);
                            break; // 假设一个实例对同一标签只注册一次
                        }
                    }

                    if (tagEvents.Count <= 0)
                        _subscribeTag2Methods.Remove(tag);
                }

                // 从实例映射中移除
                WeakReference<object> foundKey = null;
                foreach (var key in _subscribeInstance2Methods.Keys)
                {
                    if (key.TryGetTarget(out var target) && ReferenceEquals(target, instance))
                    {
                        foundKey = key;
                        break;
                    }
                }

                if (foundKey != null)
                {
                    var instanceEvents = _subscribeInstance2Methods[foundKey];
                    for (var i = instanceEvents.Count - 1; i >= 0; i--)
                    {
                        if (instanceEvents[i].Tag == tag)
                        {
                            instanceEvents.RemoveAt(i);
                            break;
                        }
                    }

                    if (instanceEvents.Count <= 0)
                        _subscribeInstance2Methods.Remove(foundKey);
                }
            }
        }

        /// <summary>
        /// 注销实例的所有事件
        /// </summary>
        public void UnregisterAllEvents(object instance)
        {
            if (instance == null)
                return;

            lock (_lock)
            {
                WeakReference<object> foundKey = null;
                foreach (var key in _subscribeInstance2Methods.Keys)
                {
                    if (key.TryGetTarget(out var target) && ReferenceEquals(target, instance))
                    {
                        foundKey = key;
                        break;
                    }
                }

                if (foundKey == null) return;

                var methods = _subscribeInstance2Methods[foundKey];
                var tmpMethods = new List<MessageEvent>(methods);

                foreach (var method in tmpMethods)
                {
                    UnregisterEvent(method.Tag, instance);
                }
            }
        }

        /// <summary>
        /// 获取指定标签的所有事件
        /// </summary>
        public List<MessageEvent> GetEvents(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return new List<MessageEvent>();

            lock (_lock)
            {
                if (_subscribeTag2Methods.TryGetValue(tag, out var events))
                {
                    // 返回副本，避免外部修改
                    return new List<MessageEvent>(events);
                }
                return new List<MessageEvent>();
            }
        }

        /// <summary>
        /// 检查实例是否已注册
        /// </summary>
        public bool IsRegistered(object instance)
        {
            if (instance == null)
                return false;

            lock (_lock)
            {
                foreach (var key in _subscribeInstance2Methods.Keys)
                {
                    if (key.TryGetTarget(out var target) && ReferenceEquals(target, instance))
                    {
                        return _subscribeInstance2Methods[key].Count > 0;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 获取类型的缓存方法列表
        /// </summary>
        public List<MessageEvent> GetCachedMethods(Type type)
        {
            if (type == null)
                return null;

            lock (_staticLock)
            {
                return _classType2Methods.TryGetValue(type, out var methods) ? methods : null;
            }
        }

        /// <summary>
        /// 缓存类型的方法列表
        /// </summary>
        public void CacheMethods(Type type, List<MessageEvent> methods)
        {
            if (type == null || methods == null)
                return;

            lock (_staticLock)
            {
                _classType2Methods[type] = methods;
            }
        }

        /// <summary>
        /// 清除类型缓存
        /// </summary>
        public void ClearTypeCache()
        {
            lock (_staticLock)
            {
                _classType2Methods.Clear();
            }
        }

        /// <summary>
        /// 清除所有注册数据
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _subscribeInstance2Methods.Clear();
                _subscribeTag2Methods.Clear();
            }

            ClearTypeCache();
        }

        /// <summary>
        /// 获取注册统计信息
        /// </summary>
        public (int instanceCount, int eventCount, int tagCount) GetRegistrationStats()
        {
            lock (_lock)
            {
                int instanceCount = _subscribeInstance2Methods.Count;
                int eventCount = _subscribeInstance2Methods.Values.Sum(list => list.Count);
                int tagCount = _subscribeTag2Methods.Count;

                return (instanceCount, eventCount, tagCount);
            }
        }

        #endregion

        #region 内部辅助方法

        /// <summary>
        /// 通过反射扫描类型中的订阅方法
        /// </summary>
        /// <param name="type">要扫描的类型</param>
        /// <returns>扫描到的事件方法列表</returns>
        internal List<MessageEvent> ScanTypeMethods(Type type)
        {
            var events = new List<MessageEvent>();
            var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var method in methods)
            {
                var methodAttrs = method.GetCustomAttributes(typeof(SubscriberAttribute), false);
                if (methodAttrs.Length == 0) continue;

                if (methodAttrs[0] is SubscriberAttribute subscriberAttribute)
                {
                    var tags = new List<string>();

                    // 处理显式指定的标签
                    if (subscriberAttribute.Tags != null)
                    {
                        tags.AddRange(subscriberAttribute.Tags);
                    }
                    else
                    {
                        // 自动推断标签（基于参数类型）
                        var paramTypes = method.GetParameters();
                        foreach (var paramInfo in paramTypes)
                        {
                            var paramType = paramInfo.ParameterType;
                            if (typeof(IMessageData).IsAssignableFrom(paramType))
                            {
                                var tag = paramType.FullName;
                                if (!tags.Contains(tag))
                                    tags.Add(tag);
                            }
                        }
                    }

                    // 参数校验并创建事件
                    foreach (var tag in tags)
                    {
                        if (ValidateMethodSignature(method, tag))
                        {
                            // 使用null作为临时实例，后续会在Register时替换
                            events.Add(new MessageEvent(method, null, tag, subscriberAttribute.Priority));
                        }
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// 验证方法签名是否与标签匹配
        /// </summary>
        /// <param name="method">要验证的方法</param>
        /// <param name="tag">消息标签</param>
        /// <returns>是否匹配</returns>
        private bool ValidateMethodSignature(MethodInfo method, string tag)
        {
            var paramInfos = method.GetParameters();

            // 如果是类型标签（包含命名空间），需要验证参数
            if (tag.Contains(".") && tag != "System.String")
            {
                // 应该有一个参数且实现IMessageData
                if (paramInfos.Length != 1)
                {
                    Debug.LogError($"[EventRegistry] 方法 {method.Name} 的参数数量与类型标签不匹配，注册失败");
                    return false;
                }

                if (!typeof(IMessageData).IsAssignableFrom(paramInfos[0].ParameterType))
                {
                    Debug.LogError($"[EventRegistry] 方法 {method.Name} 的参数类型与标签不匹配，注册失败");
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}