using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 流式资源加载策略接口
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
public interface IStreamingAssetsLoaderStrategy<T> : IDisposable
{
    /// <summary>
    /// 异步加载资源
    /// </summary>
    Task<T> LoadAsync(string fullPath, 
        Action<T> onSuccess, 
        Action<string> onError = null,
        Action<float> progressCallback = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// 释放资源
    /// </summary>
    void Release(T resource);
}