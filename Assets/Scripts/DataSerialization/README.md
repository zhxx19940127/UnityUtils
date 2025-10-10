# 统一序列化系统架构说明

## 🎯 系统概述

该统一序列化系统采用**策略模式+插件式架构**的设计，支持延迟初始化和自定义策略注册，提供完全可扩展的序列化解决方案。

## 📁 文件结构

```
DataSerializationTools/
├── Base/                                    # 核心接口和基础组件
│   ├── ISerializationStrategy.cs            # 策略接口定义
│   └── SerializableClass.cs                 # Unity类型序列化支持
├── Main/                                    # 主要管理器
│   └── SerializationManager.cs              # 统一管理器(插件式+延迟加载)
├── Strategy/                                # 策略实现
│   ├── BinarySerializationStrategy.cs       # Binary序列化策略
│   ├── JsonSerializationStrategy.cs         # JSON序列化策略 (LitJson)
│   ├── XmlSerializationStrategy.cs          # XML序列化策略
│   └── CsvSerializationStrategy.cs          # CSV序列化策略
├── Example/                                 # 使用示例
│   ├── SerializationExample.cs              # 基础功能演示
│   └── PluginStrategyExample.cs             # 插件策略演示
├── LitJson/                                 # JSON序列化依赖库
│   └── ...                                  # LitJson库文件
└── README.md                                # 本文档
```

## 🔧 核心设计

### 1. 策略模式 (Strategy Pattern)

所有序列化格式都实现统一的 `ISerializationStrategy` 接口：

```csharp
namespace DataSerialization
{
    public interface ISerializationStrategy
    {
        string[] SupportedExtensions { get; }
        string FormatName { get; }
        bool SupportsCompression { get; }
        
        string Serialize(object obj);
        byte[] SerializeToBytes(object obj);
        T Deserialize<T>(string data) where T : new();
        T DeserializeFromBytes<T>(byte[] data) where T : new();
        string SerializeList<T>(IEnumerable<T> list);
        List<T> DeserializeList<T>(string data) where T : new();
        bool SaveToFile(object obj, string filePath);
        T LoadFromFile<T>(string filePath) where T : new();
    }
}
```

### 2. 插件式架构 (Plugin Architecture)

`SerializationManager` 支持动态策略注册和延迟初始化：

```csharp
// 注册策略类型（延迟初始化）
SerializationManager.RegisterStrategy(CustomFormat.Base64, typeof(Base64Strategy));

// 注册策略工厂
SerializationManager.RegisterStrategy(CustomFormat.Encrypted, () => new EncryptedStrategy());

// 注册策略实例（立即初始化）
SerializationManager.RegisterStrategy(CustomFormat.Compressed, compressedInstance);

// 延迟初始化 - 第一次使用时才创建
var strategy = SerializationManager.GetStrategy(CustomFormat.Base64);
```

### 3. 统一API (Unified API)

完全向后兼容的统一接口：

```csharp
// 自动格式检测
SerializationManager.SaveToFile(data, "file.json");
SerializationManager.LoadFromFile<T>("file.xml");

// 指定格式操作
SerializationManager.Serialize(data, SerializationFormat.Json);
SerializationManager.Deserialize<T>(jsonData, SerializationFormat.Json);

// 智能推荐
var format = SerializationManager.RecommendFormat(data.GetType());

// 格式转换
var xml = SerializationManager.ConvertFormat<T>(jsonData, 
    SerializationFormat.Json, SerializationFormat.Xml);
```

## 🚀 支持的格式

| 格式 | 策略类 | 文件扩展名 | 特点 |
|------|--------|------------|------|
| **JSON** | JsonSerializationStrategy | .json, .js | 通用数据交换，Web API |
| **XML** | XmlSerializationStrategy | .xml | 配置文件，复杂结构 |
| **CSV** | CsvSerializationStrategy | .csv, .txt | 表格数据，数据分析 |
| **Binary** | BinarySerializationStrategy | .dat, .bin, .data | 高性能，Unity兼容 |

## 💡 核心特性

### 1. 插件式架构
支持运行时动态注册自定义序列化策略：
```csharp
// 注册自定义策略
SerializationManager.RegisterStrategy(CustomFormat.MyFormat, typeof(MyCustomStrategy));

// 获取已注册格式
var formats = SerializationManager.GetRegisteredFormats();
```

