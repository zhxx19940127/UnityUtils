using System;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace UnityUtils.EditorTools.AutoUI
{
    /// <summary>
    /// 异步在脚本导入完成后，将生成的脚本组件自动挂到预制体根节点。
    /// 之所以异步：新脚本导入编译需要时间，等待编译完成后再反射类型。
    /// </summary>
    internal static class GeneratedScriptAttacher
    {
    private const string SessionKey = "AutoUI_AttachQueue";
    private const string LinePrefix = "M|"; // 标记为“手动挂载”来源，拦截历史格式

        /// <summary>
        /// 记录一个待挂载事件。参数：预制体GUID + 类名 + 脚本资产路径
        /// </summary>
        public static void EnqueueAttach(GameObject prefab, string className, string scriptAssetPath)
        {
            if (prefab == null || string.IsNullOrEmpty(className)) return;
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
            // 优先尝试立即挂载（如果类型已经可用，则不必等待脚本重载）
            if (!TryAttach(guid, className, scriptAssetPath))
            {
                // 类型暂不可用，入队等待 DidReloadScripts
                var item = LinePrefix + guid + "|" + className + "|" + (scriptAssetPath ?? string.Empty);
                var existed = SessionState.GetString(SessionKey, string.Empty);
                // 去重
                if (!string.IsNullOrEmpty(existed))
                {
                    var set = new System.Collections.Generic.HashSet<string>(existed.Split(new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries));
                    if (!set.Add(item)) return; // 已存在
                    // 稳定排序：按字符串比较
                    var arr = new System.Collections.Generic.List<string>(set);
                    arr.Sort(StringComparer.Ordinal);
                    SessionState.SetString(SessionKey, string.Join("\n", arr));
                }
                else
                {
                    SessionState.SetString(SessionKey, item);
                }
            }
            else
            {
                // 立即挂载成功，刷新窗口显示
                UnityUtils.EditorTools.AutoUI.AutoUICodeGeneratorWindow.RefreshAllOpenWindows();
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var payload = SessionState.GetString(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(payload)) return;
            SessionState.SetString(SessionKey, string.Empty);

            var lines = payload.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
            var toAssign = new System.Collections.Generic.List<(string guid, string className)>();
            foreach (var line in lines)
            {
                try
                {
                    if (!line.StartsWith(LinePrefix, StringComparison.Ordinal))
                    {
                        // 丢弃旧版本遗留的无前缀记录，避免“生成后自动挂载”的误解
                        continue;
                    }
                    var pure = line.Substring(LinePrefix.Length);
                    var parts = pure.Split('|');
                    if (parts.Length < 2) continue;
                    var guid = parts[0];
                    var className = parts[1];
                    var scriptAssetPath = parts.Length >= 3 ? parts[2] : string.Empty;
                    if (TryAttach(guid, className, scriptAssetPath))
                    {
                        toAssign.Add((guid, className));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AutoUI] 自动挂载脚本失败: {ex}");
                }
            }
            // 刷新窗口显示，避免用户需要再点一次
            UnityUtils.EditorTools.AutoUI.AutoUICodeGeneratorWindow.RefreshAllOpenWindows();

            // 如果当前是序列化模式，尝试为成功挂载的 Prefab 赋 SerializedReference
            var settings = UnityUtils.EditorTools.AutoUI.AutoUICodeGenSettings.Ensure();
            if (settings.initAssignMode == UnityUtils.EditorTools.AutoUI.AutoUICodeGenSettings.InitAssignMode.SerializedReferences)
            {
                foreach (var item in toAssign)
                {
                    var path = AssetDatabase.GUIDToAssetPath(item.guid);
                    var go = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
                    if (go == null) continue;
                    // 重新收集字段并赋值
                    var fields = UnityUtils.EditorTools.AutoUI.UICodeGenerator.CollectFields(go, settings);
                    var stats = UnityUtils.EditorTools.AutoUI.SerializedReferenceAssigner.Assign(go, item.className, fields);
                    Debug.Log($"[AutoUI] 序列化引用赋值：成功 {stats.success}/{stats.total}，缺路径 {stats.missingPath}，缺组件 {stats.missingComponent}");
                }
            }
        }

        private static bool TryAttach(string prefabGuid, string className, string scriptAssetPath)
        {
            var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (string.IsNullOrEmpty(prefabPath)) return false;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (go == null) return false;

            // 通过脚本资源更精确获取类型；若不可用则回退到按类名扫描
            Type type = null;
            if (!string.IsNullOrEmpty(scriptAssetPath))
            {
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptAssetPath);
                if (ms != null)
                {
                    try { type = ms.GetClass(); } catch { }
                }
            }
            if (type == null)
            {
                type = FindTypeByName(className);
            }
            if (type == null) return false; // 类型尚不可用，等下次 reload
            if (!typeof(MonoBehaviour).IsAssignableFrom(type)) return true; // 无需挂载

            // 如果已有该组件则不重复挂载
            if (go.GetComponent(type) != null) return true;

            var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
            if (prefab == null) return false;
            try
            {
                prefab.AddComponent(type);
                PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
                Debug.Log($"[AutoUI] 已为预制体自动添加脚本：{className} -> {prefabPath}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }
        }

        private static Type FindTypeByName(string className)
        {
            // 精确按类名查找（不含命名空间），若有多个匹配，取第一个
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = asm.GetTypes();
                    foreach (var t in types)
                    {
                        if (t.Name == className) return t;
                    }
                }
                catch {}
            }
            return null;
        }
    }
}
