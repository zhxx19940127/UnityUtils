using System;

/// <summary>
/// 日志级别枚举，控制订阅者的日志输出行为
/// </summary>
public enum SubscriberLogLevel
{
    /// <summary>
    /// 显示所有日志（注册、分发、取消注册）
    /// </summary>
    All = 0,

    /// <summary>
    /// 只显示注册和取消注册日志，不显示分发日志
    /// </summary>
    RegistrationOnly = 1,

    /// <summary>
    /// 只显示分发日志，不显示注册和取消注册日志
    /// </summary>
    DispatchOnly = 2,

    /// <summary>
    /// 不显示任何日志
    /// </summary>
    None = 3
}

/// <summary>
/// 订阅者特性，用于标记可以自动注册的事件方法
/// 支持多标签、优先级设置、日志控制等功能
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public class SubscriberAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public SubscriberAttribute(SubscriberLogLevel logLevel = SubscriberLogLevel.All)
    {
        Priority = 0;
        LogLevel = logLevel;
    }

    /// <summary>
    /// 单标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag, int priority = 0, SubscriberLogLevel logLevel = SubscriberLogLevel.All)
    {
        Tags = new[] { tag };
        Priority = priority;
        LogLevel = logLevel;
    }

    /// <summary>
    /// 双标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag1, string tag2, int priority = 0,
        SubscriberLogLevel logLevel = SubscriberLogLevel.All)
    {
        Tags = new[] { tag1, tag2 };
        Priority = priority;
        LogLevel = logLevel;
    }

    /// <summary>
    /// 三标签构造函数
    /// </summary>
    public SubscriberAttribute(string tag1, string tag2, string tag3, int priority = 0,
        SubscriberLogLevel logLevel = SubscriberLogLevel.All)
    {
        Tags = new[] { tag1, tag2, tag3 };
        Priority = priority;
        LogLevel = logLevel;
    }


    /// <summary>
    /// 标签数组，支持一个方法监听多个消息
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// 优先级，数值越大优先级越高
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 日志级别，控制具体的日志输出行为
    /// </summary>
    public SubscriberLogLevel LogLevel { get; set; }
}