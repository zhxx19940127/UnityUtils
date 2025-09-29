using UnityEngine;

namespace UnityUtils.EditorTools.AutoUI
{
    /// <summary>
    /// 代码生成器配置（Project 范围）
    /// </summary>
    public class AutoUICodeGenSettings : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GeneratedUITool/Editor/AutoUICodeGenSettings.asset";

        [Header("默认目录")]
        [Tooltip("UI 预制体所在目录（可多级）")]
        public string prefabFolder = "Assets/Resources/UI";

        [Tooltip("脚本输出目录（建议放在非 Editor 下）")]
        public string scriptOutputFolder = "Assets/Scripts/GeneratedUI";

        [Header("生成选项")]
        [Tooltip("为未标记的 Button/Toggle/Slider/InputField/TMP 相关组件自动生成字段并在 Awake/Start 中查找")]
        public bool autoIncludeCommonControls = true;

        [Tooltip("在 Awake 中生成查找代码（否则在 Start）")]
        public bool assignInAwake = true;

        [Tooltip("类名是否强制以大写字母开头")]
        public bool requireUppercaseClassName = true;

        public static AutoUICodeGenSettings Ensure()
        {
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<AutoUICodeGenSettings>(DefaultAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<AutoUICodeGenSettings>();
                var dir = System.IO.Path.GetDirectoryName(DefaultAssetPath);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }
                UnityEditor.AssetDatabase.CreateAsset(settings, DefaultAssetPath);
                UnityEditor.AssetDatabase.SaveAssets();
            }
            return settings;
        }
    }
}
