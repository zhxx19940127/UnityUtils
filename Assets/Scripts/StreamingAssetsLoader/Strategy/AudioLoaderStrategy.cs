using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 音频加载策略
/// </summary>
public class AudioLoaderStrategy : IStreamingAssetsLoaderStrategy<AudioClip>
{
    private AudioType audioType = AudioType.UNKNOWN; // 音频类型
    private UnityWebRequest _currentRequest; // 当前请求
    
    /// <summary>
    /// 异步加载音频
    /// </summary>
    public async Task<AudioClip> LoadAsync(string fullPath, 
        Action<AudioClip> onSuccess, 
        Action<string> onError = null,
        Action<float> progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 自动检测音频类型
            if (audioType == AudioType.UNKNOWN)
                audioType = DetectAudioType(fullPath);

            // 创建音频请求
            _currentRequest = UnityWebRequestMultimedia.GetAudioClip(fullPath, audioType);
            
            // WebGL平台特殊处理：启用MP3压缩
            #if UNITY_WEBGL && !UNITY_EDITOR
            if (audioType == AudioType.MPEG)
                DownloadHandlerAudioClip.compressed = true;
            #endif

            // 发送请求
            var operation = _currentRequest.SendWebRequest();

            // 等待加载完成，同时检查取消请求
            while (!operation.isDone && !cancellationToken.IsCancellationRequested)
            {
                // 更新进度
                progressCallback?.Invoke(_currentRequest.downloadProgress);
                // 等待下一帧
                await Task.Yield();
            }

            // 如果请求被取消，抛出异常
            cancellationToken.ThrowIfCancellationRequested();

            // 检查请求结果
            if (_currentRequest.result != UnityWebRequest.Result.Success)
                throw new Exception($"音频加载失败: {_currentRequest.error}");

            // 获取音频剪辑
            var clip = DownloadHandlerAudioClip.GetContent(_currentRequest);
            // 调用成功回调
            onSuccess?.Invoke(clip);
            return clip;
        }
        finally
        {
            // 确保释放请求资源
            _currentRequest?.Dispose();
            _currentRequest = null;
        }
    }
    
    /// <summary>
    /// 根据文件扩展名检测音频类型
    /// </summary>
    private AudioType DetectAudioType(string path)
    {
        // 获取文件扩展名并转换为小写
        string ext = Path.GetExtension(path).ToLower();
        // 根据扩展名返回对应的音频类型
        return ext switch {
            ".wav"  => AudioType.WAV,      // WAV格式
            ".mp3"  => AudioType.MPEG,     // MP3格式
            ".ogg"  => AudioType.OGGVORBIS,// OGG格式
            ".aiff" => AudioType.AIFF,     // AIFF格式
            _ => AudioType.WAV // 默认使用WAV格式
        };
    }
    
    /// <summary>
    /// 释放音频资源
    /// </summary>
    public void Release(AudioClip clip)
    {
        if (clip != null)
            UnityEngine.Object.Destroy(clip);
    }
    
    /// <summary>
    /// 释放策略资源 - 清理当前请求
    /// </summary>
    public void Dispose()
    {
        _currentRequest?.Dispose();
        _currentRequest = null;
    }
}