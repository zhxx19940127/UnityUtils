using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LitJson;

namespace DataSerialization
{
    /// <summary>
    /// 统一序列化系统使用示例
    /// 演示如何使用策略模式统一入口进行各种格式的序列化操作
    /// </summary>
    public class SerializationExample : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerData
        {
            public string playerName = "玩家001";
            public int level = 25;
            public float experience = 1500.5f;
            public SerializableVector3 position = new SerializableVector3(10, 20, 30);
            public SerializableColor playerColor = new SerializableColor(1f, 0.5f, 0.2f, 1f);
            public List<string> inventory = new List<string> { "剑", "盾牌", "药水" };

            // 无参构造函数，确保反序列化正常工作
            public PlayerData()
            {
                inventory = new List<string>();
                position = new SerializableVector3(0, 0, 0);
                playerColor = new SerializableColor(1, 1, 1, 1);
            }
        }

        void Start()
        {
            // 演示统一序列化系统的使用
            DemonstrateUnifiedSerialization();
        }

        void DemonstrateUnifiedSerialization()
        {
            Debug.Log("=== 统一序列化系统演示开始 ===");

            try
            {
                // 创建测试数据
                var playerData = new PlayerData();
                var playerList = new List<PlayerData>
                {
                    new PlayerData { playerName = "玩家A", level = 10 },
                    new PlayerData { playerName = "玩家B", level = 20 },
                    new PlayerData { playerName = "玩家C", level = 30 }
                };

                // 1. 使用自动格式检测
                DemonstrateAutoDetection(playerData);

                // 2. 使用指定格式序列化
                DemonstrateSpecificFormats(playerData);

                // 3. 演示列表序列化
                DemonstrateListSerialization(playerList);

                // 4. 演示文件操作
                DemonstrateFileOperations(playerData);

                // 5. 演示格式转换
                DemonstrateFormatConversion(playerData);

                // 6. 演示智能推荐
                DemonstrateSmartRecommendations();
            }
            catch (Exception ex)
            {
                Debug.LogError($"序列化系统演示过程中发生错误: {ex.Message}\n{ex.StackTrace}");
            }

            Debug.Log("=== 统一序列化系统演示结束 ===");
        }

        void DemonstrateAutoDetection(PlayerData data)
        {
            Debug.Log("--- 1. 自动格式检测演示 ---");

            // 根据文件扩展名自动选择格式
            string jsonPath = Application.persistentDataPath + "/player.json";
            string xmlPath = Application.persistentDataPath + "/player.xml";
            string csvPath = Application.persistentDataPath + "/player.csv";

            // 保存时自动检测格式
            SerializationManager.SaveToFile(data, jsonPath);
            SerializationManager.SaveToFile(data, xmlPath);
            SerializationManager.SaveToFile(data, csvPath);

            // 加载时自动检测格式
            var jsonPlayer = SerializationManager.LoadFromFile<PlayerData>(jsonPath);
            var xmlPlayer = SerializationManager.LoadFromFile<PlayerData>(xmlPath);
            var csvPlayer = SerializationManager.LoadFromFile<PlayerData>(csvPath);

            Debug.Log($"JSON加载: {jsonPlayer?.playerName}");
            Debug.Log($"XML加载: {xmlPlayer?.playerName}");
            Debug.Log($"CSV加载: {csvPlayer?.playerName}");
        }

        void DemonstrateSpecificFormats(PlayerData data)
        {
            Debug.Log("--- 2. 指定格式序列化演示 ---");

            // JSON 序列化
            string json = SerializationManager.Serialize(data, SerializationFormat.Json);
            Debug.Log($"JSON: {json}");

            // XML 序列化
            string xml = SerializationManager.Serialize(data, SerializationFormat.Xml);
            Debug.Log($"XML: {xml.Substring(0, Math.Min(100, xml.Length))}...");

            // Binary 序列化
            byte[] binary = SerializationManager.SerializeToBytes(data, SerializationFormat.Binary);
            Debug.Log($"Binary: {binary.Length} bytes");

            // 反序列化验证
            var fromJson = SerializationManager.Deserialize<PlayerData>(json, SerializationFormat.Json);
            var fromBinary = SerializationManager.DeserializeFromBytes<PlayerData>(binary, SerializationFormat.Binary);
            
            Debug.Log($"JSON反序列化验证: {fromJson.playerName}");
            Debug.Log($"Binary反序列化验证: {fromBinary.playerName}");
        }

