# StreamingAssetsLoader 项目文档

## 项目简介

StreamingAssetsLoader 是一个用于 Unity 的资源加载工具，旨在简化和统一 StreamingAssets 目录下各类资源（如音频、图片、文本、二进制文件等）的加载流程。通过策略模式，支持灵活扩展不同类型资源的加载方式。

## 目录结构说明

```
Base/
    IStreamingAssetsLoaderStrategy.cs         # 加载策略接口定义
    WebRequestLoader.cs                      # 基于WebRequest的通用加载器
Entrance/
    StreamingAssetsLoader.cs                 # 资源加载入口与管理器
Strategy/
    AudioLoaderStrategy.cs                   # 音频资源加载策略
    BinaryLoaderStrategy.cs                  # 二进制资源加载策略
    ImageLoaderStrategy.cs                   # 图片资源加载策略
    TextLoaderStrategy.cs                    # 文本资源加载策略
```

## 主要脚本/接口功能描述

### Base/IStreamingAssetsLoaderStrategy.cs
定义了资源加载策略的接口，所有具体的资源加载策略需实现该接口，确保统一的加载方法。

### Base/WebRequestLoader.cs
实现了基于 UnityWebRequest 的通用资源加载逻辑，供各类具体策略复用。

### Entrance/StreamingAssetsLoader.cs
资源加载的统一入口，负责根据资源类型选择合适的加载策略，并对外提供简洁的加载 API。

### Strategy/AudioLoaderStrategy.cs
实现音频文件的加载策略，支持如 .mp3、.wav 等格式。

### Strategy/BinaryLoaderStrategy.cs
实现二进制文件的加载策略，适用于自定义数据、配置等二进制内容。

### Strategy/ImageLoaderStrategy.cs
实现图片文件的加载策略，支持如 .png、.jpg 等格式。

### Strategy/TextLoaderStrategy.cs
实现文本文件的加载策略，支持如 .txt、.json 等格式。

## 使用方法

1. 将本项目文件夹（Base、Entrance、Strategy）拷贝到你的 Unity 项目 `Assets/Scripts/` 目录下。
2. 在代码中通过 `StreamingAssetsLoader` 提供的 API 加载资源。例如：

```csharp
using Entrance;
...
StreamingAssetsLoader.Instance.Load<AudioClip>("audio/bgm.mp3", (clip) => {
    // 使用加载到的音频clip
});
```

3. 支持异步加载和回调，具体API请参考 `StreamingAssetsLoader.cs`。

## 扩展说明：自定义Loader策略

1. 新建策略类，实现 `IStreamingAssetsLoaderStrategy` 接口。
2. 在 `StreamingAssetsLoader` 中注册你的自定义策略。
3. 调用时指定资源类型，系统会自动选择对应策略。

示例：
```csharp
public class CustomLoaderStrategy : IStreamingAssetsLoaderStrategy {
    // 实现接口方法
}
...
StreamingAssetsLoader.Instance.RegisterStrategy<CustomType>(new CustomLoaderStrategy());
```

## 其他说明

- 支持多种资源类型的灵活扩展。
- 推荐在 Unity 2019 及以上版本使用。
- 如需支持更多格式或特殊需求，可参考现有策略实现自定义扩展。

---

如有问题或建议，欢迎反馈与交流。
