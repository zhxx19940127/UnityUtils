using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace DataSerialization
{
    /// <summary>
    /// 二进制序列化策略
    /// 使用可序列化类型后，支持直接二进制序列化，无需回退机制
    /// </summary>
    public class BinarySerializationStrategy : ISerializationStrategy
    {
        public string[] SupportedExtensions => new[] { ".dat", ".bin", ".data" };
        public string FormatName => "Binary";
        public bool SupportsCompression => true;

        public string Serialize(object obj)
        {
            byte[] data = SerializeToBytes(obj);
            return data != null ? Convert.ToBase64String(data) : string.Empty;
        }

        public byte[] SerializeToBytes(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[BinarySerializationStrategy] 序列化对象为 null");
                return null;
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, obj);
                    return stream.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializationStrategy] 序列化失败: {ex.Message}");
                return null;
            }
        }

        public T Deserialize<T>(string data) where T : new()
        {
            if (string.IsNullOrEmpty(data)) return default;
            try
            {
                byte[] bytes = Convert.FromBase64String(data);
                return DeserializeFromBytes<T>(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializationStrategy] Base64 反序列化失败: {ex.Message}");
                return default;
            }
        }

        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning("[BinarySerializationStrategy] 反序列化数据为空");
                return default;
            }

            try
            {
                using (var stream = new MemoryStream(data))
                {
                    var formatter = new BinaryFormatter();
                    return (T)formatter.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializationStrategy] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        public string SerializeList<T>(IEnumerable<T> list)
        {
            return Serialize(list);
        }

        public List<T> DeserializeList<T>(string data) where T : new()
        {
            return Deserialize<List<T>>(data) ?? new List<T>();
        }

        public bool SaveToFile(object obj, string filePath)
        {
            if (obj == null)
            {
                Debug.LogWarning("[BinarySerializationStrategy] 保存对象为 null");
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[BinarySerializationStrategy] 文件路径为空");
                return false;
            }

            try
            {
                // 确保目录存在
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                byte[] data = SerializeToBytes(obj);
                if (data == null) return false;

                File.WriteAllBytes(filePath, data);
                Debug.Log($"[BinarySerializationStrategy] 二进制文件保存成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializationStrategy] 保存二进制文件失败: {ex.Message}");
                return false;
            }
        }

        public T LoadFromFile<T>(string filePath) where T : new()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[BinarySerializationStrategy] 文件路径为空");
                return default;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[BinarySerializationStrategy] 文件不存在: {filePath}");
                return default;
            }

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                return DeserializeFromBytes<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BinarySerializationStrategy] 加载二进制文件失败: {ex.Message}");
                return default;
            }
        }
    }
}