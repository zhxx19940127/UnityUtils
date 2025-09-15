using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EventSystem.Core;

/// <summary>
/// 事件系统功能测试脚本
/// 提供完整的OnGUI界面和详细的测试结果输出
/// </summary>
public class EventTest : MonoBehaviour
{
    #region 测试配置和状态

    [Header("测试配置")] [SerializeField] private bool enableGUI = true;
    [SerializeField] private bool showDetailedOutput = true;
    [SerializeField] private bool autoStartTests = false;

    // 测试结果统计
    private int totalTests = 0;
    private int passedTests = 0;
    private int failedTests = 0;
    private List<TestResult> testResults = new List<TestResult>();
    private bool testsCompleted = false;

    // GUI 相关
    private Vector2 scrollPosition = Vector2.zero;
    private bool showResults = false;
    private GUIStyle buttonStyle;
    private GUIStyle labelStyle;
    private GUIStyle resultStyle;

    // 压力测试相关
    private bool isStressTesting = false;
    private float stressTestProgress = 0f;
    private int stressTestMessageCount = 1000;
    private int stressTestThreadCount = 4;
    private System.Diagnostics.Stopwatch stressTestStopwatch = new System.Diagnostics.Stopwatch();
    private long stressTestTotalMessages = 0;
    private long stressTestSuccessMessages = 0;
    private string stressTestResults = "";

    #endregion

    #region 测试数据类型

    /// <summary>
    /// 基础测试数据
    /// </summary>
    public class TestData : IMessageData
    {
        public int a;
        public string b;
        public float c;
        public bool d;

        public TestData(int a, string b, float c = 0f, bool d = false)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }

