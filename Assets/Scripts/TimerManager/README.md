# TimerManager 使用说明

## 简介

`TimerManager` 是一个基于 Unity 的高效定时器管理系统，支持多种定时模式（普通、重复、真实时间、帧定时），并提供链式定时器、进度回调、批量操作等高级功能。适用于游戏开发中各种延时、循环、动画、冷却等场景。

---

## 主要特性

- 支持单次、重复、真实时间、帧数等多种定时模式
- 支持带参数、进度回调、上下文回调
- 支持链式定时器任务流，条件与错误处理
- 支持暂停、恢复、销毁、批量操作
- 支持全局时间缩放
- 支持定时器状态查询与调试输出

---

## 快速上手

### 1. 创建基本定时器
```csharp
TimerManager.Instance.AddTimer("timer1", 5f, () => Debug.Log("完成"));
```

### 2. 创建带参数定时器
```csharp
TimerManager.Instance.AddTimer<int>("timer2", 3f, value => Debug.Log($"参数: {value}"), 42);
```

### 3. 创建带进度回调的定时器
```csharp
TimerManager.Instance.AddTimerWithProgress("timer3", 4f, () => Debug.Log("完成"), progress => Debug.Log($"进度: {progress}"));
```

### 4. 创建帧定时器
```csharp
TimerManager.Instance.AddFrameTimer("frameTimer", 60, () => Debug.Log("60帧完成"));
```

### 5. 创建上下文定时器
```csharp
class MyContext : ITimerContext { public void Execute() { Debug.Log("上下文回调"); } }
TimerManager.Instance.AddTimer("timerCtx", 2f, new MyContext());
```

---

## 链式定时器

### 基本链式用法
```csharp
TimerManager.Instance.CreateChain()
    .AddFirst("step1", 2f, () => Debug.Log("第一步"))
    .Then("step2", 3f, () => Debug.Log("第二步"))
    .Then("step3", 1f, () => Debug.Log("第三步"))
    .Run();
```

### 完整链式配置
```csharp
TimerManager.Instance.CreateChain()
    .AddFirst("intro", 5f, ShowIntro)
        .WithMode(TimerMode.Realtime)
    .Then("dialog", 3f, ShowDialog)
        .WithCompensation(false)
    .Then("effect", 2f, PlayEffect)
    .Run();
```

### 条件与错误处理
```csharp
TimerManager.Instance.CreateChain()
    .AddFirst("init", 0.5f, () => Debug.Log("初始化开始"))
    .ThenIf(() => GameState.IsReady, "load", 1f, () => Debug.Log("加载资源"))
    .WithMode(TimerMode.Realtime)
    .Then("spawn", 2f, SpawnEnemies)
    .OnError(err => Debug.LogError($"链式错误: {err}"))
    .Run();
```

### 延迟执行
```csharp
// 延迟2秒后执行
TimerManager.Instance.CreateChain()
    .Delay(2f, () => Debug.Log("2秒后执行"))
    .Run();

// 延迟30帧后执行
TimerManager.Instance.CreateChain()
    .DelayFrames(30, () => Debug.Log("30帧后执行"))
    .Run();
```

---

## 定时器控制与查询

### 暂停/恢复/销毁定时器
```csharp
TimerManager.Instance.PauseTimer("timer1");
TimerManager.Instance.ResumeTimer("timer1");
TimerManager.Instance.DestroyTimer("timer1");
```

### 批量操作
```csharp
TimerManager.Instance.PauseTimersWithPrefix("enemy_");
TimerManager.Instance.ResumeTimersWithPrefix("enemy_");
TimerManager.Instance.DestroyTimersWithPrefix("enemy_");
```

### 清空所有定时器
```csharp
TimerManager.Instance.ClearAllTimers();
```

### 查询定时器状态
```csharp
bool isActive = TimerManager.Instance.IsTimerActive("timer1");
float remaining = TimerManager.Instance.GetTimerRemaining("timer1");
float progress = TimerManager.Instance.GetTimerProgress("timer2");
```

### 打印所有活动定时器
```csharp
TimerManager.Instance.LogActiveTimers();
```

---

## API 参考

### 主要方法
- `AddTimer(key, duration, onComplete, mode, enableCompensation)`
- `AddTimer<T>(key, duration, onComplete, param, mode, enableCompensation)`
- `AddTimerWithProgress(key, duration, onComplete, onProgress, mode, enableCompensation)`
- `AddFrameTimer(key, frameCount, onComplete)`
- `AddTimer(key, duration, ITimerContext, mode, enableCompensation)`
- `PauseTimer(key)` / `ResumeTimer(key)` / `DestroyTimer(key)`
- `PauseTimersWithPrefix(prefix)` / `ResumeTimersWithPrefix(prefix)` / `DestroyTimersWithPrefix(prefix)`
- `ClearAllTimers()`
- `IsTimerActive(key)`
- `GetTimerRemaining(key)`
- `GetTimerProgress(key)`
- `LogActiveTimers()`
- `CreateChain()`

### TimerMode 枚举
- `Normal`：普通定时器
- `Repeat`：重复定时器
- `Realtime`：真实时间（不受 Time.timeScale 影响）
- `FrameBased`：基于帧数

---

## 进阶说明

- 支持全局时间缩放（`TimeScale` 属性）
- 支持链式任务流、条件跳转、错误处理
- 支持进度回调、上下文回调、帧定时
- 内部自动管理定时器生命周期，无需手动清理

---

## 注意事项

- 定时器 key 推荐唯一，重复添加会覆盖旧定时器
- duration 必须大于 0
- 回调函数建议使用 lambda 或方法引用，避免闭包泄漏
- 链式定时器支持条件与错误处理，便于复杂流程编排

---

## 联系与反馈
如有问题或建议，欢迎联系作者或提交 issue。
