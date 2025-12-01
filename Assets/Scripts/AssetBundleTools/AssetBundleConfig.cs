using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AssetBundle配置类
/// 用于管理所有AssetBundle的名称和映射关系
/// </summary>
public class AssetBundleConfig : MonoBehaviour
{
    // 配置文件路径
    public const string CONFIG_FILE_NAME = "AssetBundleConfig.json";

    // AssetBundle基础路径
    public static string AssetBundleBasePath
    {
        get
        {
            string streamingPath = Application.streamingAssetsPath;
            string basePath;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: 确保路径正确拼接（处理尾部斜杠）
            streamingPath = streamingPath.TrimEnd('/');
            basePath = streamingPath + "/AssetBundles";
#else
            // 其他平台：使用 Path.Combine
            basePath = System.IO.Path.Combine(streamingPath, "AssetBundles");
#endif

            return basePath;
        }
    }

    // 获取特定AssetBundle的完整路径
    public static string GetAssetBundlePath(string bundleName)
    {
        return System.IO.Path.Combine(AssetBundleBasePath, bundleName);
    }

    // 获取配置文件路径
    public static string GetConfigPath()
    {
        return System.IO.Path.Combine(AssetBundleBasePath, CONFIG_FILE_NAME);
    }
}

/// <summary>
/// AssetBundle配置数据结构
/// </summary>
[System.Serializable]
public class AssetBundleConfigData
{
    public AssetBundleItemInfo[] bundles;
}

[System.Serializable]
public class AssetBundleItemInfo
{
    public string bundleName; // AB包名称
    public string assetName; // 资源名称
    public string assetPath; // 资源路径
}