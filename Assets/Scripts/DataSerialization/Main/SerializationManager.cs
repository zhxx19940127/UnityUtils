using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DataSerialization
{
    /// <summary>
    /// 序列化策略注册信息
    /// </summary>
    public class SerializationStrategyInfo
    {
        public SerializationFormat Format { get; set; }
        public Type StrategyType { get; set; }
        public Func<ISerializationStrategy> Factory { get; set; }
        public bool IsLazyLoaded { get; set; } = true;
        public ISerializationStrategy Instance { get; set; }

        public SerializationStrategyInfo(SerializationFormat format, Type strategyType)
        {
            Format = format;
            StrategyType = strategyType;
            Factory = () => (ISerializationStrategy)Activator.CreateInstance(strategyType);
        }

        public SerializationStrategyInfo(SerializationFormat format, Func<ISerializationStrategy> factory)
        {
            Format = format;
            Factory = factory;
        }
    }

    /// <summary>
    /// 统一数据序列化管理器 - 支持插件式策略加载和延迟初始化
    /// </summary>
    public static class SerializationManager
    {
        private static readonly Dictionary<SerializationFormat, SerializationStrategyInfo> _strategyInfos;
        private static readonly object _lock = new object();

        static SerializationManager()
        {
            _strategyInfos = new Dictionary<SerializationFormat, SerializationStrategyInfo>();
            RegisterDefaultStrategies();
        }

        #region 策略注册管理

        /// <summary>
        /// 注册默认策略
        /// </summary>
        private static void RegisterDefaultStrategies()
        {
            RegisterStrategy(SerializationFormat.Binary, typeof(BinarySerializationStrategy));
            RegisterStrategy(SerializationFormat.Json, typeof(JsonSerializationStrategy));
            RegisterStrategy(SerializationFormat.Xml, typeof(XmlSerializationStrategy));
            RegisterStrategy(SerializationFormat.Csv, typeof(CsvSerializationStrategy));
        }

        /// <summary>
        /// 注册序列化策略
        /// </summary>
        public static void RegisterStrategy(SerializationFormat format, Type strategyType)
        {
            if (!typeof(ISerializationStrategy).IsAssignableFrom(strategyType))
            {
                throw new ArgumentException($"策略类型 {strategyType.Name} 必须实现 ISerializationStrategy 接口");
            }

            lock (_lock)
            {
                _strategyInfos[format] = new SerializationStrategyInfo(format, strategyType);
            }

            Debug.Log($"[SerializationManager] 注册策略: {format} -> {strategyType.Name}");
        }

        /// <summary>
        /// 注册自定义序列化策略工厂
        /// </summary>
        public static void RegisterStrategy(SerializationFormat format, Func<ISerializationStrategy> factory)
        {
            lock (_lock)
            {
                _strategyInfos[format] = new SerializationStrategyInfo(format, factory);
            }

            Debug.Log($"[SerializationManager] 注册自定义策略工厂: {format}");
        }

        /// <summary>
        /// 注册策略实例（立即初始化）
        /// </summary>
        public static void RegisterStrategy(SerializationFormat format, ISerializationStrategy instance)
        {
            lock (_lock)
            {
                var info = new SerializationStrategyInfo(format, () => instance)
                {
                    IsLazyLoaded = false,
                    Instance = instance
                };
                _strategyInfos[format] = info;
            }

            Debug.Log($"[SerializationManager] 注册策略实例: {format} -> {instance.GetType().Name}");
        }

        /// <summary>
        /// 取消注册策略
        /// </summary>
        public static bool UnregisterStrategy(SerializationFormat format)
        {
            lock (_lock)
            {
                return _strategyInfos.Remove(format);
            }
        }

        /// <summary>
        /// 获取已注册的策略格式
        /// </summary>
        public static SerializationFormat[] GetRegisteredFormats()
        {
            lock (_lock)
            {
                return _strategyInfos.Keys.ToArray();
            }
        }

        #endregion

        #region 策略获取（延迟初始化）

        /// <summary>
        /// 获取指定格式的策略（延迟初始化）
        /// </summary>
        public static ISerializationStrategy GetStrategy(SerializationFormat format)
        {
            lock (_lock)
            {
                if (!_strategyInfos.TryGetValue(format, out var info))
                {
                    throw new NotSupportedException($"不支持的序列化格式: {format}");
                }

                // 延迟初始化
                if (info.Instance == null && info.IsLazyLoaded)
                {
                    try
                    {
                        info.Instance = info.Factory();
                        Debug.Log($"[SerializationManager] 延迟初始化策略: {format} -> {info.Instance.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[SerializationManager] 初始化策略失败 {format}: {ex.Message}");
                        throw;
                    }
                }

                return info.Instance;
            }
        }

        /// <summary>
        /// 根据文件扩展名自动选择策略
        /// </summary>
        public static ISerializationStrategy GetStrategyByExtension(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();

            lock (_lock)
            {
                foreach (var kvp in _strategyInfos)
                {
                    var strategy = GetStrategy(kvp.Key);
                    if (strategy.SupportedExtensions.Contains(extension))
                        return strategy;
                }
            }

            Debug.LogWarning($"[SerializationManager] 未找到支持扩展名 {extension} 的策略，使用默认 JSON 策略");
            return GetStrategy(SerializationFormat.Json);
        }

        /// <summary>
        /// 预加载所有策略（可选的性能优化）
        /// </summary>
        public static void PreloadAllStrategies()
        {
            lock (_lock)
            {
                foreach (var format in _strategyInfos.Keys.ToArray())
                {
                    GetStrategy(format); // 触发延迟初始化
                }
            }

            Debug.Log("[SerializationManager] 所有策略预加载完成");
        }

        /// <summary>
        /// 清理未使用的策略实例（内存优化）
        /// </summary>
        public static void CleanupUnusedStrategies()
        {
            lock (_lock)
            {
                foreach (var info in _strategyInfos.Values)
                {
                    if (info.IsLazyLoaded && info.Instance != null)
                    {
                        // 可以添加使用计数或最后使用时间来决定是否清理
                        // 这里简单演示概念
                        Debug.Log($"[SerializationManager] 可考虑清理策略: {info.Format}");
                    }
                }
            }
        }

        #endregion

        #region 统一接口（保持向后兼容）

        /// <summary>
        /// 根据数据类型推荐最佳策略
        /// </summary>
        public static SerializationFormat RecommendFormat(Type dataType, bool prioritizePerformance = false)
        {
            // 性能优先
            if (prioritizePerformance)
                return SerializationFormat.Binary;

            // 根据数据类型推荐
            if (dataType.IsArray || (dataType.IsGenericType && dataType.GetGenericTypeDefinition() == typeof(List<>)))
            {
                // 列表/数组类型优先使用 CSV
                return SerializationFormat.Csv;
            }

            if (dataType.Name.Contains("Config") || dataType.Name.Contains("Setting"))
            {
                // 配置类型优先使用 XML
                return SerializationFormat.Xml;
            }

            // 默认使用 JSON
            return SerializationFormat.Json;
        }

        /// <summary>
        /// 序列化对象
        /// </summary>
        public static string Serialize(object obj, SerializationFormat format)
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.Serialize(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 序列化失败 ({format}): {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 序列化对象为字节数组
        /// </summary>
        public static byte[] SerializeToBytes(object obj, SerializationFormat format)
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.SerializeToBytes(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 字节序列化失败 ({format}): {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 反序列化对象
        /// </summary>
        public static T Deserialize<T>(string data, SerializationFormat format) where T : new()
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 反序列化失败 ({format}): {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 从字节数组反序列化对象
        /// </summary>
        public static T DeserializeFromBytes<T>(byte[] data, SerializationFormat format) where T : new()
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.DeserializeFromBytes<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 字节反序列化失败 ({format}): {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 序列化列表
        /// </summary>
        public static string SerializeList<T>(IEnumerable<T> list, SerializationFormat format)
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.SerializeList(list);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 列表序列化失败 ({format}): {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 反序列化列表
        /// </summary>
        public static List<T> DeserializeList<T>(string data, SerializationFormat format) where T : new()
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.DeserializeList<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 列表反序列化失败 ({format}): {ex.Message}");
                return new List<T>();
            }
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 保存到文件（自动选择格式）
        /// </summary>
        public static bool SaveToFile(object obj, string filePath)
        {
            try
            {
                var strategy = GetStrategyByExtension(filePath);
                return strategy.SaveToFile(obj, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 保存文件失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存到文件（指定格式）
        /// </summary>
        public static bool SaveToFile(object obj, string filePath, SerializationFormat format)
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.SaveToFile(obj, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 保存文件失败 ({format}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从文件加载（自动选择格式）
        /// </summary>
        public static T LoadFromFile<T>(string filePath) where T : new()
        {
            try
            {
                var strategy = GetStrategyByExtension(filePath);
                return strategy.LoadFromFile<T>(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 加载文件失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 从文件加载（指定格式）
        /// </summary>
        public static T LoadFromFile<T>(string filePath, SerializationFormat format) where T : new()
        {
            try
            {
                var strategy = GetStrategy(format);
                return strategy.LoadFromFile<T>(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 加载文件失败 ({format}): {ex.Message}");
                return default;
            }
        }

        #endregion

        #region Unity 路径集成

        /// <summary>
        /// 保存到 PersistentDataPath
        /// </summary>
        public static bool SaveToPersistentDataPath(object obj, string fileName, SerializationFormat format)
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            return SaveToFile(obj, filePath, format);
        }

        /// <summary>
        /// 从 PersistentDataPath 加载
        /// </summary>
        public static T LoadFromPersistentDataPath<T>(string fileName, SerializationFormat format) where T : new()
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            return LoadFromFile<T>(filePath, format);
        }

        /// <summary>
        /// 从 StreamingAssets 加载
        /// </summary>
        public static T LoadFromStreamingAssets<T>(string fileName, SerializationFormat format) where T : new()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, fileName);
            return LoadFromFile<T>(filePath, format);
        }

        /// <summary>
        /// 从 Resources 加载
        /// </summary>
        public static T LoadFromResources<T>(string resourcePath, SerializationFormat format) where T : new()
        {
            try
            {
                var textAsset = Resources.Load<TextAsset>(resourcePath);
                if (textAsset == null)
                {
                    Debug.LogWarning($"[SerializationManager] Resources 中找不到文件: {resourcePath}");
                    return default;
                }

                var strategy = GetStrategy(format);
                return strategy.Deserialize<T>(textAsset.text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 从 Resources 加载失败: {ex.Message}");
                return default;
            }
        }

        #endregion

        #region 格式转换

        /// <summary>
        /// 格式转换
        /// </summary>
        public static string ConvertFormat<T>(string data, SerializationFormat fromFormat, SerializationFormat toFormat) where T : new()
        {
            try
            {
                // 反序列化
                var obj = Deserialize<T>(data, fromFormat);
                if (obj == null) return string.Empty;

                // 重新序列化
                return Serialize(obj, toFormat);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 格式转换失败 ({fromFormat} -> {toFormat}): {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 文件格式转换
        /// </summary>
        public static bool ConvertFile<T>(string inputPath, string outputPath) where T : new()
        {
            try
            {
                var inputStrategy = GetStrategyByExtension(inputPath);
                var outputStrategy = GetStrategyByExtension(outputPath);

                var data = inputStrategy.LoadFromFile<T>(inputPath);
                if (data == null) return false;

                return outputStrategy.SaveToFile(data, outputPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SerializationManager] 文件格式转换失败: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取所有支持的格式信息
        /// </summary>
        public static Dictionary<SerializationFormat, string[]> GetSupportedFormats()
        {
            var result = new Dictionary<SerializationFormat, string[]>();
            lock (_lock)
            {
                foreach (var kvp in _strategyInfos)
                {
                    var strategy = GetStrategy(kvp.Key);
                    result[kvp.Key] = strategy.SupportedExtensions;
                }
            }

            return result;
        }

        /// <summary>
        /// 获取格式性能信息
        /// </summary>
        public static string GetFormatInfo(SerializationFormat format)
        {
            var strategy = GetStrategy(format);
            return $"{strategy.FormatName} - 扩展名: {string.Join(", ", strategy.SupportedExtensions)} - 压缩支持: {strategy.SupportsCompression}";
        }

        /// <summary>
        /// 智能推荐格式
        /// </summary>
        public static SerializationFormat SmartRecommend<T>(T obj, string useCase = "")
        {
            var type = typeof(T);

            // 根据用例推荐
            switch (useCase.ToLower())
            {
                case "save":
                case "archive":
                    return SerializationFormat.Binary;
                case "config":
                case "settings":
                    return SerializationFormat.Xml;
                case "api":
                case "network":
                    return SerializationFormat.Json;
                case "data":
                case "table":
                    return SerializationFormat.Csv;
                default:
                    return RecommendFormat(type);
            }
        }

        #endregion
    }
}