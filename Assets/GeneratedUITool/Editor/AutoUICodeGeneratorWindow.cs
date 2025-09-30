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
            EditorGUILayout.PrefixLabel("包含组件");
            using (new EditorGUILayout.VerticalScope())
            {
                _settings.autoIncludeCommonControls = EditorGUILayout.ToggleLeft(
                    "自动包含常用控件(Button/Toggle/Slider/InputField 及 TMP 相关)", _settings.autoIncludeCommonControls);

                _settings.autoIncludeExtendedControls = EditorGUILayout.ToggleLeft(
                    "扩展：自动包含 ScrollRect/Scrollbar/Dropdown",
                    _settings.autoIncludeExtendedControls);
            }

            GUILayout.Space(10);
            EditorGUILayout.PrefixLabel("生成设置");
            using (new EditorGUILayout.VerticalScope())
            {
                _settings.initAssignMode = (AutoUICodeGenSettings.InitAssignMode)EditorGUILayout.EnumPopup(
                    new GUIContent("生成与赋值方式", "选择在 Awake 查找、Start 查找，或使用序列化引用（仅生成 [SerializeField] 字段，编辑器赋值）"),
                    _settings.initAssignMode);

                _settings.autoAddScriptToPrefab =
                    EditorGUILayout.ToggleLeft("生成后自动把脚本组件添加到预制体根节点", _settings.autoAddScriptToPrefab);

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
            }


            if (EditorGUI.EndChangeCheck()) SaveSettings();

            GUILayout.Space(10);
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
        }

        private void DrawPrefabList(bool expand = false)
        {
            EditorGUILayout.LabelField("预制体列表", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _search = EditorGUILayout.TextField(_search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? GUI.skin.textField);
                _filterOnlyUngenerated = GUILayout.Toggle(_filterOnlyUngenerated, new GUIContent("只未生成"), GUILayout.Width(80));
                _filterOnlyUnattached = GUILayout.Toggle(_filterOnlyUnattached, new GUIContent("只未挂载"), GUILayout.Width(80));
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
                using (new EditorGUILayout.HorizontalScope())
                {
                    _selected[i] = EditorGUILayout.Toggle(_selected[i], GUILayout.Width(20));
                    EditorGUILayout.ObjectField(go, typeof(GameObject), false);
                    if (GUILayout.Button("生成脚本", GUILayout.Width(100)))
                    {
                        var res = UICodeGenerator.GenerateScript(go, _settings.scriptOutputFolder, _settings);
                        if (res.assignStats != null)
                        {
                            var s = res.assignStats;
                            _lastGenerateMessage = $"赋值: 成功 {s.success}/总 {s.total}，缺路径 {s.missingPath}，缺组件 {s.missingComponent}";
                        }
                    }
                    if (GUILayout.Button("打开脚本", GUILayout.Width(80))) OpenScript(go);
                    if (GUILayout.Button("定位预制", GUILayout.Width(80))) EditorGUIUtility.PingObject(go);
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
                    canceled = EditorUtility.DisplayCancelableProgressBar("批量生成 UI 脚本", go.name, total > 0 ? (float)done / total : 0f);
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
            }
        }

        private static bool HasGeneratedScript(GameObject go)
        {
            var settings = AutoUICodeGenSettings.Ensure();
            if (string.IsNullOrEmpty(settings.scriptOutputFolder)) return false;
            var path = Path.Combine(settings.scriptOutputFolder, go.name + ".cs").Replace("\\", "/");
            return File.Exists(path);
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
                var msPath = Path.Combine(settings.scriptOutputFolder, className + ".cs");
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(msPath);
                var t = ms != null ? ms.GetClass() : null;
                if (t == null) return false;
                return prefab.GetComponent(t) != null;
            }
            finally
            {
                if (prefab != null) PrefabUtility.UnloadPrefabContents(prefab);
            }
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
                    var canceled = EditorUtility.DisplayCancelableProgressBar("生成UI代码", prefabs[i].name, (float)i / prefabs.Count);
                    if (canceled) break;
                    UICodeGenerator.GenerateScript(prefabs[i], settings.scriptOutputFolder, settings);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
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
                    var canceled = EditorUtility.DisplayCancelableProgressBar("生成UI代码", selection[i].name, (float)i / selection.Length);
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

        private void SaveSettings()
        {
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
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
    }
}