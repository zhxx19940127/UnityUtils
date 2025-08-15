namespace GameObjectToolkit
{
    using UnityEngine;
    using System.IO;
    using System.Collections;

    /// <summary>
    /// Unity 图像处理工具类
    /// 提供图像加载、保存、转换、截图等功能
    /// </summary>
    public static class ImageUtils
    {
        #region 图像加载与保存

        /// <summary>
        /// 从文件加载Texture2D（支持JPG/PNG）
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <param name="mipmap">是否生成mipmap</param>
        /// <param name="linear">是否为线性颜色空间</param>
        /// <returns>加载的Texture2D对象</returns>
        public static Texture2D LoadTextureFromFile(string filePath, bool mipmap = false, bool linear = false)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"ImageUtils: 文件不存在 - {filePath}");
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, mipmap, linear);

                if (tex.LoadImage(fileData)) // 自动识别JPG/PNG
                {
                    return tex;
                }

                Debug.LogError($"ImageUtils: 不支持的图像格式 - {filePath}");
                return null;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageUtils: 加载纹理失败 - {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存Texture2D为PNG文件
        /// </summary>
        /// <param name="texture">要保存的纹理</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="quality">PNG质量(1-100)</param>
        public static bool SaveTextureAsPNG(Texture2D texture, string filePath, int quality = 100)
        {
            if (texture == null)
            {
                Debug.LogError("ImageUtils: 纹理为空");
                return false;
            }

            try
            {
                byte[] pngData = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngData);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageUtils: 保存PNG失败 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存Texture2D为JPG文件
        /// </summary>
        /// <param name="texture">要保存的纹理</param>
        /// <param name="filePath">保存路径</param>
        /// <param name="quality">JPG质量(1-100)</param>
        public static bool SaveTextureAsJPG(Texture2D texture, string filePath, int quality = 75)
        {
            if (texture == null)
            {
                Debug.LogError("ImageUtils: 纹理为空");
                return false;
            }

            try
            {
                byte[] jpgData = texture.EncodeToJPG(quality);
                File.WriteAllBytes(filePath, jpgData);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageUtils: 保存JPG失败 - {e.Message}");
                return false;
            }
        }

        #endregion

        #region 图像转换

        /// <summary>
        /// 将Texture2D转换为Sprite
        /// </summary>
        /// <param name="texture">源纹理</param>
        /// <param name="pivot">中心点(默认0.5,0.5)</param>
        /// <param name="pixelsPerUnit">每单位像素数</param>
        public static Sprite TextureToSprite(Texture2D texture, Vector2 pivot = default, float pixelsPerUnit = 100.0f)
        {
            if (texture == null)
            {
                Debug.LogError("ImageUtils: 纹理为空");
                return null;
            }

            if (pivot == default)
            {
                pivot = new Vector2(0.5f, 0.5f);
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit);
        }

        /// <summary>
        /// 将RenderTexture转换为Texture2D
        /// </summary>
        /// <param name="renderTexture">源RenderTexture</param>
        /// <param name="textureFormat">目标格式</param>
        public static Texture2D RenderTextureToTexture2D(RenderTexture renderTexture,
            TextureFormat textureFormat = TextureFormat.RGBA32)
        {
            if (renderTexture == null)
            {
                Debug.LogError("ImageUtils: RenderTexture为空");
                return null;
            }

            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, textureFormat, false);
            try
            {
                RenderTexture.active = renderTexture;
                tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                return tex;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ImageUtils: 转换失败 - {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// 调整纹理大小
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="targetWidth">目标宽度</param>
        /// <param name="targetHeight">目标高度</param>
        /// <param name="filterMode">过滤模式</param>
        public static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight,
            FilterMode filterMode = FilterMode.Bilinear)
        {
            if (source == null)
            {
                Debug.LogError("ImageUtils: 源纹理为空");
                return null;
            }

            // 创建临时RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = filterMode;
            RenderTexture.active = rt;

            // 缩放绘制
            Graphics.Blit(source, rt);

            // 转换回Texture2D
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            // 清理
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        #endregion

        #region 屏幕截图

        /// <summary>
        /// 捕获全屏截图（协程方式）
        /// </summary>
        /// <param name="callback">截图完成回调</param>
        /// <param name="filename">保存文件名（可选）</param>
        public static IEnumerator CaptureScreenshotCoroutine(System.Action<Texture2D> callback, string filename = null)
        {
            // 等待帧渲染完成
            yield return new WaitForEndOfFrame();

            Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            screenshot.Apply();

            callback?.Invoke(screenshot);

            if (!string.IsNullOrEmpty(filename))
            {
                string path = Path.Combine(Application.persistentDataPath, filename);
                SaveTextureAsPNG(screenshot, path);
                Debug.Log($"截图已保存到: {path}");
            }
        }

        /// <summary>
        /// 捕获指定摄像机的渲染画面
        /// </summary>
        /// <param name="camera">目标摄像机</param>
        /// <param name="width">截图宽度</param>
        /// <param name="height">截图高度</param>
        public static Texture2D CaptureCameraRender(Camera camera, int width, int height)
        {
            if (camera == null)
            {
                Debug.LogError("ImageUtils: 摄像机为空");
                return null;
            }

            RenderTexture rt = new RenderTexture(width, height, 24);
            camera.targetTexture = rt;
            camera.Render();

            Texture2D result = RenderTextureToTexture2D(rt);
            camera.targetTexture = null;
            RenderTexture.DestroyImmediate(rt);

            return result;
        }

        #endregion

        #region 图像处理

        /// <summary>
        /// 裁剪纹理
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="x">起始X坐标</param>
        /// <param name="y">起始Y坐标</param>
        /// <param name="width">裁剪宽度</param>
        /// <param name="height">裁剪高度</param>
        public static Texture2D CropTexture(Texture2D source, int x, int y, int width, int height)
        {
            if (source == null)
            {
                Debug.LogError("ImageUtils: 源纹理为空");
                return null;
            }

            if (x < 0 || y < 0 || width <= 0 || height <= 0 ||
                x + width > source.width || y + height > source.height)
            {
                Debug.LogError("ImageUtils: 裁剪参数无效");
                return null;
            }

            Color[] pixels = source.GetPixels(x, y, width, height);
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 改变纹理透明度
        /// </summary>
        /// <param name="source">源纹理</param>
        /// <param name="alpha">目标透明度(0-1)</param>
        public static Texture2D ChangeTextureAlpha(Texture2D source, float alpha)
        {
            if (source == null)
            {
                Debug.LogError("ImageUtils: 源纹理为空");
                return null;
            }

            Color[] pixels = source.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = alpha;
            }

            Texture2D result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true);
            result.SetPixels(pixels);
            result.Apply();
            return result;
        }

        /// <summary>
        /// 创建纯色纹理
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <param name="color">颜色</param>
        public static Texture2D CreateColorTexture(int width, int height, Color color)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion

        #region 图像分析

        /// <summary>
        /// 计算纹理的平均颜色
        /// </summary>
        public static Color CalculateAverageColor(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("ImageUtils: 纹理为空");
                return Color.clear;
            }

            Color[] pixels = texture.GetPixels();
            Color sum = Color.black;
            foreach (Color pixel in pixels)
            {
                sum += pixel;
            }

            return sum / pixels.Length;
        }

        /// <summary>
        /// 检查纹理是否为透明纹理
        /// </summary>
        public static bool IsTransparentTexture(Texture2D texture)
        {
            if (texture == null)
            {
                Debug.LogError("ImageUtils: 纹理为空");
                return false;
            }

            Color[] pixels = texture.GetPixels();
            foreach (Color pixel in pixels)
            {
                if (pixel.a < 1.0f)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}