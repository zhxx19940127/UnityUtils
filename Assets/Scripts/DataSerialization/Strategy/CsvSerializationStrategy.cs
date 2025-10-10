using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace DataSerialization
{
    /// <summary>
    /// CSV 序列化策略
    /// </summary>
    public class CsvSerializationStrategy : ISerializationStrategy
    {
        /// <summary>
        /// 默认分隔符
        /// </summary>
        public const char DefaultSeparator = ',';

        /// <summary>
        /// 默认编码格式
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        public string[] SupportedExtensions => new[] { ".csv", ".txt" };
        public string FormatName => "CSV";
        public bool SupportsCompression => false;

        public string Serialize(object obj)
        {
            // CSV 主要用于列表数据，单个对象转为单行
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
            {
                // 如果是可枚举类型，使用列表序列化
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    list.Add(item);
                }

                return SerializeInternal(list, true, DefaultSeparator);
            }
            else
            {
                // 单个对象包装成列表
                var list = new List<object> { obj };
                return SerializeInternal(list, true, DefaultSeparator);
            }
        }

        public byte[] SerializeToBytes(object obj)
        {
            string csv = Serialize(obj);
            return string.IsNullOrEmpty(csv) ? null : DefaultEncoding.GetBytes(csv);
        }

        public T Deserialize<T>(string data) where T : new()
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogWarning("[CsvSerializationStrategy] CSV 字符串为空");
                return default;
            }

            try
            {
                // 对于CSV，我们假设单个对象反序列化需要new()约束
                // 检查类型是否有无参构造函数
                var type = typeof(T);
                if (!HasParameterlessConstructor(type))
                {
                    Debug.LogWarning($"[CsvSerializationStrategy] 类型 {type.Name} 没有无参数构造函数");
                    return default;
                }

                // 直接使用内部逻辑，避免泛型约束问题
                var lines = SplitLines(data);
                if (lines.Count == 0) return default;

                var properties = GetSerializableProperties<T>();
                if (properties.Length == 0)
                {
                    Debug.LogWarning($"[CsvSerializationStrategy] 类型 {typeof(T).Name} 没有可序列化的属性");
                    return default;
                }

                int startIndex = 1; // 假设有表头
                PropertyInfo[] orderedProperties = properties;

                // 如果有表头,根据表头顺序重新排列属性
                if (lines.Count > 0)
                {
                    var headers = ParseLine(lines[0], DefaultSeparator);
                    orderedProperties = MapPropertiesByHeaders<T>(headers, properties);
                    if (orderedProperties == null)
                    {
                        Debug.LogWarning("[CsvSerializationStrategy] 表头映射失败，使用默认属性顺序");
                        orderedProperties = properties;
                    }
                }

                // 解析第一行数据
                if (startIndex < lines.Count)
                {
                    var line = lines[startIndex].Trim();
                    if (!string.IsNullOrEmpty(line))
                    {
                        var values = ParseLine(line, DefaultSeparator);
                        if (values.Length > 0)
                        {
                            return CreateInstanceWithoutConstraint<T>(values, orderedProperties);
                        }
                    }
                }

                return default;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            if (data == null || data.Length == 0) return default;
            string csv = DefaultEncoding.GetString(data);
            return Deserialize<T>(csv);
        }

        public string SerializeList<T>(IEnumerable<T> list)
        {
            return SerializeInternal(list, true, DefaultSeparator);
        }

        public List<T> DeserializeList<T>(string data) where T : new()
        {
            return DeserializeInternal<T>(data, true, DefaultSeparator);
        }

        public bool SaveToFile(object obj, string filePath)
        {
            if (obj == null)
            {
                Debug.LogWarning("[CsvSerializationStrategy] 保存数据为 null");
                return false;
            }

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[CsvSerializationStrategy] 文件路径为空");
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

                string csv = Serialize(obj);
                File.WriteAllText(filePath, csv, DefaultEncoding);

                Debug.Log($"[CsvSerializationStrategy] CSV 文件保存成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 保存 CSV 文件失败: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public T LoadFromFile<T>(string filePath) where T : new()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[CsvSerializationStrategy] 文件路径为空");
                return default;
            }

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[CsvSerializationStrategy] 文件不存在: {filePath}");
                return default;
            }

            try
            {
                string csv = File.ReadAllText(filePath, DefaultEncoding);
                var list = DeserializeInternal<T>(csv, true, DefaultSeparator);
                return list.Count > 0 ? list[0] : default;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 加载 CSV 文件失败: {ex.Message}\n{ex.StackTrace}");
                return default;
            }
        }

        /// <summary>
        /// 从文件加载列表
        /// </summary>
        public List<T> LoadListFromFile<T>(string filePath) where T : new()
        {
            if (string.IsNullOrEmpty(filePath))
            {
                Debug.LogWarning("[CsvSerializationStrategy] 文件路径为空");
                return new List<T>();
            }

            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[CsvSerializationStrategy] 文件不存在: {filePath}");
                return new List<T>();
            }

            try
            {
                string csv = File.ReadAllText(filePath, DefaultEncoding);
                return DeserializeInternal<T>(csv, true, DefaultSeparator);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 加载 CSV 文件失败: {ex.Message}\n{ex.StackTrace}");
                return new List<T>();
            }
        }

        // ==================== 内部实现方法 ====================

        /// <summary>
        /// 将对象列表序列化为 CSV 字符串
        /// </summary>
        private string SerializeInternal<T>(IEnumerable<T> data, bool includeHeader = true,
            char separator = DefaultSeparator)
        {
            if (data == null)
            {
                Debug.LogWarning("[CsvSerializationStrategy] 序列化数据为 null");
                return string.Empty;
            }

            try
            {
                var sb = new StringBuilder();
                var properties = GetSerializableProperties<T>();

                if (properties.Length == 0)
                {
                    Debug.LogWarning($"[CsvSerializationStrategy] 类型 {typeof(T).Name} 没有可序列化的属性");
                    return string.Empty;
                }

                // 写入表头
                if (includeHeader)
                {
                    var headers = properties.Select(p => GetColumnName(p)).ToArray();
                    sb.AppendLine(string.Join(separator.ToString(), headers));
                }

                // 写入数据行
                foreach (var item in data)
                {
                    if (item == null) continue;

                    var values = properties.Select(p => FormatValue(p.GetValue(item), separator)).ToArray();
                    sb.AppendLine(string.Join(separator.ToString(), values));
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 序列化失败: {ex.Message}\n{ex.StackTrace}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从 CSV 字符串反序列化为对象列表
        /// </summary>
        private List<T> DeserializeInternal<T>(string csv, bool hasHeader = true, char separator = DefaultSeparator)
            where T : new()
        {
            if (string.IsNullOrEmpty(csv))
            {
                Debug.LogWarning("[CsvSerializationStrategy] CSV 字符串为空");
                return new List<T>();
            }

            try
            {
                var lines = SplitLines(csv);
                if (lines.Count == 0) return new List<T>();

                var properties = GetSerializableProperties<T>();
                if (properties.Length == 0)
                {
                    Debug.LogWarning($"[CsvSerializationStrategy] 类型 {typeof(T).Name} 没有可序列化的属性");
                    return new List<T>();
                }

                var result = new List<T>();
                int startIndex = hasHeader ? 1 : 0;
                PropertyInfo[] orderedProperties = properties;

                // 如果有表头,根据表头顺序重新排列属性
                if (hasHeader && lines.Count > 0)
                {
                    var headers = ParseLine(lines[0], separator);
                    orderedProperties = MapPropertiesByHeaders<T>(headers, properties);
                    if (orderedProperties == null)
                    {
                        Debug.LogWarning("[CsvSerializationStrategy] 表头映射失败，使用默认属性顺序");
                        orderedProperties = properties;
                    }
                }

                // 解析数据行
                for (int i = startIndex; i < lines.Count; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var values = ParseLine(line, separator);
                    if (values.Length == 0) continue;

                    var item = CreateInstance<T>(values, orderedProperties);
                    if (item != null)
                    {
                        result.Add(item);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 反序列化失败: {ex.Message}\n{ex.StackTrace}");
                return new List<T>();
            }
        }

        // ==================== 工具方法 ====================

        /// <summary>
        /// 获取类型的可序列化属性
        /// </summary>
        private static PropertyInfo[] GetSerializableProperties<T>()
        {
            return typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => IsSupportedType(p.PropertyType))
                .Where(p => !HasIgnoreAttribute(p))
                .OrderBy(p => GetColumnOrder(p))
                .ToArray();
        }

        /// <summary>
        /// 检查是否为支持的类型
        /// </summary>
        private static bool IsSupportedType(Type type)
        {
            return type.IsPrimitive ||
                   type == typeof(string) ||
                   type == typeof(DateTime) ||
                   type == typeof(decimal) ||
                   type.IsEnum ||
                   (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                    IsSupportedType(Nullable.GetUnderlyingType(type)));
        }

        /// <summary>
        /// 检查属性是否有忽略特性
        /// </summary>
        private static bool HasIgnoreAttribute(PropertyInfo property)
        {
            return property.GetCustomAttribute<CsvIgnoreAttribute>() != null;
        }

        /// <summary>
        /// 获取列名
        /// </summary>
        private static string GetColumnName(PropertyInfo property)
        {
            var columnAttr = property.GetCustomAttribute<CsvColumnAttribute>();
            return columnAttr?.Name ?? property.Name;
        }

        /// <summary>
        /// 获取列排序
        /// </summary>
        private static int GetColumnOrder(PropertyInfo property)
        {
            var columnAttr = property.GetCustomAttribute<CsvColumnAttribute>();
            return columnAttr?.Order ?? int.MaxValue;
        }

        /// <summary>
        /// 格式化值为 CSV 字段
        /// </summary>
        private static string FormatValue(object value, char separator)
        {
            if (value == null) return string.Empty;

            string stringValue;

            // 特殊类型处理
            switch (value)
            {
                case DateTime dateTime:
                    stringValue = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                case float floatValue:
                    stringValue = floatValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case double doubleValue:
                    stringValue = doubleValue.ToString(CultureInfo.InvariantCulture);
                    break;
                case decimal decimalValue:
                    stringValue = decimalValue.ToString(CultureInfo.InvariantCulture);
                    break;
                default:
                    stringValue = value.ToString();
                    break;
            }

            // 如果包含分隔符、引号或换行符,需要用引号包围
            if (stringValue.Contains(separator) || stringValue.Contains('"') || stringValue.Contains('\n'))
            {
                stringValue = '"' + stringValue.Replace("\"", "\"\"") + '"';
            }

            return stringValue;
        }

        /// <summary>
        /// 解析 CSV 行
        /// </summary>
        private static string[] ParseLine(string line, char separator)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // 双引号转义
                        currentField.Append('"');
                        i++; // 跳过下一个引号
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == separator && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// 分割 CSV 字符串为行
        /// </summary>
        private static List<string> SplitLines(string csv)
        {
            var lines = new List<string>();
            var currentLine = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char c = csv[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        currentLine.Append("\"\"");
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                        currentLine.Append(c);
                    }
                }
                else if ((c == '\n' || c == '\r') && !inQuotes)
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    // 跳过 \r\n 中的 \n
                    if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                    {
                        i++;
                    }
                }
                else
                {
                    currentLine.Append(c);
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines;
        }

        /// <summary>
        /// 根据表头顺序映射属性
        /// </summary>
        private static PropertyInfo[] MapPropertiesByHeaders<T>(string[] headers, PropertyInfo[] properties)
        {
            try
            {
                var propertyMap =
                    properties.ToDictionary(p => GetColumnName(p), p => p, StringComparer.OrdinalIgnoreCase);
                var orderedProperties = new PropertyInfo[headers.Length];

                for (int i = 0; i < headers.Length; i++)
                {
                    var headerName = headers[i].Trim();
                    if (propertyMap.TryGetValue(headerName, out var property))
                    {
                        orderedProperties[i] = property;
                    }
                    else
                    {
                        Debug.LogWarning($"[CsvSerializationStrategy] 找不到对应属性: {headerName}");
                        return null; // 如果有列无法映射，返回null使用默认顺序
                    }
                }

                return orderedProperties;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 属性映射失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建实例并赋值
        /// </summary>
        private static T CreateInstance<T>(string[] values, PropertyInfo[] properties) where T : new()
        {
            try
            {
                var instance = new T();

                for (int i = 0; i < Math.Min(values.Length, properties.Length); i++)
                {
                    var property = properties[i];
                    var value = values[i]?.Trim();

                    if (string.IsNullOrEmpty(value))
                        continue;

                    var convertedValue = ConvertValue(value, property.PropertyType);
                    if (convertedValue != null)
                    {
                        property.SetValue(instance, convertedValue);
                    }
                }

                return instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 创建实例失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 创建实例并赋值（无泛型约束版本）
        /// </summary>
        private static T CreateInstanceWithoutConstraint<T>(string[] values, PropertyInfo[] properties)
        {
            try
            {
                var type = typeof(T);
                object instance;

                // 创建实例
                if (type.IsValueType)
                {
                    instance = Activator.CreateInstance(type);
                }
                else
                {
                    var constructor = type.GetConstructor(Type.EmptyTypes);
                    if (constructor == null)
                    {
                        Debug.LogError($"[CsvSerializationStrategy] 类型 {type.Name} 没有无参数构造函数");
                        return default;
                    }

                    instance = constructor.Invoke(null);
                }

                for (int i = 0; i < Math.Min(values.Length, properties.Length); i++)
                {
                    var property = properties[i];
                    var value = values[i]?.Trim();

                    if (string.IsNullOrEmpty(value))
                        continue;

                    var convertedValue = ConvertValue(value, property.PropertyType);
                    if (convertedValue != null)
                    {
                        property.SetValue(instance, convertedValue);
                    }
                }

                return (T)instance;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 创建实例失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 转换值类型
        /// </summary>
        private static object ConvertValue(string value, Type targetType)
        {
            try
            {
                if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (string.IsNullOrEmpty(value))
                        return null;
                    targetType = Nullable.GetUnderlyingType(targetType);
                }

                if (targetType == typeof(string))
                    return value;

                if (targetType == typeof(DateTime))
                    return DateTime.Parse(value);

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, value, true);

                if (targetType == typeof(bool))
                    return bool.Parse(value);

                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CsvSerializationStrategy] 类型转换失败: {value} -> {targetType.Name}, {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查类型是否有无参构造函数
        /// </summary>
        private static bool HasParameterlessConstructor(Type type)
        {
            return type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null;
        }
    }

    // ==================== 特性定义 ====================

    /// <summary>
    /// CSV 列特性
    /// 用于指定列名和排序
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvColumnAttribute : Attribute
    {
        /// <summary>
        /// 列名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 排序(数字越小越靠前)
        /// </summary>
        public int Order { get; set; } = int.MaxValue;

        public CsvColumnAttribute(string name)
        {
            Name = name;
        }

        public CsvColumnAttribute(string name, int order)
        {
            Name = name;
            Order = order;
        }
    }

    /// <summary>
    /// CSV 忽略特性
    /// 标记不需要序列化的属性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvIgnoreAttribute : Attribute
    {
    }
}