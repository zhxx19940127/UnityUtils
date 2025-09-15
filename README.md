# UnityUtils 项目简介

UnityUtils 是一套面向 Unity 引擎开发者的高效实用工具集，旨在提升开发效率、规范项目结构、简化常见功能实现。项目包含大量常用静态工具方法、模块化架构、状态机和高级协程管理系统，适用于中大型 Unity 项目快速集成与扩展。

## 协程管理与策略工具（CoroutineManager）

- 支持高性能协程池化，批量管理/命名/分类/自动清理协程
- 丰富的策略模式（如分帧处理、条件等待、链式编排、动画插值等）
- 可与主工程无缝集成，极大提升复杂流程与动画控制的效率与可维护性
- 支持链式流程、进度回调、调试与性能监控等高级用法

## 状态管理系统（StatusManagementSystem）

- 灵活的状态机架构，支持延迟加载、状态切换验证、历史记录、事件监听等
- 内置调试与性能监控工具，便于开发、测试与维护
- 适合角色AI、流程状态、UI状态等多种场景

## 常用工具（Utils）

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
- **RandomUtils、MathUtils、GameObjectUtils、**：常用数学运算、随机数生成、游戏对象管理 等

## 目录结构预览

```
Assets/Scripts/
  ├─ Base/                   # 基础架构与单例模板
  ├─ Utils/                  # 常用工具类（动画、碰撞、路径、图片、UI等）
  ├─ CoroutineManager/       # 协程管理与策略工具
  └─ StatusManagementSystem/ # 状态管理系统相关
```

## 使用方式

直接将本工具集源码引入你的项目，在 C# 脚本中按需静态调用相关类和方法即可。详细 API 及用法请参考各工具类源码及注释。
本工程几乎纯代码,可以选择选用的工具添加到您的工程当中

----

如有建议或需求欢迎 issue 与 PR！  
项目地址：[zhxx19940127/UnityUtils](https://github.com/zhxx19940127/UnityUtils)
