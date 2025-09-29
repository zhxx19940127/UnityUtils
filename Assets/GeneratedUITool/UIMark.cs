using UnityEngine;

namespace UnityUtils.UI
{
    /// <summary>
    /// 标记类：用于辅助 UI 代码生成。
    /// - fieldName: 生成字段名（不填则使用对象名转为合法标识符）
    /// - ignoreChildren: 代码生成时忽略该物体的所有子物体（不再向下遍历）
    /// - targetKind: 导出目标（自动/指定组件/RectTransform/GameObject）
    /// - componentTypeFullName: 当 targetKind=Component 时，指定组件的完整类型名
    /// </summary>
    public class UIMark : MonoBehaviour
    {
        public enum ExportTargetKind
        {
            Auto,
            Component,
            RectTransform,
            GameObject,
        }

        [Tooltip("生成字段名（留空则使用对象名）")] public string fieldName;

        [Tooltip("忽略该物体的所有子物体（终止向下扫描）")] public bool ignoreChildren = false;

        [Header("导出目标")] public ExportTargetKind targetKind = ExportTargetKind.Auto;

        [Tooltip("当目标为 Component 时，使用该类型（完整限定名），如 UnityEngine.UI.Button 或 TMPro.TMP_Text")]
        public string componentTypeFullName;

        private void Reset()
        {
            // 添加组件时，自动设置字段名和推荐组件类型
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                fieldName = SanitizeToIdentifier(gameObject.name);
            }

            // 推荐一个组件类型（优先级：交互 > 文本/图像 > RectTransform > GameObject）
            var (kind, typeFullName) = RecommendTarget(this);
            targetKind = kind;
            componentTypeFullName = typeFullName;
        }

        private static (ExportTargetKind, string) RecommendTarget(UIMark mark)
        {
            var go = mark != null ? mark.gameObject : null;
            if (go == null) return (ExportTargetKind.GameObject, null);

            // 交互优先
            var t = FindFirstTypeOn(go,
                "UnityEngine.UI.Button",
                "UnityEngine.UI.Toggle",
                "UnityEngine.UI.Slider",
                "UnityEngine.UI.InputField",
                "TMPro.TMP_InputField");
            if (t != null) return (ExportTargetKind.Component, t.FullName);

            // 文本/图像中优先
            t = FindFirstTypeOn(go,
                "TMPro.TMP_Text",
                "UnityEngine.UI.Text",
                "UnityEngine.UI.Image",
                "UnityEngine.UI.RawImage");
            if (t != null) return (ExportTargetKind.Component, t.FullName);

            // 其次 RectTransform
            if (go.GetComponent<RectTransform>() != null)
                return (ExportTargetKind.RectTransform, typeof(RectTransform).FullName);

            // 最后 GameObject
            return (ExportTargetKind.GameObject, null);
        }

        private static System.Type FindFirstTypeOn(GameObject go, params string[] candidateFullNames)
        {
            foreach (var fullName in candidateFullNames)
            {
                var t = FindType(fullName);
                if (t != null && go.GetComponent(t) != null) return t;
            }

            return null;
        }

        private static System.Type FindType(string fullName)
        {
            var t = System.Type.GetType(fullName);
            if (t != null) return t;
            var asms = System.AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                try
                {
                    t = asms[i].GetType(fullName);
                    if (t != null) return t;
                }
                catch
                {
                }
            }

            return null;
        }

        private static string SanitizeToIdentifier(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "field";
            var sb = new System.Text.StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                var ch = raw[i];
                sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
            }

            if (char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }
    }
}