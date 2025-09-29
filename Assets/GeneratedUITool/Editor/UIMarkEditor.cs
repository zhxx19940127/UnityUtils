using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityUtils.UI;

namespace UnityUtils.EditorTools.AutoUI
{
    [CustomEditor(typeof(UIMark))]
    [CanEditMultipleObjects]
    public class UIMarkEditor : Editor
    {
        SerializedProperty fieldNameProp;
        SerializedProperty ignoreChildrenProp;
        SerializedProperty targetKindProp;
        SerializedProperty componentTypeProp;

        private static readonly string[] PriorityInteractive = new[]
        {
            "UnityEngine.UI.Button",
            "UnityEngine.UI.Toggle",
            "UnityEngine.UI.Slider",
            "UnityEngine.UI.InputField",
            "TMPro.TMP_InputField",
        };
        private static readonly string[] PriorityMedium = new[]
        {
            "TMPro.TMP_Text",
            "UnityEngine.UI.Text",
            "UnityEngine.UI.Image",
            "UnityEngine.UI.RawImage",
        };

        void OnEnable()
        {
            fieldNameProp = serializedObject.FindProperty("fieldName");
            ignoreChildrenProp = serializedObject.FindProperty("ignoreChildren");
            targetKindProp = serializedObject.FindProperty("targetKind");
            componentTypeProp = serializedObject.FindProperty("componentTypeFullName");
            // no legacy fields
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var mark = target as UIMark;

            EditorGUILayout.LabelField("UIMark", EditorStyles.boldLabel);

            // 字段名
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(fieldNameProp, new GUIContent("字段名"));
                if (GUILayout.Button("自动", GUILayout.Width(60)))
                {
                    fieldNameProp.stringValue = SanitizeToIdentifier(mark.gameObject.name);
                }
            }

            // 忽略子物体
            EditorGUILayout.PropertyField(ignoreChildrenProp, new GUIContent("忽略子物体"));

            // 目标类型
            EditorGUILayout.PropertyField(targetKindProp, new GUIContent("导出目标"));

            // 可选组件类型（当选择 Component 时）
            if ((UIMark.ExportTargetKind)targetKindProp.enumValueIndex == UIMark.ExportTargetKind.Component)
            {
                var options = CollectComponentOptions(mark.gameObject);
                var labels = options.Select(o => o.label).ToArray();
                var idx = Math.Max(0, Array.FindIndex(options, o => o.fullName == componentTypeProp.stringValue));
                var newIdx = EditorGUILayout.Popup(new GUIContent("组件类型"), idx, labels);
                if (newIdx >= 0 && newIdx < options.Length)
                {
                    componentTypeProp.stringValue = options[newIdx].fullName;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("按优先级自动选择", GUILayout.Width(140)))
                    {
                        var rec = Recommend(mark);
                        targetKindProp.enumValueIndex = (int)rec.kind;
                        componentTypeProp.stringValue = rec.typeFullName;
                    }
                    if (GUILayout.Button("清空选择", GUILayout.Width(100)))
                    {
                        componentTypeProp.stringValue = string.Empty;
                    }
                }
            }

            // nothing legacy

            serializedObject.ApplyModifiedProperties();
        }

        private static (UIMark.ExportTargetKind kind, string typeFullName) Recommend(UIMark mark)
        {
            // 封装与 runtime 一致的优先级逻辑
            var go = mark.gameObject;
            var t = FindFirstTypeOn(go, PriorityInteractive);
            if (t != null) return (UIMark.ExportTargetKind.Component, t.FullName);
            t = FindFirstTypeOn(go, PriorityMedium);
            if (t != null) return (UIMark.ExportTargetKind.Component, t.FullName);
            if (go.GetComponent<RectTransform>() != null) return (UIMark.ExportTargetKind.RectTransform, typeof(RectTransform).FullName);
            return (UIMark.ExportTargetKind.GameObject, null);
        }

        private static (string label, string fullName)[] CollectComponentOptions(GameObject go)
        {
            var list = new List<(string label, string fullName)>();

            // 分三段按优先级加入
            Append(go, list, PriorityInteractive);
            Append(go, list, PriorityMedium);

            // 其他所有组件（排除 Transform/RectTransform 和重复）
            var all = go.GetComponents<Component>();
            foreach (var c in all)
            {
                if (c == null) continue;
                var t = c.GetType();
                if (t == typeof(Transform) || t == typeof(RectTransform)) continue;
                var full = t.FullName;
                if (list.Any(x => x.fullName == full)) continue;
                list.Add(($"(其他) {t.Name}", full));
            }

            if (list.Count == 0)
            {
                list.Add(("(无可用组件，默认 GameObject)", string.Empty));
            }
            return list.ToArray();
        }

        private static void Append(GameObject go, List<(string label, string fullName)> list, string[] fullNames)
        {
            foreach (var fn in fullNames)
            {
                var t = FindType(fn);
                if (t != null && go.GetComponent(t) != null)
                {
                    list.Add(($"{t.Name}", t.FullName));
                }
            }
        }

        private static Type FindFirstTypeOn(GameObject go, string[] candidates)
        {
            foreach (var fn in candidates)
            {
                var t = FindType(fn);
                if (t != null && go.GetComponent(t) != null) return t;
            }
            return null;
        }

        private static Type FindType(string fullName)
        {
            var t = Type.GetType(fullName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName);
                    if (t != null) return t;
                }
                catch { }
            }
            return null;
        }

        private static string SanitizeToIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "field";
            var chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
            if (char.IsDigit(chars[0])) return "_" + new string(chars);
            return new string(chars);
        }
    }
}
