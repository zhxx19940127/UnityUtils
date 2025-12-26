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
    /// SOAP XML 序列化策略
    /// </summary>
    public class SoapXmlSerializationStrategy : ISerializationStrategy
    {
        /// <summary>
        /// 默认编码格式
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// SOAP 命名空间
        /// </summary>
        private const string SoapEnvelopeNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
        private const string XsdNamespace = "http://www.w3.org/2001/XMLSchema";

        public string[] SupportedExtensions => new[] { ".xml" };
        public string FormatName => "SOAP-XML";
        public bool SupportsCompression => false;

        /// <summary>
        /// 序列化对象并包装为 SOAP 格式
        /// </summary>
        public string Serialize(object obj)
        {
            if (obj == null)
            {
                Debug.LogWarning("[SoapXmlSerializationStrategy] 序列化对象为 null");
                return string.Empty;
            }

            try
            {
                // 先序列化内部对象
                string innerXml = SerializeInner(obj);
                
                // 包装为 SOAP 格式
                return WrapInSoapEnvelope(innerXml);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoapXmlSerializationStrategy] 序列化失败: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 序列化为字节数组（包含 SOAP 包装）
        /// </summary>
        public byte[] SerializeToBytes(object obj)
        {
            string xml = Serialize(obj);
            return string.IsNullOrEmpty(xml) ? null : DefaultEncoding.GetBytes(xml);
        }

        /// <summary>
        /// 反序列化 SOAP 格式的 XML
        /// </summary>
        public T Deserialize<T>(string data) where T : new()
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[SoapXmlSerializationStrategy] XML 字符串为空");
                return default;
            }

            try
            {
                // 尝试从 SOAP 信封中提取内容
                string innerXml = ExtractFromSoapEnvelope(data);
                
                // 反序列化内部内容
                var serializer = new XmlSerializer(typeof(T));
                using (var stringReader = new StringReader(innerXml))
                {
                    return (T)serializer.Deserialize(stringReader);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoapXmlSerializationStrategy] 反序列化失败: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }

        /// <summary>
        /// 从字节数组反序列化
        /// </summary>
        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            if (data == null || data.Length == 0) return default;
            string xml = DefaultEncoding.GetString(data);
            return Deserialize<T>(xml);
        }

        /// <summary>
        /// 序列化列表
        /// </summary>
        public string SerializeList<T>(IEnumerable<T> list)
        {
            return Serialize(list);
        }

        /// <summary>
        /// 反序列化列表
        /// </summary>
        public List<T> DeserializeList<T>(string data) where T : new()
        {
            return Deserialize<List<T>>(data);
        }

        /// <summary>
        /// 反序列化 SOAP 信封（直接反序列化整个 SOAP 结构）
        /// </summary>
        public T DeserializeSoapEnvelope<T>(string data) where T : new()
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[SoapXmlSerializationStrategy] SOAP XML 字符串为空");
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
                Debug.LogError($"[SoapXmlSerializationStrategy] SOAP 反序列化失败: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }

        /// <summary>
        /// 序列化内部对象（不包含 SOAP 包装）
        /// </summary>
        private string SerializeInner(object obj)
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
                           NewLineHandling = NewLineHandling.Replace,
                           OmitXmlDeclaration = true // SOAP body 中不需要 XML 声明
                       }))
                {
                    serializer.Serialize(xmlWriter, obj);
                    return stringWriter.ToString();
                }
            }
        }

        /// <summary>
        /// 将 XML 数据包装为 SOAP 信封格式
        /// </summary>
        private string WrapInSoapEnvelope(string xmlData)
        {
            StringBuilder soap = new StringBuilder();
            
            soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soap.Append($"<soap:Envelope xmlns:xsi=\"{XsiNamespace}\" xmlns:xsd=\"{XsdNamespace}\" xmlns:soap=\"{SoapEnvelopeNamespace}\">");
            soap.Append("<soap:Body>");
            soap.Append(xmlData);
            soap.Append("</soap:Body>");
            soap.Append("</soap:Envelope>");

            return soap.ToString();
        }

        /// <summary>
        /// 从 SOAP 信封中提取内部 XML 数据
        /// </summary>
        private string ExtractFromSoapEnvelope(string soapXml)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(soapXml);

                // 创建命名空间管理器
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                nsmgr.AddNamespace("soap", SoapEnvelopeNamespace);

                // 查找 Body 节点
                XmlNode bodyNode = doc.SelectSingleNode("//soap:Body", nsmgr);
                
                if (bodyNode != null && bodyNode.HasChildNodes)
                {
                    // 返回 Body 的第一个子节点的 XML
                    return bodyNode.FirstChild.OuterXml;
                }

                // 如果没有找到 SOAP Body，返回原始数据
                Debug.LogWarning("[SoapXmlSerializationStrategy] 未找到 SOAP Body 节点，返回原始数据");
                return soapXml;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SoapXmlSerializationStrategy] 提取 SOAP Body 失败，返回原始数据: {ex.Message}");
                return soapXml;
            }
        }

        /// <summary>
        /// 创建自定义 XML 元素（用于特殊格式的序列化）
        /// </summary>
        public string SerializeCustomXml(string rootElementName, string xmlns, Dictionary<string, string> elements)
        {
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                XmlElement root = xmlDocument.CreateElement(rootElementName);
                
                if (!string.IsNullOrEmpty(xmlns))
                {
                    root.SetAttribute("xmlns", xmlns);
                }

                foreach (var kvp in elements)
                {
                    XmlElement element = xmlDocument.CreateElement(kvp.Key);
                    element.InnerText = kvp.Value;
                    root.AppendChild(element);
                }

                xmlDocument.AppendChild(root);
                return xmlDocument.InnerXml;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SoapXmlSerializationStrategy] 创建自定义 XML 失败: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 将数据包装为 SOAP 格式的字节数组（便捷方法）
        /// </summary>
        public static byte[] WrapInSoap(string xmlData)
        {
            StringBuilder soap = new StringBuilder();
            
            soap.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            soap.Append($"<soap:Envelope xmlns:xsi=\"{XsiNamespace}\" xmlns:xsd=\"{XsdNamespace}\" xmlns:soap=\"{SoapEnvelopeNamespace}\">");
            soap.Append("<soap:Body>");
            soap.Append(xmlData);
            soap.Append("</soap:Body>");
            soap.Append("</soap:Envelope>");

            Debug.Log($"[SoapXmlSerializationStrategy] SOAP 包装:\n{soap}");

            return DefaultEncoding.GetBytes(soap.ToString());
        }
    }

    #region SOAP 数据模型定义

    /// <summary>
    /// SOAP 信封
    /// </summary>
    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class SoapEnvelope
    {
        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public SoapBody Body { get; set; }
    }

    /// <summary>
    /// SOAP Body
    /// </summary>
    public class SoapBody
    {
        [XmlElement(ElementName = "DataListResponse", Namespace = "http://tempuri.org/")]
        public DataListResponse DataListResponse { get; set; }
    }

    /// <summary>
    /// 数据列表响应
    /// </summary>
    public class DataListResponse
    {
        [XmlElement(ElementName = "DataListResult")]
        public DataListResult DataListResult { get; set; }
    }

    /// <summary>
    /// 数据列表结果
    /// </summary>
    public class DataListResult
    {
        [XmlElement(ElementName = "RData")]
        public RData RData { get; set; }
    }

    /// <summary>
    /// 数据记录
    /// </summary>
    public class RData
    {
        [XmlElement(ElementName = "Timestamp")]
        public DateTime Timestamp { get; set; }
        
        [XmlElement(ElementName = "Type")]
        public string Type { get; set; }
        
        [XmlElement(ElementName = "Quality")]
        public string Quality { get; set; }
        
        [XmlElement(ElementName = "Value")]
        public ValueData Value { get; set; }
        
        [XmlElement(ElementName = "Tag")]
        public string Tag { get; set; }
        
        [XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string ValueTmpType { get; set; }
        
        [XmlText]
        public string ValueTmp { get; set; }
    }

    /// <summary>
    /// 值数据
    /// </summary>
    public class ValueData
    {
        [XmlElement(ElementName = "time")]
        public DateTime Time { get; set; }
        
        [XmlElement(ElementName = "b")]
        public bool BooleanValue { get; set; }
        
        [XmlElement(ElementName = "i8")]
        public long Int64Value { get; set; }
        
        [XmlElement(ElementName = "i4")]
        public int Int32Value { get; set; }
        
        [XmlElement(ElementName = "i2")]
        public short Int16Value { get; set; }
        
        [XmlElement(ElementName = "i1")]
        public sbyte Int8Value { get; set; }
        
        [XmlElement(ElementName = "r8")]
        public double DoubleValue { get; set; }
        
        [XmlElement(ElementName = "r4")]
        public float FloatValue { get; set; }
    }

    /// <summary>
    /// 数据列表 API 参数
    /// </summary>
    public class DataListApiParam
    {
        public string tmgTagList { get; set; }
    }

    #endregion
}
