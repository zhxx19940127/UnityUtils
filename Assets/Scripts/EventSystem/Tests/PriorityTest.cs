using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 事件优先级测试脚本
/// 验证事件系统的优先级机制是否正确工作
/// </summary>
public class PriorityTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool runTestOnStart = true;

    private List<string> executionOrder = new List<string>();

    void Start()
    {
        if (runTestOnStart)
        {
            TestPriority();
        }
    }

    /// <summary>
    /// 测试优先级功能
    /// </summary>
    public void TestPriority()
    {
        Debug.Log("=== 开始优先级测试 ===");

        // 注册测试处理器
        Message.DefaultEvent.Register(this);

        // 清空执行顺序记录
        executionOrder.Clear();

        // 发送测试消息
        var testData = new EventTest.TestData(100, "优先级测试");
        Message.DefaultEvent.Post(testData);
        //Message.DefaultEvent.Post("TestEvent", testData);
        // 输出执行顺序
        Debug.Log("事件执行顺序：");
        for (int i = 0; i < executionOrder.Count; i++)
        {
            Debug.Log($"{i + 1}. {executionOrder[i]}");
        }

        // 验证优先级是否正确（应该是高优先级先执行）
        bool priorityCorrect = executionOrder.Count >= 3 &&
                               executionOrder[0].Contains("高优先级") &&
                               executionOrder[1].Contains("中优先级") &&
                               executionOrder[2].Contains("低优先级");

        string result = priorityCorrect ? "✅ 优先级测试通过" : "❌ 优先级测试失败";
        Debug.Log($"{result} - 执行顺序：{string.Join(" → ", executionOrder)}");

        Debug.Log("=== 优先级测试结束 ===");
    }

    /// <summary>
    /// 高优先级处理器 (优先级 100)
    /// </summary>
    [Subscriber("TestEvent", 100)]
    public void OnHighPriorityEvent(EventTest.TestData data)
    {
        string message = "高优先级处理器 (100)";
        executionOrder.Add(message);
        Debug.Log($"[高优先级] {message}: {data}");
    }

    /// <summary>
    /// 中优先级处理器 (优先级 50)
    /// </summary>
    [Subscriber("TestEvent", 50)]
    public void OnMidPriorityEvent(EventTest.TestData data)
    {
        string message = "中优先级处理器 (50)";
        executionOrder.Add(message);
        Debug.Log($"[中优先级] {message}: {data}");
    }

    /// <summary>
    /// 低优先级处理器 (优先级 10)
    /// </summary>
    [Subscriber("TestEvent", 10)]
    public void OnLowPriorityEvent(EventTest.TestData data)
    {
        string message = "低优先级处理器 (10)";
        executionOrder.Add(message);
        Debug.Log($"[低优先级] {message}: {data}");
    }

    /// <summary>
    /// 默认优先级处理器 (优先级 0)
    /// </summary>
    [Subscriber("TestEvent")]
    public void OnDefaultPriorityEvent(EventTest.TestData data)
    {
        string message = "默认优先级处理器 (0)";
        executionOrder.Add(message);
        Debug.Log($"[默认优先级] {message}: {data}");
    }

    /// <summary>
    /// 负优先级处理器 (优先级 -10)
    /// </summary>
    [Subscriber("TestEvent", -10)]
    public void OnNegativePriorityEvent(EventTest.TestData data)
    {
        string message = "负优先级处理器 (-10)";
        executionOrder.Add(message);
        Debug.Log($"[负优先级] {message}: {data}");
    }
}