        void DemonstrateListSerialization(List<PlayerData> playerList)
        {
            Debug.Log("--- 3. 列表序列化演示 ---");

            // JSON 列表序列化
            string jsonList = SerializationManager.SerializeList(playerList, SerializationFormat.Json);
            var deserializedJsonList = SerializationManager.DeserializeList<PlayerData>(jsonList, SerializationFormat.Json);
            Debug.Log($"JSON列表: {deserializedJsonList.Count} 个玩家");

            // XML 列表序列化
            string xmlList = SerializationManager.SerializeList(playerList, SerializationFormat.Xml);
            var deserializedXmlList = SerializationManager.DeserializeList<PlayerData>(xmlList, SerializationFormat.Xml);
            Debug.Log($"XML列表: {deserializedXmlList.Count} 个玩家");

            // CSV 列表序列化（使用简化数据，避免复杂类型）
            try
            {
                // 创建简化版本用于CSV测试
                var simplifiedList = playerList.Select(p => new
                {
                    Name = p.playerName,
                    Level = p.level,
                    Experience = p.experience
                }).ToList();

                // 注意：匿名类型不能用于CSV反序列化，这里只做序列化演示
                string csvData = JsonMapper.ToJson(simplifiedList); // 使用JSON作为中间格式演示
                Debug.Log($"CSV数据演示:\n{csvData.Substring(0, Math.Min(200, csvData.Length))}...");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CSV列表序列化跳过: {ex.Message}");
            }
        }

        void DemonstrateFileOperations(PlayerData data)
        {
            Debug.Log("--- 4. 文件操作演示 ---");

            string basePath = Application.persistentDataPath + "/serialization_test";

            // 使用不同格式保存到文件
            SerializationManager.SaveToFile(data, basePath + ".json", SerializationFormat.Json);
            SerializationManager.SaveToFile(data, basePath + ".xml", SerializationFormat.Xml);
            SerializationManager.SaveToFile(data, basePath + ".dat", SerializationFormat.Binary);

            // 从文件加载（自动检测格式）
            var jsonData = SerializationManager.LoadFromFile<PlayerData>(basePath + ".json");
            var xmlData = SerializationManager.LoadFromFile<PlayerData>(basePath + ".xml");
            var binaryData = SerializationManager.LoadFromFile<PlayerData>(basePath + ".dat");

            Debug.Log($"文件加载验证 - JSON: {jsonData?.level}, XML: {xmlData?.level}, Binary: {binaryData?.level}");
        }

