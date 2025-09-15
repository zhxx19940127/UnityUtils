using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// Web请求加载器 - 提供通用的二进制数据加载功能
/// </summary>
public class WebRequestLoader
{
    /// <summary>
    /// 异步加载二进制数据
    /// </summary>
    public static async Task<byte[]> LoadBinaryAsync(string path, 
        Action<float> progress = null,
        CancellationToken cancellationToken = default,
        int timeout = 30)
    {
        // 创建UnityWebRequest对象
        using var www = UnityWebRequest.Get(path);
        www.timeout = timeout; // 设置超时时间(秒)
        www.downloadHandler = new DownloadHandlerBuffer(); // 使用缓冲下载处理器
        
        // 发送请求
        var operation = www.SendWebRequest();

        // 等待加载完成，同时检查取消请求
        while (!operation.isDone && !cancellationToken.IsCancellationRequested)
        {
            // 更新进度
            progress?.Invoke(www.downloadProgress);
            // 等待下一帧
            await Task.Yield();
        }

        // 如果请求被取消，抛出异常
        cancellationToken.ThrowIfCancellationRequested();

        // 检查请求结果
        if (www.result != UnityWebRequest.Result.Success)
            throw new Exception($"[{www.responseCode}] {www.error}");

        // 返回下载的数据
        return www.downloadHandler.data;
    }
}
