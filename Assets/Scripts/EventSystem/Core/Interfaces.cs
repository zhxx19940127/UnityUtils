using System;
using System.Collections.Generic;

namespace EventSystem.Core
{
    /// <summary>
    /// 事件注册管理器接口 - 负责事件的注册、注销和查找
    /// </summary>
    public interface IEventRegistry
    {
        #region 注册管理

        /// <summary>
        /// 注册事件到指定标签
        /// </summary>
        void RegisterEvent(MessageEvent messageEvent);

        /// <summary>
        /// 从标签中注销事件
        /// </summary>
        void UnregisterEvent(string tag, object instance);

        /// <summary>
        /// 注销实例的所有事件
        /// </summary>
        void UnregisterAllEvents(object instance);

        /// <summary>
        /// 获取指定标签的所有事件
        /// </summary>
        List<MessageEvent> GetEvents(string tag);

        /// <summary>
        /// 检查实例是否已注册
        /// </summary>
        bool IsRegistered(object instance);

        #endregion

        #region 类型缓存管理

        /// <summary>
        /// 获取类型的缓存方法列表
        /// </summary>
        List<MessageEvent> GetCachedMethods(Type type);

        /// <summary>
        /// 缓存类型的方法列表
        /// </summary>
        void CacheMethods(Type type, List<MessageEvent> methods);

        /// <summary>
        /// 清除类型缓存
        /// </summary>
        void ClearTypeCache();

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 清除所有注册数据
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取注册统计信息
        /// </summary>
        (int instanceCount, int eventCount, int tagCount) GetRegistrationStats();

        #endregion
    }

    /// <summary>
    /// 事件分发器接口 - 负责消息的分发和执行
    /// </summary>
    public interface IEventDispatcher
    {
        #region 消息分发

        /// <summary>
        /// 分发消息到指定事件列表
        /// </summary>
        void DispatchMessage(string tag, List<MessageEvent> events, object[] parameters);

        /// <summary>
        /// 分发消息并收集返回值
        /// </summary>
        List<TResult> DispatchMessageWithResult<TResult>(string tag, List<MessageEvent> events, object[] parameters);

        #endregion

        #region 执行策略

        /// <summary>
        /// 设置是否启用优先级排序
        /// </summary>
        bool EnablePrioritySorting { get; set; }

        /// <summary>
        /// 设置是否启用异常处理
        /// </summary>
        bool EnableExceptionHandling { get; set; }

        #endregion
    }

    /// <summary>
    /// 内存管理器接口 - 负责弱引用的管理和内存优化
    /// </summary>
    public interface IMemoryManager : IDisposable
    {
        #region 弱引用管理

        /// <summary>
        /// 创建或获取对象的弱引用
        /// </summary>
        WeakReference<object> GetOrCreateWeakReference(object instance);

        /// <summary>
        /// 清理无效的弱引用
        /// </summary>
        int CleanupDeadReferences();

        /// <summary>
        /// 获取活跃的弱引用数量
        /// </summary>
        int GetActiveReferenceCount();

        #endregion

        #region 对象池管理

        /// <summary>
        /// 从对象池获取列表
        /// </summary>
        List<T> GetPooledList<T>();

        /// <summary>
        /// 归还列表到对象池
        /// </summary>
        void ReturnPooledList<T>(List<T> list);

        /// <summary>
        /// 清理对象池
        /// </summary>
        void ClearPools();

        #endregion
    }

    /// <summary>
    /// 异步处理器接口 - 负责异步消息的处理
    /// </summary>
    public interface IAsyncProcessor : IDisposable
    {
        #region 异步消息处理

        /// <summary>
        /// 添加异步消息到队列
        /// </summary>
        void EnqueueAsyncMessage(string tag, object[] parameters);

        /// <summary>
        /// 检查是否正在处理异步消息
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// 获取队列中待处理的消息数量
        /// </summary>
        int QueueCount { get; }

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 启动异步处理
        /// </summary>
        void StartProcessing();

        /// <summary>
        /// 停止异步处理
        /// </summary>
        void StopProcessing();

        /// <summary>
        /// 清空异步消息队列
        /// </summary>
        void ClearQueue();

        #endregion

    }

    /// <summary>
    /// 拦截器管理器接口 - 负责消息拦截器的管理
    /// </summary>
    public interface IInterceptorManager
    {
        #region 拦截器管理

        /// <summary>
        /// 添加拦截器
        /// </summary>
        void AddInterceptor(IMessageInterceptor interceptor);

        /// <summary>
        /// 移除拦截器
        /// </summary>
        void RemoveInterceptor(IMessageInterceptor interceptor);

        /// <summary>
        /// 检查消息是否应该被处理
        /// </summary>
        bool ShouldProcessMessage(string tag, object[] parameters);

        /// <summary>
        /// 获取所有拦截器
        /// </summary>
        IReadOnlyList<IMessageInterceptor> GetInterceptors();

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 清除所有拦截器
        /// </summary>
        void Clear();

        /// <summary>
        /// 获取拦截器数量
        /// </summary>
        int Count { get; }

        #endregion
    }

#if UNITY_EDITOR
    /// <summary>
    /// 事件统计接口 - 负责统计和调试信息的收集（仅编辑器模式）
    /// </summary>
    public interface IEventStatistics
    {
        #region 统计信息

        /// <summary>
        /// 记录消息调用
        /// </summary>
        void RecordMessage(string tag);

        /// <summary>
        /// 记录错误信息
        /// </summary>
        void RecordError(string tag, Exception exception);

        /// <summary>
        /// 获取消息统计
        /// </summary>
        Dictionary<string, int> GetMessageStats();

        /// <summary>
        /// 获取错误历史
        /// </summary>
        Dictionary<string, List<Exception>> GetErrorHistory();

        #endregion

        #region 生命周期管理

        /// <summary>
        /// 清空所有统计信息
        /// </summary>
        void ClearStats();

        /// <summary>
        /// 设置错误历史记录的最大数量
        /// </summary>
        void SetMaxErrorHistory(int maxCount);

        #endregion
    }
#endif
}