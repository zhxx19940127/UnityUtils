using UnityEngine;
using System;
using System.Collections.Generic;

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

    [Tooltip("为每个私有字段生成同名 PascalCase 只读属性")]
    public bool generateReadOnlyProperties = false;

    [Tooltip("将字段命名为 _camelCase（例如 btn_ok -> _btnOk）")]
    public bool privateFieldUnderscoreCamelCase = true;

    [Tooltip("字段使用组件前缀（Button->btn, Toggle->tog, Slider->sld, InputField/TMP_InputField->input, Text/TMP_Text->txt, Image/RawImage->img, RectTransform->rt, GameObject->go）")]
    public bool useComponentPrefixForFields = true;

    [Tooltip("属性名移除组件前缀（例如 _btnOk 的属性名为 Ok 或 BtnOk；建议开启）")]
    public bool stripPrefixInPropertyNames = true;

    [Tooltip("生成后自动将脚本组件添加到对应预制体的根节点上")]
    public bool autoAddScriptToPrefab = false;

        [Serializable]
        public class TypePrefix
        {
            public string typeFullName;
            public string prefix;
        }

        [Header("组件前缀映射（可编辑）")]
        public List<TypePrefix> componentPrefixes = new List<TypePrefix>();

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
                settings.FillDefaultPrefixes();
            }
            if (settings.componentPrefixes == null || settings.componentPrefixes.Count == 0)
            {
                settings.FillDefaultPrefixes();
            }
            return settings;
        }

        public void FillDefaultPrefixes()
        {
            componentPrefixes = new List<TypePrefix>
            {
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.Button).FullName, prefix = "btn" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.Toggle).FullName, prefix = "tog" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.Slider).FullName, prefix = "sld" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.InputField).FullName, prefix = "input" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.Text).FullName, prefix = "txt" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.Image).FullName, prefix = "img" },
                new TypePrefix{ typeFullName = typeof(UnityEngine.UI.RawImage).FullName, prefix = "img" },
                new TypePrefix{ typeFullName = "TMPro.TMP_InputField", prefix = "input" },
                new TypePrefix{ typeFullName = "TMPro.TMP_Text", prefix = "txt" },
                new TypePrefix{ typeFullName = typeof(RectTransform).FullName, prefix = "rt" },
                new TypePrefix{ typeFullName = typeof(GameObject).FullName, prefix = "go" },
            };
        }
    }
}
