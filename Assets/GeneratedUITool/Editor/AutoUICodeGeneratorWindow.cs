using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityUtils.EditorTools.AutoUI
{
    public class AutoUICodeGeneratorWindow : EditorWindow
    {
        private AutoUICodeGenSettings _settings;
        private Vector2 _scroll;
        private readonly List<GameObject> _prefabList = new List<GameObject>();
        private readonly List<bool> _selected = new List<bool>();
        private const float StatusLabelWidth = 40f; // 单个状态标签宽度（三字）
        private const float StatusAreaWidth = 10f; // 固定状态区域宽度，容纳3个状态与间距，避免按钮抖动
        private const float LeftAreaWidth = 280f; // 固定左侧 勾选框+预制体 对象区域宽度
        private const float ItemButtonWidth = 56f; // 条目右侧按钮统一宽度（两字）
        private bool _prefixMappingFoldout = false; // 组件前缀映射折叠，默认折叠
        private int _lastGenerateSuccess;
        private int _lastGenerateFail;
        private string _lastGenerateMessage;
        private string _search = string.Empty;
        private bool _filterOnlyUngenerated = false;
        private bool _filterOnlyUnattached = false;
        private bool _filterOnlyErrors = false;

        [MenuItem("Tools/UI/自动UI代码生成器")]
        public static void Open()
        {
            var window = GetWindow<AutoUICodeGeneratorWindow>("自动UI代码生成器");
            window.minSize = new Vector2(680, 420);
            window.Show();
        }

        public static void RefreshAllOpenWindows()
        {
            // 查找所有打开的窗口并刷新重绘
            var windows = Resources.FindObjectsOfTypeAll<AutoUICodeGeneratorWindow>();
            foreach (var w in windows)
            {
                try
                {
                    w.RefreshPrefabs();
                    w.Repaint();
                }
                catch
                {
                }
            }
        }

        private void OnEnable()
        {
            _settings = AutoUICodeGenSettings.Ensure();
            RefreshPrefabs();
        }

        private void OnGUI()
        {
            if (_settings == null) _settings = AutoUICodeGenSettings.Ensure();
            using (new EditorGUILayout.VerticalScope())
            {
                DrawHeader();
                EditorGUILayout.Space();
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandHeight(true)))
                {
                    DrawPrefabList(true);
                }

                EditorGUILayout.Space();
                DrawFooter();
            }

            HandleDragAndDrop();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("UI预制体路径");
                _settings.prefabFolder = EditorGUILayout.TextField(_settings.prefabFolder);
                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    var p = EditorUtility.OpenFolderPanel("选择 UI 预制体目录", Application.dataPath, string.Empty);
                    if (!string.IsNullOrEmpty(p))
                    {
                        _settings.prefabFolder = AbsoluteToProjectPath(p);
                        SaveSettings();
                        RefreshPrefabs();
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("脚本生成路径");
                _settings.scriptOutputFolder = EditorGUILayout.TextField(_settings.scriptOutputFolder);
                if (GUILayout.Button("选择", GUILayout.Width(60)))
                {
                    var p = EditorUtility.OpenFolderPanel("选择脚本输出目录", Application.dataPath, string.Empty);
                    if (!string.IsNullOrEmpty(p))
                    {
                        _settings.scriptOutputFolder = AbsoluteToProjectPath(p);
                        SaveSettings();
                    }
                }
            }

            GUILayout.Space(10);
            EditorGUILayout.LabelField("包含组件", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope())
            {
                _settings.autoIncludeCommonControls = EditorGUILayout.ToggleLeft(
                    "自动包含常用控件(Button/Toggle/Slider/InputField 及 TMP 相关)", _settings.autoIncludeCommonControls);

                _settings.autoIncludeExtendedControls = EditorGUILayout.ToggleLeft(
                    "扩展：自动包含 ScrollRect/Scrollbar/Dropdown",
                    _settings.autoIncludeExtendedControls);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("生成设置", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.HelpBox("生成前请确认赋值方式符合预期，存在脚本情况下更改赋值方式会导致错误！",
                    MessageType.Warning);
                // 仅两种：方法赋值（生成 InitRefs 供手动调用）/ 序列化引用
                var modeChoices = new[] { "方法赋值", "序列化引用" };
                int modeIndex = _settings.initAssignMode == AutoUICodeGenSettings.InitAssignMode.SerializedReferences
                    ? 1
                    : 0;
                modeIndex = EditorGUILayout.Popup(
                    new GUIContent("字段赋值方式", "方法赋值：生成 InitRefs 方法，由你在合适时机调用；序列化引用：生成 [SerializeField] 并由编辑器赋值"),
                    modeIndex, modeChoices);
                _settings.initAssignMode = modeIndex == 1
                    ? AutoUICodeGenSettings.InitAssignMode.SerializedReferences
                    : AutoUICodeGenSettings.InitAssignMode.Find;

                // 取消自动挂载选项：由用户手动触发挂载

                _settings.requireUppercaseClassName =
                    EditorGUILayout.ToggleLeft("类名首字母需大写", _settings.requireUppercaseClassName);

                _settings.generateReadOnlyProperties = EditorGUILayout.ToggleLeft(
                    "为每个字段生成同名 PascalCase 只读属性",
                    _settings.generateReadOnlyProperties);

                _settings.privateFieldUnderscoreCamelCase =
                    EditorGUILayout.ToggleLeft("是否使用下划线命名（示例：btn_ok -> _btnOk）",
                        _settings.privateFieldUnderscoreCamelCase);

                _settings.useComponentPrefixForFields = EditorGUILayout.ToggleLeft(
                    "是否组件前缀 前缀在下面列表中设置",
                    _settings.useComponentPrefixForFields);

                // 可编辑前缀表（支持折叠，默认折叠）
                if (_settings.useComponentPrefixForFields)
                {
                    EditorGUILayout.Space();
                    _prefixMappingFoldout = EditorGUILayout.Foldout(_prefixMappingFoldout, "组件前缀映射", true);
                    if (_prefixMappingFoldout)
                    {
                        using (new EditorGUILayout.VerticalScope("box"))
                        {
                            for (int i = 0; i < _settings.componentPrefixes.Count; i++)
                            {
                                var item = _settings.componentPrefixes[i];
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    item.typeFullName = EditorGUILayout.TextField(item.typeFullName);
                                    item.prefix = EditorGUILayout.TextField(item.prefix, GUILayout.Width(120));
                                    if (GUILayout.Button("删除", GUILayout.Width(60)))
                                    {
                                        _settings.componentPrefixes.RemoveAt(i);
                                        i--;
                                        continue;
                                    }
                                }
                            }

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                if (GUILayout.Button("新增映射", GUILayout.Width(100)))
                                {
                                    _settings.componentPrefixes.Add(new AutoUICodeGenSettings.TypePrefix
                                        { typeFullName = "", prefix = "" });
                                }

                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("重置为默认", GUILayout.Width(120)))
                                {
                                    _settings.FillDefaultPrefixes();
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("类包装", EditorStyles.boldLabel);
                _settings.wrapNamespace = EditorGUILayout.TextField(new GUIContent("命名空间(可选)", "生成类外层的命名空间，留空则不使用"),
                    _settings.wrapNamespace);
                _settings.baseClassFullName = EditorGUILayout.TextField(
                    new GUIContent("基类全名(可选)", "例如 MyGame.UI.BaseView；为空默认为 UnityEngine.MonoBehaviour"),
                    _settings.baseClassFullName);
            }


            if (EditorGUI.EndChangeCheck()) SaveSettings();

            GUILayout.Space(10);
        }

        private void DrawPrefabList(bool expand = false)
        {
            EditorGUILayout.LabelField("预制体列表", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新预制体列表", GUILayout.Width(140))) RefreshPrefabs();
                if (GUILayout.Button("重新扫描并生成全部", GUILayout.Width(180)))
                {
                    RefreshPrefabs();
                    GenerateSelected(prefabAll: true);
                }

                GUILayout.FlexibleSpace();
                //EditorGUILayout.HelpBox("可拖拽文件夹到窗口，快速设置路径并刷新。", MessageType.Info);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search,
                    GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField);
                _filterOnlyUngenerated =
                    GUILayout.Toggle(_filterOnlyUngenerated, new GUIContent("只未生成"), GUILayout.Width(80));
                _filterOnlyUnattached =
                    GUILayout.Toggle(_filterOnlyUnattached, new GUIContent("只未挂载"), GUILayout.Width(80));
                _filterOnlyErrors = GUILayout.Toggle(_filterOnlyErrors, new GUIContent("只异常"), GUILayout.Width(80));
            }

            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (expand)
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            else
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _prefabList.Count; i++)
            {
                var go = _prefabList[i];
                if (!string.IsNullOrEmpty(_search) && go.name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (_filterOnlyUngenerated && HasGeneratedScript(go))
                    continue;
                if (_filterOnlyUnattached && IsAttached(go))
                    continue;
                if (_filterOnlyErrors && !HasErrors(go))
                    continue;
                using (new EditorGUILayout.HorizontalScope())
                {
                    // 左侧固定宽度：勾选框 + 预制体对象
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(LeftAreaWidth)))
                    {
                        _selected[i] = EditorGUILayout.Toggle(_selected[i], GUILayout.Width(20));
                        GUILayout.Space(4);
                        EditorGUILayout.ObjectField(go, typeof(GameObject), false, GUILayout.ExpandWidth(true));
                    }

                    // 中间固定宽度显示状态，避免按钮区域抖动
                    GUILayout.Space(8);
                    using (new EditorGUILayout.HorizontalScope(GUILayout.Width(StatusAreaWidth)))
                    {
                        GUILayout.FlexibleSpace();
                        DrawStatusLabels(go);
                        GUILayout.FlexibleSpace();
                    }

                    GUILayout.Space(8);

                    // 右侧操作按钮
                    if (GUILayout.Button("生成", GUILayout.Width(ItemButtonWidth)))
                    {
                        var res = UICodeGenerator.GenerateScript(go, _settings.scriptOutputFolder, _settings);
                        if (res.assignStats != null)
                        {
                            var s = res.assignStats;
                            _lastGenerateMessage =
                                $"赋值: 成功 {s.success}/总 {s.total}，缺路径 {s.missingPath}，缺组件 {s.missingComponent}";
                        }
                        // // 生成后：自动挂载
                        // var scriptPath = Path.Combine(_settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
                        // // 序列化模式：生成挂载前收集字段，挂载后尝试赋值
                        // List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields = null;
                        // bool doAssignAfter = _settings.initAssignMode == AutoUICodeGenSettings.InitAssignMode.SerializedReferences;
                        // if (doAssignAfter)
                        // {
                        //     fields = UICodeGenerator.CollectFields(go, _settings);
                        // }
                        //
                        // GeneratedScriptAttacher.EnqueueAttach(go, go.name, scriptPath);
                        //
                        // if (doAssignAfter)
                        // {
                        //     EditorApplication.delayCall += () => { TryAssignSerializedReferences(go, go.name, fields); };
                        // }
                        //
                        // ShowNotification(new GUIContent("已生成并请求挂载"));
                    }

                    if (HasGeneratedScript(go) && !IsAttached(go))
                    {
                        if (GUILayout.Button("挂载", GUILayout.Width(ItemButtonWidth)))
                        {
                            MountOne(go);
                        }
                    }

                    if (HasGeneratedScript(go))
                    {
                        if (GUILayout.Button("打开", GUILayout.Width(ItemButtonWidth))) OpenScript(go);
                        if (GUILayout.Button("删除", GUILayout.Width(ItemButtonWidth))) DeleteOne(go);
                    }

                    if (GUILayout.Button("检查", GUILayout.Width(ItemButtonWidth))) LogAttachDiagnostics(go);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (GUILayout.Button("全选", GUILayout.Width(80)))
                {
                    for (int i = 0; i < _selected.Count; i++) _selected[i] = true;
                }

                if (GUILayout.Button("全不选", GUILayout.Width(80)))
                {
                    for (int i = 0; i < _selected.Count; i++) _selected[i] = false;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("为所选预制生成脚本", GUILayout.Height(28)))
                {
                    GenerateSelected();
                }

                GUILayout.Space(8);
                if (GUILayout.Button("为所选已生成脚本的预制挂载脚本", GUILayout.Height(28)))
                {
                    MountForSelected();
                }
            }

            if (!string.IsNullOrEmpty(_lastGenerateMessage))
            {
                EditorGUILayout.HelpBox(_lastGenerateMessage,
                    _lastGenerateFail == 0 ? MessageType.Info : MessageType.Warning);
            }
        }

        private void GenerateSelected(bool prefabAll = false)
        {
            _lastGenerateSuccess = 0;
            _lastGenerateFail = 0;
            _lastGenerateMessage = string.Empty;

            try
            {
                int total = 0;
                bool canceled = false;
                EditorUtility.DisplayProgressBar("批量生成 UI 脚本", "开始...", 0f);
                for (int i = 0; i < _prefabList.Count; i++)
                {
                    if (prefabAll || _selected[i]) total++;
                }

                int done = 0;
                int assignOk = 0, assignTotal = 0, missingPath = 0, missingComp = 0;
                for (int i = 0; i < _prefabList.Count; i++)
                {
                    if (!(prefabAll || _selected[i])) continue;
                    var go = _prefabList[i];
                    canceled = EditorUtility.DisplayCancelableProgressBar("批量生成 UI 脚本", go.name,
                        total > 0 ? (float)done / total : 0f);
                    if (canceled) break;
                    try
                    {
                        var res = UICodeGenerator.GenerateScript(go, _settings.scriptOutputFolder, _settings);
                        if (res.assignStats != null)
                        {
                            assignOk += res.assignStats.success;
                            assignTotal += res.assignStats.total;
                            missingPath += res.assignStats.missingPath;
                            missingComp += res.assignStats.missingComponent;
                        }

                        _lastGenerateSuccess++;
                    }
                    catch (System.Exception ex)
                    {
                        _lastGenerateFail++;
                        Debug.LogError($"[AutoUI] 生成失败 {go?.name}: {ex.Message}");
                    }

                    done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _lastGenerateMessage = $"生成完成：成功 {_lastGenerateSuccess}，失败 {_lastGenerateFail}";
                // 自动挂载已取消
            }
        }

        private static bool HasGeneratedScript(GameObject go)
        {
            if (go)
            {
                var settings = AutoUICodeGenSettings.Ensure();
                if (string.IsNullOrEmpty(settings.scriptOutputFolder)) return false;
                var path = Path.Combine(settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
                return File.Exists(path);
            }

            return false;
        }

        private static bool IsAttached(GameObject go)
        {
            var path = AssetDatabase.GetAssetPath(go);
            if (string.IsNullOrEmpty(path)) return false;
            var settings = AutoUICodeGenSettings.Ensure();
            var className = go.name;
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (prefab == null) return false;
                var msPath = Path.Combine(settings.scriptOutputFolder, className + ".cs").Replace("\\", "/");
                var expectedMs = AssetDatabase.LoadAssetAtPath<MonoScript>(msPath);

                // 首选：直接比较组件所挂脚本（MonoScript）是否与生成脚本一致
                var mbs = prefab.GetComponents<MonoBehaviour>();
                for (int i = 0; i < mbs.Length; i++)
                {
                    var mb = mbs[i];
                    if (mb == null) continue; // Missing script
                    if (expectedMs != null)
                    {
                        var mbMs = MonoScript.FromMonoBehaviour(mb);
                        if (mbMs != null && mbMs == expectedMs) return true;
                    }
                }

                // 次选：类型严格相等
                var t = expectedMs != null ? expectedMs.GetClass() : null;
                if (t != null && prefab.GetComponent(t) != null) return true;

                // 兜底：短名匹配（可能与同名类冲突，但可以解决常见显示未挂载问题）
                for (int i = 0; i < mbs.Length; i++)
                {
                    var mb = mbs[i];
                    if (mb == null) continue;
                    if (mb.GetType().Name == className) return true;
                }

                return false;
            }
            finally
            {
                if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static System.Type FindTypeByName(string className)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = asm.GetTypes();
                    for (int i = 0; i < types.Length; i++)
                    {
                        var t = types[i];
                        if (t != null && t.Name == className)
                        {
                            if (typeof(MonoBehaviour).IsAssignableFrom(t)) return t;
                        }
                    }
                }
                catch
                {
                }
            }

            return null;
        }

        [MenuItem("Assets/生成UI代码(支持多选)")]
        private static void GenerateForSelected()
        {
            var settings = AutoUICodeGenSettings.Ensure();
            var selection = Selection.objects;
            var prefabs = new List<GameObject>();
            foreach (var obj in selection)
            {
                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) prefabs.Add(go);
            }

            if (prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("生成UI代码", "请选择一个或多个预制体", "确定");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("生成UI代码", "开始...", 0f);
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var canceled =
                        EditorUtility.DisplayCancelableProgressBar("生成UI代码", prefabs[i].name, (float)i / prefabs.Count);
                    if (canceled) break;
                    UICodeGenerator.GenerateScript(prefabs[i], settings.scriptOutputFolder, settings);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // 自动挂载已取消
            }
        }

        [MenuItem("GameObject/生成UI代码", false, 49)]
        private static void GenerateFromHierarchy()
        {
            var settings = AutoUICodeGenSettings.Ensure();
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("生成UI代码", "请在层级视图选择一个或多个预制体实例/Prefab 根节点", "确定");
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("生成UI代码", "开始...", 0f);
                for (int i = 0; i < selection.Length; i++)
                {
                    var canceled = EditorUtility.DisplayCancelableProgressBar("生成UI代码", selection[i].name,
                        (float)i / selection.Length);
                    if (canceled) break;
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(selection[i]);
                    var go = prefab != null ? prefab : selection[i];
                    if (go != null)
                        UICodeGenerator.GenerateScript(go, settings.scriptOutputFolder, settings);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                // 自动挂载已取消
            }
        }

        private static void OpenScript(GameObject go)
        {
            var settings = AutoUICodeGenSettings.Ensure();
            var path = Path.Combine(settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (asset != null) AssetDatabase.OpenAsset(asset);
            else EditorUtility.DisplayDialog("打开脚本", "未找到脚本: " + path, "确定");
        }

        private void RefreshPrefabs()
        {
            _prefabList.Clear();
            _selected.Clear();

            if (string.IsNullOrEmpty(_settings.prefabFolder) || !AssetDatabase.IsValidFolder(_settings.prefabFolder))
            {
                return;
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { _settings.prefabFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null)
                {
                    _prefabList.Add(go);
                    _selected.Add(false);
                }
            }
        }

        private void HandleDragAndDrop()
        {
            var evt = Event.current;
            if (evt == null) return;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var path in DragAndDrop.paths)
                    {
                        var projectPath = AbsoluteToProjectPath(path);
                        if (AssetDatabase.IsValidFolder(projectPath))
                        {
                            _settings.prefabFolder = projectPath;
                            SaveSettings();
                            RefreshPrefabs();
                            Repaint();
                            break;
                        }
                    }
                }

                evt.Use();
            }
        }

        private void DrawStatusLabels(GameObject go)
        {
            var green = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.2f, 0.7f, 0.2f) } };
            var red = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.9f, 0.3f, 0.3f) } };
            var yellow = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.95f, 0.75f, 0.2f) } };

            // 1) 生成状态（始终显示）
            bool generated = HasGeneratedScript(go);
            GUILayout.Label(generated ? "已生成" : "未生成", generated ? green : red, GUILayout.Width(StatusLabelWidth));

            // 2) 挂载状态（始终显示；未生成时可视为未挂载）
            GUILayout.Space(6);
            bool attached = generated && IsAttached(go);
            GUILayout.Label(attached ? "已挂载" : "未挂载", attached ? green : yellow, GUILayout.Width(StatusLabelWidth));

            // 3) 异常状态（序列化模式显示真实结果；否则默认为无异常）
            GUILayout.Space(6);
            bool hasError = false;
            var settings = AutoUICodeGenSettings.Ensure();
            if (settings.initAssignMode == AutoUICodeGenSettings.InitAssignMode.SerializedReferences)
            {
                var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go));
                if (SerializedReferenceAssigner.TryGetLastStatsByGuid(guid, out var stats))
                {
                    hasError = stats.total > 0 && stats.success < stats.total;
                }
            }

            GUILayout.Label(hasError ? "有异常" : "无异常", hasError ? red : green, GUILayout.Width(StatusLabelWidth));
        }

        private void SaveSettings()
        {
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }

        // 自动挂载提示已移除

        private static bool HasErrors(GameObject go)
        {
            var settings = AutoUICodeGenSettings.Ensure();
            if (settings.initAssignMode != AutoUICodeGenSettings.InitAssignMode.SerializedReferences)
                return false;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(go));
            if (SerializedReferenceAssigner.TryGetLastStatsByGuid(guid, out var stats))
            {
                return stats.total > 0 && stats.success < stats.total;
            }

            return false;
        }

        private static void LogAttachDiagnostics(GameObject go)
        {
            var settings = AutoUICodeGenSettings.Ensure();
            var prefabPath = AssetDatabase.GetAssetPath(go);
            var className = go.name;
            var msPath = Path.Combine(settings.scriptOutputFolder, className + ".cs").Replace("\\", "/");
            var expectedMs = AssetDatabase.LoadAssetAtPath<MonoScript>(msPath);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"预制体: {prefabPath}");
            sb.AppendLine($"脚本路径: {msPath}");
            sb.AppendLine($"脚本存在: {File.Exists(msPath)}");
            sb.AppendLine($"MonoScript: {(expectedMs ? expectedMs.name : "<null>")}");

            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (prefab == null)
                {
                    EditorUtility.DisplayDialog("检查状态", "无法加载 Prefab 内容", "确定");
                    return;
                }

                var mbs = prefab.GetComponents<MonoBehaviour>();
                sb.AppendLine($"组件数量: {mbs.Length}");
                for (int i = 0; i < mbs.Length; i++)
                {
                    var mb = mbs[i];
                    if (mb == null)
                    {
                        sb.AppendLine($"#{i}: Missing Script");
                        continue;
                    }

                    var ms = MonoScript.FromMonoBehaviour(mb);
                    var msName = ms ? ms.name : "<null>";
                    sb.AppendLine($"#{i}: {mb.GetType().FullName} (脚本: {msName})");
                    if (expectedMs && ms == expectedMs) sb.AppendLine("  - 匹配: MonoScript 相同");
                    if (mb.GetType().Name == className) sb.AppendLine("  - 匹配: 类型短名相同");
                }

                EditorUtility.DisplayDialog("检查状态", sb.ToString(), "确定");
            }
            finally
            {
                if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static string AbsoluteToProjectPath(string abs)
        {
            if (string.IsNullOrEmpty(abs)) return abs;
            abs = abs.Replace("\\", "/");
            var dataPath = Application.dataPath.Replace("\\", "/");
            if (abs.StartsWith(dataPath))
            {
                return "Assets" + abs.Substring(dataPath.Length);
            }

            // 如果是 project 根目录或其子目录
            var projectRoot = Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            if (abs.StartsWith(projectRoot))
            {
                var rel = abs.Substring(projectRoot.Length + 1);
                if (!rel.StartsWith("Assets")) rel = Path.Combine("Assets", rel).Replace("\\", "/");
                return rel;
            }

            return abs;
        }

        private void DeleteOne(GameObject go)
        {
            if (go == null) return;
            var settings = AutoUICodeGenSettings.Ensure();
            var prefabPath = AssetDatabase.GetAssetPath(go);
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var scriptPath = Path.Combine(settings.scriptOutputFolder ?? string.Empty, go.name + ".cs")
                .Replace("\\", "/");

            if (!EditorUtility.DisplayDialog("删除生成内容",
                    $"将移除预制体上的脚本并删除脚本文件:\n{scriptPath}\n\n是否继续？", "确定", "取消"))
            {
                return;
            }

            // 1) 从 Prefab 移除组件
            try
            {
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
                    if (prefab != null)
                    {
                        bool changed = false;
                        // 通过 MonoScript 精确比对
                        MonoScript expectedMs = null;
                        if (!string.IsNullOrEmpty(scriptPath))
                            expectedMs = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

                        var mbs = prefab.GetComponents<MonoBehaviour>();
                        for (int i = mbs.Length - 1; i >= 0; i--)
                        {
                            var mb = mbs[i];
                            if (mb == null) continue;
                            bool match = false;
                            if (expectedMs != null)
                            {
                                var ms = MonoScript.FromMonoBehaviour(mb);
                                if (ms != null && ms == expectedMs) match = true;
                            }

                            // 兜底：按短名匹配
                            if (!match && mb.GetType().Name == go.name) match = true;
                            if (match)
                            {
                                DestroyImmediate(mb, true);
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                        }

                        PrefabUtility.UnloadPrefabContents(prefab);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoUI] 从预制移除组件失败: {ex.Message}");
            }

            // 2) 删除脚本文件
            try
            {
                if (!string.IsNullOrEmpty(scriptPath) && AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath) != null)
                {
                    if (!AssetDatabase.DeleteAsset(scriptPath))
                    {
                        Debug.LogWarning($"[AutoUI] 删除脚本失败: {scriptPath}");
                    }
                    else
                    {
                        AssetDatabase.Refresh();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoUI] 删除脚本文件失败: {ex.Message}");
            }

            // 3) 清空异常状态缓存
            try
            {
                if (!string.IsNullOrEmpty(guid)) SerializedReferenceAssigner.ClearStatsForGuid(guid);
            }
            catch
            {
            }

            // 4) 刷新界面
            RefreshPrefabs();
            Repaint();
            ShowNotification(new GUIContent("已删除并移除挂载"));
        }

        private void MountOne(GameObject go)
        {
            var settings = AutoUICodeGenSettings.Ensure();
            var scriptPath = Path.Combine(settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
            if (!File.Exists(scriptPath))
            {
                EditorUtility.DisplayDialog("挂载脚本", $"未找到脚本: {scriptPath}", "确定");
                return;
            }

            // 如果是序列化引用模式：先收集字段，挂载后进行赋值
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields = null;
            bool doAssignAfter = settings.initAssignMode == AutoUICodeGenSettings.InitAssignMode.SerializedReferences;
            if (doAssignAfter)
            {
                fields = UICodeGenerator.CollectFields(go, settings);
            }

            GeneratedScriptAttacher.EnqueueAttach(go, go.name, scriptPath);

            if (doAssignAfter)
            {
                // 尝试立即赋值一次（若类型可用且已挂载），否则在脚本重载后窗口刷新再手动赋值
                EditorApplication.delayCall += () => { TryAssignSerializedReferences(go, go.name, fields); };
            }

            ShowNotification(new GUIContent("已请求挂载，编译完成后会自动挂载"));
        }

        private void MountForSelected()
        {
            int total = 0;
            for (int i = 0; i < _prefabList.Count; i++)
            {
                if (_selected[i]) total++;
            }

            if (total == 0)
            {
                ShowNotification(new GUIContent("请先选择预制体"));
                return;
            }

            try
            {
                EditorUtility.DisplayProgressBar("挂载脚本", "开始...", 0f);
                int done = 0;
                var settings = AutoUICodeGenSettings.Ensure();
                for (int i = 0; i < _prefabList.Count; i++)
                {
                    if (!_selected[i]) continue;
                    var go = _prefabList[i];
                    var scriptPath = Path.Combine(settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
                    var canceled =
                        EditorUtility.DisplayCancelableProgressBar("挂载脚本", go.name,
                            total > 0 ? (float)done / total : 0f);
                    if (canceled) break;
                    if (File.Exists(scriptPath))
                    {
                        List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields =
                            null;
                        bool doAssignAfter = settings.initAssignMode ==
                                             AutoUICodeGenSettings.InitAssignMode.SerializedReferences;
                        if (doAssignAfter)
                            fields = UICodeGenerator.CollectFields(go, settings);

                        GeneratedScriptAttacher.EnqueueAttach(go, go.name, scriptPath);

                        if (doAssignAfter)
                        {
                            EditorApplication.delayCall += () =>
                            {
                                TryAssignSerializedReferences(go, go.name, fields);
                            };
                        }
                    }

                    done++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                ShowNotification(new GUIContent("挂载请求已提交，编译完成后会自动挂载"));
            }
        }

        private static void TryAssignSerializedReferences(GameObject prefab, string className,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
        {
            if (prefab == null || fields == null || fields.Count == 0) return;
            var settings = AutoUICodeGenSettings.Ensure();
            if (settings.initAssignMode != AutoUICodeGenSettings.InitAssignMode.SerializedReferences) return;

            // 等待一帧再查找，确保挂载后的 Prefab 已保存
            EditorApplication.delayCall += () =>
            {
                try
                {
                    var stats = SerializedReferenceAssigner.Assign(prefab, className, fields);
                    if (stats != null)
                    {
                        Debug.Log(
                            $"[AutoUI] 序列化引用赋值：成功 {stats.success}/{stats.total}，缺路径 {stats.missingPath}，缺组件 {stats.missingComponent}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoUI] 序列化引用赋值失败: {ex.Message}");
                }
            };
        }
    }
}