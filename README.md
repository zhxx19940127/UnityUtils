# UnityUtils 项目简介

UnityUtils 是一套面向 Unity 引擎开发者的高效实用工具集，旨在提升开发效率、规范项目结构、简化常见功能实现。工具类均采用静态方法设计，开箱即用，覆盖了动画、碰撞、路径查找、图像处理、音视频、设备信息、正则、文件、UI、数学、随机等多个开发场景。

## 主要功能与工具类

- **TweenUtils 补间动画工具类**：实现对象属性的平滑过渡
- **CollisionUtils 碰撞检测工具类**：提供多种碰撞检测算法，助力物理开发
- **PathfindingUtils 路径查找工具类**：支持基于 NavMesh 和 A* 的路径查找
- **ImageUtils 图像处理工具类**：图片加载、保存、格式转换、截图等功能
- **AudioUtils 音频处理工具类**、**VideoUtils 视频处理工具类**：音视频文件的便捷处理
- **ClipboardUtils 剪贴板工具类**：跨平台剪贴板操作
- **DeviceUtils 设备信息工具类**：快速获取设备系统/硬件信息
- **RegexUtils 正则表达式工具类**：简化字符串匹配与处理
- **FileUtils 文件工具类**：封装常用文件操作
- **UIUtils UI 工具类**：UI 显示、交互、动画等便捷接口
- **DebugLogger 调试工具类**：统一调试日志输出，支持详细日志与异常捕捉
- **CoroutineUtils 携程工具类**：让协程使用更优雅
- **RandomUtils 随机工具类**、**MathUtils 数学工具类**、**GameObjectUtils 游戏对象工具类**：常用数学、随机、对象管理方法

此外还包含基础架构类（如单例模板）、状态管理系统相关的接口与调试工具，适用于中大型 Unity 项目快速集成与扩展。

## 目录结构预览

```
Assets/Scripts/
  ├─ Base/         # 基础架构与单例模板
  ├─ Utils/        # 各类工具类（动画、碰撞、路径、图片、UI等）
  └─ StatusManagementSystem/ # 状态管理系统相关
```

## 使用方式

直接将本工具集源码引入你的项目，在 C# 脚本中按需静态调用相关类和方法即可。详细 API 及用法请参考各工具类源码及注释。

----

如有建议或需求欢迎 issue 与 PR！  
项目地址：[zhxx19940127/UnityUtils](https://github.com/zhxx19940127/UnityUtils)