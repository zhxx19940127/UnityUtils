# CoroutineManager 功能说明文档

## 目录
- [简介](#简介)
- [文件结构](#文件结构)
- [核心特性](#核心特性)
- [快速上手](#快速上手)
- [API 说明](#api-说明)
- [高级用法](#高级用法)
- [调试与监控](#调试与监控)
- [最佳实践](#最佳实践)
- [常见问题](#常见问题)

---

## 简介

`CoroutineManager` 是一个基于策略模式的 Unity 高级协程管理器，支持协程池化、链式调用、分类管理、调试监控等功能，极大提升协程的可维护性和性能。

---

## 文件结构

```
CoroutineManager/
├── CoroutineStrategy/
│   ├── Base/
│   │   ├── ICoroutineStrategy.cs
│   │   └── CoroutineStrategyBase.cs
│   ├── Strategy/
│   │   ├── DelayedActionStrategy.cs
│   │   ├── DelayedFrameActionStrategy.cs
│   │   ├── RepeatActionStrategy.cs
│   │   ├── ExecuteWhenStrategy.cs
│   │   ├── LerpValueStrategy.cs
│   │   ├── LerpVector3Strategy.cs
│   │   ├── LerpColorStrategy.cs
│   │   ├── ShakeStrategy.cs
│   │   ├── ProcessOverFramesStrategy.cs
│   │   ├── ProcessWithProgressStrategy.cs
│   │   ├── WaitForSecondsStrategy.cs
│   │   ├── WaitUntilStrategy.cs
│   │   ├── CustomCoroutineStrategy.cs
│   │   └── ChainExecutionStrategy.cs
│   └── StrategyFactory/
│       └── StrategyFactory.cs
├── Entrance/
│   ├── CoroutineManager.cs
│   └── CoroutineChain.cs
└── Example/
    └── StrategyCoroutineDemo.cs
```

---

## 核心特性

- **策略模式**：所有协程通过策略对象统一管理，易于扩展和维护。
- **协程池化**：自动复用包装器，减少GC压力。
- **链式调用**：支持流畅的 Then/Parallel/Repeat API，轻松实现复杂动画和流程。
- **分类与命名**：协程可按名称和分类批量管理。
- **调试监控**：内置日志、统计、池状态监控。
- **丰富内置策略**：延迟、重复、插值、分帧、条件等待、震动等。
- **自定义扩展**：支持自定义策略和自定义协程包装。

---

## 快速上手

### 1. 启用调试模式（开发阶段建议开启）
```csharp
CoroutineManager.SetDebugMode(true);
```

### 2. 延迟执行
```csharp
CoroutineManager.DelayedAction(() => Debug.Log("2秒后执行"), 2f);
```

### 3. 重复执行
```csharp
CoroutineManager.RepeatAction(() => Debug.Log("每秒执行"), 1f, 5);
```

### 4. 插值动画
```csharp
CoroutineManager.LerpValue(0f, 100f, 2f, value => {
    progressBar.value = value;
});
```

### 5. 链式调用
```csharp
CoroutineChain.Create()
    .ThenDelay(1f)
    .ThenDo(() => Debug.Log("动画开始"))
    .ThenLerpVector3(start, end, 2f, pos => obj.position = pos)
    .ThenShake(obj, 1f)
    .ThenDo(() => Debug.Log("动画结束"))
    .Start("MyChain");
```

---

## API 说明

### 快捷方法
| 方法 | 说明 |
|------|------|
| DelayedAction | 延迟执行一次动作 |
| DelayedFrameAction | 延迟N帧执行动作 |
| RepeatAction | 按间隔重复执行动作 |
| LerpValue | 浮点数插值动画 |
| LerpVector3 | 向量插值动画 |
| LerpColor | 颜色插值动画 |
| Shake | 震动效果 |
| ExecuteWhen | 条件满足时执行 |
| WaitForSeconds | 等待指定时间后回调 |
| WaitUntil | 等待条件成立后回调 |
| ProcessOverFrames | 分帧处理集合数据 |
| Custom | 包装自定义协程 |

### 管理与控制
| 方法 | 说明 |
|------|------|
| StopManagedCoroutine | 停止指定协程（按名/对象）|
| StopCoroutinesByCategory | 停止某分类下所有协程 |
| StopAllManagedCoroutines | 停止所有托管协程 |
| HasCoroutine | 检查协程是否存在 |
| GetRunningCoroutineNames | 获取所有运行中协程名 |
| GetCoroutineStats | 获取统计信息 |
| LogPoolStatus | 打印池详细状态 |
| ClearCoroutinePool | 清理池 |
| ResetManager | 重置管理器 |

### 缓动函数
- Linear, EaseInQuad, EaseOutQuad, EaseInOutQuad, EaseInCubic, EaseOutCubic, EaseInOutCubic, EaseInSine, EaseOutSine, EaseInOutSine, EaseOutBounce

### 内置策略类型说明

| 策略类名                  | 主要功能与用途                                                                 |
|---------------------------|------------------------------------------------------------------------------|
| DelayedActionStrategy     | 延迟指定秒数后执行一次指定动作，常用于定时触发、延迟提示等                   |
| DelayedFrameActionStrategy| 延迟指定帧数后执行一次动作，适合帧同步、动画帧控制等                         |
| RepeatActionStrategy      | 按指定间隔重复执行动作，可设置次数，常用于周期性任务、心跳、轮询等           |
| LerpValueStrategy         | 在指定时间内对 float 数值做插值动画，适合进度条、属性渐变等                   |
| LerpVector3Strategy       | 在指定时间内对 Vector3 做插值动画，常用于物体移动、平滑过渡等                 |
| LerpColorStrategy         | 在指定时间内对 Color 做插值动画，常用于UI/材质颜色渐变                       |
| ShakeStrategy             | 让目标对象在指定时间内产生震动效果，常用于相机/物体震屏、特效等              |
| ExecuteWhenStrategy       | 条件满足时执行一次动作，可设置超时时间，适合异步等待、状态切换等             |
| WaitForSecondsStrategy    | 单纯等待指定秒数后回调，常用于流程控制、冷却等待等                           |
| WaitUntilStrategy         | 等待条件成立后回调，支持取消和检查间隔，适合异步加载、状态等待等             |
| ProcessOverFramesStrategy | 将大批量数据分帧处理，避免主线程卡顿，适合批量初始化、数据导入等             |
| ProcessWithProgressStrategy| 分帧处理并带进度回调，适合需要进度反馈的大数据处理                          |
| CustomCoroutineStrategy   | 包装自定义 IEnumerator 协程，适合特殊流程和自定义逻辑                        |
| ChainExecutionStrategy    | 支持链式步骤组合与执行，适合复杂动画、流程编排、任务序列                     |

---

## 高级用法

### 1. 条件等待与回调
```csharp
CoroutineManager.WaitUntil(
    () => isReady,
    () => Debug.Log("条件满足，继续执行")
);
```

### 2. 分帧处理大数据
```csharp
CoroutineManager.ProcessOverFrames(bigList, item => Process(item), 50, () => Debug.Log("全部处理完毕"));
```

### 3. 自定义策略
```csharp
public class MyStrategy : CoroutineStrategyBase
{
    public override IEnumerator CreateCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("自定义任务完成");
    }
}
CoroutineManager.StartCoroutine(new MyStrategy());
```

---

## 调试与监控

- 启用调试：`CoroutineManager.SetDebugMode(true);`
- 查看池状态：`CoroutineManager.LogPoolStatus();`
- 获取统计：`var stats = CoroutineManager.GetCoroutineStats();`
- 分类管理：`CoroutineManager.StopCoroutinesByCategory("UI");`

---

## 最佳实践

- 重要协程请命名，便于调试和控制
- 分类管理可批量停止相关协程
- 频繁创建建议开启池化（默认开启）
- 生产环境关闭调试模式
- OnDestroy/场景切换时主动清理协程

---

## 常见问题

| 问题 | 解决建议 |
|------|----------|
| 协程未执行 | 检查CoroutineRunner是否存在，GameObject未被销毁 |
| 性能下降 | 检查协程数量，合理使用池化与分帧 |
| 内存泄漏 | 定期清理池和停止无用协程 |
| 日志过多 | 关闭调试模式 |

---

**CoroutineManager** —— 让Unity协程管理更高效、更优雅！
