using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataSerialization
{
    /// <summary>
    /// 自定义策略示例 - 简单的Base64策略
    /// </summary>
    public class Base64SerializationStrategy : ISerializationStrategy
    {
        public string[] SupportedExtensions => new[] { ".b64", ".base64" };
        public string FormatName => "Base64";
        public bool SupportsCompression => false;

        public string Serialize(object obj)
        {
            try
            {
                string json = UnityEngine.JsonUtility.ToJson(obj);
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Base64Strategy] 序列化失败: {ex.Message}");
                return string.Empty;
            }
        }

        public byte[] SerializeToBytes(object obj)
        {
            string base64 = Serialize(obj);
            return string.IsNullOrEmpty(base64) ? null : System.Text.Encoding.UTF8.GetBytes(base64);
        }

        public T Deserialize<T>(string data) where T : new()
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(data);
                string json = System.Text.Encoding.UTF8.GetString(bytes);
                return UnityEngine.JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Base64Strategy] 反序列化失败: {ex.Message}");
                return default;
            }
        }

        public T DeserializeFromBytes<T>(byte[] data) where T : new()
        {
            string base64 = System.Text.Encoding.UTF8.GetString(data);
            return Deserialize<T>(base64);
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
            try
            {
                string data = Serialize(obj);
                if (string.IsNullOrEmpty(data)) return false;

                System.IO.File.WriteAllText(filePath, data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Base64Strategy] 保存文件失败: {ex.Message}");
                return false;
            }
        }

        public T LoadFromFile<T>(string filePath) where T : new()
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) return default;
                string data = System.IO.File.ReadAllText(filePath);
                return Deserialize<T>(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Base64Strategy] 加载文件失败: {ex.Message}");
                return default;
            }
        }
    }

    /// <summary>
    /// 插件式策略加载示例
    /// </summary>
    public class PluginStrategyExample : MonoBehaviour
    {
        // 自定义格式枚举（扩展原有枚举）
        public enum CustomSerializationFormat
        {
            Base64 = 100, // 使用100以上避免与原有格式冲突
            Encrypted = 101,
            Compressed = 102
        }

        [System.Serializable]
        public class TestData
        {
            public string message = "Hello Plugin Strategy!";
            public int number = 42;
            public bool flag = true;

            public TestData()
            {
            }
        }

        void Start()
        {
            DemonstratePluginStrategies();
        }

        void DemonstratePluginStrategies()
        {
            Debug.Log("=== 插件式策略演示开始 ===");

            // 1. 注册自定义策略类型
            RegisterCustomStrategies();

            // 2. 测试延迟初始化
            TestLazyInitialization();

            // 3. 测试自定义策略
            TestCustomStrategy();

            // 4. 演示策略管理
            DemonstrateStrategyManagement();

            Debug.Log("=== 插件式策略演示结束 ===");
        }

        void RegisterCustomStrategies()
        {
            Debug.Log("--- 注册自定义策略 ---");

            // 方法1: 注册策略类型（延迟初始化）
            SerializationManager.RegisterStrategy(
                (SerializationFormat)CustomSerializationFormat.Base64,
                typeof(Base64SerializationStrategy)
            );

            // 方法2: 注册策略工厂
            SerializationManager.RegisterStrategy(
                (SerializationFormat)CustomSerializationFormat.Encrypted,
                () => new Base64SerializationStrategy() // 这里可以返回加密策略的实例
            );

            // 方法3: 注册策略实例（立即初始化）
            var compressedStrategy = new Base64SerializationStrategy(); // 这里可以是压缩策略
            SerializationManager.RegisterStrategy(
                (SerializationFormat)CustomSerializationFormat.Compressed,
                compressedStrategy
            );

            Debug.Log($"已注册的格式: {string.Join(", ", SerializationManager.GetRegisteredFormats())}");
        }

        void TestLazyInitialization()
        {
            Debug.Log("--- 测试延迟初始化 ---");

            // 第一次获取策略时才会实际创建实例
            Debug.Log("第一次获取Base64策略...");
            var strategy1 = SerializationManager.GetStrategy((SerializationFormat)CustomSerializationFormat.Base64);

            Debug.Log("第二次获取Base64策略（应该复用实例）...");
            var strategy2 = SerializationManager.GetStrategy((SerializationFormat)CustomSerializationFormat.Base64);

            Debug.Log($"实例相同: {ReferenceEquals(strategy1, strategy2)}");
        }

        void TestCustomStrategy()
        {
            Debug.Log("--- 测试自定义策略 ---");

            var testData = new TestData
            {
                message = "自定义策略测试",
                number = 999,
                flag = false
            };

            // 使用自定义Base64策略
            string serialized =
                SerializationManager.Serialize(testData, (SerializationFormat)CustomSerializationFormat.Base64);
            Debug.Log($"Base64序列化结果: {serialized.Substring(0, Math.Min(50, serialized.Length))}...");

            var deserialized =
                SerializationManager.Deserialize<TestData>(serialized,
                    (SerializationFormat)CustomSerializationFormat.Base64);
            Debug.Log($"反序列化结果: {deserialized.message}, {deserialized.number}, {deserialized.flag}");

            // 文件操作测试
            string testFile = Application.persistentDataPath + "/test_plugin.b64";
            bool saved = SerializationManager.SaveToFile(testData, testFile,
                (SerializationFormat)CustomSerializationFormat.Base64);
            if (saved)
            {
                var loaded = SerializationManager.LoadFromFile<TestData>(testFile,
                    (SerializationFormat)CustomSerializationFormat.Base64);
                Debug.Log($"文件操作成功: {loaded?.message}");
            }
        }

        void DemonstrateStrategyManagement()
        {
            Debug.Log("--- 策略管理演示 ---");

            // 预加载所有策略
            SerializationManager.PreloadAllStrategies();

            // 获取支持的格式
            var supportedFormats = SerializationManager.GetSupportedFormats();
            foreach (var kvp in supportedFormats)
            {
                Debug.Log($"格式 {kvp.Key}: 支持扩展名 {string.Join(", ", kvp.Value)}");
            }

            // 清理示例（实际项目中可根据需要调用）
            SerializationManager.CleanupUnusedStrategies();

            // 取消注册示例
            bool unregistered =
                SerializationManager.UnregisterStrategy((SerializationFormat)CustomSerializationFormat.Encrypted);
            Debug.Log($"取消注册加密策略: {unregistered}");
        }

        void OnDestroy()
        {
            // 清理测试文件
            try
            {
                string testFile = Application.persistentDataPath + "/test_plugin.b64";
                if (System.IO.File.Exists(testFile))
                {
                    System.IO.File.Delete(testFile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"清理测试文件失败: {ex.Message}");
            }
        }
    }
}