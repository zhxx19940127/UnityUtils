using System;
using System.Collections.Generic;
using UnityEngine;


public static class MessageHelper
{
    public static List<string> GetFilterMessageName()
    {
        // 检查是否在主线程，Resources.Load 只能在主线程调用
        try
        {
            var t = UnityEngine.Resources.Load<TextAsset>("MessageFilter");
            if (t == null)
            {
                return new List<string>();
            }

            string s = t.text;
            var allFilter = new List<string>();
            var ss = s.Split(',');
            for (int i = 0; i < ss.Length; i++)
            {
                allFilter.Add(ss[i]);
            }

            return allFilter;
        }
        catch (UnityException)
        {
            // 非主线程调用 Resources.Load 会抛出异常
            // 返回空列表避免崩溃
            return new List<string>();
        }
    }
}


/// <summary>
/// 订阅者特性，用于标记可以自动注册的事件方法
/// 支持多标签、优先级设置等功能
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class SubscriberAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SubscriberAttribute()
    {
        Priority = 0;
    }

    /// <summary>
    /// 单标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag, int priority = 0)
    {
        Tags = new[] { tag };
        Priority = priority;
    }

    /// <summary>
    /// 双标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag1, string tag2, int priority = 0)
    {
        Tags = new[] { tag1, tag2 };
        Priority = priority;
    }

    /// <summary>
    /// 三标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag1, string tag2, string tag3, int priority = 0)
    {
        Tags = new[] { tag1, tag2, tag3 };
        Priority = priority;
    }

    /// <summary>
    /// 标签数组，支持一个方法监听多个消息
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// 优先级，数值越大优先级越高
    /// </summary>
    public int Priority { get; set; }
}

public interface IMessageData
{
}

/// <summary>
/// 消息拦截器接口
/// </summary>
public interface IMessageInterceptor
{
    /// <summary>
    /// 判断是否应该处理该消息
    /// </summary>
    /// <param name="tag">消息标签</param>
    /// <param name="parameters">消息参数</param>
    /// <returns>true表示继续处理，false表示拦截</returns>
    bool ShouldProcess(string tag, object[] parameters);
}