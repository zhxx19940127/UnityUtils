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
    }
}