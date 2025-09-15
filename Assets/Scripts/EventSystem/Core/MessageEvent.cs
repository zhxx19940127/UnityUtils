using System;
using System.Collections.Generic;

namespace EventSystem.Core
{
    /// <summary>
    /// 消息事件封装类 - 包含事件的所有必要信息
    /// </summary>
    public class MessageEvent
    {
        #region 属性

        /// <summary>
        /// 事件执行委托（无返回值）
        /// </summary>
        public Action<object, object> Action { get; }

        /// <summary>
        /// 事件执行委托（有返回值）
        /// </summary>
        public Func<object, object, object> FuncAction { get; }

        /// <summary>
        /// 目标实例对象
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// 事件标签
        /// </summary>
        public string Tag { get; }

        /// <summary>
        /// 执行优先级（数值越高优先级越高）
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 是否有返回值
        /// </summary>
        public bool HasReturnValue => FuncAction != null;

        #endregion

        #region 构造函数

        /// <summary>
        /// 复制构造函数，用于为新实例创建事件副本
        /// </summary>
        public MessageEvent(MessageEvent messageEvent, object instance)
        {
            Action = messageEvent.Action;
            FuncAction = messageEvent.FuncAction;
            Tag = messageEvent.Tag;
            Instance = instance;
            Priority = messageEvent.Priority;
        }

        /// <summary>
        /// 反射方法构造函数
        /// </summary>
        public MessageEvent(System.Reflection.MethodInfo methodInfo, object instance, string tag, int priority = 0)
        {
            Instance = instance;
            Tag = tag;
            Priority = priority;
            
            // 根据方法返回值类型决定使用Action还是FuncAction
            if (methodInfo.ReturnType == typeof(void))
            {
                Action = (ins, o) => methodInfo.Invoke(ins, o as object[]);
                FuncAction = null;
            }
            else
            {
                Action = null;
                FuncAction = (ins, o) => methodInfo.Invoke(ins, o as object[]);
            }
        }

        /// <summary>
        /// Action委托构造函数
        /// </summary>
        public MessageEvent(Action<object> action, object instance, string tag, int priority = 0)
        {
            Action = (ins, o) => action(o);
            FuncAction = null;
            Instance = instance;
            Tag = tag;
            Priority = priority;
        }

        /// <summary>
        /// Func委托构造函数，支持返回值
        /// </summary>
        public MessageEvent(Func<object, object> func, object instance, string tag, int priority = 0)
        {
            Action = null;
            FuncAction = (ins, o) => func(o);
            Instance = instance;
            Tag = tag;
            Priority = priority;
        }

        #endregion

        #region 执行方法

        /// <summary>
        /// 调用事件方法，无返回值
        /// </summary>
        public void Invoke(params object[] parameters)
        {
            try
            {
                Action?.Invoke(Instance, parameters);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"执行方法出错=>{Instance.GetType().Name} {Tag} {e}");
                throw; // 重新抛出异常，让调用者决定如何处理
            }
        }

        /// <summary>
        /// 调用事件方法，带返回值
        /// </summary>
        public object InvokeWithResult(params object[] parameters)
        {
            try
            {
                return FuncAction?.Invoke(Instance, parameters);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"执行带返回值的方法出错=>{Instance.GetType().Name} {Tag} {e}");
                throw; // 重新抛出异常，让调用者决定如何处理
            }
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 相等性比较
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MessageEvent other)
            {
                return ReferenceEquals(Instance, other.Instance) && 
                       Tag == other.Tag && 
                       Priority == other.Priority;
            }
            return false;
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + (Instance?.GetHashCode() ?? 0);
                hash = hash * 23 + (Tag?.GetHashCode() ?? 0);
                hash = hash * 23 + Priority.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"MessageEvent: {Instance?.GetType().Name}.{Tag} (Priority: {Priority})";
        }

        #endregion
    }

    /// <summary>
    /// 弱引用比较器 - 用于弱引用字典的键比较
    /// </summary>
    public class WeakReferenceComparer : IEqualityComparer<WeakReference<object>>
    {
        /// <summary>
        /// 比较两个弱引用是否指向同一个对象
        /// </summary>
        public bool Equals(WeakReference<object> x, WeakReference<object> y)
        {
            if (x == null || y == null) return false;
            if (!x.TryGetTarget(out var xTarget) || !y.TryGetTarget(out var yTarget)) return false;
            return ReferenceEquals(xTarget, yTarget);
        }

        /// <summary>
        /// 获取弱引用的哈希码，基于目标对象的哈希码
        /// </summary>
        public int GetHashCode(WeakReference<object> obj)
        {
            if (obj == null) return 0;
            if (!obj.TryGetTarget(out var target)) return 0;
            return target.GetHashCode();
        }
    }
}