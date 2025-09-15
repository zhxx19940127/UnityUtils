using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using EventSystem.Core;
using EventSystem.Components;

/// <summary>
/// 重构后的事件消息系统门面类
/// 作为各个专业模块的协调器，提供统一的对外接口
/// 基于模块化架构设计，支持依赖注入和组件替换
/// </summary>
public class Message : IEventBus, IDisposable
{
    #region 单例模式

    /// <summary>
    /// 消息系统单例实例
    /// </summary>
    private static Message _instance;

    /// <summary>
    /// 单例锁
    /// </summary>
    private static readonly object _singletonLock = new object();

    /// <summary>
    /// 默认事件系统实例，全局访问点
    /// </summary>
    public static Message DefaultEvent
    {
        get
        {
            if (_instance == null)
            {
                lock (_singletonLock)
                {
                    if (_instance == null)
                    {
                        _instance = new Message();
                    }
                }
            }

            return _instance;
        }
    }

    #endregion

    #region 组件模块

    /// <summary>
    /// 内存管理器
    /// </summary>
    private readonly IMemoryManager _memoryManager;

    /// <summary>
    /// 事件注册管理器
    /// </summary>
    private readonly IEventRegistry _eventRegistry;

    /// <summary>
    /// 事件分发器
    /// </summary>
    private readonly IEventDispatcher _eventDispatcher;

    /// <summary>
    /// 异步处理器
    /// </summary>
    private readonly IAsyncProcessor _asyncProcessor;

    /// <summary>
    /// 拦截器管理器
    /// </summary>
    private readonly IInterceptorManager _interceptorManager;

#if UNITY_EDITOR
    /// <summary>
    /// 统计管理器（仅编辑器模式）
    /// </summary>
    private readonly IEventStatistics _statistics;
#endif

    #endregion

    #region 辅助数据

    /// <summary>
    /// 类型到标签的映射缓存
    /// </summary>
    private readonly Dictionary<Type, string> _type2Tag = new Dictionary<Type, string>();

    /// <summary>
    /// 类型标签缓存锁
    /// </summary>
    private readonly object _typeTagLock = new object();

    /// <summary>
    /// 消息过滤列表，用于编辑器下的日志过滤
    /// </summary>
    private List<string> _filterate = new List<string>();

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下的初始化标志
    /// </summary>
    private bool _isInit = false;
#endif

    #endregion

    #region 构造函数

    /// <summary>
    /// 默认构造函数 - 使用默认组件实现
    /// </summary>
    public Message()
    {
        // 创建默认组件实例
        _memoryManager = new MemoryManager();
        _eventRegistry = new EventRegistry(_memoryManager);
        _eventDispatcher = new EventDispatcher(_memoryManager
#if UNITY_EDITOR
            , _statistics = new EventStatistics()
#endif
        );
        _asyncProcessor = new AsyncEventProcessor(this);
        _interceptorManager = new InterceptorManager();

#if UNITY_EDITOR
        Debug.Log("[Message] 事件系统已初始化（使用默认组件）");
#endif
    }

    /// <summary>
    /// 依赖注入构造函数 - 支持自定义组件实现
    /// </summary>
    /// <param name="memoryManager">内存管理器</param>
    /// <param name="eventRegistry">事件注册管理器</param>
    /// <param name="eventDispatcher">事件分发器</param>
    /// <param name="asyncProcessor">异步处理器</param>
    /// <param name="interceptorManager">拦截器管理器</param>
    /// <param name="statistics">统计管理器（可选，仅编辑器模式）</param>
    public Message(
        IMemoryManager memoryManager,
        IEventRegistry eventRegistry,
        IEventDispatcher eventDispatcher,
        IAsyncProcessor asyncProcessor,
        IInterceptorManager interceptorManager
#if UNITY_EDITOR
        , IEventStatistics statistics = null
#endif
    )
    {
        _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        _eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
        _eventDispatcher = eventDispatcher ?? throw new ArgumentNullException(nameof(eventDispatcher));
        _asyncProcessor = asyncProcessor ?? throw new ArgumentNullException(nameof(asyncProcessor));
        _interceptorManager = interceptorManager ?? throw new ArgumentNullException(nameof(interceptorManager));

#if UNITY_EDITOR
        _statistics = statistics;
        Debug.Log("[Message] 事件系统已初始化（使用自定义组件）");
#endif
    }

