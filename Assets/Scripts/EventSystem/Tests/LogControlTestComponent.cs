using EventSystem.Core;
using UnityEngine;

/// <summary>
/// SubscriberAttribute 日志控制功能测试类
/// 演示不同日志级别的效果
/// </summary>
public class LogControlTestComponent : MonoBehaviour
{
    #region 测试方法 - 不同日志级别

    /// <summary>
    /// 默认配置：显示所有日志
    /// </summary>
    [Subscriber("TestDefault")]
    private void OnTestDefault()
    {
        Debug.Log("执行 TestDefault 方法");
    }

    /// <summary>
    /// 禁用所有日志
    /// </summary>
    [Subscriber("TestDisabled", logLevel: SubscriberLogLevel.None)]
    private void OnTestDisabled()
    {
        Debug.Log("执行 TestDisabled 方法");
    }

    /// <summary>
    /// 只显示注册/注销日志
    /// </summary>
    [Subscriber("TestRegistrationOnly", logLevel: SubscriberLogLevel.RegistrationOnly)]
    private void OnTestRegistrationOnly()
    {
        Debug.Log("执行 TestRegistrationOnly 方法");
    }

    /// <summary>
    /// 只显示分发日志
    /// </summary>
    [Subscriber("TestDispatchOnly", logLevel: SubscriberLogLevel.DispatchOnly)]
    private void OnTestDispatchOnly()
    {
        Debug.Log("执行 TestDispatchOnly 方法");
    }

    /// <summary>
    /// 完全禁用日志
    /// </summary>
    [Subscriber("TestNone", logLevel: SubscriberLogLevel.None)]
    private void OnTestNone()
    {
        Debug.Log("执行 TestNone 方法");
    }

    /// <summary>
    /// 高优先级事件，显示所有日志
    /// </summary>
    [Subscriber("TestHighPriority", priority: 100, logLevel: SubscriberLogLevel.All)]
    private void OnTestHighPriority()
    {
        Debug.Log("执行 TestHighPriority 方法");
    }

    /// <summary>
    /// 多标签事件，只显示分发日志
    /// </summary>
    [Subscriber("TestMulti1", "TestMulti2", logLevel: SubscriberLogLevel.DispatchOnly)]
    private void OnTestMultiple()
    {
        Debug.Log("执行 TestMultiple 方法");
    }

    #endregion

    #region Unity 生命周期

    void Start()
    {
        // 注册组件到事件系统
        Message.DefaultEvent.Register(this);

        Debug.Log("=== LogControlTestComponent 已注册，开始测试 ===");

        // 延迟执行测试，确保注册完成
        Invoke(nameof(RunTests), 1f);
    }

    void OnDestroy()
    {
        // 注销组件
        Message.DefaultEvent.Unregister(this);
    }

    #endregion

    #region 测试执行

    /// <summary>
    /// 运行所有测试用例
    /// </summary>
    private void RunTests()
    {
        Debug.Log("\n=== 开始执行日志控制测试 ===\n");

        // 测试1：默认配置（应该显示注册和分发日志）
        Debug.Log("--- 测试1: 默认配置 ---");
        Message.DefaultEvent.Post("TestDefault");

        // 测试2：禁用日志（不应该显示任何日志）
        Debug.Log("\n--- 测试2: 禁用日志 ---");
        Message.DefaultEvent.Post("TestDisabled");

        // 测试3：只显示注册日志（只在注册时显示，分发时不显示）
        Debug.Log("\n--- 测试3: 只显示注册日志 ---");
        Message.DefaultEvent.Post("TestRegistrationOnly");

        // 测试4：只显示分发日志
        Debug.Log("\n--- 测试4: 只显示分发日志 ---");
        Message.DefaultEvent.Post("TestDispatchOnly");

        // 测试5：完全禁用日志
        Debug.Log("\n--- 测试5: 完全禁用日志 ---");
        Message.DefaultEvent.Post("TestNone");

        // 测试6：高优先级事件
        Debug.Log("\n--- 测试6: 高优先级事件 ---");
        Message.DefaultEvent.Post("TestHighPriority");

        // 测试7：多标签事件
        Debug.Log("\n--- 测试7: 多标签事件 ---");
        Message.DefaultEvent.Post("TestMulti1");
        Message.DefaultEvent.Post("TestMulti2");

        Debug.Log("\n=== 日志控制测试完成 ===\n");
    }

    #endregion

    #region 编辑器测试按钮

#if UNITY_EDITOR
    [ContextMenu("运行日志控制测试")]
    private void RunTestsFromEditor()
    {
        if (Application.isPlaying)
        {
            RunTests();
        }
        else
        {
            Debug.LogWarning("请在运行时执行测试");
        }
    }

    [ContextMenu("手动注册组件")]
    private void ManualRegister()
    {
        if (Application.isPlaying)
        {
            Message.DefaultEvent.Register(this);
            Debug.Log("手动注册完成");
        }
    }

    [ContextMenu("手动注销组件")]
    private void ManualUnregister()
    {
        if (Application.isPlaying)
        {
            Message.DefaultEvent.Unregister(this);
            Debug.Log("手动注销完成");
        }
    }
#endif

    #endregion
}

/// <summary>
/// 简单的测试消息数据类
/// </summary>
public class LogTestMessageData : IMessageData
{
    public string TestMessage { get; set; }
    public int TestValue { get; set; }

    public LogTestMessageData(string message, int value = 0)
    {
        TestMessage = message;
        TestValue = value;
    }
}

/// <summary>
/// 第二个测试组件，用于验证多实例场景
/// </summary>
public class LogControlTestComponent2 : MonoBehaviour
{
    [Subscriber("SharedEvent", priority: 50, logLevel: SubscriberLogLevel.All)]
    private void OnSharedEvent()
    {
        Debug.Log("LogControlTestComponent2 处理 SharedEvent");
    }

    [Subscriber("SharedEvent", priority: 10, logLevel: SubscriberLogLevel.DispatchOnly)]
    private void OnSharedEventLowPriority()
    {
        Debug.Log("LogControlTestComponent2 低优先级处理 SharedEvent");
    }

    void Start()
    {
        Message.DefaultEvent.Register(this);

        // 延迟测试共享事件
        Invoke(nameof(TestSharedEvent), 2f);
    }

    void OnDestroy()
    {
        Message.DefaultEvent.Unregister(this);
    }

    private void TestSharedEvent()
    {
        Debug.Log("\n=== 测试共享事件（多实例场景） ===");
        Message.DefaultEvent.Post("SharedEvent");
    }
}