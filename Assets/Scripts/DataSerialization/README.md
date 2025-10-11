# 数据序列化系统（纯序列化/反序列化）

一个基于策略模式、可扩展的 Unity 数据序列化框架。系统专注“数据格式转换”，彻底移除了文件读写能力，可与任意存储/网络层自由组合。

- 支持格式：JSON / XML / Binary / CSV
- 统一入口：`SerializationManager`
- 单一职责：仅序列化与反序列化，不做文件 I/O
- 可扩展：运行时注册自定义策略，支持延迟初始化

---

## 目录结构

```
DataSerializationTools/
├── Base/
│   ├── ISerializationStrategy.cs      # 策略接口（纯序列化，无文件I/O）
│   └── SerializableClass.cs           # Unity 类型可序列化包装
├── Main/
│   └── SerializationManager.cs        # 统一管理器（注册/获取/统一API）
├── Strategy/
│   ├── BinarySerializationStrategy.cs # Binary
│   ├── JsonSerializationStrategy.cs   # JSON (LitJson)
│   ├── XmlSerializationStrategy.cs    # XML
│   └── CsvSerializationStrategy.cs    # CSV
└── Example/
    └── SerializationExample.cs        # 演示脚本（仅序列化/反序列化）
```

提示：文件保存/加载请使用你自己的存储层（如 WebGL 的 PlayerPrefs/IndexedDB、本地 File、或网络接口），本系统不包含任何文件 I/O。

---

## 快速开始

```csharp
using DataSerialization;

// 1) 获取策略
var jsonStrategy = SerializationManager.GetStrategy(SerializationFormat.Json);

// 2) 序列化/反序列化对象
string json = jsonStrategy.Serialize(myObject);
var obj = jsonStrategy.Deserialize<MyClass>(json);

// 3) 二进制
byte[] bin = SerializationManager
    .GetStrategy(SerializationFormat.Binary)
    .SerializeToBytes(myObject);
var obj2 = SerializationManager
    .GetStrategy(SerializationFormat.Binary)
    .DeserializeFromBytes<MyClass>(bin);

// 4) 列表
string csv = SerializationManager
    .GetStrategy(SerializationFormat.Csv)
    .SerializeList(list);
var list2 = SerializationManager
    .GetStrategy(SerializationFormat.Csv)
    .DeserializeList<MyType>(csv);
```

---

## API 摘要

### ISerializationStrategy

```csharp
public interface ISerializationStrategy
{
    // 基本
    string Serialize(object obj);
    T Deserialize<T>(string data) where T : new();

    // 字节数组
    byte[] SerializeToBytes(object obj);
    T DeserializeFromBytes<T>(byte[] data) where T : new();

    // 列表
    string SerializeList<T>(IEnumerable<T> list);
    List<T> DeserializeList<T>(string data) where T : new();

    // 元信息
    string[] SupportedExtensions { get; }
    string FormatName { get; }
    bool SupportsCompression { get; }
}
```

### SerializationManager（节选）

- 获取策略：`GetStrategy(SerializationFormat format)`
- 根据扩展名选择：`GetStrategyByExtension(string filePath)`
- 便捷方法：`Serialize/Deserialize`、`SerializeToBytes/DeserializeFromBytes`、`SerializeList/DeserializeList`
- 策略注册：`RegisterStrategy(format, Type|Func|Instance)`、`UnregisterStrategy(format)`、`PreloadAllStrategies()`

---

## 支持的格式

- JSON：人类可读/跨平台/调试友好
- XML：结构化/自描述/标准化
- Binary：体积小/速度快/支持复杂对象
- CSV：表格/Excel 友好/扁平数据

建议：开发期推荐 JSON；性能敏感数据推荐 Binary；表格数据使用 CSV。

---

## Unity 类型支持

在 `SerializableClass.cs` 中提供常见 Unity 类型的可序列化包装，并支持隐式转换（Vector2/3、Quaternion、Color、Rect、Bounds
等），可直接在数据类中使用这些包装类型以获得 JSON/XML 的兼容性。

```csharp
[Serializable]
public class PlayerData
{
    public SerializableVector3 position = new Vector3(10, 20, 30);
    public SerializableColor color = Color.red;
}
```

---

## 使用示例（节选自 `SerializationExample.cs`）

```csharp
// 1. 指定格式序列化
string json = SerializationManager.Serialize(data, SerializationFormat.Json);
string xml  = SerializationManager.Serialize(data, SerializationFormat.Xml);
byte[] bin  = SerializationManager.SerializeToBytes(data, SerializationFormat.Binary);

// 2. 反序列化
var a = SerializationManager.Deserialize<MyType>(json, SerializationFormat.Json);
var b = SerializationManager.DeserializeFromBytes<MyType>(bin, SerializationFormat.Binary);

// 3. 列表
string jsonList = SerializationManager.SerializeList(list, SerializationFormat.Json);
var listBack = SerializationManager.DeserializeList<MyType>(jsonList, SerializationFormat.Json);

// 4. 格式转换
string xmlFromJson = SerializationManager.ConvertFormat<MyType>(json, SerializationFormat.Json, SerializationFormat.Xml);
```

