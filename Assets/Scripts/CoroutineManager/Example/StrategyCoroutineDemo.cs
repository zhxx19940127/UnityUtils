using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading;
using System;

/// <summary>
/// 策略模式协程演示脚本
/// 展示如何使用策略模式来管理协程
/// </summary>
public class StrategyCoroutineDemo : MonoBehaviour
{
    private Transform cubeTransform;
    private Image fadeImage; // 可选：用于透明度演示


    [Header("GUI设置")] public bool showGUI = true;
    public int buttonWidth = 200;
    public int buttonHeight = 30;
    public int spacing = 5;

    [Header("演示参数")] public float delayTime = 2f;
    public float repeatInterval = 1f;
    public int repeatCount = 3;
    public float lerpDuration = 2f;
    public float shakeDuration = 1.5f;

    private List<string> logMessages = new List<string>();

    void Start()
    {
        // 自动创建演示对象
        AutoCreateDemoObjects();

        LogMessage("策略协程演示系统已初始化");
    }

    /// <summary>
    /// OnGUI绘制界面
    /// </summary>
    void OnGUI()
    {
        if (!showGUI) return;

        // 设置GUI皮肤样式
        GUI.skin.button.fontSize = 12;
        GUI.skin.label.fontSize = 11;
        GUI.skin.box.fontSize = 10;

        int startX = 10;
        int startY = 10;
        int currentY = startY;

        // 标题
        GUI.Label(new Rect(startX, currentY, 300, 25), "策略模式协程演示系统", GUI.skin.GetStyle("Box"));
        currentY += 30;

        // 单一策略按钮
        GUI.Label(new Rect(startX, currentY, 200, 20), "单一策略演示:");
        currentY += 25;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "延迟策略"))
        {
            DelayStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "重复策略"))
        {
            RepeatStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "插值动画策略"))
        {
            LerpStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "震动策略"))
        {
            ShakeStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        // 复合策略按钮
        GUI.Label(new Rect(startX, currentY, 200, 20), "复合策略演示:");
        currentY += 25;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "批量策略"))
        {
            BatchStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "嵌套回调链式"))
        {
            ChainedStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "流畅链式调用"))
        {
            FluentChainDemo();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "自定义复合策略"))
        {
            CustomCompositeStrategyDemo();
        }

        currentY += buttonHeight + spacing;

        // 控制按钮
        GUI.Label(new Rect(startX, currentY, 200, 20), "控制操作:");
        currentY += 25;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "停止所有协程"))
        {
            StopAllCoroutines();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "停止动画协程"))
        {
            StopAnimationCoroutines();
        }

        currentY += buttonHeight + spacing;

        if (GUI.Button(new Rect(startX, currentY, buttonWidth, buttonHeight), "重置演示对象"))
        {
            ResetDemoObjects();
        }

        currentY += buttonHeight + spacing * 2;

        // 日志显示区域
        DrawLogArea(startX, currentY);

        // 统计信息显示区域（放在日志区域下方）
        DrawStatsArea(startX, currentY + 240);
    }

    /// <summary>
    /// 绘制日志区域
    /// </summary>
    void DrawLogArea(int x, int y)
    {
        GUI.Label(new Rect(x, y, 300, 20), "运行日志:");
        y += 25;

        string logText = string.Join("\n", logMessages);

        // 设置日志文本样式
        GUIStyle logStyle = new GUIStyle(GUI.skin.box);
        logStyle.alignment = TextAnchor.UpperLeft;
        logStyle.wordWrap = true;
        logStyle.normal.textColor = Color.white;

        GUI.Box(new Rect(x, y, 300, 200), logText, logStyle);
    }

    /// <summary>
    /// 延迟策略演示
    /// </summary>
    void DelayStrategyDemo()
    {
        var strategy = new DelayedActionStrategy(
            () => LogMessage("策略模式延迟执行完成！"),
            delayTime,
            "StrategyDelay"
        );

        var coroutine = CoroutineManager.StartCoroutine(strategy);
        LogMessage($"启动策略延迟协程: {CoroutineManager.GetCoroutineName(coroutine)}");
    }

    /// <summary>
    /// 重复策略演示
    /// </summary>
    void RepeatStrategyDemo()
    {
        LogMessage($"启动策略重复协程: 间隔{repeatInterval}秒, 执行{repeatCount}次");
        var strategy = new RepeatActionStrategy(
            () => LogMessage($"策略重复执行 #{DateTime.Now:HH:mm:ss}"),
            repeatInterval,
            repeatCount,
            "StrategyRepeat"
        );
        CoroutineManager.StartCoroutine(strategy);
    }

    /// <summary>
    /// 插值动画策略演示
    /// </summary>
    void LerpStrategyDemo()
    {
        if (cubeTransform != null)
        {
            Vector3 startPos = cubeTransform.position;
            Vector3 endPos = startPos + Vector3.up * 3f;

            var strategy = new LerpVector3Strategy(
                startPos,
                endPos,
                lerpDuration,
                pos => cubeTransform.position = pos,
                CoroutineManager.Easing.EaseInOutSine,
                "StrategyLerp"
            );

            CoroutineManager.StartCoroutine(strategy);
            LogMessage($"启动策略插值动画，持续 {lerpDuration} 秒");
        }
    }

    /// <summary>
    /// 震动策略演示
    /// </summary>
    void ShakeStrategyDemo()
    {
        if (cubeTransform != null)
        {
            var strategy = new ShakeStrategy(
                cubeTransform,
                shakeDuration,
                0.3f,
                20f,
                "StrategyShake"
            );

            CoroutineManager.StartCoroutine(strategy);
            LogMessage($"启动策略震动效果，持续 {shakeDuration} 秒");
        }
    }

    /// <summary>
    /// 停止所有协程
    /// </summary>
    new void StopAllCoroutines()
    {
        int count = CoroutineManager.StopAllManagedCoroutines();
        LogMessage($"停止了 {count} 个协程");
    }

    /// <summary>
    /// 停止动画协程
    /// </summary>
    void StopAnimationCoroutines()
    {
        int count = CoroutineManager.StopCoroutinesByCategory("Animation");
        if (count > 0)
            LogMessage($"停止了 {count} 个动画协程");
        else
            LogMessage("没有找到动画协程");
    }

    /// <summary>
    /// 批量策略演示
    /// </summary>
    void BatchStrategyDemo()
    {
        LogMessage("启动批量策略演示");

        // 创建多个不同类型的策略
        var strategies = new ICoroutineStrategy[]
        {
            new DelayedActionStrategy(
                () => LogMessage("批量策略1: 延迟1秒完成"),
                1f,
                "Batch1"
            ),

            new DelayedActionStrategy(
                () => LogMessage("批量策略2: 延迟2秒完成"),
                2f,
                "Batch2"
            ),

            new RepeatActionStrategy(
                () => LogMessage("批量策略3: 重复执行"),
                0.5f,
                3,
                "Batch3"
            ),

            new ExecuteWhenStrategy(
                () => LogMessage("批量策略4: 条件满足"),
                () => Time.time % 5f < 1f, // 每5秒满足一次条件
                5f,
                "Batch4"
            )
        };

        // 批量启动策略
        var coroutines = CoroutineManager.StartCoroutines(strategies);
        LogMessage($"批量启动了 {coroutines.Count} 个策略协程");
    }

    /// <summary>
    /// 嵌套回调策略演示（旧方式）
    /// </summary>
    void ChainedStrategyDemo()
    {
        LogMessage("启动嵌套回调策略演示");

        // 第一步：延迟策略
        var step1Strategy = new DelayedActionStrategy(
            () =>
            {
                LogMessage("嵌套回调 - 步骤1完成，启动步骤2");

                // 第二步：在第一步完成后启动插值策略
                if (cubeTransform != null)
                {
                    var step2Strategy = new LerpVector3Strategy(
                        cubeTransform.position,
                        cubeTransform.position + Vector3.right * 2f,
                        1f,
                        pos => cubeTransform.position = pos,
                        CoroutineManager.Easing.EaseOutBounce,
                        "ChainStep2"
                    );

                    CoroutineManager.StartCoroutine(step2Strategy);

                    // 第三步：在第二步启动后再启动震动策略
                    CoroutineManager.DelayedAction(() =>
                    {
                        LogMessage("嵌套回调 - 步骤3开始");
                        var step3Strategy = new ShakeStrategy(
                            cubeTransform,
                            1f,
                            0.2f,
                            15f,
                            "ChainStep3"
                        );
                        CoroutineManager.StartCoroutine(step3Strategy);
                    }, 0.5f, "ChainStep3Trigger");
                }
            },
            1.5f,
            "ChainStep1"
        );

        CoroutineManager.StartCoroutine(step1Strategy);
    }

    /// <summary>
    /// 流畅链式调用演示（新方式）
    /// </summary>
    void FluentChainDemo()
    {
        LogMessage("启动流畅链式调用演示");

        if (cubeTransform != null)
        {
            // 这才是真正的链式调用！
            CoroutineChain.Create()
                .ThenDo(() => LogMessage("链式步骤1: 开始动画序列"))
                .ThenDelay(0.5f)
                .ThenDo(() => LogMessage("链式步骤2: 延迟完成，开始移动"))
                .ThenLerpVector3(
                    cubeTransform.position,
                    cubeTransform.position + Vector3.up * 2f,
                    1f,
                    pos => cubeTransform.position = pos,
                    CoroutineManager.Easing.EaseOutBounce
                )
                .ThenDo(() => LogMessage("链式步骤3: 向上移动完成，开始水平移动"))
                .ThenLerpVector3(
                    cubeTransform.position,
                    cubeTransform.position + Vector3.right * 2f,
                    1f,
                    pos => cubeTransform.position = pos,
                    CoroutineManager.Easing.EaseInOutSine
                )
                .ThenDo(() => LogMessage("链式步骤4: 水平移动完成，开始震动"))
                .ThenShake(cubeTransform, 1f, 0.2f, 15f)
                .ThenDo(() => LogMessage("链式步骤5: 震动完成，开始颜色渐变"))
                .ThenLerpValue(1f, 0f, 1f, alpha =>
                {
                    if (fadeImage != null)
                    {
                        var color = fadeImage.color;
                        color.a = alpha;
                        fadeImage.color = color;
                    }
                }, CoroutineManager.Easing.EaseInOutQuad)
                .ThenDo(() => LogMessage("链式步骤6: 全部完成！"))
                .Start("FluentChain");

            LogMessage("流畅链式调用设置完成，开始执行");
        }
    }

    /// <summary>
    /// 自定义复合策略演示
    /// </summary>
    void CustomCompositeStrategyDemo()
    {
        LogMessage("启动自定义复合策略演示");

        // 创建一个自定义策略，内部组合多个行为
        var compositeStrategy = new CustomCompositeStrategy();
        CoroutineManager.StartCoroutine(compositeStrategy);
    }

    /// <summary>
    /// 记录消息
    /// </summary>
    private void LogMessage(string message)
    {
        string timeStamp = DateTime.Now.ToString("HH:mm:ss");
        string fullMessage = $"[{timeStamp}] {message}";

        logMessages.Add(fullMessage);
        if (logMessages.Count > 8)
        {
            logMessages.RemoveAt(0);
        }

        // 日志现在通过OnGUI显示
        Debug.Log(fullMessage);
    }

    /// <summary>
    /// 绘制统计信息区域
    /// </summary>
    void DrawStatsArea(int x, int y)
    {
        GUI.Label(new Rect(x, y, 250, 20), "协程统计信息:");
        y += 25;

        var stats = CoroutineManager.GetCoroutineStats();
        string statsText = stats.ToString(); // 转换为字符串

        // 将每个逗号替换成换行符，让统计信息更易读
        statsText = statsText.Replace(",", "\n\n");

        // 设置文本样式以支持换行
        GUIStyle textStyle = new GUIStyle(GUI.skin.box);
        textStyle.alignment = TextAnchor.UpperLeft;
        textStyle.wordWrap = true;
        textStyle.normal.textColor = Color.white;

        GUI.Box(new Rect(x, y, 250, 150), statsText, textStyle);
    }


    #region 自动创建演示对象

    /// <summary>
    /// 自动创建演示所需的对象
    /// </summary>
    void AutoCreateDemoObjects()
    {
        // 创建立方体演示对象
        if (cubeTransform == null)
        {
            CreateDemoCube();
        }

        // 创建UI画布和渐变图片
        if (fadeImage == null)
        {
            CreateDemoUI();
        }
    }

    /// <summary>
    /// 创建演示用的立方体
    /// </summary>
    void CreateDemoCube()
    {
        // 创建立方体GameObject
        GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObj.name = "Demo_Cube";

        // 设置位置到摄像机前方
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 pos = mainCam.transform.position + mainCam.transform.forward * 5f;
            cubeObj.transform.position = pos;
        }
        else
        {
            cubeObj.transform.position = new Vector3(0, 1, 0);
        }

        // 设置缩放
        cubeObj.transform.localScale = Vector3.one * 0.8f;


        // 创建一个简单的彩色材质
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.7f, 1f, 1f); // 浅蓝色
        cubeObj.GetComponent<Renderer>().material = mat;

        cubeTransform = cubeObj.transform;
        LogMessage("自动创建了演示立方体");
    }

    /// <summary>
    /// 创建演示用的UI界面
    /// </summary>
    void CreateDemoUI()
    {
        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Demo_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在最上层

            // 添加CanvasScaler和GraphicRaycaster
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            LogMessage("自动创建了UI画布");
        }

        // 创建渐变图片
        GameObject imageObj = new GameObject("Demo_FadeImage");
        imageObj.transform.SetParent(canvas.transform, false);

        // 添加Image组件
        fadeImage = imageObj.AddComponent<Image>();

        // 设置RectTransform
        RectTransform rectTransform = imageObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // 设置图片属性
        fadeImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色

        // 创建一个简单的1x1白色贴图
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.zero);
        fadeImage.sprite = sprite;

        LogMessage("自动创建了渐变UI图片");
    }

    /// <summary>
    /// 重置演示对象到初始状态
    /// </summary>
    [ContextMenu("重置演示对象")]
    void ResetDemoObjects()
    {
        if (cubeTransform != null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 pos = mainCam.transform.position + mainCam.transform.forward * 5f;
                cubeTransform.position = pos;
            }
            else
            {
                cubeTransform.position = new Vector3(0, 1, 0);
            }

            cubeTransform.rotation = Quaternion.identity;
            cubeTransform.localScale = Vector3.one * 0.8f;
        }

        if (fadeImage != null)
        {
            fadeImage.color = new Color(0, 0, 0, 0.5f);
        }

        LogMessage("演示对象已重置");
    }

    #endregion

    void OnDestroy()
    {
        CoroutineManager.StopAllManagedCoroutines();
    }
}

