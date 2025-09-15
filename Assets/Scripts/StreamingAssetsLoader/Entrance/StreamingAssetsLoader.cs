using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 流式资源加载器 - 提供跨平台的StreamingAssets资源加载功能
/// </summary>
public static class StreamingAssetsLoader
{
    // 加载策略字典 - 将类型映射到对应的加载策略实现
    private static readonly Dictionary<Type, object> Strategies = new Dictionary<Type, object>()
    {
        { typeof(string), new TextLoaderStrategy() }, // 文本加载策略
        { typeof(Texture2D), new ImageLoaderStrategy() }, // 图片加载策略
        { typeof(AudioClip), new AudioLoaderStrategy() }, // 音频加载策略
        { typeof(byte[]), new BinaryLoaderStrategy() } // 二进制加载策略
    };

    /// <summary>
    /// 注册自定义加载策略
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="strategy">加载策略实例</param>
    public static void RegisterStrategy<T>(IStreamingAssetsLoaderStrategy<T> strategy)
    {
        Strategies[typeof(T)] = strategy;
    }

    /// <summary>
    /// 异步加载资源----
    /// 需要取消则new CancellationTokenSource,然后传入
    /// CancellationTokenSource.Token 类型为 CancellationToken
    /// 取消加载调用 CancellationTokenSource.Cancel();
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="relativePath">相对路径</param>
    /// <param name="onSuccess">加载成功回调</param>
    /// <param name="onError">加载失败回调</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>加载任务</returns>
    public static async Task<T> LoadAsync<T>(string relativePath,
        Action<T> onSuccess,
        Action<string> onError = null,
        Action<float> progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取平台特定的完整路径
            var fullPath = GetPlatformPath(relativePath);
            Debug.LogError("完整路径: " + fullPath);
            // 查找对应的加载策略
            if (!Strategies.TryGetValue(typeof(T), out var strategy))
                throw new NotSupportedException($"不支持的类型: {typeof(T)}");

            // 调用策略的异步加载方法
            return await ((IStreamingAssetsLoaderStrategy<T>)strategy)
                .LoadAsync(fullPath, onSuccess, onError, progressCallback, cancellationToken);
        }
        catch (OperationCanceledException) // 处理取消操作
        {
            Debug.LogWarning($"加载已取消: {relativePath}");
            onError?.Invoke("操作已取消");
            return default;
        }
        catch (Exception e) // 处理其他异常
        {
            Debug.LogError($"加载失败: {relativePath}\n{e}");
            onError?.Invoke($"加载失败: {e.Message}");
            return default;
        }
    }

    /// <summary>
    /// 获取平台特定的完整路径
    /// </summary>
    /// <param name="relativePath">相对路径</param>
    /// <returns>完整路径</returns>
    static string GetPlatformPath(string relativePath)
    {
        // 组合基础路径和相对路径
        var path = Path.Combine(Application.streamingAssetsPath, relativePath);

        // 规范化路径分隔符
        path = path.Replace('\\', '/');

        // 移除可能存在的重复斜杠
        path = path.Replace(":////", ":///")
            .Replace(":///", "://")
            .Replace("//", "/");
        // 平台特定路径处理
#if UNITY_ANDROID
        // Android平台需要添加jar:前缀
        if (!path.StartsWith("jar:"))
            path = "jar:" + path;
#elif UNITY_IOS
        // iOS平台需要添加file://前缀
        if (!path.StartsWith("file://"))
            path = "file://" + path;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Windows平台需要添加file:///前缀
        if (!path.StartsWith("file://"))
            path = "file:///" + path;
#endif

        return path;
    }
}