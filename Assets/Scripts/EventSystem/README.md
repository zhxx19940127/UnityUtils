# EventSystem 详细文档

本目录下包含用于 Unity 项目的事件系统相关脚本，主要用于消息分发、事件监听与辅助功能。以下为各文件详细说明：

---

## 1. IMessageData.cs

```csharp
public interface IMessageData
{
}
```

### 说明

- **IMessageData**：事件消息数据的标记接口。
- 用于类型约束，所有自定义消息数据类型需实现该接口。

---

## 2. Message.cs

### 主要功能

- 提供全局消息分发、注册、注销机制。
- 支持通过字符串 tag 或实现 IMessageData 的类型进行消息分发。
- 支持按对象、类型、tag 进行事件方法的管理。

### 主要成员

- `Dictionary<Type, List<MessageEvent>> _classType2Methods`：类型到事件方法的映射。
- `Dictionary<object, List<MessageEvent>> _subscribeInstance2Methods`：对象到事件方法的映射。
- `Dictionary<string, List<MessageEvent>> _subscribeTag2Methods`：tag 到事件方法的映射。
- `Dictionary<Type, string> _type2Tag`：类型到 tag 的映射。
- `List<string> Filterate`：用于过滤日志输出的消息名列表（仅编辑器下）。
- `static Message DefaultEvent`：单例访问。

### 主要方法

- `void Post(string tag, params object[] parameters)`
  - 通过 tag 广播消息到所有监听方法。
- `void Post<T>(T inMessageData) where T : class, IMessageData`
  - 通过消息数据类型广播消息。
- `void Unregister<T>(T val) where T : class`
  - 注销某对象注册的所有事件。

### 其他说明

- 支持在 Unity 编辑器下通过 `MessageHelper.GetFilterMessageName()` 过滤部分消息日志。
- 事件方法的注册与调用细节未在片段中完全展示，建议查阅完整源码。

---

## 3. MessageHelper.cs

```csharp
public static class MessageHelper
{
    public static List<string> GetFilterMessageName()
    {
        var t = UnityEngine.Resources.Load<TextAsset>("MessageFilter");
        if (t == null)
        {
            return new List<string>();
        }
        string s = t.text;
        var allFilter = new List<string>();
        var ss = s.Split(',');
        for (int i = 0; i < ss.Length; i++)
        {
            allFilter.Add(ss[i]);
        }
        return allFilter;
    }
}
```

### 说明

- **MessageHelper**：消息系统辅助类。
- `GetFilterMessageName()`：从 Resources 目录下加载 `MessageFilter` 文本资源，并以逗号分割，返回过滤消息名列表。
- 主要用于编辑器下消息日志的过滤。

---

## 目录结构

- `IMessageData.cs`：消息数据接口。
- `Message.cs`：消息分发与管理核心。
- `MessageHelper.cs`：消息过滤辅助。

---

## 使用建议

1. 定义消息数据类型时实现 `IMessageData` 接口。
2. 通过 `Message.DefaultEvent.Post(tag, params)` 或 `Message.DefaultEvent.Post<T>(T data)` 进行消息广播。
3. 如需过滤部分消息日志，在 Resources 目录下创建 `MessageFilter.txt`，内容为逗号分隔的消息名。

---

## 示例代码

### 1. 定义自定义消息类型

```csharp
public class MyMessageData : IMessageData
{
  public int Value;
  public string Info;
}
```

### 2. 注册监听

```csharp
// 假设在某个 MonoBehaviour 脚本中
void OnEnable()
{
  Message.DefaultEvent.Register<MyMessageData>(OnMyMessage);
}

void OnDisable()
{
  Message.DefaultEvent.Unregister<MyMessageData>(OnMyMessage);
}

void OnMyMessage(MyMessageData data)
{
  Debug.Log($"收到消息: Value={data.Value}, Info={data.Info}");
}
```

### 3. 发送消息

```csharp
// 发送自定义消息
var msg = new MyMessageData { Value = 42, Info = "Hello" };
Message.DefaultEvent.Post(msg);

// 或通过字符串 tag 发送
Message.DefaultEvent.Post("MyTag", 123, "abc");
```

### 4. 注销监听

```csharp
// 通常在 OnDisable 或销毁时注销
Message.DefaultEvent.Unregister<MyMessageData>(OnMyMessage);
```

---
