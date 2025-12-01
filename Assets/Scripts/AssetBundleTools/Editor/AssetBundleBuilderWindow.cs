using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AssetBundle打包工具面板
/// 提供可视化界面，支持拖拽配置路径
/// </summary>
public class AssetBundleBuilderWindow : EditorWindow
{
    // 配置路径
    private string outputPath = "Assets/StreamingAssets/AssetBundles";
    private string modelsPath = "Assets/Models";
    private string copyToPath = ""; // 复制目标路径

    // 拖拽对象引用
    private Object outputFolderObj;
    private Object modelsFolderObj;

    // 构建选项
    private BuildTarget buildTarget = BuildTarget.WebGL;

    private BuildAssetBundleOptions buildOptions =
        BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode;

    // Shader检测选项
    private bool autoCheckShaders = true; // 默认开启自动检测Shader

    // 模型选择列表
    private List<ModelInfo> modelList = new List<ModelInfo>();
    private Vector2 modelListScrollPosition;
    private bool isAllSelected = true;

    // UI相关
    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;

    [MenuItem("AssetBundle/打包工具面板", priority = 0)]
    public static void ShowWindow()
    {
        AssetBundleBuilderWindow window = GetWindow<AssetBundleBuilderWindow>("AB包打包工具");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnEnable()
    {
        // 加载保存的配置
        LoadSettings();

        // 尝试加载文件夹对象
        if (AssetDatabase.IsValidFolder(outputPath))
            outputFolderObj = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
        if (AssetDatabase.IsValidFolder(modelsPath))
            modelsFolderObj = AssetDatabase.LoadAssetAtPath<Object>(modelsPath);

        // 刷新模型列表
        RefreshModelList();
    }

    private void OnDisable()
    {
        // 保存配置
        SaveSettings();
    }

    private void OnGUI()
    {
        InitStyles();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // ===== 标题 =====
        GUILayout.Space(10);
        GUILayout.Label("AssetBundle 打包工具", headerStyle);
        GUILayout.Space(10);

        // ===== 路径配置区 =====
        DrawPathConfigSection();

        GUILayout.Space(10);

        // ===== 模型选择区 =====
        DrawModelSelectionSection();

        GUILayout.Space(10);
        // ===== Shader检测区 =====
        DrawShaderCheckSection();

        GUILayout.Space(10);

        // ===== 构建选项区 =====
        DrawBuildOptionsSection();

        GUILayout.Space(10);

        // ===== 操作按钮区 =====
        DrawActionButtonsSection();

        GUILayout.Space(10);

        // ===== 复制AB包区 =====
        DrawCopySection();

        GUILayout.Space(10);


        // ===== 工具按钮区 =====
        DrawUtilityButtonsSection();

        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 初始化样式
    /// </summary>
    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }
    }