### 2. 延迟初始化
策略实例按需创建，优化内存使用和启动性能：
```csharp
// 第一次使用时才创建实例
## 🚀 支持的格式

| 格式 | 策略类 | 文件扩展名 | 特点 |
|------|--------|------------|------|
| **JSON** | JsonSerializationStrategy | .json, .js | 通用数据交换，Web API |
| **XML** | XmlSerializationStrategy | .xml | 配置文件，复杂结构 |
| **CSV** | CsvSerializationStrategy | .csv, .txt | 表格数据，数据分析 |
| **Binary** | BinarySerializationStrategy | .dat, .bin, .data | 高性能，Unity兼容 |

## 💡 核心特性

### 1. 插件式架构
支持运行时动态注册自定义序列化策略：
```csharp
// 注册自定义策略
manager.RegisterStrategy<CustomStrategy>(SerializationFormat.Custom);

// 获取已注册格式
var formats = manager.GetAvailableFormats();
```

### 2. 延迟初始化
策略实例按需创建，优化内存使用和启动性能：
```csharp
// 第一次使用时才创建实例
var strategy = manager.GetStrategy(SerializationFormat.JSON);

// 可选的预加载优化
manager.PreloadAllStrategies();
```

### 3. 自动格式检测
根据文件扩展名自动选择合适的序列化策略：
```csharp
// 自动检测为JSON格式
manager.SaveToFile(playerData, "save/player.json");
```

### 4. Unity类型完美支持
通过 `SerializableClass.cs` 提供完整的Unity类型序列化：
- 18种Unity类型的可序列化包装
- 隐式转换，使用体验与原生类型一致
- 所有序列化格式完全兼容

## 🛠 架构优势

### 1. **插件式扩展**
- 运行时动态注册策略
- 支持自定义序列化格式
- 完全可扩展的架构设计

### 2. **性能优化**
- 延迟初始化节省内存
- 按需创建策略实例
- 可选的预加载机制

### 3. **统一接口**
- 一套API操作所有格式
- 完全向后兼容
- 保持接口一致性

### 4. **智能化**
- 自动格式检测
- 智能推荐机制
- 无缝格式转换

### 5. **Unity深度集成**
- 完美的Unity类型支持
- Unity路径集成
- Unity控制台日志

## � 使用示例

### 基础使用

```csharp
using DataSerialization;

// 获取序列化管理器实例
var manager = SerializationManager.Instance;

// 创建数据对象
var data = new MyData { Name = "测试", Value = 42 };

// JSON 序列化
string json = manager.Serialize(data, SerializationFormat.JSON);
MyData jsonData = manager.Deserialize<MyData>(json, SerializationFormat.JSON);

// Binary 序列化
byte[] binary = manager.SerializeToBinary(data);
MyData binaryData = manager.DeserializeFromBinary<MyData>(binary);

// XML 序列化
string xml = manager.Serialize(data, SerializationFormat.XML);
MyData xmlData = manager.Deserialize<MyData>(xml, SerializationFormat.XML);
```

### 文件操作

```csharp
// 保存到文件
manager.SaveToFile(data, "data.json", SerializationFormat.JSON);
manager.SaveToFile(data, "data.xml", SerializationFormat.XML);
manager.SaveToBinaryFile(data, "data.dat");

// 从文件加载
var loadedData = manager.LoadFromFile<MyData>("data.json", SerializationFormat.JSON);
var loadedXmlData = manager.LoadFromFile<MyData>("data.xml", SerializationFormat.XML);
var loadedBinaryData = manager.LoadFromBinaryFile<MyData>("data.dat");
```

### 插件式策略注册

```csharp
// 方式1: 通过类型注册
manager.RegisterStrategy<CustomSerializationStrategy>(SerializationFormat.Custom);

// 方式2: 通过工厂函数注册
manager.RegisterStrategy(SerializationFormat.Advanced, () => new AdvancedStrategy(config));

// 方式3: 通过实例注册
var strategy = new CustomStrategy();
manager.RegisterStrategy(SerializationFormat.Custom, strategy);

// 使用自定义策略
string result = manager.Serialize(data, SerializationFormat.Custom);
```

### Unity类型序列化

```csharp
using UnityEngine;
using DataSerialization;

// Unity类型包装
var vector3Data = new SerializableVector3(new Vector3(1, 2, 3));
var colorData = new SerializableColor(Color.red);
var quaternionData = new SerializableQuaternion(Quaternion.identity);

