using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// 二进制加载策略
/// </summary>
public class BinaryLoaderStrategy : IStreamingAssetsLoaderStrategy<byte[]>
{
    /// <summary>
    /// 异步加载二进制数据
    /// </summary>
    public async Task<byte[]> LoadAsync(string fullPath, 
        Action<byte[]> onSuccess, 
        Action<string> onError = null,
        Action<float> progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 加载二进制数据
            byte[] data = await WebRequestLoader.LoadBinaryAsync(
                fullPath, 
                progressCallback, 
                cancellationToken
            );
            
            // 调用成功回调
            onSuccess?.Invoke(data);
            return data;
        }
        catch (Exception e)
        {
            // 调用错误回调
            onError?.Invoke($"二进制加载失败: {e.Message}");
            throw;
        }
    }
    
    /// <summary>
    /// 释放二进制资源 - 字节数组由GC管理
    /// </summary>
    public void Release(byte[] resource) { /* 字节数组由GC管理 */ }
    
    /// <summary>
    /// 释放策略资源 - 无需特殊处理
    /// </summary>
    public void Dispose() { /* 无需要清理的资源 */ }
}