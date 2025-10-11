using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using LitJson;

namespace DataSerialization
{
    /// <summary>
    /// JSON 序列化策略
    /// </summary>
    public class JsonSerializationStrategy : ISerializationStrategy
    {
        public string[] SupportedExtensions => new[] { ".json", ".js" };
        public string FormatName => "JSON";
        public bool SupportsCompression => false;

        public string Serialize(object obj)
        {
            try
            {
                if (obj == null) return string.Empty;
                var writer = new JsonWriter();
                writer.PrettyPrint = true;
                JsonMapper.ToJson(obj, writer);
                return writer.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationStrategy] 序列化失败: {ex.Message}");
                return string.Empty;
            }
        }

        public byte[] SerializeToBytes(object obj)
        {
            string json = Serialize(obj);
            return string.IsNullOrEmpty(json) ? null : System.Text.Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(string data) where T : new()
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return default;
                return JsonMapper.ToObject<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationStrategy] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            if (data == null || data.Length == 0) return default;
            string json = System.Text.Encoding.UTF8.GetString(data);
            return Deserialize<T>(json);
        }

        public string SerializeList<T>(IEnumerable<T> list)
        {
            try
            {
                if (list == null) return string.Empty;
                var writer = new JsonWriter();
                writer.PrettyPrint = true;
                JsonMapper.ToJson(list, writer);
                return writer.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationStrategy] 序列化列表失败: {ex.Message}");
                return string.Empty;
            }
        }

        public List<T> DeserializeList<T>(string data) where T : new()
        {
            try
            {
                if (string.IsNullOrEmpty(data)) return new List<T>();
                return JsonMapper.ToObject<List<T>>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSerializationStrategy] 反序列化列表失败: {ex.Message}");
                return new List<T>();
            }
        }
    }
}