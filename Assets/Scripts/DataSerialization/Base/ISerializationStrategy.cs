using System;
using System.Collections.Generic;

namespace DataSerialization
{
    /// <summary>
    /// 序列化格式枚举
    /// </summary>
    public enum SerializationFormat
    {
        Binary,
        Json,
        Xml,
        Csv
    }

    /// <summary>
    /// 序列化策略接口
    /// </summary>
    public interface ISerializationStrategy
    {
        /// <summary>
        /// 序列化对象为字符串
        /// </summary>
        string Serialize(object obj);

        /// <summary>
        /// 序列化对象为字节数组
        /// </summary>
        byte[] SerializeToBytes(object obj);

        /// <summary>
        /// 从字符串反序列化对象
        /// </summary>
        T Deserialize<T>(string data) where T : new();

        /// <summary>
        /// 从字节数组反序列化对象
        /// </summary>
        T DeserializeFromBytes<T>(byte[] data) where T : new();

        /// <summary>
        /// 序列化对象列表
        /// </summary>
        string SerializeList<T>(IEnumerable<T> list);

        /// <summary>
        /// 反序列化对象列表
        /// </summary>
        List<T> DeserializeList<T>(string data) where T : new();

        /// <summary>
        /// 保存到文件
        /// </summary>
        bool SaveToFile(object obj, string filePath);

        /// <summary>
        /// 从文件加载
        /// </summary>
        T LoadFromFile<T>(string filePath) where T : new();

        /// <summary>
        /// 支持的文件扩展名
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// 格式名称
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// 是否支持压缩
        /// </summary>
        bool SupportsCompression { get; }
    }
}