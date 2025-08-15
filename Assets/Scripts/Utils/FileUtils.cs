namespace GameObjectToolkit
{
    using UnityEngine;
    using System;
    using System.IO;
    using System.Text;
    using System.Security.Cryptography;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Xml.Serialization;

    /// <summary>
    /// 文件读写工具类
    /// 提供全面的文件操作功能
    /// </summary>
    public static class FileUtils
    {
        #region 基础路径

        /// <summary>
        /// 获取不同平台下的持久化数据路径
        /// </summary>
        public static string PersistentDataPath => Application.persistentDataPath;

        /// <summary>
        /// 获取不同平台下的流媒体路径
        /// </summary>
        public static string StreamingAssetsPath => Application.streamingAssetsPath;

        /// <summary>
        /// 获取临时缓存路径
        /// </summary>
        public static string TemporaryCachePath => Application.temporaryCachePath;

        #endregion

        #region 文本文件操作

        /// <summary>
        /// 同步读取文本文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="encoding">编码格式（默认UTF-8）</param>
        /// <returns>文件内容</returns>
        public static string ReadText(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                encoding = encoding ?? Encoding.UTF8;
                return File.ReadAllText(filePath, encoding);
            }
            catch (Exception e)
            {
                Debug.LogError($"读取文件失败: {filePath}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 异步读取文本文件
        /// </summary>
        public static async Task<string> ReadTextAsync(string filePath, Encoding encoding = null)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                encoding = encoding ?? Encoding.UTF8;
                using (var reader = new StreamReader(filePath, encoding))
                {
                    return await reader.ReadToEndAsync();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"异步读取文件失败: {filePath}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 同步写入文本文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">文件内容</param>
        /// <param name="encoding">编码格式（默认UTF-8）</param>
        /// <param name="overwrite">是否覆盖已有文件</param>
        public static bool WriteText(string filePath, string content, Encoding encoding = null, bool overwrite = true)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                File.WriteAllText(filePath, content, encoding);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"写入文件失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 异步写入文本文件
        /// </summary>
        public static async Task<bool> WriteTextAsync(string filePath, string content, Encoding encoding = null,
            bool overwrite = true)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                using (var writer = new StreamWriter(filePath, false, encoding))
                {
                    await writer.WriteAsync(content);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"异步写入文件失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 追加文本到文件
        /// </summary>
        public static bool AppendText(string filePath, string content, Encoding encoding = null)
        {
            try
            {
                encoding = encoding ?? Encoding.UTF8;
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(filePath, content, encoding);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"追加文件失败: {filePath}\n{e}");
                return false;
            }
        }

        #endregion

        #region 二进制文件操作

        /// <summary>
        /// 读取二进制文件
        /// </summary>
        public static byte[] ReadBytes(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"读取二进制文件失败: {filePath}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 异步读取二进制文件
        /// </summary>
        public static async Task<byte[]> ReadBytesAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[stream.Length];
                    await stream.ReadAsync(buffer, 0, (int)stream.Length);
                    return buffer;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"异步读取二进制文件失败: {filePath}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// 写入二进制文件
        /// </summary>
        public static bool WriteBytes(string filePath, byte[] data, bool overwrite = true)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                File.WriteAllBytes(filePath, data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"写入二进制文件失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 异步写入二进制文件
        /// </summary>
        public static async Task<bool> WriteBytesAsync(string filePath, byte[] data, bool overwrite = true)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"异步写入二进制文件失败: {filePath}\n{e}");
                return false;
            }
        }

        #endregion

        #region 对象序列化

        /// <summary>
        /// 二进制序列化对象到文件
        /// </summary>
        public static bool BinarySerialize<T>(string filePath, T data, bool overwrite = true)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, data);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"二进制序列化失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 从文件二进制反序列化对象
        /// </summary>
        public static T BinaryDeserialize<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return default;
            }

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    return (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"二进制反序列化失败: {filePath}\n{e}");
                return default;
            }
        }

        /// <summary>
        /// XML序列化对象到文件
        /// </summary>
        public static bool XmlSerialize<T>(string filePath, T data, bool overwrite = true)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                if (!overwrite && File.Exists(filePath))
                {
                    Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                    return false;
                }

                var serializer = new XmlSerializer(typeof(T));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, data);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"XML序列化失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 从文件XML反序列化对象
        /// </summary>
        public static T XmlDeserialize<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return default;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var reader = new StreamReader(filePath))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"XML反序列化失败: {filePath}\n{e}");
                return default;
            }
        }

        #endregion

        #region JSON操作

        /// <summary>
        /// 序列化对象为JSON并保存到文件
        /// </summary>
        public static bool SaveJson<T>(string filePath, T data, bool prettyPrint = false, bool overwrite = true)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint);
                return WriteText(filePath, json, Encoding.UTF8, overwrite);
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON序列化失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 从文件读取并反序列化JSON对象
        /// </summary>
        public static T LoadJson<T>(string filePath)
        {
            string json = ReadText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"JSON反序列化失败: {filePath}\n{e}");
                return default;
            }
        }

        /// <summary>
        /// 异步序列化对象为JSON并保存到文件
        /// </summary>
        public static async Task<bool> SaveJsonAsync<T>(string filePath, T data, bool prettyPrint = false,
            bool overwrite = true)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint);
                return await WriteTextAsync(filePath, json, Encoding.UTF8, overwrite);
            }
            catch (Exception e)
            {
                Debug.LogError($"异步JSON序列化失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 异步从文件读取并反序列化JSON对象
        /// </summary>
        public static async Task<T> LoadJsonAsync<T>(string filePath)
        {
            string json = await ReadTextAsync(filePath);
            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"异步JSON反序列化失败: {filePath}\n{e}");
                return default;
            }
        }

        #endregion

        #region 文件管理

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        public static bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        public static bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// 创建目录（包括所有子目录）
        /// </summary>
        public static bool CreateDirectory(string directoryPath)
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"创建目录失败: {directoryPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        public static bool DeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return false;
            }

            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"删除文件失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 删除目录（包括所有内容）
        /// </summary>
        public static bool DeleteDirectory(string directoryPath, bool recursive = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"目录不存在: {directoryPath}");
                return false;
            }

            try
            {
                Directory.Delete(directoryPath, recursive);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"删除目录失败: {directoryPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        public static bool CopyFile(string sourcePath, string destPath, bool overwrite = true)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"源文件不存在: {sourcePath}");
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourcePath, destPath, overwrite);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"复制文件失败: {sourcePath} -> {destPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        public static bool MoveFile(string sourcePath, string destPath, bool overwrite = true)
        {
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"源文件不存在: {sourcePath}");
                return false;
            }

            try
            {
                if (File.Exists(destPath))
                {
                    if (overwrite)
                    {
                        File.Delete(destPath);
                    }
                    else
                    {
                        Debug.LogWarning($"目标文件已存在且不允许覆盖: {destPath}");
                        return false;
                    }
                }

                string directory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Move(sourcePath, destPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"移动文件失败: {sourcePath} -> {destPath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 获取文件大小（字节）
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return -1;
            }

            try
            {
                return new FileInfo(filePath).Length;
            }
            catch (Exception e)
            {
                Debug.LogError($"获取文件大小失败: {filePath}\n{e}");
                return -1;
            }
        }

        /// <summary>
        /// 获取文件最后修改时间
        /// </summary>
        public static DateTime GetFileLastWriteTime(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return DateTime.MinValue;
            }

            try
            {
                return File.GetLastWriteTime(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"获取文件修改时间失败: {filePath}\n{e}");
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取目录下所有文件
        /// </summary>
        public static string[] GetFiles(string directoryPath, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (!Directory.Exists(directoryPath))
            {
                Debug.LogWarning($"目录不存在: {directoryPath}");
                return Array.Empty<string>();
            }

            try
            {
                return Directory.GetFiles(directoryPath, searchPattern, searchOption);
            }
            catch (Exception e)
            {
                Debug.LogError($"获取文件列表失败: {directoryPath}\n{e}");
                return Array.Empty<string>();
            }
        }

        #endregion

        #region 加密解密

        /// <summary>
        /// 计算文件的MD5哈希值
        /// </summary>
        public static string CalculateMD5(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"文件不存在: {filePath}");
                return null;
            }

            try
            {
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"计算MD5失败: {filePath}\n{e}");
                return null;
            }
        }

        /// <summary>
        /// AES加密文件
        /// </summary>
        public static bool EncryptFile(string inputFile, string outputFile, string password, bool overwrite = true)
        {
            if (!File.Exists(inputFile))
            {
                Debug.LogWarning($"输入文件不存在: {inputFile}");
                return false;
            }

            if (!overwrite && File.Exists(outputFile))
            {
                Debug.LogWarning($"输出文件已存在且不允许覆盖: {outputFile}");
                return false;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, aes.IV);
                    aes.Key = key.GetBytes(aes.KeySize / 8);

                    using (var inputStream = File.OpenRead(inputFile))
                    using (var outputStream = File.Create(outputFile))
                    {
                        // 写入IV
                        outputStream.Write(aes.IV, 0, aes.IV.Length);

                        using (var cryptoStream = new CryptoStream(
                                   outputStream,
                                   aes.CreateEncryptor(),
                                   CryptoStreamMode.Write))
                        {
                            inputStream.CopyTo(cryptoStream);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"文件加密失败: {inputFile} -> {outputFile}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// AES解密文件
        /// </summary>
        public static bool DecryptFile(string inputFile, string outputFile, string password, bool overwrite = true)
        {
            if (!File.Exists(inputFile))
            {
                Debug.LogWarning($"输入文件不存在: {inputFile}");
                return false;
            }

            if (!overwrite && File.Exists(outputFile))
            {
                Debug.LogWarning($"输出文件已存在且不允许覆盖: {outputFile}");
                return false;
            }

            try
            {
                using (var aes = Aes.Create())
                {
                    var key = new Rfc2898DeriveBytes(password, new byte[16]); // 临时IV，实际从文件读取

                    using (var inputStream = File.OpenRead(inputFile))
                    {
                        // 读取IV
                        byte[] iv = new byte[aes.IV.Length];
                        inputStream.Read(iv, 0, iv.Length);

                        aes.IV = iv;
                        aes.Key = key.GetBytes(aes.KeySize / 8);

                        using (var outputStream = File.Create(outputFile))
                        using (var cryptoStream = new CryptoStream(
                                   inputStream,
                                   aes.CreateDecryptor(),
                                   CryptoStreamMode.Read))
                        {
                            cryptoStream.CopyTo(outputStream);
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"文件解密失败: {inputFile} -> {outputFile}\n{e}");
                return false;
            }
        }

        #endregion

        #region Unity特殊支持

        /// <summary>
        /// 从StreamingAssets读取文本（支持所有平台）
        /// </summary>
        public static IEnumerator ReadTextFromStreamingAssets(string relativePath, Action<string> callback)
        {
            string filePath = Path.Combine(StreamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android平台需要使用WWW加载StreamingAssets
        using (var www = new UnityEngine.WWW(filePath))
        {
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError($"从StreamingAssets读取失败: {filePath}\n{www.error}");
                callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(www.text);
            }
        }
#else
            // 其他平台可以直接读取
            callback?.Invoke(ReadText(filePath));
            yield return null;
#endif
        }

        /// <summary>
        /// 从StreamingAssets读取二进制数据（支持所有平台）
        /// </summary>
        public static IEnumerator ReadBytesFromStreamingAssets(string relativePath, Action<byte[]> callback)
        {
            string filePath = Path.Combine(StreamingAssetsPath, relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android平台需要使用WWW加载StreamingAssets
        using (var www = new UnityEngine.WWW(filePath))
        {
            yield return www;
            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.LogError($"从StreamingAssets读取失败: {filePath}\n{www.error}");
                callback?.Invoke(null);
            }
            else
            {
                callback?.Invoke(www.bytes);
            }
        }
#else
            // 其他平台可以直接读取
            callback?.Invoke(ReadBytes(filePath));
            yield return null;
#endif
        }

        /// <summary>
        /// 保存Texture2D为图片文件
        /// </summary>
        public static bool SaveTextureAsPNG(Texture2D texture, string filePath, bool overwrite = true)
        {
            if (texture == null)
            {
                Debug.LogWarning("Texture2D为空");
                return false;
            }

            if (!overwrite && File.Exists(filePath))
            {
                Debug.LogWarning($"文件已存在且不允许覆盖: {filePath}");
                return false;
            }

            try
            {
                byte[] pngData = texture.EncodeToPNG();
                return WriteBytes(filePath, pngData);
            }
            catch (Exception e)
            {
                Debug.LogError($"保存Texture2D失败: {filePath}\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 从文件加载Texture2D
        /// </summary>
        public static Texture2D LoadTextureFromFile(string filePath)
        {
            byte[] fileData = ReadBytes(filePath);
            if (fileData == null || fileData.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData))
            {
                return texture;
            }

            return null;
        }

        #endregion
    }
}