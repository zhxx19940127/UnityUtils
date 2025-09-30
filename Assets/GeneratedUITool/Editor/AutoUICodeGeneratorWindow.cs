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
            EditorGUILayout.LabelField("基础设置", EditorStyles.boldLabel);
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

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.autoIncludeCommonControls = EditorGUILayout.ToggleLeft(
                    "自动包含常用控件(Button/Toggle/Slider/InputField 及 TMP 相关)", _settings.autoIncludeCommonControls);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.assignInAwake = EditorGUILayout.ToggleLeft("在 Awake 中赋值(否则 Start)", _settings.assignInAwake);
                _settings.requireUppercaseClassName =
                    EditorGUILayout.ToggleLeft("类名首字母需大写", _settings.requireUppercaseClassName);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.generateReadOnlyProperties = EditorGUILayout.ToggleLeft("为每个字段生成同名 PascalCase 只读属性",
                    _settings.generateReadOnlyProperties);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.privateFieldUnderscoreCamelCase =
                    EditorGUILayout.ToggleLeft("字段命名为 _camelCase（示例：btn_ok -> _btnOk）",
                        _settings.privateFieldUnderscoreCamelCase);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.autoAddScriptToPrefab =
                    EditorGUILayout.ToggleLeft("生成后自动把脚本组件添加到预制体根节点", _settings.autoAddScriptToPrefab);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.useComponentPrefixForFields = EditorGUILayout.ToggleLeft(
                    "字段使用组件前缀 (Button->btn, Toggle->tog, Slider->sld, InputField->input, Text->txt, Image->img)",
                    _settings.useComponentPrefixForFields);
            }

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

            if (EditorGUI.EndChangeCheck()) SaveSettings();

            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新预制体列表", GUILayout.Width(140))) RefreshPrefabs();
                GUILayout.FlexibleSpace();
                //EditorGUILayout.HelpBox("可拖拽文件夹到窗口，快速设置路径并刷新。", MessageType.Info);
            }
        }

        private void DrawPrefabList(bool expand = false)
        {
            EditorGUILayout.LabelField("预制体列表", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandHeight(true));
            if (expand)
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            else
                _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _prefabList.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _selected[i] = EditorGUILayout.Toggle(_selected[i], GUILayout.Width(20));
                    EditorGUILayout.ObjectField(_prefabList[i], typeof(GameObject), false);
                    if (GUILayout.Button("生成脚本", GUILayout.Width(100)))
                    {
                        UICodeGenerator.GenerateScript(_prefabList[i], _settings.scriptOutputFolder, _settings);
                    }
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
                    for (int i = 0; i < _prefabList.Count; i++)
                    {
                        if (_selected[i])
                        {
                            UICodeGenerator.GenerateScript(_prefabList[i], _settings.scriptOutputFolder, _settings);
                        }
                    }
                }
            }
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