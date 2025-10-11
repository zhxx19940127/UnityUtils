using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace DataSerialization
{
    /// <summary>
    /// XML 序列化策略
    /// </summary>
    public class XmlSerializationStrategy : ISerializationStrategy
    {
        /// <summary>
        /// 默认编码格式
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public string[] SupportedExtensions => new[] { ".xml" };
        public string FormatName => "XML";
        public bool SupportsCompression => false;

        public string Serialize(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[XmlSerializationStrategy] 序列化对象为 null");
                return string.Empty;
            }

            try
            {
                var serializer = new XmlSerializer(obj.GetType());
                using (var stringWriter = new StringWriter())
                {
                    using (var xmlWriter = XmlWriter.Create(stringWriter, new XmlWriterSettings
                           {
                               Encoding = DefaultEncoding,
                               Indent = true,
                               IndentChars = "  ",
                               NewLineChars = "\n",
                               NewLineHandling = NewLineHandling.Replace
                           }))
                    {
                        serializer.Serialize(xmlWriter, obj);
                        return stringWriter.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XmlSerializationStrategy] 序列化失败: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }

        public byte[] SerializeToBytes(object obj)
        {
            string xml = Serialize(obj);
            return string.IsNullOrEmpty(xml) ? null : System.Text.Encoding.UTF8.GetBytes(xml);
        }

        public T Deserialize<T>(string data) where T : new()
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[XmlSerializationStrategy] XML 字符串为空");
                return default;
            }

            try
            {
                var serializer = new XmlSerializer(typeof(T));
                using (var stringReader = new StringReader(data))
                {
                    return (T)serializer.Deserialize(stringReader);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XmlSerializationStrategy] 反序列化失败: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }

        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            if (data == null || data.Length == 0) return default;
            string xml = System.Text.Encoding.UTF8.GetString(data);
            return Deserialize<T>(xml);
        }

        public string SerializeList<T>(IEnumerable<T> list)
        {
            return Serialize(list);
        }

        public List<T> DeserializeList<T>(string data) where T : new()
        {
            return Deserialize<List<T>>(data);
        }
    }
}