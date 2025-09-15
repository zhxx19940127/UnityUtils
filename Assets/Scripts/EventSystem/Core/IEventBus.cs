using System;
using System.Collections.Generic;

namespace EventSystem.Core
{
    /// <summary>
    /// 事件总线主接口 - 对外提供统一的事件系统访问点
    /// </summary>
    public interface IEventBus
    {
        #region 消息发送接口

        /// <summary>
        /// 发送字符串标签消息
        /// </summary>
        void Post(string tag, params object[] parameters);

        /// <summary>
        /// 发送泛型类型消息
        /// </summary>
        void Post<T>(T messageData) where T : class, IMessageData;

        /// <summary>
        /// 异步发送字符串标签消息
        /// </summary>
        void PostAsync(string tag, params object[] parameters);

        /// <summary>
        /// 异步发送泛型类型消息
        /// </summary>
        void PostAsync<T>(T messageData) where T : class, IMessageData;

        /// <summary>
        /// 发送消息并收集返回值
        /// </summary>
        List<TResult> PostWithResult<TMessage, TResult>(TMessage message) where TMessage : class, IMessageData;

        /// <summary>
        /// 发送消息并收集返回值（使用字符串标签）
        /// </summary>
        List<TResult> PostWithResult<TResult>(string tag, params object[] parameters);

        #endregion

        #region 事件注册接口

        /// <summary>
        /// 自动注册对象中的所有订阅方法
        /// </summary>
        void Register<T>(T instance) where T : class;

        /// <summary>
        /// 手动注册指定方法
        /// </summary>
        void Register<T>(T instance, Action method, string tag) where T : class;
        void Register<T, P>(T instance, Action<P> method, string tag) where T : class;
        void Register<T, P, P2>(T instance, Action<P, P2> method, string tag) where T : class;
        void Register<T, P, P2, P3>(T instance, Action<P, P2, P3> method, string tag) where T : class;
        void Register<T, P, P2, P3, P4>(T instance, Action<P, P2, P3, P4> method, string tag) where T : class;
        void Register<T, P, P2, P3, P4, P5>(T instance, Action<P, P2, P3, P4, P5> method, string tag) where T : class;

        /// <summary>
        /// 类型安全的消息注册
        /// </summary>
        void Register<TInstance, TMessage>(TInstance instance, Action<TMessage> handler)
            where TInstance : class
            where TMessage : class, IMessageData;

        void Register<TInstance, TMessage, TResult>(TInstance instance, Func<TMessage, TResult> handler)
            where TInstance : class
            where TMessage : class, IMessageData;

        #endregion

        #region 事件注销接口

        /// <summary>
        /// 注销对象的所有订阅
        /// </summary>
        void Unregister<T>(T instance) where T : class;

        /// <summary>
        /// 注销特定标签的订阅
        /// </summary>
        void UnregisterOneMethod(string tag, object instance);

        /// <summary>
        /// 类型安全的消息注销
        /// </summary>
        void Unregister<TInstance, TMessage>(TInstance instance)
            where TInstance : class
            where TMessage : class, IMessageData;

        #endregion

        #region 系统管理接口

        /// <summary>
        /// 添加消息拦截器
        /// </summary>
        void AddInterceptor(IMessageInterceptor interceptor);

        /// <summary>
        /// 移除消息拦截器
        /// </summary>
        void RemoveInterceptor(IMessageInterceptor interceptor);

        /// <summary>
        /// 清除所有注册的事件
        /// </summary>
        void Clear();

        /// <summary>
        /// 清理无效的弱引用
        /// </summary>
        /// <returns>清理的弱引用数量</returns>
        int RemoveDeadWeakReferences();

        #endregion

#if UNITY_EDITOR
        #region 调试和统计接口（仅编辑器模式）

        /// <summary>
        /// 获取消息统计信息
        /// </summary>
        Dictionary<string, int> GetMessageStats();

        /// <summary>
        /// 获取错误历史记录
        /// </summary>
        Dictionary<string, List<Exception>> GetErrorHistory();

        /// <summary>
        /// 清空统计信息
        /// </summary>
        void ClearStats();

        #endregion
#endif
    }
}