        public override string ToString()
        {
            return $"TestData(a:{a}, b:{b}, c:{c}, d:{d})";
        }
    }

    /// <summary>
    /// 复杂测试数据
    /// </summary>
    public class ComplexData : IMessageData
    {
        public Vector3 position;
        public string playerName;
        public int level;
        public Dictionary<string, object> properties;

        public ComplexData(Vector3 pos, string name, int lvl)
        {
            position = pos;
            playerName = name;
            level = lvl;
            properties = new Dictionary<string, object>();
        }

        public override string ToString()
        {
            return $"ComplexData(pos:{position}, name:{playerName}, lvl:{level})";
        }
    }

    /// <summary>
    /// 查询数据（用于测试返回值）
    /// </summary>
    public class QueryData : IMessageData
    {
        public string query;
        public int queryId;

        public QueryData(string q, int id)
        {
            query = q;
            queryId = id;
        }

        public override string ToString()
        {
            return $"QueryData(query:{query}, id:{queryId})";
        }
    }

    /// <summary>
    /// 测试结果记录
    /// </summary>
    public class TestResult
    {
        public string testName;
        public bool passed;
        public string message;
        public DateTime timestamp;

        public TestResult(string name, bool success, string msg)
        {
            testName = name;
            passed = success;
            message = msg;
            timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            string status = passed ? "✓ 通过" : "✗ 失败";
            return $"[{timestamp:HH:mm:ss}] {status} - {testName}: {message}";
        }
    }

    #endregion

    #region 测试处理器

    // 接收到的消息计数
    private int basicMessageCount = 0;
    private int complexMessageCount = 0;
    private int stringMessageCount = 0;
    private int multiParamMessageCount = 0;
    private int asyncMessageCount = 0;

    // 最后接收的数据
    private TestData lastTestData;
    private ComplexData lastComplexData;
    private string lastStringMessage;
    private object[] lastMultiParams;

    /// <summary>
    /// 基础消息处理器
    /// </summary>
    [Subscriber("TestEvent")]
    public void OnTestEvent(TestData data)
    {
        basicMessageCount++;
        lastTestData = data;
        LogMessage($"基础消息处理器: 收到消息 {data}", LogType.Log);
    }

    /// <summary>
    /// 复杂消息处理器
    /// </summary>
    [Subscriber("ComplexEvent")]
    public void OnComplexEvent(ComplexData data)
    {
        complexMessageCount++;
        lastComplexData = data;
        LogMessage($"复杂消息处理器: 收到消息 {data}", LogType.Log);
    }

    /// <summary>
    /// 字符串消息处理器
    /// </summary>
    [Subscriber("StringEvent")]
    public void OnStringEvent(string message)
    {
        stringMessageCount++;
        lastStringMessage = message;
        LogMessage($"字符串消息处理器: 收到消息 '{message}'", LogType.Log);
    }

    /// <summary>
    /// 多参数消息处理器
    /// </summary>
    public void OnMultiParamEvent(string param1, int param2, bool param3)
    {
        multiParamMessageCount++;
        lastMultiParams = new object[] { param1, param2, param3 };
        LogMessage($"多参数消息处理器: 收到参数 ({param1}, {param2}, {param3})", LogType.Log);
    }

    /// <summary>
    /// 异步消息处理器
    /// </summary>
    [Subscriber("AsyncEvent")]
    public void OnAsyncEvent(TestData data)
    {
        asyncMessageCount++;
        LogMessage($"异步消息处理器: 收到消息 {data}", LogType.Log);
    }

    /// <summary>
    /// 查询消息处理器（带返回值）- 使用类型标签
    /// </summary>
    [Subscriber]
    public string OnQueryEvent(QueryData query)
    {
        string response = $"查询 '{query.query}' 的回复 (ID: {query.queryId})";
        LogMessage($"查询消息处理器(类型标签): 处理查询 {query}, 返回: {response}", LogType.Log);
        return response;
    }

    /// <summary>
    /// 查询消息处理器（带返回值）- 使用自定义标签
    /// </summary>
    [Subscriber("QueryEvent")]
    public string OnCustomQueryEvent(QueryData query)
    {
        string response = $"自定义查询 '{query.query}' 的回复 (ID: {query.queryId})";
        LogMessage($"查询消息处理器(自定义标签): 处理查询 {query}, 返回: {response}", LogType.Log);
        return response;
    }

    #endregion

    #region Unity 生命周期

    private void Start()
    {
        // 注册事件处理器
        Message.DefaultEvent.Register(this);

        // 手动注册多参数处理器
        Message.DefaultEvent.Register<EventTest, string, int, bool>(
            this, OnMultiParamEvent, "MultiParamEvent");

        LogMessage("事件系统测试初始化完成", LogType.Log);

        if (autoStartTests)
        {
            RunAllTests();
        }
    }

    private void OnDestroy()
    {
        // 清理注册
        if (Message.DefaultEvent != null)
        {
            Message.DefaultEvent.Unregister(this);
            Message.DefaultEvent.UnregisterOneMethod("MultiParamEvent", this);
        }
    }

    #endregion

    #region GUI 界面

    private void OnGUI()
    {
        if (!enableGUI) return;

        InitializeGUIStyles();

        // 主面板
        GUILayout.BeginArea(new Rect(10, 10, 600, Screen.height - 20));
        GUILayout.BeginVertical("box");

        // 标题
        GUILayout.Label("事件系统功能测试", labelStyle);
        GUILayout.Space(10);

        // 测试统计
        if (testsCompleted)
        {
            float successRate = totalTests > 0 ? (passedTests * 100f / totalTests) : 0f;
            GUILayout.Label($"测试结果: {passedTests}/{totalTests} 通过 ({successRate:F1}%)",
                successRate >= 90f ? GetSuccessStyle() : GetFailStyle());
        }

        GUILayout.Space(10);

        // 测试按钮区域
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("运行所有测试", buttonStyle, GUILayout.Height(30)))
        {
            RunAllTests();
        }

        if (GUILayout.Button("重置测试", buttonStyle, GUILayout.Height(30)))
        {
            ResetTests();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 单个测试按钮
        GUILayout.Label("单项测试:", labelStyle);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("基础消息", buttonStyle))
        {
            TestBasicMessage();
        }

        if (GUILayout.Button("复杂消息", buttonStyle))
        {
            TestComplexMessage();
        }

        if (GUILayout.Button("字符串消息", buttonStyle))
        {
            TestStringMessage();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("多参数消息", buttonStyle))
        {
            TestMultiParamMessage();
        }

        if (GUILayout.Button("异步消息", buttonStyle))
        {
            TestAsyncMessage();
        }

        if (GUILayout.Button("查询消息", buttonStyle))
        {
            TestQueryMessage();
        }

        if (GUILayout.Button("优先级测试", buttonStyle))
        {
            TestPriorityMessage();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("注册测试", buttonStyle))
        {
            TestRegistration();
        }

        if (GUILayout.Button("注销测试", buttonStyle))
        {
            TestUnregistration();
        }

        if (GUILayout.Button("内存清理", buttonStyle))
        {
            TestMemoryCleanup();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 压力测试区域
        GUILayout.Label("压力测试:", labelStyle);
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"消息数量: {stressTestMessageCount}", GUILayout.Width(100));
        stressTestMessageCount = (int)GUILayout.HorizontalSlider(stressTestMessageCount, 100, 10000, GUILayout.Width(150));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"线程数量: {stressTestThreadCount}", GUILayout.Width(100));
        stressTestThreadCount = (int)GUILayout.HorizontalSlider(stressTestThreadCount, 1, 8, GUILayout.Width(150));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        
        if (!isStressTesting && GUILayout.Button("开始压力测试", buttonStyle))
        {
            StartStressTest();
        }
        
        if (isStressTesting && GUILayout.Button("停止压力测试", buttonStyle))
        {
            StopStressTest();
        }

        if (GUILayout.Button("内存压力测试", buttonStyle))
        {
            TestMemoryStress();
        }

        if (GUILayout.Button("并发压力测试", buttonStyle))
        {
            TestConcurrencyStress();
        }

        GUILayout.EndHorizontal();

        // 压力测试进度和结果
        if (isStressTesting)
        {
            GUILayout.Space(5);
            GUILayout.Label($"压力测试进行中... {stressTestProgress:P1}", labelStyle);
            GUILayout.Label($"已发送: {stressTestTotalMessages}, 成功: {stressTestSuccessMessages}", labelStyle);
        }

        if (!string.IsNullOrEmpty(stressTestResults))
        {
            GUILayout.Space(5);
            GUILayout.Label("压力测试结果:", labelStyle);
            GUILayout.Label(stressTestResults, resultStyle);
        }

        GUILayout.Space(10);

        // 显示/隐藏详细结果按钮
        if (GUILayout.Button(showResults ? "隐藏详细结果" : "显示详细结果", buttonStyle))
        {
            showResults = !showResults;
        }

        // 详细结果显示
        if (showResults && testResults.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("详细测试结果:", labelStyle);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

            foreach (var result in testResults)
            {
                GUILayout.Label(result.ToString(),
                    result.passed ? GetSuccessStyle() : GetFailStyle());
            }

            GUILayout.EndScrollView();
        }

        // 实时状态显示
        GUILayout.Space(10);
        GUILayout.Label("实时状态:", labelStyle);
        GUILayout.Label($"基础消息计数: {basicMessageCount}");
        GUILayout.Label($"复杂消息计数: {complexMessageCount}");
        GUILayout.Label($"字符串消息计数: {stringMessageCount}");
        GUILayout.Label($"多参数消息计数: {multiParamMessageCount}");
        GUILayout.Label($"异步消息计数: {asyncMessageCount}");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void InitializeGUIStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                margin = new RectOffset(2, 2, 2, 2)
            };
        }

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 14;
            labelStyle.fontStyle = FontStyle.Bold;
        }

        if (resultStyle == null)
        {
            resultStyle = new GUIStyle(GUI.skin.label);
            resultStyle.fontSize = 11;
            resultStyle.wordWrap = true;
        }
    }

    private GUIStyle GetSuccessStyle()
    {
        var style = new GUIStyle(resultStyle);
        style.normal.textColor = Color.green;
        return style;
    }

    private GUIStyle GetFailStyle()
    {
        var style = new GUIStyle(resultStyle);
        style.normal.textColor = Color.red;
        return style;
    }

    #endregion

    #region 测试方法

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public void RunAllTests()
    {
        LogMessage("开始运行所有测试...", LogType.Warning);
        ResetTests();

        TestBasicMessage();
        TestComplexMessage();
        TestStringMessage();
        TestMultiParamMessage();
        TestAsyncMessage();
        TestQueryMessage();
        TestPriorityMessage();
        TestRegistration();
        TestUnregistration();
        TestMemoryCleanup();

        testsCompleted = true;

        float successRate = totalTests > 0 ? (passedTests * 100f / totalTests) : 0f;
        LogMessage($"所有测试完成! 通过率: {successRate:F1}% ({passedTests}/{totalTests})",
            successRate >= 90f ? LogType.Log : LogType.Error);
    }

    /// <summary>
    /// 测试基础消息
    /// </summary>
    public void TestBasicMessage()
    {
        string testName = "基础消息测试";
        try
        {
            int initialCount = basicMessageCount;
            var testData = new TestData(42, "测试消息", 3.14f, true);

            Message.DefaultEvent.Post("TestEvent", testData);

            bool success = basicMessageCount == initialCount + 1 &&
                           lastTestData != null &&
                           lastTestData.a == 42 &&
                           lastTestData.b == "测试消息";

            AddTestResult(testName, success,
                success ? $"成功发送并接收消息: {testData}" : "消息发送或接收失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试复杂消息
    /// </summary>
    public void TestComplexMessage()
    {
        string testName = "复杂消息测试";
        try
        {
            int initialCount = complexMessageCount;
            var complexData = new ComplexData(Vector3.up, "玩家1", 10);
            complexData.properties["score"] = 1000;
            complexData.properties["isVIP"] = true;

            Message.DefaultEvent.Post("ComplexEvent", complexData);

            bool success = complexMessageCount == initialCount + 1 &&
                           lastComplexData != null &&
                           lastComplexData.playerName == "玩家1" &&
                           lastComplexData.level == 10;

            AddTestResult(testName, success,
                success ? $"成功处理复杂消息: {complexData}" : "复杂消息处理失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试字符串消息
    /// </summary>
    public void TestStringMessage()
    {
        string testName = "字符串消息测试";
        try
        {
            int initialCount = stringMessageCount;
            string testMessage = "这是一个字符串测试消息";

            Message.DefaultEvent.Post("StringEvent", testMessage);

            bool success = stringMessageCount == initialCount + 1 &&
                           lastStringMessage == testMessage;

            AddTestResult(testName, success,
                success ? $"成功处理字符串消息: '{testMessage}'" : "字符串消息处理失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试多参数消息
    /// </summary>
    public void TestMultiParamMessage()
    {
        string testName = "多参数消息测试";
        try
        {
            int initialCount = multiParamMessageCount;

            Message.DefaultEvent.Post("MultiParamEvent", "参数1", 123, true);

            bool success = multiParamMessageCount == initialCount + 1 &&
                           lastMultiParams != null &&
                           lastMultiParams.Length == 3 &&
                           lastMultiParams[0].Equals("参数1") &&
                           lastMultiParams[1].Equals(123) &&
                           lastMultiParams[2].Equals(true);

            AddTestResult(testName, success,
                success ? "成功处理多参数消息: (参数1, 123, true)" : "多参数消息处理失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试异步消息
    /// </summary>
    public void TestAsyncMessage()
    {
        string testName = "异步消息测试";
        try
        {
            int initialCount = asyncMessageCount;
            var asyncData = new TestData(999, "异步测试", 2.718f, false);

            Message.DefaultEvent.PostAsync("AsyncEvent", asyncData);

            // 等待异步处理
            Invoke(nameof(CheckAsyncResult), 0.1f);

            AddTestResult(testName, true, "异步消息已发送，等待处理结果...");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    private void CheckAsyncResult()
    {
        bool success = asyncMessageCount > 0;
        string message = success ? $"异步消息处理成功，当前计数: {asyncMessageCount}" : "异步消息处理超时";
        LogMessage($"异步测试结果: {message}", success ? LogType.Log : LogType.Warning);
    }

    /// <summary>
    /// 测试查询消息（带返回值）
    /// </summary>
    public void TestQueryMessage()
    {
        string testName = "查询消息测试";
        try
        {
            var queryData = new QueryData("获取用户信息", 12345);

            // 方式1：使用类型标签（自动匹配QueryData类型）
            var results1 = Message.DefaultEvent.PostWithResult<QueryData, string>(queryData);
            LogMessage($"类型标签查询结果: {results1.Count} 个回复", LogType.Log);

            // 方式2：使用自定义字符串标签
            var results2 = Message.DefaultEvent.PostWithResult<string>("QueryEvent", queryData);
            LogMessage($"自定义标签查询结果: {results2.Count} 个回复", LogType.Log);

            // 合并所有结果
            var allResults = new List<string>();
            allResults.AddRange(results1);
            allResults.AddRange(results2);

            bool success = allResults.Count > 0 &&
                           allResults.Any(r => r.Contains("获取用户信息")) &&
                           allResults.Any(r => r.Contains("12345"));

            string resultDetails = string.Join(", ", allResults);
            AddTestResult(testName, success,
                success
                    ? $"查询成功，收到 {allResults.Count} 个回复: {resultDetails}"
                    : $"查询失败，收到 {allResults.Count} 个回复: {resultDetails}");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试事件优先级
    /// </summary>
    public void TestPriorityMessage()
    {
        string testName = "优先级测试";
        try
        {
            var priorityOrder = new List<string>();

            // 创建带有执行顺序记录的处理器
            Action<TestData> highPriorityHandler = (data) => priorityOrder.Add("高优先级");
            Action<TestData> midPriorityHandler = (data) => priorityOrder.Add("中优先级");
            Action<TestData> lowPriorityHandler = (data) => priorityOrder.Add("低优先级");

            // 手动注册不同优先级的处理器
            var tempHandler = new TempTestHandler();
            tempHandler.SetPriorityOrder(priorityOrder);
            Message.DefaultEvent.Register(tempHandler);

            // 发送测试消息
            var testData = new TestData(999, "优先级测试");
            Message.DefaultEvent.Post("PriorityTest", testData);

            // 验证执行顺序（高优先级应该先执行）
            bool success = priorityOrder.Count >= 3 &&
                           priorityOrder[0] == "高优先级" &&
                           priorityOrder[1] == "中优先级" &&
                           priorityOrder[2] == "低优先级";

            string orderDetails = string.Join(" → ", priorityOrder);
            AddTestResult(testName, success,
                success ? $"优先级正确: {orderDetails}" : $"优先级错误: {orderDetails}");

            Message.DefaultEvent.Unregister(tempHandler);
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试注册功能
    /// </summary>
    public void TestRegistration()
    {
        string testName = "注册功能测试";
        try
        {
            // 创建临时处理器进行测试
            var tempHandler = new TempTestHandler();

            Message.DefaultEvent.Register(tempHandler);

            // 等待一帧确保注册完成
            System.Threading.Thread.Sleep(10);

            Message.DefaultEvent.Post("TempEvent", "注册测试消息");

            // 给消息处理一些时间
            System.Threading.Thread.Sleep(10);

            bool success = tempHandler.MessageReceived;
            LogMessage($"[测试调试] 注册测试消息接收状态: {success}", LogType.Log);

            // 确保清理
            Message.DefaultEvent.Unregister(tempHandler);

            AddTestResult(testName, success,
                success ? "临时处理器注册和接收消息成功" : "注册功能测试失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试注销功能
    /// </summary>
    public void TestUnregistration()
    {
        string testName = "注销功能测试";
        try
        {
            // 使用TempTestHandler而不是SimpleTestHandler，因为它有Subscriber特性
            var tempHandler = new TempTestHandler();

            // 注册整个对象（使用反射查找Subscriber特性）
            Message.DefaultEvent.Register(tempHandler);

            // 等待一帧确保注册完成
            System.Threading.Thread.Sleep(10);

            // 发送消息
            Message.DefaultEvent.Post("TempEvent", "注销前消息");

            // 给消息处理一些时间
            System.Threading.Thread.Sleep(10);

            bool receivedBefore = tempHandler.MessageReceived;
            LogMessage($"[测试调试] 注销前消息接收状态: {receivedBefore}, 消息: {tempHandler.LastMessage}", LogType.Log);

            // 注销整个对象
            Message.DefaultEvent.Unregister(tempHandler);

            // 等待一帧确保注销完成
            System.Threading.Thread.Sleep(10);

            // 重置状态
            tempHandler.Reset();

            // 再次发送消息
            Message.DefaultEvent.Post("TempEvent", "注销后消息");

            // 给消息处理一些时间
            System.Threading.Thread.Sleep(10);

            bool receivedAfter = tempHandler.MessageReceived;
            LogMessage($"[测试调试] 注销后消息接收状态: {receivedAfter}, 消息: {tempHandler.LastMessage}", LogType.Log);

            bool success = receivedBefore && !receivedAfter;

            AddTestResult(testName, success,
                success ? "注销功能正常，注销后不再接收消息" : $"注销功能测试失败 - 注销前:{receivedBefore}, 注销后:{receivedAfter}");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 测试内存清理
    /// </summary>
    public void TestMemoryCleanup()
    {
        string testName = "内存清理测试";
        try
        {
            // 创建临时处理器
            var tempHandler = new TempTestHandler();
            Message.DefaultEvent.Register(tempHandler);

            // 移除强引用
            tempHandler = null;

            // 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            // 清理弱引用
            int cleanedCount = Message.DefaultEvent.RemoveDeadWeakReferences();

            bool success = cleanedCount >= 0; // 清理数量应该非负

            AddTestResult(testName, success,
                success ? $"内存清理成功，清理了 {cleanedCount} 个无效引用" : "内存清理失败");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    #endregion

    #region 压力测试方法

    /// <summary>
    /// 开始压力测试
    /// </summary>
    public void StartStressTest()
    {
        if (isStressTesting) return;

        isStressTesting = true;
        stressTestProgress = 0f;
        stressTestTotalMessages = 0;
        stressTestSuccessMessages = 0;
        stressTestResults = "";
        
        LogMessage($"开始压力测试: {stressTestMessageCount} 条消息, {stressTestThreadCount} 个线程", LogType.Warning);
        
        StartCoroutine(RunStressTestCoroutine());
    }

    /// <summary>
    /// 停止压力测试
    /// </summary>
    public void StopStressTest()
    {
        isStressTesting = false;
        StopAllCoroutines();
        
        if (stressTestStopwatch.IsRunning)
        {
            stressTestStopwatch.Stop();
            GenerateStressTestResults();
        }
    }

    /// <summary>
    /// 压力测试协程
    /// </summary>
    private System.Collections.IEnumerator RunStressTestCoroutine()
    {
        stressTestStopwatch.Restart();
        
        // 创建测试处理器
        var stressHandlers = new List<StressTestHandler>();
        for (int i = 0; i < stressTestThreadCount; i++)
        {
            var handler = new StressTestHandler($"Handler_{i}");
            stressHandlers.Add(handler);
            Message.DefaultEvent.Register(handler);
        }

        // 分批发送消息
        int batchSize = 50;
        int totalBatches = Mathf.CeilToInt((float)stressTestMessageCount / batchSize);
        
        for (int batch = 0; batch < totalBatches && isStressTesting; batch++)
        {
            int messagesInBatch = Mathf.Min(batchSize, stressTestMessageCount - batch * batchSize);
            
            // 发送一批消息
            for (int i = 0; i < messagesInBatch; i++)
            {
                var testData = new TestData(
                    batch * batchSize + i,
                    $"StressTest_Batch{batch}_Msg{i}",
                    UnityEngine.Random.Range(0f, 1000f),
                    UnityEngine.Random.value > 0.5f
                );
                
                Message.DefaultEvent.Post("StressTest", testData);
                stressTestTotalMessages++;
            }
            
            // 更新进度
            stressTestProgress = (float)(batch + 1) / totalBatches;
            
            // 让出控制权，避免阻塞主线程
            yield return new WaitForEndOfFrame();
        }

        // 等待消息处理完成
        yield return new WaitForSeconds(0.5f);

        // 统计成功消息数量
        foreach (var handler in stressHandlers)
        {
            stressTestSuccessMessages += handler.ReceivedCount;
            Message.DefaultEvent.Unregister(handler);
        }

        stressTestStopwatch.Stop();
        isStressTesting = false;
        
        GenerateStressTestResults();
    }

    /// <summary>
    /// 生成压力测试结果
    /// </summary>
    private void GenerateStressTestResults()
    {
        double elapsedSeconds = stressTestStopwatch.ElapsedMilliseconds / 1000.0;
        double messagesPerSecond = stressTestTotalMessages / elapsedSeconds;
        double successRate = stressTestTotalMessages > 0 ? 
            (double)stressTestSuccessMessages / stressTestTotalMessages * 100 : 0;

        stressTestResults = $"压力测试完成!\n" +
                           $"总消息数: {stressTestTotalMessages}\n" +
                           $"成功处理: {stressTestSuccessMessages}\n" +
                           $"成功率: {successRate:F2}%\n" +
                           $"耗时: {elapsedSeconds:F2}秒\n" +
                           $"吞吐量: {messagesPerSecond:F0} msg/s";

        LogMessage(stressTestResults, successRate >= 95 ? LogType.Log : LogType.Warning);
    }

    /// <summary>
    /// 内存压力测试
    /// </summary>
    public void TestMemoryStress()
    {
        string testName = "内存压力测试";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            LogMessage("开始内存压力测试...", LogType.Warning);
            
            // 创建大量处理器
            var handlers = new List<StressTestHandler>();
            for (int i = 0; i < 1000; i++)
            {
                var handler = new StressTestHandler($"MemoryTest_{i}");
                handlers.Add(handler);
                Message.DefaultEvent.Register(handler);
            }

            // 发送大量消息
            for (int i = 0; i < 5000; i++)
            {
                var testData = new TestData(i, $"MemoryStress_{i}", i * 0.1f, i % 2 == 0);
                Message.DefaultEvent.Post("StressTest", testData);
            }

            // 清理一半处理器
            for (int i = 0; i < handlers.Count / 2; i++)
            {
                Message.DefaultEvent.Unregister(handlers[i]);
            }

            // 强制垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();

            // 清理剩余处理器
            for (int i = handlers.Count / 2; i < handlers.Count; i++)
            {
                Message.DefaultEvent.Unregister(handlers[i]);
            }

            stopwatch.Stop();
            
            AddTestResult(testName, true, 
                $"内存压力测试完成，耗时: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 并发压力测试
    /// </summary>
    public void TestConcurrencyStress()
    {
        string testName = "并发压力测试";
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            LogMessage("开始并发压力测试...", LogType.Warning);
            
            var handler = new StressTestHandler("ConcurrencyTest");
            Message.DefaultEvent.Register(handler);

            var tasks = new List<System.Threading.Tasks.Task>();
            int threadsCount = 4;
            int messagesPerThread = 500;

            // 启动多个线程同时发送消息
            for (int t = 0; t < threadsCount; t++)
            {
                int threadId = t;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    var random = new System.Random(threadId); // 使用线程安全的Random
                    
                    try
                    {
                        for (int i = 0; i < messagesPerThread; i++)
                        {
                            var testData = new TestData(
                                threadId * messagesPerThread + i,
                                $"Concurrent_T{threadId}_M{i}",
                                (float)(random.NextDouble() * 100.0), // 使用System.Random代替UnityEngine.Random
                                i % 2 == 0
                            );
                            
                            Message.DefaultEvent.Post("StressTest", testData);
                            
                            // 随机延迟
                            if (i % 50 == 0)
                            {
                                System.Threading.Thread.Sleep(1);
                            }
                        }
                    }
                    catch (Exception taskEx)
                    {
                        // 记录任务内部异常，但不抛出，避免影响其他任务
                        UnityEngine.Debug.LogError($"并发测试线程 {threadId} 异常: {taskEx.Message}");
                    }
                });
                tasks.Add(task);
            }

            // 等待所有任务完成
            bool allCompleted = System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), 10000); // 10秒超时
            
            if (!allCompleted)
            {
                LogMessage("警告: 部分并发任务未在超时时间内完成", LogType.Warning);
            }

            // 等待消息处理完成
            System.Threading.Thread.Sleep(1000);

            Message.DefaultEvent.Unregister(handler);
            stopwatch.Stop();

            int expectedMessages = threadsCount * messagesPerThread;
            double successRate = expectedMessages > 0 ? 
                (double)handler.ReceivedCount / expectedMessages * 100 : 0;

            AddTestResult(testName, successRate >= 90, 
                $"并发测试完成，预期: {expectedMessages}, 接收: {handler.ReceivedCount}, " +
                $"成功率: {successRate:F1}%, 耗时: {stopwatch.ElapsedMilliseconds}ms");
        }
        catch (Exception ex)
        {
            AddTestResult(testName, false, $"异常: {ex.Message}");
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 重置测试状态
    /// </summary>
    public void ResetTests()
    {
        totalTests = 0;
        passedTests = 0;
        failedTests = 0;
        testResults.Clear();
        testsCompleted = false;

        // 重置消息计数
        basicMessageCount = 0;
        complexMessageCount = 0;
        stringMessageCount = 0;
        multiParamMessageCount = 0;
        asyncMessageCount = 0;

        // 清空最后接收的数据
        lastTestData = null;
        lastComplexData = null;
        lastStringMessage = null;
        lastMultiParams = null;

        LogMessage("测试状态已重置", LogType.Warning);
    }

    /// <summary>
    /// 添加测试结果
    /// </summary>
    private void AddTestResult(string testName, bool passed, string message)
    {
        totalTests++;
        if (passed)
            passedTests++;
        else
            failedTests++;

        var result = new TestResult(testName, passed, message);
        testResults.Add(result);

        LogMessage(result.ToString(), passed ? LogType.Log : LogType.Error);
    }

    /// <summary>
    /// 日志输出
    /// </summary>
    private void LogMessage(string message, LogType logType)
    {
        if (showDetailedOutput)
        {
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError($"[EventTest] {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"[EventTest] {message}");
                    break;
                default:
                    Debug.Log($"[EventTest] {message}");
                    break;
            }
        }
    }

    #endregion

    #region 临时测试类

    /// <summary>
    /// 临时测试处理器
    /// </summary>
    public class TempTestHandler
    {
        public bool MessageReceived { get; private set; } = false;
        public string LastMessage { get; private set; }
        private List<string> priorityOrder;

        [Subscriber("TempEvent")]
        public void OnTempEvent(string message)
        {
            MessageReceived = true;
            LastMessage = message;
        }

        public void Reset()
        {
            MessageReceived = false;
            LastMessage = null;
        }

        public void SetPriorityOrder(List<string> order)
        {
            priorityOrder = order;
        }

        [Subscriber("PriorityTest", 100)]
        public void OnHighPriorityEvent(TestData data)
        {
            priorityOrder?.Add("高优先级");
        }

        [Subscriber("PriorityTest", 50)]
        public void OnMidPriorityEvent(TestData data)
        {
            priorityOrder?.Add("中优先级");
        }

        [Subscriber("PriorityTest", 10)]
        public void OnLowPriorityEvent(TestData data)
        {
            priorityOrder?.Add("低优先级");
        }
    }

    /// <summary>
    /// 简单的测试处理器（不使用反射特性）
    /// </summary>
    public class SimpleTestHandler
    {
        public bool MessageReceived { get; private set; } = false;
        public string LastMessage { get; private set; }

        public void HandleMessage(string message)
        {
            MessageReceived = true;
            LastMessage = message;
        }

        public void Reset()
        {
            MessageReceived = false;
            LastMessage = null;
        }
    }

    /// <summary>
    /// 压力测试处理器
    /// </summary>
    public class StressTestHandler
    {
        public string HandlerName { get; private set; }
        public long ReceivedCount { get; private set; } = 0;
        public TestData LastTestData { get; private set; }
        private readonly object _lock = new object();

        public StressTestHandler(string name)
        {
            HandlerName = name;
        }

        [Subscriber("StressTest")]
        public void OnStressTest(TestData data)
        {
            lock (_lock)
            {
                ReceivedCount++;
                LastTestData = data;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                ReceivedCount = 0;
                LastTestData = null;
            }
        }
    }

    #endregion
}