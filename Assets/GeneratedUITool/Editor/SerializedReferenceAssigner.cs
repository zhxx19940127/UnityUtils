using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityUtils.EditorTools.AutoUI
{
    /// <summary>
    /// 在 SerializedReferences 模式下，为生成的 [SerializeField] 字段赋值引用。
    /// 会扫描生成的字段路径并将引用写回到 Prefab 实例中。
    /// </summary>
    public static class SerializedReferenceAssigner
    {
        public class AssignStats
        {
            public int total;
            public int success;
            public int missingPath;
            public int missingComponent;
        }

        public static AssignStats Assign(GameObject prefabRoot, string className,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
        {
            var stats = new AssignStats();
            if (prefabRoot == null) return stats;

            var prefabPath = AssetDatabase.GetAssetPath(prefabRoot);
            if (string.IsNullOrEmpty(prefabPath)) return stats;

            var go = PrefabUtility.LoadPrefabContents(prefabPath);
            if (go == null) return stats;
            try
            {
                var comp = go.GetComponent(className);
                if (comp == null)
                {
                    // 如果类型还未编译可用或未挂载，直接返回（外层生成器有自动挂载流程）
                    return stats;
                }

                var so = new SerializedObject(comp);
                foreach (var f in fields)
                {
                    stats.total++;
                    var sp = so.FindProperty(f.name);
                    if (sp == null)
                    {
                        continue;
                    }

                    Transform tr = string.IsNullOrEmpty(f.path) ? go.transform : go.transform.Find(f.path);
                    if (tr == null)
                    {
                        stats.missingPath++;
                        continue;
                    }

                    UnityEngine.Object value = null;
                    if (f.isComponent)
                    {
                        var t = UICodeGenerator.UICodeGenerator_Internals.FindType(f.typeFullName);
                        if (t == null)
                        {
                            stats.missingComponent++;
                        }
                        else
                        {
                            var comps = tr.GetComponents(t);
                            if (comps != null && comps.Length > f.compIndex)
                            {
                                value = comps[f.compIndex] as UnityEngine.Object;
                            }
                            else
                            {
                                stats.missingComponent++;
                            }
                        }
                    }
                    else if (f.typeFullName == typeof(GameObject).FullName)
                    {
                        value = tr.gameObject;
                    }
                    else if (f.typeFullName == typeof(RectTransform).FullName)
                    {
                        value = tr.GetComponent<RectTransform>();
                    }

                    if (value != null)
                    {
                        sp.objectReferenceValue = value;
                        stats.success++;
                    }
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                return stats;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(go);
            }
        }
    }
}