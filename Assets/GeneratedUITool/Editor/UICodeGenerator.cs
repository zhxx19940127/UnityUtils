using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityUtils.UI;

namespace UnityUtils.EditorTools.AutoUI
{
    public static class UICodeGenerator
    {
        private static IEnumerable<Type> GetAutoIncludeTypes()
        {
            // 仅包含需求中指定的基础 UI 控件
            var list = new List<Type>
            {
                typeof(Button), typeof(Toggle), typeof(Slider), typeof(InputField),
            };

            // 尝试反射获取 TMP 相关类型，不依赖脚本宏定义
            TryAddType(list, "TMPro.TMP_Text");
            TryAddType(list, "TMPro.TMP_InputField");

            return list;
        }

        private static void TryAddType(List<Type> list, string fullName)
        {
            var t = FindType(fullName);
            if (t != null) list.Add(t);
        }

        private static Type FindType(string fullName)
        {
            // 优先直接 Type.GetType，再遍历 AppDomain
            var t = Type.GetType(fullName);
            if (t != null) return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    t = asm.GetType(fullName);
                    if (t != null) return t;
                }
                catch
                {
                }
            }

            return null;
        }

        public static void GenerateScript(GameObject prefab, string scriptFolder, AutoUICodeGenSettings settings)
        {
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("错误", "预制体为空", "确定");
                return;
            }

            var className = prefab.name;
            if (!IsValidClassName(className, settings.requireUppercaseClassName))
            {
                EditorUtility.DisplayDialog("命名不规范",
                    $"预制体名称不符合类名规则: {className}\n需以字母开头，仅包含字母数字下划线{(settings.requireUppercaseClassName ? "，且首字母大写" : string.Empty)}。",
                    "确定");
                return;
            }

            // 收集字段
            var fields = new List<(string typeFullName, string name, string path, bool isComponent)>();

            // 1) 自动包含常用控件
            if (settings.autoIncludeCommonControls)
            {
                foreach (var t in GetAutoIncludeTypes())
                {
                    var comps = prefab.GetComponentsInChildren(t, true);
                    foreach (var c in comps)
                    {
                        var tr = (c as Component).transform;
                        if (IsUnderIgnoredMark(tr)) continue; // respect ignoreChildren
                        var typeName = t.FullName; // 使用完整限定名，避免额外 using
                        var fieldName = MakeSafeFieldName(tr.name, t.Name);
                        var path = GetPath(tr, prefab.transform);
                        fields.Add((typeName, fieldName, path, true));
                    }
                }
            }

            // 2) UIMark 控制
            var marks = prefab.GetComponentsInChildren<UIMark>(true);
            foreach (var mark in marks)
            {
                if (mark == null) continue;
                var tr = mark.transform;
                // 如果 ignoreChildren，则不再处理其子物体，交给 IsUnderIgnoredMark 过滤
                var fieldBaseName = string.IsNullOrWhiteSpace(mark.fieldName) ? tr.name : mark.fieldName.Trim();
                var fieldName = MakeSafeFieldName(fieldBaseName);
                var path = GetPath(tr, prefab.transform);

                // 仅使用新配置：目标类型
                switch (mark.targetKind)
                {
                    case UIMark.ExportTargetKind.Component:
                    {
                        var t = FindType(mark.componentTypeFullName);
                        if (t == null)
                        {
                            // 如果用户选择的类型无效，则回退到自动推荐
                            AddAutoByPriority(tr.gameObject, fieldName, path, fields);
                        }
                        else
                        {
                            var comp = tr.GetComponent(t);
                            if (comp != null)
                            {
                                fields.Add((t.FullName, fieldName, path, true));
                            }
                            else
                            {
                                // 没有该组件，降级为 RectTransform / GameObject
                                AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                            }
                        }
                    }
                        break;
                    case UIMark.ExportTargetKind.RectTransform:
                        fields.Add((typeof(RectTransform).FullName, fieldName, path, false));
                        break;
                    case UIMark.ExportTargetKind.GameObject:
                        fields.Add((typeof(GameObject).FullName, fieldName, path, false));
                        break;
                    case UIMark.ExportTargetKind.Auto:
                    default:
                    {
                        // 先按优先级（交互 > 文本/图像 > RectTransform > GameObject）
                        if (!AddAutoByPriority(tr.gameObject, fieldName, path, fields))
                        {
                            AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                        }
                    }
                        break;
                }
            }

            // 去重：按路径+类型唯一
            fields = fields
                .GroupBy(f => (f.path, f.typeFullName))
                .Select(g => g.First())
                .OrderBy(f => f.typeFullName)
                .ThenBy(f => f.name)
                .ToList();

            // 字段名唯一性处理
            EnsureUniqueFieldNames(fields);

            // 生成代码：单文件 + 标记段（避免覆盖用户代码，后续仅替换标记区）
            if (!Directory.Exists(scriptFolder)) Directory.CreateDirectory(scriptFolder);

            var userPath = Path.Combine(scriptFolder, className + ".cs");
            if (File.Exists(userPath))
            {
                var original = File.ReadAllText(userPath, Encoding.UTF8);
                // 1) 更新类名（仅替换第一个 class 声明的类名）
                var updated = ReplaceFirstClassName(original, className);
                // 2) Upsert 字段段落
                updated = UpsertSection(updated, FieldsStartMarker, FieldsEndMarker, BuildFieldsSection(fields));
                // 3) Upsert 赋值方法段落（Awake/Start）
                updated = UpsertSection(updated, AssignStartMarker, AssignEndMarker,
                    BuildAssignSection(fields, settings.assignInAwake));

                File.WriteAllText(userPath, updated, Encoding.UTF8);
                AssetDatabase.ImportAsset(RelativeToProject(userPath));
                EditorUtility.DisplayDialog("完成", $"已更新: {userPath}", "确定");
            }
            else
            {
                // 首次生成：写入 using + 类定义 + 标记段落
                var code = BuildFullFileWithMarkers(className, fields, settings.assignInAwake);
                File.WriteAllText(userPath, code, Encoding.UTF8);
                AssetDatabase.ImportAsset(RelativeToProject(userPath));
                EditorUtility.DisplayDialog("完成", $"已生成: {userPath}", "确定");
            }
        }

        private static string BuildClassCode(string className,
            List<(string typeFullName, string name, string path, bool isComponent)> fields, bool assignInAwake,
            bool includeUsings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 自动生成，请勿手改（可重复生成覆盖）");
            if (includeUsings)
            {
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("using UnityEngine.UI;");
                sb.AppendLine();
            }

            sb.AppendLine($"public class {className} : MonoBehaviour");
            sb.AppendLine("{");

            // 字段（标记段）
            sb.Append(BuildFieldsSection(fields));

            // 赋值方法（标记段）
            sb.Append(BuildAssignSection(fields, assignInAwake));

            sb.AppendLine("}");
            return sb.ToString();
        }

        private const string FieldsStartMarker = "// <auto-fields>";
        private const string FieldsEndMarker = "// </auto-fields>";
        private const string AssignStartMarker = "// <auto-assign>";
        private const string AssignEndMarker = "// </auto-assign>";

        private static string BuildFullFileWithMarkers(string className,
            List<(string typeFullName, string name, string path, bool isComponent)> fields, bool assignInAwake)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 自动生成，请勿手改（可重复生成覆盖）");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine("using UnityEngine.UI;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : MonoBehaviour");
            sb.AppendLine("{");
            sb.Append(BuildFieldsSection(fields));
            sb.Append(BuildAssignSection(fields, assignInAwake));
            sb.AppendLine();
            sb.AppendLine("    // <user-code>");
            sb.AppendLine("    // 你的手写逻辑请写在这里，生成器不会修改此区域");
            sb.AppendLine("    // </user-code>");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildFieldsSection(
            List<(string typeFullName, string name, string path, bool isComponent)> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    " + FieldsStartMarker);
            foreach (var f in fields)
            {
                sb.AppendLine($"    private {f.typeFullName} {f.name};".Insert(0, "    "));
            }

            sb.AppendLine("    " + FieldsEndMarker);
            sb.AppendLine();
            return sb.ToString();
        }

        private static string BuildAssignSection(
            List<(string typeFullName, string name, string path, bool isComponent)> fields, bool assignInAwake)
        {
            var method = assignInAwake ? "Awake" : "Start";
            var sb = new StringBuilder();
            sb.AppendLine("    " + AssignStartMarker);
            sb.AppendLine($"    private void {method}()");
            sb.AppendLine("    {");
            foreach (var f in fields)
            {
                bool isRoot = string.IsNullOrEmpty(f.path);
                if (f.isComponent)
                {
                    if (isRoot)
                        sb.AppendLine($"        {f.name} = GetComponent<{f.typeFullName}>();");
                    else
                        sb.AppendLine(
                            $"        {f.name} = transform.Find(\"{EscapePath(f.path)}\").GetComponent<{f.typeFullName}>();");
                }
                else
                {
                    if (f.typeFullName == typeof(GameObject).FullName)
                    {
                        if (isRoot)
                            sb.AppendLine($"        {f.name} = gameObject;");
                        else
                            sb.AppendLine($"        {f.name} = transform.Find(\"{EscapePath(f.path)}\").gameObject;");
                    }
                    else if (f.typeFullName == typeof(RectTransform).FullName)
                    {
                        if (isRoot)
                            sb.AppendLine($"        {f.name} = GetComponent<UnityEngine.RectTransform>();");
                        else
                            sb.AppendLine(
                                $"        {f.name} = transform.Find(\"{EscapePath(f.path)}\").GetComponent<UnityEngine.RectTransform>();");
                    }
                    else
                    {
                        sb.AppendLine($"        // 未知类型处理: {f.typeFullName}");
                    }
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine("    " + AssignEndMarker);
            sb.AppendLine();
            return sb.ToString();
        }

        private static string UpsertSection(string source, string startMarker, string endMarker, string newSection)
        {
            if (string.IsNullOrEmpty(source)) return newSection;
            var start = source.IndexOf(startMarker, StringComparison.Ordinal);
            var end = source.IndexOf(endMarker, StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                end += endMarker.Length;
                return source.Substring(0, start) + newSection + source.Substring(end);
            }

            // 未找到标记则插入：字段在类首个 '{' 之后，赋值段落在最后一个 '}' 之前
            var openIdx = source.IndexOf('{');
            var closeIdx = source.LastIndexOf('}');
            if (openIdx >= 0)
            {
                if (startMarker == FieldsStartMarker)
                {
                    return source.Insert(openIdx + 1, "\n" + newSection);
                }
                else if (endMarker == AssignEndMarker && closeIdx > openIdx)
                {
                    return source.Insert(closeIdx, newSection);
                }
            }

            return source + "\n" + newSection;
        }

        private static string ReplaceFirstClassName(string source, string newClassName)
        {
            if (string.IsNullOrEmpty(source)) return source;
            try
            {
                var rx = new System.Text.RegularExpressions.Regex(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)");
                var m = rx.Match(source);
                if (m.Success && m.Groups.Count > 1)
                {
                    var oldName = m.Groups[1].Value;
                    var pattern = @"\bclass\s+" + System.Text.RegularExpressions.Regex.Escape(oldName) + @"\b";
                    var rx2 = new System.Text.RegularExpressions.Regex(pattern);
                    return rx2.Replace(source, "class " + newClassName, 1);
                }
            }
            catch
            {
            }

            return source;
        }

        // removed partial detection; now single-file with markers

        private static string ExtractTopUsings(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var usingLines = new List<string>();
            int i = 0;
            // 跳过文件头部空行
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
            // 收集连续的 using 行
            while (i < lines.Length)
            {
                var l = lines[i];
                if (l.TrimStart().StartsWith("using "))
                {
                    usingLines.Add(l);
                    i++;
                }
                else
                {
                    break;
                }
            }

            return string.Join("\n", usingLines);
        }

        private static string EscapePath(string p) => p.Replace("\\", "\\\\").Replace("\"", "\\\"");

        private static string RelativeToProject(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return absolutePath;
            var projectRoot = System.IO.Directory.GetParent(Application.dataPath).FullName.Replace("\\", "/");
            var normalized = absolutePath.Replace("\\", "/");
            if (normalized.StartsWith(projectRoot))
            {
                return normalized.Substring(projectRoot.Length + 1);
            }

            return absolutePath;
        }

        private static bool IsValidClassName(string name, bool requireUppercase)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$")) return false;
            if (requireUppercase && !char.IsUpper(name[0])) return false;
            return true;
        }

        private static string MakeSafeFieldName(string raw, string typeHint = null)
        {
            if (string.IsNullOrWhiteSpace(raw)) raw = "field";
            var name = new string(raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
            if (char.IsDigit(name[0])) name = "_" + name;
            // 避免与类型同名导致歧义：追加后缀
            if (!string.IsNullOrEmpty(typeHint) && string.Equals(name, typeHint, StringComparison.Ordinal))
                name = name + "_";
            return name;
        }

        private static string GetPath(Transform target, Transform root)
        {
            var stack = new Stack<string>();
            var cur = target;
            while (cur != null && cur != root)
            {
                stack.Push(cur.name);
                cur = cur.parent;
            }

            return string.Join("/", stack);
        }

        private static bool IsUnderIgnoredMark(Transform tr)
        {
            if (tr == null) return false;
            var cur = tr;
            while (cur != null)
            {
                var mark = cur.GetComponent<UIMark>();
                if (mark != null && mark.ignoreChildren && cur != tr)
                    return true;
                cur = cur.parent;
            }

            return false;
        }

        // 根据优先级自动添加（交互 > 文本/图像 > RectTransform > GameObject），返回是否已添加组件类型
        private static bool AddAutoByPriority(GameObject go, string fieldName, string path,
            List<(string typeFullName, string name, string path, bool isComponent)> fields)
        {
            var t = FindFirstTypeOn(go,
                new[]
                {
                    "UnityEngine.UI.Button",
                    "UnityEngine.UI.Toggle",
                    "UnityEngine.UI.Slider",
                    "UnityEngine.UI.InputField",
                    "TMPro.TMP_InputField",
                });
            if (t == null)
            {
                t = FindFirstTypeOn(go, new[]
                {
                    "TMPro.TMP_Text",
                    "UnityEngine.UI.Text",
                    "UnityEngine.UI.Image",
                    "UnityEngine.UI.RawImage",
                });
            }

            if (t != null)
            {
                fields.Add((t.FullName, fieldName, path, true));
                return true;
            }

            return false;
        }

        private static void AddFallbackRectOrGO(Transform tr, string fieldName, string path,
            List<(string typeFullName, string name, string path, bool isComponent)> fields, bool preferRect)
        {
            if (preferRect && tr.GetComponent<RectTransform>() != null)
            {
                fields.Add((typeof(RectTransform).FullName, fieldName, path, false));
            }
            else
            {
                fields.Add((typeof(GameObject).FullName, fieldName, path, false));
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

        private static void EnsureUniqueFieldNames(
            List<(string typeFullName, string name, string path, bool isComponent)> fields)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                var baseName = f.name;
                var name = baseName;
                int idx = 1;
                while (used.Contains(name))
                {
                    name = baseName + "_" + idx++;
                }

                used.Add(name);
                if (name != f.name)
                {
                    fields[i] = (f.typeFullName, name, f.path, f.isComponent);
                }
            }
        }
    }
}