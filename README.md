# UnityUtils 项目简介

UnityUtils 是一套面向 Unity 引擎开发者的高效实用工具集，旨在提升开发效率、规范项目结构、简化常见功能实现。项目包含大量常用静态工具方法、组件化设计、状态机、协程管理、事件系统等模块，适用于各类游戏及应用开发场景。

## 上下文（Context ）
- 用于管理应用程序中的全局对象和资源
- 支持类型键和字符串键两种存储方式


## 自动 UI 代码生成工具（UnityUtils/GeneratedUITool）
本工具用于基于 UI 预制体自动生成访问脚本，避免手写查找与拖拽，支持单文件可重生成、序列化引用赋值、命名空间与基类包装、批量工作流等。
- 自动生成 UI 访问脚本（单文件，重复生成仅覆盖标记段，保留 using 与用户代码）
- 支持 UIMark 标记导出目标：组件/RectTransform/GameObject/Auto，支持忽略子级与组件索引
- 自动包含常用控件（Button/Toggle/Slider/InputField/TMP），可扩展包含 ScrollRect/Scrollbar/Dropdown
- 两种“生成与赋值”模式：
方法赋值：生成 public void InitRefs()；把查找逻辑放入该方法，需你在合适时机手动调用
序列化引用：生成 [SerializeField] 字段并在编辑器中写引用（无需运行时查找）
- 支持命名空间包装与自定义基类（默认 MonoBehaviour）
- 统一的命名与属性生成：_camelCase、组件前缀映射、属性名去前缀（可选）


## 反射缓存工具（ReflectionToolkit）

对 反射相关的高频操作（类型查找、MethodInfo/PropertyInfo/字段/特性/委托解析等）提供预加载,缓存,清理等一些列功能,方便之后快速调用
- 可观测：统一统计结构（命中率 / 内存使用 / 使用频率 / 清理结果）
- 可扩展：模块化 ICacheModule，策略化 ICleanupStrategy，可插拔
- 自适应：全局策略组合 + 模块内部 OnSmartCleanup 双层裁剪
- 可维护：统一入口 ReflectionToolkit，分层清晰（Core / Interfaces / Modules / Strategies）
- 可控内存：LRU / 使用频率 / 内存压力 / 定时 / 自适应 多策略叠加


## 协程管理与策略工具（CoroutineManager）

- 支持高性能协程池化，批量管理/命名/分类/自动清理协程
- 丰富的策略模式（如分帧处理、条件等待、链式编排、动画插值等）
- 可与主工程无缝集成，极大提升复杂流程与动画控制的效率与可维护性
- 支持链式流程、进度回调、调试与性能监控等高级用法

## 状态管理系统（StatusManagementSystem）

- 灵活的状态机架构，支持延迟加载、状态切换验证、历史记录、事件监听等
- 内置调试与性能监控工具，便于开发、测试与维护
- 适合角色AI、流程状态、UI状态等多种场景

## 事件系统（EventSystem）

- 统一的全局事件派发与监听机制，支持广播、订阅、反订阅
- 支持强类型/弱类型事件，泛型事件参数，类型安全
- 事件队列、优先级、一次性事件、持久事件等扩展用法
- 适合解耦模块间通信，UI、逻辑、动画、网络等各类场景

## 流式加载器(StreamingAssetsLoader)

流式加载器（StreamingAssetsLoader）是一个针对 Unity 项目 StreamingAssets 目录的资源加载工具。
- 支持同步与异步加载 StreamingAssets 目录下的文件和数据
- 兼容各主流平台（如 Windows、Android、iOS 等）
- 封装了常用的字节流、文本、JSON 等加载方式，简化资源获取流程

## 计时器(TimerManager)

计时器（TimerManager）提供高效灵活的计时器及定时任务管理功能。
- 支持单次/循环定时回调、倒计时、延时执行等
- 支持任务动态暂停、恢复、移除、查询剩余时间
- 性能优良，适合用于 UI 倒计时、技能冷却、动画延迟、定时触发等场景

## 常用工具

- **TweenUtils 补间动画工具类**：实现对象属性的平滑过渡
- **CollisionUtils 碰撞检测工具类**：提供各种碰撞检测算法，便于物理相关开发
- **PathfindingUtils 路径查找工具类**：支持基于 NavMesh 和 A* 算法的路径查找
- **ImageUtils 图像处理工具类**：图片的加载、保存、格式转换、截图等功能
- **AudioUtils 音频处理工具类**、**VideoUtils 视频处理工具类**：便捷实现音频和视频相关操作
- **ClipboardUtils 剪贴板工具类**：实现跨平台剪贴板操作
- **DeviceUtils 设备信息工具类**：获取设备硬件、系统等相关信息
- **RegexUtils 正则表达式工具类**：方便字符串匹配及处理
- **FileUtils 文件工具类**：封装常用文件操作
- **UIUtils UI 工具类**：简化 UI 各类操作，包括显示、动画、交互等
- **DebugLogger 调试工具类**：统一调试日志输出，支持详细日志与异常捕捉
- **RandomUtils、MathUtils、GameObjectUtils**：常用数学运算、随机数生成、游戏对象管理等

## 目录结构预览

```
Assets
/GeneratedUITool             # UI脚本自动生成工具,自动绑定
/Scripts/
  ├─ Base/                   # 基础架构与单例模板
  ├─ Utils/                  # 常用工具类（动画、碰撞、路径、图片、UI等）
  ├─ CoroutineManager/       # 协程管理与策略工具
  ├─ StatusManagementSystem/ # 状态管理系统相关
  ├─ EventSystem/            # 事件系统相关
  ├─ StreamingAssetsLoader/  # 流式加载器相关
  ├─ ReflectionToolkit/      # 反射缓存工具
  ├─ Context /               # 上下文管理
  └─ TimerManager/           # 计时器相关


```

## 使用方式

直接将本工具集源码引入你的项目，在 C# 脚本中按需静态调用相关类和方法即可。详细 API 及用法请参考各工具类源码及注释。  
本工程几乎纯代码,可以选择需要的工具添加到您的工程当中.

----

如有建议或需求欢迎 issue 与 PR！  
项目地址：[zhxx19940127/UnityUtils](https://github.com/zhxx19940127/UnityUtils)
