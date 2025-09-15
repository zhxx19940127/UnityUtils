using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 专门测试查询消息功能的简单测试类
/// </summary>
public class QueryMessageTest : MonoBehaviour
{
    [Header("测试设置")]
    public bool runTestOnStart = true;
    
    private void Start()
    {
        if (runTestOnStart)
        {
            StartCoroutine(TestQueryMessageCoroutine());
        }
    }
    
    private System.Collections.IEnumerator TestQueryMessageCoroutine()
    {
        yield return new WaitForSeconds(0.5f); // 等待一会儿让系统初始化
        
        Debug.Log("=== 开始查询消息测试 ===");
        
        // 注册测试处理器
        Message.DefaultEvent.Register(this);
        
        // 创建查询数据
        var queryData = new EventTest.QueryData("测试查询", 999);
        Debug.Log($"发送查询: {queryData}");
        
        // 发送查询并获取结果
        var results = Message.DefaultEvent.PostWithResult<EventTest.QueryData, string>(queryData);
        
        Debug.Log($"收到 {results.Count} 个回复:");
        for (int i = 0; i < results.Count; i++)
        {
            Debug.Log($"回复 {i + 1}: {results[i]}");
        }
        
        // 验证结果
        bool success = results != null && results.Count > 0 && 
                       results[0].Contains("测试查询") && results[0].Contains("999");
        
        Debug.Log($"测试结果: {(success ? "成功" : "失败")}");
        Debug.Log("=== 查询消息测试结束 ===");
    }
    
    /// <summary>
    /// 查询消息处理器
    /// </summary>
    [Subscriber]
    public string OnQueryMessage(EventTest.QueryData query)
    {
        string response = $"对查询 '{query.query}' 的响应 (ID: {query.queryId}) - 时间: {DateTime.Now:HH:mm:ss}";
        Debug.Log($"处理查询: {query}, 响应: {response}");
        return response;
    }
}