// 序列化Unity类型
string json = manager.Serialize(vector3Data, SerializationFormat.JSON);
var deserializedVector = manager.Deserialize<SerializableVector3>(json, SerializationFormat.JSON);

// 隐式转换
Vector3 originalVector = deserializedVector; // 自动转换回Unity类型
```

### 高级功能

```csharp
// 预加载所有策略(可选优化)
manager.PreloadAllStrategies();

// 清理未使用的策略
manager.Cleanup();

// 检查策略是否已注册
bool hasJson = manager.HasStrategy(SerializationFormat.JSON);

// 获取所有可用格式
var formats = manager.GetAvailableFormats();
```

## 🔄 重构变更

### 移除的组件
- ❌ `BinaryHelper/` 文件夹及所有文件
- ❌ `JsonHelper/` 文件夹及所有文件  
- ❌ `XmlHelper/` 文件夹及所有文件
- ❌ `CsvHelper/` 文件夹及所有文件
- ❌ `YamlSerializationStrategy` (已删除YAML支持)

### 集成到策略
- ✅ BinaryHelper → BinarySerializationStrategy (直接集成)
- ✅ JsonHelper → JsonSerializationStrategy (LitJson集成)
- ✅ XmlHelper → XmlSerializationStrategy (直接集成)
- ✅ CsvHelper → CsvSerializationStrategy (完整移植)

### 架构升级
- ✅ 插件式动态注册系统
- ✅ 延迟初始化优化
- ✅ 统一 DataSerialization 命名空间
- ✅ 完整的Unity类型支持
- ✅ 三种策略注册方式

### 文件结构重组
```
DataSerializationTools/
├── Base/                    # 基础接口和类型
│   ├── ISerializationStrategy.cs
│   └── SerializableClass.cs
├── Main/                    # 核心管理器
│   └── SerializationManager.cs
├── Strategy/               # 策略实现
│   ├── BinarySerializationStrategy.cs
│   ├── JsonSerializationStrategy.cs
│   ├── XmlSerializationStrategy.cs
│   └── CsvSerializationStrategy.cs
├── Example/               # 示例代码
│   └── SerializationExample.cs
└── LitJson/              # JSON依赖库
    └── [LitJson源码文件]
```

## 📋 依赖库

- **LitJson**: JSON序列化 (JsonSerializationStrategy)
- **System.Xml**: XML序列化 (XmlSerializationStrategy)
- **System.Runtime.Serialization**: Binary序列化 (BinarySerializationStrategy)

## 🎮 Unity集成

该系统完全兼容Unity，支持：
- Unity路径 (persistentDataPath, streamingAssetsPath)
- Unity类型序列化 (Vector3, Quaternion等)
- Unity控制台日志
- Unity特性系统

### Unity类型兼容性

#### 推荐方案: 可序列化包装类型
使用 `SerializableClass.cs` 中提供的包装类型获得最佳兼容性：

```csharp
[System.Serializable]
public class PlayerData
{
    public string name;
    public SerializableVector3 position;      // 替代 Vector3
    public SerializableQuaternion rotation;   // 替代 Quaternion
    public SerializableColor color;          // 替代 Color
    public SerializableRect bounds;          // 替代 Rect
}

// 自动转换，使用时无需手动转换
Vector3 worldPos = playerData.position;  // 隐式转换
playerData.position = transform.position; // 隐式转换
```

**优势**：
- ✅ **所有格式兼容**: JSON、XML、Binary、CSV全部支持
- ✅ **真正的二进制序列化**: 无需回退机制，性能最佳
- ✅ **类型安全**: 保持原有Unity类型的所有特性
- ✅ **代码简洁**: 隐式转换，使用体验与原生类型一致

#### 备选方案: 自动回退机制 (已移除)
~~之前的版本在Binary策略中包含JSON回退机制，现在已简化~~

支持的可序列化类型：
- `SerializableVector2/3/4`, `SerializableVector2Int/3Int`
- `SerializableQuaternion`, `SerializableColor/Color32`
- `SerializableRect/RectInt`, `SerializableBounds/BoundsInt`
- `SerializableMatrix4x4`, `SerializableAnimationCurve`
- `SerializableGradient`, `SerializableKeyframe`

### 示例组件
- `SerializationExample`: 基础功能演示
- `SerializableTypesExample`: Unity类型序列化专门演示