---

## 扩展自定义策略

```csharp
using System.Text;
using DataSerialization;

public class CustomStrategy : ISerializationStrategy
{
    public string[] SupportedExtensions => new[] { ".custom" };
    public string FormatName => "Custom";
    public bool SupportsCompression => false;

    public string Serialize(object obj) => "custom-data";
    public T Deserialize<T>(string data) where T : new() => new T();
    public byte[] SerializeToBytes(object obj) => Encoding.UTF8.GetBytes(Serialize(obj));
    public T DeserializeFromBytes<T>(byte[] data) where T : new() => Deserialize<T>(Encoding.UTF8.GetString(data));
    public string SerializeList<T>(IEnumerable<T> list) => Serialize(list);
    public List<T> DeserializeList<T>(string data) where T : new() => new List<T>();
}

// 注册
SerializationManager.RegisterStrategy(SerializationFormat.Json, typeof(JsonSerializationStrategy));
SerializationManager.RegisterStrategy(SerializationFormat.Xml, () => new XmlSerializationStrategy());
SerializationManager.RegisterStrategy(SerializationFormat.Binary, new BinarySerializationStrategy());
```

---

## 最佳实践

- 数据类提供无参构造函数，初始化集合字段
- JSON/XML 下避免直接用 Unity 原生类型，改用可序列化包装
- 大数据优先 Binary；表格数据优先 CSV
- 与文件/网络结合时请在上层组合（例如 PlayerPrefs、File、UnityWebRequest）
- 启动时可 `PreloadAllStrategies()` 以避免首帧延迟

---

## 常见问题（FAQ）

### Q1: 为什么移除了文件操作?

**A**: 为了遵循单一职责原则。序列化系统只负责数据格式转换,文件I/O由专门的存储管理器处理。这样设计的好处:

- 更灵活:序列化数据可以保存到文件、网络、数据库等任何地方
- 更易测试:不依赖文件系统
- 更好维护:职责清晰,易于扩展

### Q2: WebGL 平台如何保存数据?

**A**: WebGL 不支持传统文件系统,可以使用:

1. `WebGLFileHelper` - 使用 PlayerPrefs 模拟文件系统
2. `PlayerPrefs` - 直接存储序列化后的字符串
3. `IndexedDB` - 通过 JavaScript 互操作
4. 服务器存储 - 通过网络API

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
    PlayerPrefs.SetString("data", json);
#else
    File.WriteAllText("data.json", json);
#endif
```

### Q3: 如何处理 Unity 原生类型 (Vector3/Color)?

**A**: 使用系统提供的可序列化替代类型:

```csharp
// ❌ 不推荐 (JSON/XML不支持)
public Vector3 position;

// ✅ 推荐
public SerializableVector3 position;

// 自动转换
Vector3 unityPos = new Vector3(1, 2, 3);
SerializableVector3 serializable = unityPos; // 隐式转换
```

### Q4: 性能如何选择格式?

**A**: 性能对比 (相同数据):

| 格式     | 序列化速度 | 体积    | 可读性    |
|--------|-------|-------|--------|
| Binary | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ❌      |
| JSON   | ⭐⭐⭐⭐  | ⭐⭐⭐   | ✅      |
| XML    | ⭐⭐⭐   | ⭐⭐    | ✅      |
| CSV    | ⭐⭐⭐⭐  | ⭐⭐⭐⭐  | ✅ (表格) |

**建议**: 开发阶段用JSON(调试方便),发布版本用Binary(性能优)。

### Q5: 如何处理大量数据?

**A**: 对于大量数据,建议:

```csharp
// 1. 使用 Binary 格式
var strategy = SerializationManager.GetStrategy(SerializationFormat.Binary);

// 2. 分批处理
const int batchSize = 100;
for (int i = 0; i < largeList.Count; i += batchSize)
{
    var batch = largeList.GetRange(i, Math.Min(batchSize, largeList.Count - i));
    byte[] data = strategy.SerializeToBytes(batch);
    SaveBatch(i / batchSize, data);
}

// 3. 使用压缩 (如果策略支持)
if (strategy.SupportsCompression)
{
    // 实现压缩逻辑
}
```

---


---

## 更新日志

v2.0.0（2025-10-11）

- 重构：移除文件 I/O，专注序列化/反序列化
- 新增：延迟初始化与插件式策略注册
- 新增：格式转换便捷方法
- 清理：移除 Excel 相关描述（CSV 负责表格场景）

---




---