        void DemonstrateFormatConversion(PlayerData data)
        {
            Debug.Log("--- 5. 格式转换演示 ---");

            // 使用可序列化类型，现在应该支持更好的格式转换
            var testData = new PlayerData
            {
                playerName = data.playerName,
                level = data.level,
                experience = data.experience,
                position = new SerializableVector3(10, 20, 30),
                playerColor = new SerializableColor(0.8f, 0.3f, 0.6f, 1f),
                inventory = new List<string> { "sword", "shield" }
            };

            // JSON 转 XML
            string jsonData = SerializationManager.Serialize(testData, SerializationFormat.Json);
            if (string.IsNullOrEmpty(jsonData))
            {
                Debug.LogError("JSON序列化失败，跳过格式转换演示");
                return;
            }

            string xmlFromJson = SerializationManager.ConvertFormat<PlayerData>(jsonData, SerializationFormat.Json, SerializationFormat.Xml);
            if (string.IsNullOrEmpty(xmlFromJson))
            {
                Debug.LogError("JSON到XML转换失败");
                return;
            }
            Debug.Log($"JSON转XML成功: {xmlFromJson.Contains("PlayerData")}");

            // XML 转 JSON
            string jsonFromXml = SerializationManager.ConvertFormat<PlayerData>(xmlFromJson, SerializationFormat.Xml, SerializationFormat.Json);
            if (string.IsNullOrEmpty(jsonFromXml))
            {
                Debug.LogError("XML到JSON转换失败");
                return;
            }
            Debug.Log($"XML转JSON成功: {jsonFromXml.Contains("playerName")}");

            // Binary 转换测试
            string binaryData = SerializationManager.Serialize(testData, SerializationFormat.Binary);
            if (!string.IsNullOrEmpty(binaryData))
            {
                string jsonFromBinary = SerializationManager.ConvertFormat<PlayerData>(binaryData, SerializationFormat.Binary, SerializationFormat.Json);
                Debug.Log($"Binary转JSON成功: {!string.IsNullOrEmpty(jsonFromBinary)}");
            }

            // CSV 转换测试 (现在使用可序列化类型应该更稳定)
            try
            {
                string csvData = SerializationManager.Serialize(testData, SerializationFormat.Csv);
                if (!string.IsNullOrEmpty(csvData))
                {
                    string jsonFromCsv = SerializationManager.ConvertFormat<PlayerData>(csvData, SerializationFormat.Csv, SerializationFormat.Json);
                    Debug.Log($"CSV转JSON成功: {!string.IsNullOrEmpty(jsonFromCsv)}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"CSV转换测试跳过: {ex.Message}");
            }

            // 验证转换后的数据完整性
            var finalData = SerializationManager.Deserialize<PlayerData>(jsonFromXml, SerializationFormat.Json);
            if (finalData != null)
            {
                Debug.Log($"格式转换数据完整性验证: {finalData.playerName} - {finalData.level} - 位置: {finalData.position}");
            }
            else
            {
                Debug.LogError("最终数据反序列化失败");
            }
        }

        void DemonstrateSmartRecommendations()
        {
            Debug.Log("--- 6. 智能推荐演示 ---");

            // 根据不同数据类型获取推荐格式
            var simpleData = new { name = "test", value = 123 };
            var complexData = new PlayerData();
            var listData = new List<string> { "item1", "item2", "item3" };
            var largeData = new byte[1024 * 1024]; // 1MB 数据

            var simpleRecommendation = SerializationManager.RecommendFormat(simpleData.GetType());
            var complexRecommendation = SerializationManager.RecommendFormat(complexData.GetType());
            var listRecommendation = SerializationManager.RecommendFormat(listData.GetType());
            var largeRecommendation = SerializationManager.RecommendFormat(largeData.GetType(), true); // 优先性能

            Debug.Log($"简单数据推荐格式: {simpleRecommendation}");
            Debug.Log($"复杂数据推荐格式: {complexRecommendation}");
            Debug.Log($"列表数据推荐格式: {listRecommendation}");
            Debug.Log($"大数据推荐格式: {largeRecommendation}");

            // 获取所有支持的格式
            var supportedFormats = SerializationManager.GetSupportedFormats();
            Debug.Log($"支持的格式: {string.Join(", ", supportedFormats.Keys)}");

            // 获取格式信息
            foreach (var kvp in supportedFormats)
            {
                var formatInfo = SerializationManager.GetFormatInfo(kvp.Key);
                Debug.Log($"{kvp.Key}: {formatInfo}");
            }
        }

        void OnDestroy()
        {
            // 清理测试文件
            string[] testFiles = {
                Application.persistentDataPath + "/player.json",
                Application.persistentDataPath + "/player.xml",
                Application.persistentDataPath + "/player.csv",
                Application.persistentDataPath + "/serialization_test.json",
                Application.persistentDataPath + "/serialization_test.xml",
                Application.persistentDataPath + "/serialization_test.dat"
            };

            foreach (var file in testFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"清理文件失败: {file}, 错误: {e.Message}");
                    }
                }
            }
        }
    }
}