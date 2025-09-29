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

            DrawHeader();
            EditorGUILayout.Space();
            DrawPrefabList();
            EditorGUILayout.Space();
            DrawFooter();
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
                _settings.autoIncludeCommonControls = EditorGUILayout.ToggleLeft("自动包含常用控件(Button/Toggle/Slider/InputField 及 TMP 相关)", _settings.autoIncludeCommonControls);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _settings.assignInAwake = EditorGUILayout.ToggleLeft("在 Awake 中赋值(否则 Start)", _settings.assignInAwake);
                _settings.requireUppercaseClassName = EditorGUILayout.ToggleLeft("类名首字母需大写", _settings.requireUppercaseClassName);
            }

            if (EditorGUI.EndChangeCheck()) SaveSettings();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("刷新预制体列表", GUILayout.Width(140))) RefreshPrefabs();
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("可拖拽文件夹到窗口，快速设置路径并刷新。", MessageType.Info);
            }
        }

        private void DrawPrefabList()
        {
            EditorGUILayout.LabelField("预制体列表", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
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
            using (new EditorGUILayout.HorizontalScope())
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