    #endregion

    #region IEventBus 实现 - 消息发送

    /// <summary>
    /// 发送字符串标签消息
    /// </summary>
    public void Post(string tag, params object[] parameters)
    {
#if UNITY_EDITOR
        // 编辑器下的过滤器初始化和日志
        InitializeEditorFilter();
        LogMessageIfNotFiltered(tag);
#endif

        // 拦截器检查
        if (!_interceptorManager.ShouldProcessMessage(tag, parameters))
        {
            return; // 被拦截，不继续处理
        }

        // 获取事件列表并分发
        var events = _eventRegistry.GetEvents(tag);
        if (events.Count > 0)
        {
            _eventDispatcher.DispatchMessage(tag, events, parameters);
        }
    }

    /// <summary>
    /// 发送泛型类型消息
    /// </summary>
    public void Post<T>(T messageData) where T : class, IMessageData
    {
        var tag = GetTypeTag<T>();
        Post(tag, messageData);
    }

    /// <summary>
    /// 异步发送字符串标签消息
    /// </summary>
    public void PostAsync(string tag, params object[] parameters)
    {
        _asyncProcessor.EnqueueAsyncMessage(tag, parameters);
    }

    /// <summary>
    /// 异步发送泛型类型消息
    /// </summary>
    public void PostAsync<T>(T messageData) where T : class, IMessageData
    {
        var tag = GetTypeTag<T>();
        PostAsync(tag, messageData);
    }

    /// <summary>
    /// 发送消息并收集返回值
    /// </summary>
    public List<TResult> PostWithResult<TMessage, TResult>(TMessage message) where TMessage : class, IMessageData
    {
        var tag = GetTypeTag<TMessage>();

        // 拦截器检查
        object[] parameters = { message };
        if (!_interceptorManager.ShouldProcessMessage(tag, parameters))
        {
            return new List<TResult>(); // 被拦截，返回空结果
        }

        // 获取事件列表并分发
        var events = _eventRegistry.GetEvents(tag);
        return _eventDispatcher.DispatchMessageWithResult<TResult>(tag, events, parameters);
    }

    /// <summary>
    /// 发送消息并收集返回值（使用字符串标签）
    /// </summary>
    public List<TResult> PostWithResult<TResult>(string tag, params object[] parameters)
    {
        // 拦截器检查
        if (!_interceptorManager.ShouldProcessMessage(tag, parameters))
        {
            return new List<TResult>(); // 被拦截，返回空结果
        }

        // 获取事件列表并分发
        var events = _eventRegistry.GetEvents(tag);
        return _eventDispatcher.DispatchMessageWithResult<TResult>(tag, events, parameters);
    }

    #endregion

    #region IEventBus 实现 - 事件注册

    /// <summary>
    /// 自动注册对象中的所有订阅方法
    /// </summary>
    public void Register<T>(T instance) where T : class
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        var type = instance.GetType();

        // 检查是否已注册
        if (_eventRegistry.IsRegistered(instance))
        {
            Debug.LogWarning($"[Message] {type.FullName} 已注册");
            return;
        }

        // 获取或扫描类型方法
        var cachedMethods = _eventRegistry.GetCachedMethods(type);
        if (cachedMethods == null)
        {
            // 首次注册该类型，扫描并缓存方法
            if (_eventRegistry is EventRegistry registry)
            {
                cachedMethods = registry.ScanTypeMethods(type);
                _eventRegistry.CacheMethods(type, cachedMethods);
            }
            else
            {
                Debug.LogError("[Message] EventRegistry 不支持方法扫描");
                return;
            }
        }

