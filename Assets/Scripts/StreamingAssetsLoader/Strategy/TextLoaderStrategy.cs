using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 文本加载策略
/// </summary>
public class TextLoaderStrategy : IStreamingAssetsLoaderStrategy<string>
{
    /// <summary>
    /// 异步加载文本
    /// </summary>
    public async Task<string> LoadAsync(string fullPath,
        Action<string> onSuccess,
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

            // 移除可能的BOM头
            string jsonContent = RemoveBOM(Encoding.UTF8.GetString(data));

            // 将二进制数据转换为UTF-8字符串
            var content = System.Text.Encoding.UTF8.GetString(data);
            // 调用成功回调
            onSuccess?.Invoke(content);
            return content;
        }
        catch (Exception e)
        {
            // 调用错误回调
            onError?.Invoke($"文本加载失败: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// 移除BOM头
    /// </summary>
    private string RemoveBOM(string content)
    {
        // UTF-8 BOM: EF BB BF
        if (content.StartsWith("\uFEFF"))
        {
            return content.Substring(1);
        }

        return content;
    }

    /// <summary>
    /// 释放文本资源 - 文本无需特殊释放
    /// </summary>
    public void Release(string resource)
    {
        /* 文本无需特殊释放 */
    }

    /// <summary>
    /// 释放策略资源 - 无需特殊处理
    /// </summary>
    public void Dispose()
    {
        /* 无需要清理的资源 */
    }
}