/// <summary>
/// 自定义复合策略示例
/// 展示如何创建复杂的自定义策略
/// </summary>
public class CustomCompositeStrategy : CoroutineStrategyBase
{
    public CustomCompositeStrategy() : base("CompositeStrategy", "Custom", true)
    {
    }

    public override System.Collections.IEnumerator CreateCoroutine()
    {
        Debug.Log("复合策略开始执行");

        // 第一阶段：等待
        yield return new WaitForSeconds(1f);
        Debug.Log("复合策略 - 第一阶段完成");

        // 第二阶段：重复执行一些操作
        for (int i = 0; i < 3; i++)
        {
            Debug.Log($"复合策略 - 第二阶段步骤 {i + 1}");
            yield return new WaitForSeconds(0.5f);
        }

        // 第三阶段：并行启动其他策略
        var parallelStrategy1 = new DelayedActionStrategy(
            () => Debug.Log("并行策略1完成"),
            1f,
            "Parallel1"
        );

        var parallelStrategy2 = new DelayedActionStrategy(
            () => Debug.Log("并行策略2完成"),
            1.5f,
            "Parallel2"
        );

        CoroutineManager.StartCoroutine(parallelStrategy1);
        CoroutineManager.StartCoroutine(parallelStrategy2);

        Debug.Log("复合策略执行完成");
    }

    protected override string GenerateDefaultName()
    {
        return "CustomComposite";
    }
}