        // 为实例创建具体的事件并注册
        foreach (var templateEvent in cachedMethods)
        {
            var instanceEvent = new MessageEvent(templateEvent, instance);
            _eventRegistry.RegisterEvent(instanceEvent);
        }

#if UNITY_EDITOR
        Debug.Log($"[Message] 注册完成: {type.Name} ({cachedMethods.Count} 个方法)");
#endif
    }

    /// <summary>
    /// 手动注册无参数方法
    /// </summary>
    public void Register<T>(T instance, Action method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 0, tag);
        var messageEvent = new MessageEvent(o => method(), instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 手动注册1个参数的方法
    /// </summary>
    public void Register<T, P>(T instance, Action<P> method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 1, tag);
        var messageEvent = new MessageEvent(o => method((P)(o as object[])[0]), instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 手动注册2个参数的方法
    /// </summary>
    public void Register<T, P, P2>(T instance, Action<P, P2> method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 2, tag);
        var messageEvent = new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1]);
        }, instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 手动注册3个参数的方法
    /// </summary>
    public void Register<T, P, P2, P3>(T instance, Action<P, P2, P3> method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 3, tag);
        var messageEvent = new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2]);
        }, instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 手动注册4个参数的方法
    /// </summary>
    public void Register<T, P, P2, P3, P4>(T instance, Action<P, P2, P3, P4> method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 4, tag);
        var messageEvent = new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2], (P4)paras[3]);
        }, instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 手动注册5个参数的方法
    /// </summary>
    public void Register<T, P, P2, P3, P4, P5>(T instance, Action<P, P2, P3, P4, P5> method, string tag) where T : class
    {
        ValidateMethodParameters(method.Method, 5, tag);
        var messageEvent = new MessageEvent(o =>
        {
            var paras = o as object[];
            method((P)paras[0], (P2)paras[1], (P3)paras[2], (P4)paras[3], (P5)paras[4]);
        }, instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    /// <summary>
    /// 类型安全的消息注册
    /// </summary>
    public void Register<TInstance, TMessage>(TInstance instance, Action<TMessage> handler)
        where TInstance : class
        where TMessage : class, IMessageData
    {
        var tag = GetTypeTag<TMessage>();
        Register(instance, handler, tag);
    }

    /// <summary>
    /// 类型安全的消息注册，带返回值
    /// </summary>
    public void Register<TInstance, TMessage, TResult>(TInstance instance, Func<TMessage, TResult> handler)
        where TInstance : class
        where TMessage : class, IMessageData
    {
        var tag = GetTypeTag<TMessage>();
        var messageEvent = new MessageEvent(msg => handler((TMessage)msg), instance, tag);
        _eventRegistry.RegisterEvent(messageEvent);
    }

    #endregion

    #region IEventBus 实现 - 事件注销

    /// <summary>
    /// 注销对象的所有订阅
    /// </summary>
    public void Unregister<T>(T instance) where T : class
    {
        if (instance == null) return;
        _eventRegistry.UnregisterAllEvents(instance);
    }

    /// <summary>
    /// 注销特定标签的订阅
    /// </summary>
    public void UnregisterOneMethod(string tag, object instance)
    {
        if (string.IsNullOrEmpty(tag) || instance == null) return;
        _eventRegistry.UnregisterEvent(tag, instance);
    }

    /// <summary>
    /// 类型安全的消息注销
    /// </summary>
    public void Unregister<TInstance, TMessage>(TInstance instance)
        where TInstance : class
        where TMessage : class, IMessageData
    {
        var tag = GetTypeTag<TMessage>();
        UnregisterOneMethod(tag, instance);
    }

    #endregion

    #region IEventBus 实现 - 系统管理

    /// <summary>
    /// 添加消息拦截器
    /// </summary>
    public void AddInterceptor(IMessageInterceptor interceptor)
    {
        _interceptorManager.AddInterceptor(interceptor);
    }

    /// <summary>
    /// 移除消息拦截器
    /// </summary>
    public void RemoveInterceptor(IMessageInterceptor interceptor)
    {
        _interceptorManager.RemoveInterceptor(interceptor);
    }

    /// <summary>
    /// 清除所有注册的事件
    /// </summary>
    public void Clear()
    {
        _eventRegistry.Clear();
        _interceptorManager.Clear();
        _asyncProcessor.ClearQueue();

        lock (_typeTagLock)
        {
            _type2Tag.Clear();
        }

#if UNITY_EDITOR
        _statistics?.ClearStats();
        Debug.Log("[Message] 事件系统已清理");
#endif
    }

    /// <summary>
    /// 清理无效的弱引用
    /// </summary>
    public int RemoveDeadWeakReferences()
    {
        var cleanedCount = _memoryManager.CleanupDeadReferences();

#if UNITY_EDITOR
        if (cleanedCount > 0)
        {
            Debug.Log($"[Message] 清理了 {cleanedCount} 个无效弱引用");
        }
#endif
        return cleanedCount;
    }

    #endregion

    #region IEventBus 实现 - 调试统计（仅编辑器模式）

#if UNITY_EDITOR
    /// <summary>
    /// 获取消息统计信息
    /// </summary>
    public Dictionary<string, int> GetMessageStats()
    {
        return _statistics?.GetMessageStats() ?? new Dictionary<string, int>();
    }

    /// <summary>
    /// 获取错误历史记录
    /// </summary>
    public Dictionary<string, List<Exception>> GetErrorHistory()
    {
        return _statistics?.GetErrorHistory() ?? new Dictionary<string, List<Exception>>();
    }

    /// <summary>
    /// 清空统计信息
    /// </summary>
    public void ClearStats()
    {
        _statistics?.ClearStats();
    }
#endif

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取类型对应的标签
    /// </summary>
    private string GetTypeTag<T>() where T : class, IMessageData
    {
        var type = typeof(T);

        lock (_typeTagLock)
        {
            if (!_type2Tag.TryGetValue(type, out var tag))
            {
                tag = type.FullName;
                _type2Tag[type] = tag;
            }

            return tag;
        }
    }

    /// <summary>
    /// 验证方法参数
    /// </summary>
    private void ValidateMethodParameters(MethodInfo method, int expectedParamCount, string tag)
    {
        if (method.GetParameters().Length != expectedParamCount)
        {
            Debug.LogError($"[Message] 注册方法参数数量与预期不匹配 - Tag: {tag}, " +
                           $"Expected: {expectedParamCount}, Actual: {method.GetParameters().Length}");
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 初始化编辑器过滤器（仅编辑器模式）
    /// </summary>
    private void InitializeEditorFilter()
    {
        if (!_isInit)
        {
            var list = MessageHelper.GetFilterMessageName();
            _filterate.AddRange(list);
            _isInit = true;
        }
    }

    /// <summary>
    /// 记录未过滤的消息日志（仅编辑器模式）
    /// </summary>
    private void LogMessageIfNotFiltered(string tag)
    {
        if (!_filterate.Contains(tag))
        {
            Debug.Log($"PostMessage====>: {tag}");
        }
    }
#endif

    #endregion

    #region 组件访问器（用于高级使用和测试）

#if UNITY_EDITOR
    /// <summary>
    /// 获取内存管理器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IMemoryManager GetMemoryManager() => _memoryManager;

    /// <summary>
    /// 获取事件注册管理器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IEventRegistry GetEventRegistry() => _eventRegistry;

    /// <summary>
    /// 获取事件分发器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IEventDispatcher GetEventDispatcher() => _eventDispatcher;

    /// <summary>
    /// 获取异步处理器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IAsyncProcessor GetAsyncProcessor() => _asyncProcessor;

    /// <summary>
    /// 获取拦截器管理器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IInterceptorManager GetInterceptorManager() => _interceptorManager;

    /// <summary>
    /// 获取统计管理器组件（仅编辑器模式，用于调试）
    /// </summary>
    public IEventStatistics GetStatistics() => _statistics;

    /// <summary>
    /// 获取系统总体状态报告（仅编辑器模式）
    /// </summary>
    public string GetSystemStatusReport()
    {
        var stats = _eventRegistry.GetRegistrationStats();
        var memoryStats = (_memoryManager as MemoryManager)?.GetMemoryStats();
        var asyncStats = (_asyncProcessor as AsyncEventProcessor)?.GetStats();

        var report = "=== Event System Status Report ===\n";
        report += $"Registration: {stats.instanceCount} instances, {stats.eventCount} events, {stats.tagCount} tags\n";
        report +=
            $"Memory: {memoryStats?.WeakReferenceCount ?? 0} weak refs, {memoryStats?.TotalPooledObjects ?? 0} pooled objects\n";
        report += $"Async: {_asyncProcessor.QueueCount} queued, Processing: {_asyncProcessor.IsProcessing}\n";
        report += $"Interceptors: {_interceptorManager.Count} active\n";

        if (asyncStats != null)
        {
            report += $"Async Stats: {asyncStats}\n";
        }

        return report;
    }
#endif

    #endregion

    #region 生命周期管理

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _asyncProcessor?.Dispose();
        (_memoryManager as IDisposable)?.Dispose();
        Clear();

#if UNITY_EDITOR
        Debug.Log("[Message] 事件系统已释放资源");
#endif
    }

    #endregion
}