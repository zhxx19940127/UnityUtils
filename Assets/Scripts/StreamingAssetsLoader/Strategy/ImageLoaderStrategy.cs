using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 图片加载策略
/// </summary>
public class ImageLoaderStrategy : IStreamingAssetsLoaderStrategy<Texture2D>
{
    public bool generateMipmaps = true; // 是否生成Mipmaps
    public TextureFormat format = TextureFormat.RGBA32; // 纹理格式

    public ImageLoaderStrategy()
    {
    }

    public ImageLoaderStrategy(bool generateMipmaps, TextureFormat format)
    {
        this.generateMipmaps = generateMipmaps;
        this.format = format;
    }

    /// <summary>
    /// 异步加载图片
    /// </summary>
    public async Task<Texture2D> LoadAsync(string fullPath,
        Action<Texture2D> onSuccess,
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

            // 创建新纹理
            var texture = new Texture2D(2, 2, format, generateMipmaps);
            texture.name = Path.GetFileName(fullPath); // 设置纹理名称

            // 加载图片数据
            if (texture.LoadImage(data))
            {
                // 调用成功回调
                onSuccess?.Invoke(texture);
                return texture;
            }

            // 加载失败时销毁纹理
            UnityEngine.Object.Destroy(texture);
            throw new Exception("无效的图片数据");
        }
        catch (Exception e)
        {
            // 调用错误回调
            onError?.Invoke($"图片加载失败: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 释放纹理资源
    /// </summary>
    public void Release(Texture2D texture)
    {
        if (texture != null)
            UnityEngine.Object.Destroy(texture);
    }

    /// <summary>
    /// 释放策略资源 - 无需特殊处理
    /// </summary>
    public void Dispose()
    {
        /* 无需要清理的资源 */
    }
}