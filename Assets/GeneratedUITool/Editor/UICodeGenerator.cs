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
        // 暴露给同程序集内部使用的工具（序列化赋值器）
        internal static class UICodeGenerator_Internals
        {
            public static System.Type FindType(string fullName) => UnityUtils.EditorTools.AutoUI.UICodeGenerator.FindType(fullName);
        }

        public struct GenerationResult
        {
            public bool fileChanged;
            public SerializedReferenceAssigner.AssignStats assignStats;
        }

        private static IEnumerable<Type> GetAutoIncludeTypes(AutoUICodeGenSettings settings)
        {
            var list = new List<Type>
            {
                typeof(Button), typeof(Toggle), typeof(Slider), typeof(InputField),
            };
            TryAddType(list, "TMPro.TMP_Text");
            TryAddType(list, "TMPro.TMP_InputField");
            if (settings != null && settings.autoIncludeExtendedControls)
            {
                TryAddType(list, "UnityEngine.UI.ScrollRect");
                TryAddType(list, "UnityEngine.UI.Scrollbar");
                TryAddType(list, "UnityEngine.UI.Dropdown");
            }
            return list;
        }

        private static void TryAddType(List<Type> list, string fullName)
        {
            var t = FindType(fullName);
            if (t != null) list.Add(t);
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

    public static GenerationResult GenerateScript(GameObject prefab, string scriptFolder, AutoUICodeGenSettings settings)
        {
            var result = new GenerationResult { fileChanged = false, assignStats = null };
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("错误", "预制体为空", "确定");
                return result;
            }

            var className = prefab.name;
            if (!IsValidClassName(className, settings.requireUppercaseClassName))
            {
                EditorUtility.DisplayDialog("命名不规范",
                    $"预制体名称不符合类名规则: {className}\n需以字母开头，仅包含字母数字下划线{(settings.requireUppercaseClassName ? "，且首字母大写" : string.Empty)}。",
                    "确定");
                return result;
            }

            // 收集字段: (typeFullName, name, path, isComponent, compIndex)
            var fields = new List<(string typeFullName, string name, string path, bool isComponent, int compIndex)>();

            // 1) 自动包含常用控件
            if (settings.autoIncludeCommonControls)
            {
                foreach (var t in GetAutoIncludeTypes(settings))
                {
                    var comps = prefab.GetComponentsInChildren(t, true);
                    foreach (var c in comps)
                    {
                        var tr = (c as Component).transform;
                        if (IsUnderIgnoredMark(tr)) continue;
                        var typeName = t.FullName;
                        var fieldName = MakeSafeFieldName(tr.name, t.Name);
                        var path = GetPath(tr, prefab.transform);
                        int idx = 0;
                        var all = tr.GetComponents(t);
                        for (int k = 0; k < all.Length; k++) { if (ReferenceEquals(all[k], c)) { idx = k; break; } }
                        fields.Add((typeName, fieldName, path, true, idx));
                    }
                }
            }

            // 2) UIMark 控制
            var marks = prefab.GetComponentsInChildren<UIMark>(true);
            foreach (var mark in marks)
            {
                if (mark == null) continue;
                var tr = mark.transform;
                var fieldBaseName = string.IsNullOrWhiteSpace(mark.fieldName) ? tr.name : mark.fieldName.Trim();
                var fieldName = MakeSafeFieldName(fieldBaseName);
                var path = GetPath(tr, prefab.transform);

                switch (mark.targetKind)
                {
                    case UIMark.ExportTargetKind.Component:
                    {
                        var t = FindType(mark.componentTypeFullName);
                        if (t == null)
                        {
                            // 无效类型，回退自动
                            AddAutoByPriority(tr.gameObject, fieldName, path, fields);
                        }
                        else
                        {
                            var comps = tr.GetComponents(t);
                            if (comps != null && comps.Length > 0)
                            {
                                var idx = Mathf.Clamp(mark.componentIndex, 0, comps.Length - 1);
                                fields.Add((t.FullName, fieldName, path, true, idx));
                            }
                            else
                            {
                                AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                            }
                        }
                    }
                        break;
                    case UIMark.ExportTargetKind.RectTransform:
                        fields.Add((typeof(RectTransform).FullName, fieldName, path, false, 0));
                        break;
                    case UIMark.ExportTargetKind.GameObject:
                        fields.Add((typeof(GameObject).FullName, fieldName, path, false, 0));
                        break;
                    case UIMark.ExportTargetKind.Auto:
                    default:
                    {
                        if (!AddAutoByPriority(tr.gameObject, fieldName, path, fields))
                        {
                            AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                        }
                    }
                        break;
                }
            }

            // 去重：按 (path, type, compIndex)
            fields = fields
                .GroupBy(f => (f.path, f.typeFullName, f.compIndex))
                .Select(g => g.First())
                .OrderBy(f => f.typeFullName)
                .ThenBy(f => f.name)
                .ToList();

            // 字段命名：组件前缀
            if (settings.useComponentPrefixForFields)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    var prefix = GetPrefixForTypeFullName(f.typeFullName, settings);
                    var baseName = f.name;
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        var lower = baseName.ToLowerInvariant();
                        if (!(lower.StartsWith(prefix + "_") || lower.StartsWith(prefix)))
                        {
                            baseName = prefix + "_" + baseName;
                        }
                    }
                    fields[i] = (f.typeFullName, baseName, f.path, f.isComponent, f.compIndex);
                }
            }

            // 字段命名：_camelCase
            if (settings.privateFieldUnderscoreCamelCase)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    var camel = ToCamelCase(f.name);
                    var finalName = camel.StartsWith("_") ? camel : "_" + camel;
                    fields[i] = (f.typeFullName, finalName, f.path, f.isComponent, f.compIndex);
                }
            }

            EnsureUniqueFieldNames(fields);

            if (!Directory.Exists(scriptFolder)) Directory.CreateDirectory(scriptFolder);
            var userPath = Path.Combine(scriptFolder, className + ".cs");
            bool serializedMode = settings.initAssignMode == AutoUICodeGenSettings.InitAssignMode.SerializedReferences;

            if (File.Exists(userPath))
            {
                var original = File.ReadAllText(userPath, Encoding.UTF8);
                var updated = ReplaceClassSignature(original, className, settings);
                updated = UpsertSectionInClass(updated, className, FieldsStartMarker, FieldsEndMarker, BuildFieldsSection(fields, serializedMode));
                if (settings.generateReadOnlyProperties)
                    updated = UpsertSectionInClass(updated, className, PropsStartMarker, PropsEndMarker, BuildReadOnlyPropertiesSection(fields));
                else
                    updated = UpsertSectionInClass(updated, className, PropsStartMarker, PropsEndMarker, string.Empty);
                var assignSection = serializedMode ? string.Empty : BuildInitMethodSection(fields);
                var beforeAssign = updated;
                updated = UpsertSectionInClass(updated, className, AssignStartMarker, AssignEndMarker, assignSection);
                if (string.IsNullOrEmpty(beforeAssign) || (beforeAssign.IndexOf(AssignStartMarker, StringComparison.Ordinal) < 0 && !string.IsNullOrEmpty(assignSection)))
                {
                    if (AutoUICodeGenSettings.Ensure().logMarkerRecovery)
                        Debug.Log("[AutoUI] 检测到赋值标记段缺失，已自动恢复。");
                }

                if (!string.Equals(original, updated, StringComparison.Ordinal))
                {
                    File.WriteAllText(userPath, updated, Encoding.UTF8);
                    AssetDatabase.ImportAsset(RelativeToProject(userPath));
                    result.fileChanged = true;
                }

                if (serializedMode)
                {
                    result.assignStats = SerializedReferenceAssigner.Assign(prefab, className, fields);
                }

                EditorUtility.DisplayDialog("完成", result.fileChanged ? $"已更新: {userPath}" : $"无变化: {userPath}", "确定");
            }
            else
            {
                var code = BuildFullFileWithMarkers(className, fields, serializedMode, settings);
                File.WriteAllText(userPath, code, Encoding.UTF8);
                AssetDatabase.ImportAsset(RelativeToProject(userPath));
                result.fileChanged = true;

                if (serializedMode)
                {
                    result.assignStats = SerializedReferenceAssigner.Assign(prefab, className, fields);
                }

                EditorUtility.DisplayDialog("完成", $"已生成: {userPath}", "确定");
            }

            return result;
        }

        // 提供给外部在不生成文件时收集字段（用于序列化引用赋值）
        public static List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> CollectFields(GameObject prefab, AutoUICodeGenSettings settings)
        {
            var fields = new List<(string typeFullName, string name, string path, bool isComponent, int compIndex)>();
            if (prefab == null) return fields;

            // 自动包含
            if (settings.autoIncludeCommonControls)
            {
                foreach (var t in GetAutoIncludeTypes(settings))
                {
                    var comps = prefab.GetComponentsInChildren(t, true);
                    foreach (var c in comps)
                    {
                        var tr = (c as Component).transform;
                        if (IsUnderIgnoredMark(tr)) continue;
                        var typeName = t.FullName;
                        var fieldName = MakeSafeFieldName(tr.name, t.Name);
                        var path = GetPath(tr, prefab.transform);
                        int idx = 0;
                        var all = tr.GetComponents(t);
                        for (int k = 0; k < all.Length; k++) { if (ReferenceEquals(all[k], c)) { idx = k; break; } }
                        fields.Add((typeName, fieldName, path, true, idx));
                    }
                }
            }

            // UIMark
            var marks = prefab.GetComponentsInChildren<UIMark>(true);
            foreach (var mark in marks)
            {
                if (mark == null) continue;
                var tr = mark.transform;
                var fieldBaseName = string.IsNullOrWhiteSpace(mark.fieldName) ? tr.name : mark.fieldName.Trim();
                var fieldName = MakeSafeFieldName(fieldBaseName);
                var path = GetPath(tr, prefab.transform);

                switch (mark.targetKind)
                {
                    case UIMark.ExportTargetKind.Component:
                    {
                        var t = FindType(mark.componentTypeFullName);
                        if (t == null)
                        {
                            AddAutoByPriority(tr.gameObject, fieldName, path, fields);
                        }
                        else
                        {
                            var comps = tr.GetComponents(t);
                            if (comps != null && comps.Length > 0)
                            {
                                var idx = Mathf.Clamp(mark.componentIndex, 0, comps.Length - 1);
                                fields.Add((t.FullName, fieldName, path, true, idx));
                            }
                            else
                            {
                                AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                            }
                        }
                    }
                        break;
                    case UIMark.ExportTargetKind.RectTransform:
                        fields.Add((typeof(RectTransform).FullName, fieldName, path, false, 0));
                        break;
                    case UIMark.ExportTargetKind.GameObject:
                        fields.Add((typeof(GameObject).FullName, fieldName, path, false, 0));
                        break;
                    case UIMark.ExportTargetKind.Auto:
                    default:
                    {
                        if (!AddAutoByPriority(tr.gameObject, fieldName, path, fields))
                        {
                            AddFallbackRectOrGO(tr, fieldName, path, fields, preferRect: true);
                        }
                    }
                        break;
                }
            }

            // 去重、排序与命名规则保持与生成一致
            fields = fields
                .GroupBy(f => (f.path, f.typeFullName, f.compIndex))
                .Select(g => g.First())
                .OrderBy(f => f.typeFullName)
                .ThenBy(f => f.name)
                .ToList();

            if (settings.useComponentPrefixForFields)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    var prefix = GetPrefixForTypeFullName(f.typeFullName, settings);
                    var baseName = f.name;
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        var lower = baseName.ToLowerInvariant();
                        if (!(lower.StartsWith(prefix + "_") || lower.StartsWith(prefix)))
                        {
                            baseName = prefix + "_" + baseName;
                        }
                    }
                    fields[i] = (f.typeFullName, baseName, f.path, f.isComponent, f.compIndex);
                }
            }

            if (settings.privateFieldUnderscoreCamelCase)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var f = fields[i];
                    var camel = ToCamelCase(f.name);
                    var finalName = camel.StartsWith("_") ? camel : "_" + camel;
                    fields[i] = (f.typeFullName, finalName, f.path, f.isComponent, f.compIndex);
                }
            }

            EnsureUniqueFieldNames(fields);
            return fields;
        }

        private static string BuildClassCode(string className,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields,
            bool includeUsings, bool serializedMode, AutoUICodeGenSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// 自动生成，请勿手改（可重复生成覆盖）");
            if (includeUsings)
            {
                sb.AppendLine("using UnityEngine;");
                sb.AppendLine("using UnityEngine.UI;");
                sb.AppendLine();
            }

            var baseCls = string.IsNullOrWhiteSpace(settings.baseClassFullName) ? "UnityEngine.MonoBehaviour" : settings.baseClassFullName.Trim();

            Action body = () =>
            {
                sb.AppendLine($"public class {className} : {baseCls}");
                sb.AppendLine("{");
            sb.Append(BuildFieldsSection(fields, serializedMode));
                if (AutoUICodeGenSettings.Ensure().generateReadOnlyProperties)
                    sb.Append(BuildReadOnlyPropertiesSection(fields));
                if (!serializedMode) sb.Append(BuildInitMethodSection(fields));
                sb.AppendLine("    // <user-code>");
                sb.AppendLine("    // 你的手写逻辑请写在这里，生成器不会修改此区域");
                sb.AppendLine("    // </user-code>");
                sb.AppendLine("}");
            };

            if (!string.IsNullOrWhiteSpace(settings.wrapNamespace))
            {
                sb.AppendLine($"namespace {settings.wrapNamespace.Trim()}");
                sb.AppendLine("{");
                body();
                sb.AppendLine("}");
            }
            else
            {
                body();
            }
            return sb.ToString();
        }

        private const string FieldsStartMarker = "// <auto-fields>";
        private const string FieldsEndMarker = "// </auto-fields>";
        private const string PropsStartMarker = "// <auto-props>";
        private const string PropsEndMarker = "// </auto-props>";
    private const string AssignStartMarker = "// <auto-assign>";
    private const string AssignEndMarker = "// </auto-assign>";

        private static string BuildFullFileWithMarkers(string className,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields,
            bool serializedMode, AutoUICodeGenSettings settings)
        {
            return BuildClassCode(className, fields, includeUsings: true, serializedMode: serializedMode, settings: settings);
        }

        private static string BuildFieldsSection(
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields, bool serializedMode = false)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    " + FieldsStartMarker);
            foreach (var f in fields)
            {
                var attr = serializedMode ? "[SerializeField] " : string.Empty;
                sb.AppendLine($"    {attr}private {f.typeFullName} {f.name};");
            }
            sb.AppendLine("    " + FieldsEndMarker);
            return sb.ToString();
        }

        private static string BuildReadOnlyPropertiesSection(List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    " + PropsStartMarker);
            foreach (var f in fields)
            {
                var baseName = f.name.TrimStart('_');
                if (AutoUICodeGenSettings.Ensure().stripPrefixInPropertyNames)
                {
                    baseName = StripKnownPrefixes(baseName);
                }
                var propName = ToPascalCase(baseName);
                sb.AppendLine($"    public {f.typeFullName} {propName} => {f.name};");
            }
            sb.AppendLine("    " + PropsEndMarker);
            return sb.ToString();
        }

        private static string BuildInitMethodSection(
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    " + AssignStartMarker);
            sb.AppendLine($"    public void InitRefs()");
            sb.AppendLine("    {");
            int __idx = 0;
            foreach (var f in fields)
            {
                bool isRoot = string.IsNullOrEmpty(f.path);
                if (f.isComponent)
                {
                    if (isRoot)
                    {
                        sb.AppendLine($"        var __comps = GetComponents<{f.typeFullName}>();");
                        sb.AppendLine($"        {f.name} = (__comps != null && __comps.Length > {f.compIndex}) ? __comps[{f.compIndex}] : null;");
                        sb.AppendLine($"        if ({f.name} == null) Debug.LogError(\"[AutoUI] 根节点未找到索引 {f.compIndex} 的组件 {f.typeFullName}\");");
                    }
                    else
                    {
                        var trVar = $"__tr{__idx++}";
                        sb.AppendLine($"        var {trVar} = transform.Find(\"{EscapePath(f.path)}\");");
                        sb.AppendLine($"        if ({trVar} == null) Debug.LogError(\"[AutoUI] 未找到路径: {EscapePath(f.path)}\");");
                        sb.AppendLine("        else {");
                        sb.AppendLine($"            var __comps = {trVar}.GetComponents<{f.typeFullName}>();");
                        sb.AppendLine($"            {f.name} = (__comps != null && __comps.Length > {f.compIndex}) ? __comps[{f.compIndex}] : null;");
                        sb.AppendLine($"            if ({f.name} == null) Debug.LogError(\"[AutoUI] 路径 {EscapePath(f.path)} 未找到索引 {f.compIndex} 的组件 {f.typeFullName}\");");
                        sb.AppendLine("        }");
                    }
                }
                else
                {
                    if (f.typeFullName == typeof(GameObject).FullName)
                    {
                        if (isRoot)
                            sb.AppendLine($"        {f.name} = gameObject;");
                        else
                        {
                            var trVar = $"__tr{__idx++}";
                            sb.AppendLine($"        var {trVar} = transform.Find(\"{EscapePath(f.path)}\");");
                            sb.AppendLine($"        if ({trVar} == null) Debug.LogError(\"[AutoUI] 未找到路径: {EscapePath(f.path)}\");");
                            sb.AppendLine($"        else {{ {f.name} = {trVar}.gameObject; }}");
                        }
                    }
                    else if (f.typeFullName == typeof(RectTransform).FullName)
                    {
                        if (isRoot)
                        {
                            sb.AppendLine($"        {f.name} = GetComponent<UnityEngine.RectTransform>();");
                            sb.AppendLine($"        if ({f.name} == null) Debug.LogError(\"[AutoUI] 缺少组件 UnityEngine.RectTransform 于根节点\");");
                        }
                        else
                        {
                            var trVar = $"__tr{__idx++}";
                            sb.AppendLine($"        var {trVar} = transform.Find(\"{EscapePath(f.path)}\");");
                            sb.AppendLine($"        if ({trVar} == null) Debug.LogError(\"[AutoUI] 未找到路径: {EscapePath(f.path)}\");");
                            sb.AppendLine($"        else {{ {f.name} = {trVar}.GetComponent<UnityEngine.RectTransform>(); if ({f.name} == null) Debug.LogError(\\\"[AutoUI] 路径 {EscapePath(f.path)} 缺少组件 UnityEngine.RectTransform\\\"); }}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"        // 未知类型处理: {f.typeFullName}");
                    }
                }
            }
            sb.AppendLine("    }");
            sb.AppendLine("    " + AssignEndMarker);
            return sb.ToString();
        }

    private static string UpsertSection(string source, string startMarker, string endMarker, string newSection)
        {
            if (string.IsNullOrEmpty(source)) return newSection;
            var start = source.IndexOf(startMarker, StringComparison.Ordinal);
            var end = source.IndexOf(endMarker, StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                int lineStart = source.LastIndexOf('\n', Math.Max(0, start - 1));
                if (lineStart < 0) lineStart = 0; else lineStart += 1;
                end += endMarker.Length;
                int lineEnd = source.IndexOf('\n', end);
                if (lineEnd >= 0) end = lineEnd + 1;
                return source.Substring(0, lineStart) + newSection + source.Substring(end);
            }

            var openIdx = source.IndexOf('{');
            var closeIdx = source.LastIndexOf('}');
            if (openIdx >= 0)
            {
                if (startMarker == FieldsStartMarker)
                {
                    var insertPos = openIdx + 1;
                    var needsNl = insertPos < source.Length && source[insertPos] != '\n';
                    var payload = needsNl ? ("\n" + newSection) : newSection;
                    return source.Insert(insertPos, payload);
                }
                else if (startMarker == PropsStartMarker)
                {
                    var fieldsEnd = source.IndexOf(FieldsEndMarker, StringComparison.Ordinal);
                    if (fieldsEnd >= 0)
                    {
                        int insertPos = fieldsEnd + FieldsEndMarker.Length;
                        var needsNl = insertPos < source.Length && source[insertPos] != '\n';
                        var payload = needsNl ? ("\n" + newSection) : newSection;
                        return source.Insert(insertPos, payload);
                    }
                    var assignStart = source.IndexOf(AssignStartMarker, StringComparison.Ordinal);
                    if (assignStart >= 0)
                    {
                        var needsNl = assignStart > 0 && source[assignStart - 1] != '\n';
                        var payload = needsNl ? ("\n" + newSection) : newSection;
                        return source.Insert(assignStart, payload);
                    }
                    if (closeIdx > openIdx)
                    {
                        var needsNl = closeIdx > 0 && source[closeIdx - 1] != '\n';
                        var payload = needsNl ? ("\n" + newSection) : newSection;
                        return source.Insert(closeIdx, payload);
                    }
                }
                else if (endMarker == AssignEndMarker && closeIdx > openIdx)
                {
                    var needsNl = closeIdx > 0 && source[closeIdx - 1] != '\n';
                    var payload = needsNl ? ("\n" + newSection) : newSection;
                    return source.Insert(closeIdx, payload);
                }
            }

            var suffixNeedsNl = source.Length > 0 && source[source.Length - 1] != '\n';
            return source + (suffixNeedsNl ? "\n" : string.Empty) + newSection;
        }

        // 在指定类体内插入/更新标记段，兼容命名空间包装
        private static string UpsertSectionInClass(string source, string className, string startMarker, string endMarker, string newSection)
        {
            if (string.IsNullOrEmpty(source)) return newSection;
            // 粗略定位 class 声明起点
            var rx = new System.Text.RegularExpressions.Regex(@"\bclass\s+" + System.Text.RegularExpressions.Regex.Escape(className) + @"\b");
            var m = rx.Match(source);
            if (!m.Success) return UpsertSection(source, startMarker, endMarker, newSection);
            int braceOpen = source.IndexOf('{', m.Index);
            if (braceOpen < 0) return UpsertSection(source, startMarker, endMarker, newSection);
            // 简单配对找到类的结束 '}'（不考虑嵌套类型）
            int depth = 0; int i = braceOpen;
            for (; i < source.Length; i++)
            {
                if (source[i] == '{') depth++;
                else if (source[i] == '}') { depth--; if (depth == 0) { i++; break; } }
            }
            int classEnd = i;
            var before = source.Substring(0, braceOpen + 1);
            var body = source.Substring(braceOpen + 1, Math.Max(0, classEnd - (braceOpen + 1)));
            var after = source.Substring(classEnd);

            var updatedBody = UpsertSection(body, startMarker, endMarker, newSection);
            if (ReferenceEquals(updatedBody, body)) return source; // 无变化
            return before + updatedBody + after;
        }

        private static string ReplaceClassSignature(string source, string newClassName, AutoUICodeGenSettings settings)
        {
            if (string.IsNullOrEmpty(source)) return source;
            try
            {
                var rx = new System.Text.RegularExpressions.Regex(@"\bclass\s+([A-Za-z_][A-Za-z0-9_]*)");
                var m = rx.Match(source);
                if (m.Success && m.Groups.Count > 1)
                {
                    var oldName = m.Groups[1].Value;
                    // 替换类名
                    var pattern = @"\bclass\s+" + System.Text.RegularExpressions.Regex.Escape(oldName) + @"\b";
                    var rx2 = new System.Text.RegularExpressions.Regex(pattern);
                    var replaced = rx2.Replace(source, "class " + newClassName, 1);

                    // 替换或插入基类
                    var baseCls = string.IsNullOrWhiteSpace(settings.baseClassFullName) ? "UnityEngine.MonoBehaviour" : settings.baseClassFullName.Trim();
                    var afterClassIdx = replaced.IndexOf("class " + newClassName) + ("class " + newClassName).Length;
                    // 查找 ':' 是否存在于 '{' 之前
                    int braceIdx = replaced.IndexOf('{', afterClassIdx);
                    if (braceIdx > 0)
                    {
                        int colonIdx = replaced.IndexOf(':', afterClassIdx);
                        if (colonIdx < 0 || colonIdx > braceIdx)
                        {
                            replaced = replaced.Insert(afterClassIdx, " : " + baseCls);
                        }
                        else
                        {
                            // 已有继承，尝试替换第一个基类名为新的
                            // 从 ':' 到 '{' 的区间
                            var seg = replaced.Substring(colonIdx + 1, braceIdx - (colonIdx + 1));
                            // 简单替换第一个标识符为 baseCls
                            var segRx = new System.Text.RegularExpressions.Regex(@"^[^,{]+");
                            replaced = replaced.Remove(colonIdx + 1, seg.Length)
                                .Insert(colonIdx + 1, " " + baseCls + seg.Substring(segRx.Match(seg).Value.Length));
                        }
                    }
                    return replaced;
                }
            }
            catch { }
            return source;
        }

        private static string ExtractTopUsings(string source)
        {
            if (string.IsNullOrEmpty(source)) return string.Empty;
            var lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var usingLines = new List<string>();
            int i = 0;
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i])) i++;
            while (i < lines.Length)
            {
                var l = lines[i];
                if (l.TrimStart().StartsWith("using ")) { usingLines.Add(l); i++; }
                else break;
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
                return normalized.Substring(projectRoot.Length + 1);
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

        private static bool AddAutoByPriority(GameObject go, string fieldName, string path,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
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
                fields.Add((t.FullName, fieldName, path, true, 0));
                return true;
            }
            return false;
        }

        private static void AddFallbackRectOrGO(Transform tr, string fieldName, string path,
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields, bool preferRect)
        {
            if (preferRect && tr.GetComponent<RectTransform>() != null)
                fields.Add((typeof(RectTransform).FullName, fieldName, path, false, 0));
            else
                fields.Add((typeof(GameObject).FullName, fieldName, path, false, 0));
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

        private static string GetPrefixForTypeFullName(string typeFullName, AutoUICodeGenSettings settings)
        {
            if (settings != null && settings.componentPrefixes != null)
            {
                for (int i = 0; i < settings.componentPrefixes.Count; i++)
                {
                    var item = settings.componentPrefixes[i];
                    if (!string.IsNullOrEmpty(item.typeFullName) && string.Equals(item.typeFullName, typeFullName, StringComparison.Ordinal))
                        return item.prefix ?? string.Empty;
                }
            }
            return string.Empty;
        }

        private static string StripKnownPrefixes(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var lower = name.ToLowerInvariant();

            var settings = AutoUICodeGenSettings.Ensure();
            var configured = new List<string>();
            if (settings?.componentPrefixes != null)
            {
                foreach (var item in settings.componentPrefixes)
                {
                    if (string.IsNullOrWhiteSpace(item?.prefix)) continue;
                    var p = item.prefix.Trim().ToLowerInvariant();
                    if (p.Length == 0) continue;
                    configured.Add(p);
                }
            }
            if (configured.Count == 0)
                configured.AddRange(new[] { "btn", "tog", "sld", "input", "txt", "img", "rt", "go" });

            foreach (var p in configured.OrderByDescending(s => s.Length))
            {
                var withUnderscore = p + "_";
                if (lower.StartsWith(withUnderscore)) return name.Substring(withUnderscore.Length);
                if (lower.StartsWith(p)) return name.Substring(p.Length);
            }
            return name;
        }

        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var parts = name.Split(new[] { '_', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1));
            }
            return sb.Length > 0 ? sb.ToString() : name;
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            var pascal = ToPascalCase(name);
            if (string.IsNullOrEmpty(pascal)) return name;
            return char.ToLowerInvariant(pascal[0]) + (pascal.Length > 1 ? pascal.Substring(1) : string.Empty);
        }

        private static void EnsureUniqueFieldNames(
            List<(string typeFullName, string name, string path, bool isComponent, int compIndex)> fields)
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < fields.Count; i++)
            {
                var f = fields[i];
                var baseName = f.name;
                var name = baseName;
                int idx = 1;
                while (used.Contains(name)) name = baseName + "_" + idx++;
                used.Add(name);
                if (name != f.name)
                {
                    fields[i] = (f.typeFullName, name, f.path, f.isComponent, f.compIndex);
                }
            }
        }
    }
}