    /// <summary>
    /// 绘制路径配置区域
    /// </summary>
    private void DrawPathConfigSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("路径配置", EditorStyles.boldLabel);
        GUILayout.Space(5); // AB包输出路径
        EditorGUILayout.LabelField("AB包输出路径", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        // 文件夹拖拽区域
        Object newOutputFolder =
            EditorGUILayout.ObjectField(outputFolderObj, typeof(Object), false, GUILayout.Height(20));
        if (newOutputFolder != outputFolderObj)
        {
            outputFolderObj = newOutputFolder;
            if (outputFolderObj != null)
            {
                string path = AssetDatabase.GetAssetPath(outputFolderObj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    outputPath = path;
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请拖入一个文件夹", "确定");
                    outputFolderObj = null;
                }
            }
        }

        // 创建按钮
        if (GUILayout.Button("创建", GUILayout.Width(50)))
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                AssetDatabase.Refresh();
                outputFolderObj = AssetDatabase.LoadAssetAtPath<Object>(outputPath);
                Debug.Log($"已创建文件夹: {outputPath}");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "文件夹已存在", "确定");
            }
        }

        // 打开按钮
        if (GUILayout.Button("打开", GUILayout.Width(50)))
        {
            string fullPath = Path.GetFullPath(outputPath);
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "文件夹不存在", "确定");
            }
        }

        EditorGUILayout.EndHorizontal();

        // 显示路径文本
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(outputPath);
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(10);

        // 模型文件夹路径
        EditorGUILayout.LabelField("模型文件夹路径", EditorStyles.miniBoldLabel);
        EditorGUILayout.BeginHorizontal();

        // 文件夹拖拽区域
        Object newModelsFolder =
            EditorGUILayout.ObjectField(modelsFolderObj, typeof(Object), false, GUILayout.Height(20));
        if (newModelsFolder != modelsFolderObj)
        {
            modelsFolderObj = newModelsFolder;
            if (modelsFolderObj != null)
            {
                string path = AssetDatabase.GetAssetPath(modelsFolderObj);
                if (AssetDatabase.IsValidFolder(path))
                {
                    modelsPath = path;
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "请拖入一个文件夹", "确定");
                    modelsFolderObj = null;
                }
            }
        }

        // 创建按钮
        if (GUILayout.Button("创建", GUILayout.Width(50)))
        {
            if (!Directory.Exists(modelsPath))
            {
                Directory.CreateDirectory(modelsPath);
                AssetDatabase.Refresh();
                modelsFolderObj = AssetDatabase.LoadAssetAtPath<Object>(modelsPath);
                Debug.Log($"已创建文件夹: {modelsPath}");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "文件夹已存在", "确定");
            }
        }

        // 打开按钮
        if (GUILayout.Button("打开", GUILayout.Width(50)))
        {
            string fullPath = Path.GetFullPath(modelsPath);
            if (Directory.Exists(fullPath))
            {
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "文件夹不存在", "确定");
            }
        }

        EditorGUILayout.EndHorizontal();

        // 显示路径文本
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField(modelsPath);
        EditorGUI.EndDisabledGroup();

        // 统计信息
        if (Directory.Exists(modelsPath))
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });
            EditorGUILayout.HelpBox($"检测到 {prefabGuids.Length} 个预制体", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("模型文件夹不存在", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制模型选择列表区域
    /// </summary>
    private void DrawModelSelectionSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("模型选择列表", EditorStyles.boldLabel);

        if (GUILayout.Button("刷新", GUILayout.Width(60)))
        {
            RefreshModelList();
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (modelList.Count == 0)
        {
            EditorGUILayout.HelpBox("暂无模型，请检查模型文件夹路径", MessageType.Info);
        }
        else
        {
            // 全选/取消全选按钮
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("全选", GUILayout.Height(25)))
            {
                foreach (var model in modelList)
                {
                    model.isSelected = true;
                }

                isAllSelected = true;
            }

            if (GUILayout.Button("取消全选", GUILayout.Height(25)))
            {
                foreach (var model in modelList)
                {
                    model.isSelected = false;
                }

                isAllSelected = false;
            }

            if (GUILayout.Button("反选", GUILayout.Height(25)))
            {
                foreach (var model in modelList)
                {
                    model.isSelected = !model.isSelected;
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // 模型列表滚动区域
            modelListScrollPosition = EditorGUILayout.BeginScrollView(modelListScrollPosition, GUILayout.Height(150));

            foreach (var model in modelList)
            {
                EditorGUILayout.BeginHorizontal();

                model.isSelected = EditorGUILayout.Toggle(model.isSelected, GUILayout.Width(20));
                EditorGUILayout.LabelField(model.displayName, GUILayout.ExpandWidth(true));

                // 显示AB包名称（灰色小字）
                GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel);
                miniStyle.normal.textColor = Color.gray;
                EditorGUILayout.LabelField(model.bundleName, miniStyle, GUILayout.Width(150));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // 统计信息
            int selectedCount = modelList.Count(m => m.isSelected);
            EditorGUILayout.HelpBox($"共 {modelList.Count} 个模型，已选中 {selectedCount} 个", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制构建选项区域
    /// </summary>
    private void DrawBuildOptionsSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("高级构建选项,设置平台,压缩格式等...", EditorStyles.boldLabel);

        showAdvancedOptions =
            EditorGUILayout.Foldout(showAdvancedOptions, "高级构建选项", true, EditorStyles.foldoutHeader);

        if (showAdvancedOptions)
        {
            GUILayout.Space(5);

            buildTarget = (BuildTarget)EditorGUILayout.EnumPopup("目标平台", buildTarget);

            GUILayout.Space(5);

            EditorGUILayout.LabelField("压缩选项", EditorStyles.miniBoldLabel);
            bool useChunkCompression = (buildOptions & BuildAssetBundleOptions.ChunkBasedCompression) != 0;
            bool useStrictMode = (buildOptions & BuildAssetBundleOptions.StrictMode) != 0;
            bool forceRebuild = (buildOptions & BuildAssetBundleOptions.ForceRebuildAssetBundle) != 0;

            useChunkCompression = EditorGUILayout.Toggle("Chunk压缩 (LZ4)", useChunkCompression);
            useStrictMode = EditorGUILayout.Toggle("严格模式", useStrictMode);
            forceRebuild = EditorGUILayout.Toggle("强制重新构建", forceRebuild);

            buildOptions = BuildAssetBundleOptions.None;
            if (useChunkCompression) buildOptions |= BuildAssetBundleOptions.ChunkBasedCompression;
            if (useStrictMode) buildOptions |= BuildAssetBundleOptions.StrictMode;
            if (forceRebuild) buildOptions |= BuildAssetBundleOptions.ForceRebuildAssetBundle;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制操作按钮区域
    /// </summary>
    private void DrawActionButtonsSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("构建操作", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 主构建按钮
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("构建所有模型包", GUILayout.Height(40)))
        {
            BuildAllModelBundles();
        }

        int selectedCount = modelList.Count(m => m.isSelected);
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        EditorGUI.BeginDisabledGroup(selectedCount == 0);
        if (GUILayout.Button($"构建选中模型包 ({selectedCount})", GUILayout.Height(40)))
        {
            BuildSelectedModelBundles();
        }

        EditorGUI.EndDisabledGroup();

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("标记模型", GUILayout.Height(30)))
        {
            MarkModelAssetBundles();
        }

        if (GUILayout.Button("检测共享资源", GUILayout.Height(30)))
        {
            AutoMarkSharedDependencies();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("清除标记", GUILayout.Height(30)))
        {
            ClearAssetBundleNames();
        }

        if (GUILayout.Button("生成配置文件", GUILayout.Height(30)))
        {
            GenerateAssetBundleConfig();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制复制AB包区域
    /// </summary>
    private void DrawCopySection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("复制AB包", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.LabelField("目标路径（可以是项目外路径）", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        copyToPath = EditorGUILayout.TextField(copyToPath);

        if (GUILayout.Button("浏览", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("选择AB包复制目标路径", copyToPath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                copyToPath = selectedPath;
            }
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(copyToPath));

        GUI.backgroundColor = new Color(0.4f, 0.6f, 1f);
        if (GUILayout.Button("复制AB包到目标路径", GUILayout.Height(35)))
        {
            CopyAssetBundles();
        }

        GUI.backgroundColor = Color.white;

        EditorGUI.EndDisabledGroup();

        if (string.IsNullOrEmpty(copyToPath))
        {
            EditorGUILayout.HelpBox("请先设置目标路径", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制Shader检测区域
    /// </summary>
    private void DrawShaderCheckSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("Shader检测与管理", EditorStyles.boldLabel);
        GUILayout.Space(5); // Toggle开关
        autoCheckShaders = EditorGUILayout.Toggle("构建时自动检测Shader", autoCheckShaders);

        GUILayout.Space(3);

        EditorGUILayout.HelpBox(
            "自定义Shader必须加入Always Included Shaders，否则打包后可能丢失。\n" +
            "检测后会自动添加所有使用的Shader（包括内置Shader）。",
            MessageType.Info);

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
        if (GUILayout.Button("检测模型Shader", GUILayout.Height(35)))
        {
            CheckModelShaders();
        }

        GUI.backgroundColor = Color.white;

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("自动添加到列表", GUILayout.Height(35)))
        {
            AutoAddShadersToAlwaysIncluded();
        }

        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (GUILayout.Button("打开Graphics Settings", GUILayout.Height(25)))
        {
            OpenGraphicsSettings();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制工具按钮区域
    /// </summary>
    private void DrawUtilityButtonsSection()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        GUILayout.Label("工具", EditorStyles.boldLabel);
        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("清理输出文件夹", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认", "确定要清理AB包输出文件夹吗？", "确定", "取消"))
            {
                CleanOutputFolder();
            }
        }

        if (GUILayout.Button("打开AB Browser", GUILayout.Height(30)))
        {
            OpenAssetBundleBrowser();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ===== 核心功能方法 =====

    private void BuildAllModelBundles()
    {
        Debug.Log("<color=cyan>===== 开始构建 AssetBundle =====</color>");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // 根据设置决定是否自动检测并添加Shader
        if (autoCheckShaders)
        {
            Debug.Log("<color=yellow>>>> 自动检测Shader...</color>");
            AutoAddShadersToAlwaysIncluded();
        }
        else
        {
            Debug.Log("<color=gray>已跳过Shader检测（可在面板中开启）</color>");
        }

        ClearAssetBundleNames();
        AutoMarkSharedDependencies();
        MarkModelAssetBundles();

        var manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            buildOptions,
            buildTarget
        );

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("构建失败", "AssetBundle 构建失败！请查看Console", "确定");
            Debug.LogError("<color=red>✗ AssetBundle 构建失败！</color>");
            return;
        }

        // 验证主清单
        string mainManifestPath = Path.Combine(outputPath, Path.GetFileName(outputPath));
        if (File.Exists(mainManifestPath))
        {
            Debug.Log("<color=lime>✓ 主清单生成成功</color>");

            string renamedPath = mainManifestPath + ".ab";
            if (File.Exists(renamedPath))
            {
                File.Delete(renamedPath);
            }

            File.Copy(mainManifestPath, renamedPath);
            Debug.Log("<color=cyan>✓ 已生成带后缀版本: .ab</color>");
        }

        GenerateAssetBundleConfig();

        string[] allBundles = manifest.GetAllAssetBundles();
        Debug.Log($"<color=lime>✓ 共构建 {allBundles.Length} 个 AssetBundle</color>");

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("构建完成", $"成功构建 {allBundles.Length} 个 AssetBundle！\n输出路径: {outputPath}", "确定");
        Debug.Log($"<color=green>===== AssetBundle 构建完成！=====</color>");
    }

    private void MarkModelAssetBundles()
    {
        if (!Directory.Exists(modelsPath))
        {
            EditorUtility.DisplayDialog("错误", $"模型文件夹不存在: {modelsPath}", "确定");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            string safeName = fileName.ToLower()
                .Replace(".", "-")
                .Replace(" ", "_")
                .Replace("&", "_");

            string abName = $"model_{safeName}.ab";

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer != null)
            {
                importer.assetBundleName = abName;
                count++;
            }
        }

        Debug.Log($"<color=cyan>共标记了 {count} 个模型预制体</color>");
        EditorUtility.DisplayDialog("完成", $"共标记了 {count} 个模型预制体", "确定");
    }

    private void AutoMarkSharedDependencies()
    {
        if (!Directory.Exists(modelsPath))
        {
            return;
        }

        Debug.Log("<color=yellow>开始检测共享依赖资源...</color>");

        Dictionary<string, int> dependencyCount = new Dictionary<string, int>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);

            foreach (string dep in dependencies)
            {
                if (dep.EndsWith(".cs") || dep.EndsWith(".prefab") || dep.EndsWith(".unity"))
                    continue;

                if (!dep.StartsWith("Assets/"))
                    continue;

                if (!dependencyCount.ContainsKey(dep))
                    dependencyCount[dep] = 0;

                dependencyCount[dep]++;
            }
        }

        int sharedCount = 0;
        foreach (var kvp in dependencyCount)
        {
            if (kvp.Value >= 2)
            {
                string assetPath = kvp.Key;
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer != null)
                {
                    importer.assetBundleName = "shared_common.ab";
                    sharedCount++;
                }
            }
        }

        if (sharedCount > 0)
        {
            Debug.Log($"<color=lime>✓ 共检测到 {sharedCount} 个共享资源</color>");
        }
        else
        {
            Debug.Log("<color=gray>✓ 未检测到共享资源</color>");
        }
    }

    private void ClearAssetBundleNames()
    {
        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        foreach (string abName in abNames)
        {
            AssetDatabase.RemoveAssetBundleName(abName, true);
        }

        Debug.Log("已清除所有AssetBundle标记");
    }

    private void GenerateAssetBundleConfig()
    {
        List<AssetBundleItemInfo> bundleInfos = new List<AssetBundleItemInfo>();
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            string safeName = fileName.ToLower()
                .Replace(".", "-")
                .Replace(" ", "_")
                .Replace("&", "_");

            string abName = $"model_{safeName}.ab";

            AssetBundleItemInfo info = new AssetBundleItemInfo
            {
                bundleName = abName,
                assetName = fileName,
                assetPath = assetPath
            };
            bundleInfos.Add(info);
        }

        AssetBundleConfigData config = new AssetBundleConfigData
        {
            bundles = bundleInfos.ToArray()
        };

        string json = JsonUtility.ToJson(config, true);
        string configPath = Path.Combine(outputPath, "AssetBundleConfig.json");

        System.Text.UTF8Encoding utf8WithoutBom = new System.Text.UTF8Encoding(false);
        File.WriteAllText(configPath, json, utf8WithoutBom);

        Debug.Log($"<color=yellow>配置文件生成完成: {configPath}</color>");
    }

    private void CopyAssetBundles()
    {
        if (string.IsNullOrEmpty(copyToPath))
        {
            EditorUtility.DisplayDialog("错误", "请先设置目标路径", "确定");
            return;
        }

        if (!Directory.Exists(outputPath))
        {
            EditorUtility.DisplayDialog("错误", "AB包输出文件夹不存在，请先构建AB包", "确定");
            return;
        }

        try
        {
            // 确保目标路径存在
            if (!Directory.Exists(copyToPath))
            {
                Directory.CreateDirectory(copyToPath);
            }

            // 复制所有文件
            string[] files = Directory.GetFiles(outputPath, "*", SearchOption.AllDirectories);
            int copiedCount = 0;

            foreach (string sourceFile in files)
            {
                // 跳过 .meta 文件
                if (sourceFile.EndsWith(".meta"))
                    continue;

                string relativePath = sourceFile.Substring(outputPath.Length + 1);
                string destFile = Path.Combine(copyToPath, relativePath);

                // 确保目标子文件夹存在
                string destDir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(sourceFile, destFile, true);
                copiedCount++;
            }

            Debug.Log($"<color=lime>✓ 成功复制 {copiedCount} 个文件到: {copyToPath}</color>");

            if (EditorUtility.DisplayDialog("复制完成",
                    $"成功复制 {copiedCount} 个文件到:\n{copyToPath}\n\n是否打开目标文件夹？",
                    "打开", "关闭"))
            {
                EditorUtility.RevealInFinder(copyToPath);
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("复制失败", $"复制过程中出错:\n{ex.Message}", "确定");
            Debug.LogError($"<color=red>复制AB包失败: {ex.Message}</color>");
        }
    }

    private void CleanOutputFolder()
    {
        if (Directory.Exists(outputPath))
        {
            Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
            Debug.Log("AssetBundle输出文件夹已清理");
        }
    }

    private void OpenAssetBundleBrowser()
    {
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetType("AssetBundleBrowser.AssetBundleBrowserMain");
            if (type != null)
            {
                var method = type.GetMethod("ShowWindow",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(null, null);
                    return;
                }
            }
        }

        if (EditorUtility.DisplayDialog(
                "AssetBundle Browser 未安装",
                "AssetBundle Browser 是一个官方工具，可以帮助你可视化管理 AssetBundle。\n\n是否要打开 Package Manager 安装？",
                "打开", "取消"))
        {
            UnityEditor.PackageManager.UI.Window.Open("AssetBundle Browser");
        }
    }

    // ===== Shader检测相关方法 =====

    /// <summary>
    /// 检测模型使用的所有Shader
    /// </summary>
    private void CheckModelShaders()
    {
        if (!Directory.Exists(modelsPath))
        {
            EditorUtility.DisplayDialog("错误", $"模型文件夹不存在: {modelsPath}", "确定");
            return;
        }

        Debug.Log("<color=cyan>===== 开始检测模型Shader =====</color>");

        HashSet<Shader> allShaders = new HashSet<Shader>();
        HashSet<Shader> customShaders = new HashSet<Shader>();
        HashSet<Shader> builtInShaders = new HashSet<Shader>();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null) continue;

            // 获取所有Renderer组件
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterials == null) continue;

                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null) continue;

                    Shader shader = mat.shader;

                    if (!allShaders.Contains(shader))
                    {
                        allShaders.Add(shader);

                        // 判断是否为内置Shader
                        if (IsBuiltInShader(shader))
                        {
                            builtInShaders.Add(shader);
                        }
                        else
                        {
                            customShaders.Add(shader);
                        }
                    }
                }
            }
        }

        // 获取当前Already Included Shaders列表
        SerializedObject graphicsSettings = GetGraphicsSettings();
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        HashSet<Shader> includedShaders = new HashSet<Shader>();
        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            Shader shader = alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (shader != null)
            {
                includedShaders.Add(shader);
            }
        }

        // 输出报告
        Debug.Log($"<color=yellow>检测到 {allShaders.Count} 个不同的Shader</color>");
        Debug.Log($"<color=lime>- 自定义Shader: {customShaders.Count} 个</color>");
        Debug.Log($"<color=orange>- Unity内置Shader: {builtInShaders.Count} 个</color>");

        if (customShaders.Count > 0)
        {
            Debug.Log("\n<color=cyan>【自定义Shader列表】</color>");
            int notIncludedCount = 0;

            foreach (Shader shader in customShaders)
            {
                bool isIncluded = includedShaders.Contains(shader);
                string status = isIncluded ? "<color=lime>✓ 已添加</color>" : "<color=red>✗ 未添加</color>";
                Debug.Log($"  {status} - {shader.name}");

                if (!isIncluded) notIncludedCount++;
            }

            if (notIncludedCount > 0)
            {
                Debug.LogWarning(
                    $"<color=yellow>⚠ 有 {notIncludedCount} 个自定义Shader未添加到Always Included Shaders！</color>");
            }
            else
            {
                Debug.Log("<color=lime>✓ 所有自定义Shader都已正确添加</color>");
            }
        }

        if (builtInShaders.Count > 0)
        {
            Debug.Log("\n<color=orange>【Unity内置Shader列表】（建议不要添加）</color>");
            foreach (Shader shader in builtInShaders)
            {
                bool isIncluded = includedShaders.Contains(shader);
                string status = isIncluded ? "<color=yellow>⚠ 已添加</color>" : "<color=gray>○ 未添加</color>";
                Debug.Log($"  {status} - {shader.name}");
            }
        }

        Debug.Log("<color=cyan>===== Shader检测完成 =====</color>");

        // 弹窗提示
        string message = $"检测完成！\n\n" +
                         $"自定义Shader: {customShaders.Count} 个\n" +
                         $"Unity内置Shader: {builtInShaders.Count} 个\n\n" +
                         $"详细信息请查看Console";

        if (customShaders.Count > 0 && customShaders.Any(s => !includedShaders.Contains(s)))
        {
            message += "\n\n⚠ 发现未添加的自定义Shader，建议点击'自动添加到列表'";
        }

        EditorUtility.DisplayDialog("Shader检测结果", message, "确定");
    }

    /// <summary>
    /// 自动将所有Shader添加到Always Included Shaders
    /// </summary>
    private void AutoAddShadersToAlwaysIncluded()
    {
        if (!Directory.Exists(modelsPath))
        {
            EditorUtility.DisplayDialog("错误", $"模型文件夹不存在: {modelsPath}", "确定");
            return;
        }

        Debug.Log("<color=cyan>===== 开始自动添加Shader =====</color>");

        HashSet<Shader> allShaders = new HashSet<Shader>();
        HashSet<Shader> customShaders = new HashSet<Shader>();
        HashSet<Shader> builtInShaders = new HashSet<Shader>();

        // 收集所有Shader（包括自定义和内置）
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab == null) continue;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.sharedMaterials == null) continue;

                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null) continue;

                    Shader shader = mat.shader;

                    if (!allShaders.Contains(shader))
                    {
                        allShaders.Add(shader);

                        // 分类统计
                        if (IsBuiltInShader(shader))
                        {
                            builtInShaders.Add(shader);
                        }
                        else
                        {
                            customShaders.Add(shader);
                        }
                    }
                }
            }
        }

        if (allShaders.Count == 0)
        {
            EditorUtility.DisplayDialog("提示", "未检测到任何Shader", "确定");
            Debug.Log("<color=gray>未检测到需要添加的Shader</color>");
            return;
        }

        // 获取Graphics Settings
        SerializedObject graphicsSettings = GetGraphicsSettings();
        SerializedProperty alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        // 获取已有的Shader列表
        HashSet<Shader> existingShaders = new HashSet<Shader>();
        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            Shader shader = alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;
            if (shader != null)
            {
                existingShaders.Add(shader);
            }
        }

        // 添加新的Shader
        int addedCustomCount = 0;
        int addedBuiltInCount = 0;

        // 先添加自定义Shader
        Debug.Log("\n<color=cyan>【添加自定义Shader】</color>");
        foreach (Shader shader in customShaders)
        {
            if (!existingShaders.Contains(shader))
            {
                alwaysIncludedShaders.InsertArrayElementAtIndex(alwaysIncludedShaders.arraySize);
                alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1).objectReferenceValue =
                    shader;

                addedCustomCount++;
                Debug.Log($"<color=lime>✓ 添加自定义Shader: {shader.name}</color>");
            }
            else
            {
                Debug.Log($"<color=gray>○ 已存在: {shader.name}</color>");
            }
        }

        // 再添加内置Shader
        Debug.Log("\n<color=orange>【添加内置Shader】</color>");
        foreach (Shader shader in builtInShaders)
        {
            if (!existingShaders.Contains(shader))
            {
                alwaysIncludedShaders.InsertArrayElementAtIndex(alwaysIncludedShaders.arraySize);
                alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1).objectReferenceValue =
                    shader;

                addedBuiltInCount++;
                Debug.Log($"<color=yellow>✓ 添加内置Shader: {shader.name}</color>");
            }
            else
            {
                Debug.Log($"<color=gray>○ 已存在: {shader.name}</color>");
            }
        }

        // 保存修改
        graphicsSettings.ApplyModifiedProperties();

        int totalAdded = addedCustomCount + addedBuiltInCount;
        Debug.Log($"<color=green>===== 完成！共添加 {totalAdded} 个新Shader =====</color>");
        Debug.Log($"<color=lime>- 自定义Shader: {addedCustomCount} 个</color>");
        Debug.Log($"<color=yellow>- 内置Shader: {addedBuiltInCount} 个</color>");

        EditorUtility.DisplayDialog("添加完成",
            $"成功添加 {totalAdded} 个Shader到Always Included Shaders列表！\n\n" +
            $"自定义Shader: {addedCustomCount} 个（新增）/ {customShaders.Count} 个（总计）\n" +
            $"内置Shader: {addedBuiltInCount} 个（新增）/ {builtInShaders.Count} 个（总计）\n\n" +
            $"其中 {allShaders.Count - totalAdded} 个已存在",
            "确定");
    }

    /// <summary>
    /// 判断是否为Unity内置Shader
    /// </summary>
    private bool IsBuiltInShader(Shader shader)
    {
        if (shader == null) return false;

        string shaderName = shader.name.ToLower();

        // Unity内置Shader通常以这些前缀开头
        string[] builtInPrefixes = new string[]
        {
            "standard",
            "legacy shaders/",
            "mobile/",
            "nature/",
            "particles/",
            "skybox/",
            "sprites/",
            "ui/",
            "unlit/",
            "vr/",
            "fx/",
            "hidden/"
        };

        foreach (string prefix in builtInPrefixes)
        {
            if (shaderName.StartsWith(prefix))
            {
                return true;
            }
        }

        // 检查Shader路径（内置Shader没有Assets路径）
        string assetPath = AssetDatabase.GetAssetPath(shader);
        if (string.IsNullOrEmpty(assetPath) || assetPath.StartsWith("Resources/unity"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取Graphics Settings的SerializedObject
    /// </summary>
    private SerializedObject GetGraphicsSettings()
    {
        var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];
        return new SerializedObject(graphicsSettings);
    }

    /// <summary>
    /// 打开Graphics Settings面板
    /// </summary>
    private void OpenGraphicsSettings()
    {
        SettingsService.OpenProjectSettings("Project/Graphics");
    }

    // ===== 配置保存/加载 =====

    private void SaveSettings()
    {
        EditorPrefs.SetString("ABBuilder_OutputPath", outputPath);
        EditorPrefs.SetString("ABBuilder_ModelsPath", modelsPath);
        EditorPrefs.SetString("ABBuilder_CopyToPath", copyToPath);
        EditorPrefs.SetInt("ABBuilder_BuildTarget", (int)buildTarget);
        EditorPrefs.SetInt("ABBuilder_BuildOptions", (int)buildOptions);
        EditorPrefs.SetBool("ABBuilder_AutoCheckShaders", autoCheckShaders);
    }

    private void LoadSettings()
    {
        outputPath = EditorPrefs.GetString("ABBuilder_OutputPath", "Assets/StreamingAssets/AssetBundles");
        modelsPath = EditorPrefs.GetString("ABBuilder_ModelsPath", "Assets/Models");
        copyToPath = EditorPrefs.GetString("ABBuilder_CopyToPath", "");
        buildTarget = (BuildTarget)EditorPrefs.GetInt("ABBuilder_BuildTarget", (int)BuildTarget.WebGL);
        buildOptions = (BuildAssetBundleOptions)EditorPrefs.GetInt("ABBuilder_BuildOptions",
            (int)(BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode));
        autoCheckShaders = EditorPrefs.GetBool("ABBuilder_AutoCheckShaders", true); // 默认开启
    }

    // ===== 模型选择相关方法 =====

    /// <summary>
    /// 刷新模型列表
    /// </summary>
    private void RefreshModelList()
    {
        modelList.Clear();

        if (!Directory.Exists(modelsPath))
        {
            Debug.LogWarning($"模型文件夹不存在: {modelsPath}");
            return;
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // 清理文件名
            string safeName = fileName.ToLower()
                .Replace(".", "-")
                .Replace(" ", "_")
                .Replace("&", "_");

            string abName = $"model_{safeName}.ab";

            ModelInfo model = new ModelInfo
            {
                assetPath = assetPath,
                displayName = fileName,
                bundleName = abName,
                isSelected = true // 默认全选
            };

            modelList.Add(model);
        }

        Debug.Log($"<color=cyan>刷新模型列表完成，共 {modelList.Count} 个模型</color>");
    }

    /// <summary>
    /// 构建选中的模型包
    /// </summary>
    private void BuildSelectedModelBundles()
    {
        int selectedCount = modelList.Count(m => m.isSelected);

        if (selectedCount == 0)
        {
            EditorUtility.DisplayDialog("提示", "请至少选择一个模型", "确定");
            return;
        }

        Debug.Log($"<color=cyan>===== 开始构建选中的 {selectedCount} 个模型包 =====</color>");

        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // 根据设置决定是否自动检测并添加Shader
        if (autoCheckShaders)
        {
            Debug.Log("<color=yellow>>>> 自动检测Shader...</color>");
            AutoAddShadersToAlwaysIncluded();
        }
        else
        {
            Debug.Log("<color=gray>已跳过Shader检测（可在面板中开启）</color>");
        }

        ClearAssetBundleNames();

        // 检测所有模型的共享资源（包括未选中的模型）
        AutoMarkSharedDependenciesForAll();

        // 只标记选中的模型
        MarkSelectedModelAssetBundles();

        var manifest = BuildPipeline.BuildAssetBundles(
            outputPath,
            buildOptions,
            buildTarget
        );

        if (manifest == null)
        {
            EditorUtility.DisplayDialog("构建失败", "AssetBundle 构建失败！请查看Console", "确定");
            Debug.LogError("<color=red>✗ AssetBundle 构建失败！</color>");
            return;
        }

        // 验证主清单
        string mainManifestPath = Path.Combine(outputPath, Path.GetFileName(outputPath));
        if (File.Exists(mainManifestPath))
        {
            Debug.Log("<color=lime>✓ 主清单生成成功</color>");

            string renamedPath = mainManifestPath + ".ab";
            if (File.Exists(renamedPath))
            {
                File.Delete(renamedPath);
            }

            File.Copy(mainManifestPath, renamedPath);
            Debug.Log("<color=cyan>✓ 已生成带后缀版本: .ab</color>");
        }

        GenerateAssetBundleConfig();

        string[] allBundles = manifest.GetAllAssetBundles();
        Debug.Log($"<color=lime>✓ 共构建 {allBundles.Length} 个 AssetBundle（包括共享资源包）</color>");

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("构建完成",
            $"成功构建 {selectedCount} 个选中模型的 AssetBundle！\n" +
            $"共生成 {allBundles.Length} 个包（包括共享资源）\n" +
            $"输出路径: {outputPath}",
            "确定");
        Debug.Log($"<color=green>===== AssetBundle 构建完成！=====</color>");
    }

    /// <summary>
    /// 标记选中的模型
    /// </summary>
    private void MarkSelectedModelAssetBundles()
    {
        int count = 0;

        foreach (var model in modelList.Where(m => m.isSelected))
        {
            AssetImporter importer = AssetImporter.GetAtPath(model.assetPath);
            if (importer != null)
            {
                importer.assetBundleName = model.bundleName;
                count++;
                Debug.Log($"<color=cyan>标记模型: {model.displayName} -> {model.bundleName}</color>");
            }
        }

        Debug.Log($"<color=lime>共标记了 {count} 个选中的模型预制体</color>");
    }

    /// <summary>
    /// 检测所有模型的共享依赖（包括未选中的模型）
    /// </summary>
    private void AutoMarkSharedDependenciesForAll()
    {
        if (!Directory.Exists(modelsPath))
        {
            Debug.LogWarning($"模型文件夹不存在: {modelsPath}");
            return;
        }

        Debug.Log("<color=yellow>开始检测所有模型的共享依赖资源...</color>");

        Dictionary<string, int> dependencyCount = new Dictionary<string, int>();

        // 收集所有模型的依赖（包括未选中的）
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

        foreach (string guid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);

            foreach (string dep in dependencies)
            {
                if (dep.EndsWith(".cs") || dep.EndsWith(".prefab") || dep.EndsWith(".unity"))
                    continue;

                if (!dep.StartsWith("Assets/"))
                    continue;

                if (!dependencyCount.ContainsKey(dep))
                    dependencyCount[dep] = 0;

                dependencyCount[dep]++;
            }
        }

        // 将被引用 2 次以上的资源标记为共享包
        int sharedCount = 0;
        foreach (var kvp in dependencyCount)
        {
            if (kvp.Value >= 2)
            {
                string assetPath = kvp.Key;
                AssetImporter importer = AssetImporter.GetAtPath(assetPath);

                if (importer != null)
                {
                    importer.assetBundleName = "shared_common.ab";
                    sharedCount++;

                    string assetType = Path.GetExtension(assetPath);
                    Debug.Log($"<color=yellow>[共享资源] {assetPath} ({assetType}) - 被引用 {kvp.Value} 次</color>");
                }
            }
        }

        if (sharedCount > 0)
        {
            Debug.Log($"<color=lime>✓ 共检测到 {sharedCount} 个共享资源，已标记到 shared_common.ab</color>");
        }
        else
        {
            Debug.Log("<color=gray>✓ 未检测到共享资源（每个模型使用独立材质）</color>");
        }
    }
}

/// <summary>
/// 模型信息类
/// </summary>
[System.Serializable]
public class ModelInfo
{
    public string assetPath; // 资源路径
    public string displayName; // 显示名称
    public string bundleName; // AB包名称
    public bool isSelected; // 是否选中
}