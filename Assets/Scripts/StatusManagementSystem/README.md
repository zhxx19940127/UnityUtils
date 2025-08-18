# 状态管理系统常见运行错误及解决方案

## 已修复的主要问题

### 1. 空引用异常 (NullReferenceException)

**问题描述：** 在运行时出现空引用异常

**修复内容：**
- 在 `ModeBase.SetParent()` 中添加了 null 检查
- 在 `Modes.Create()` 中添加了参数验证和空检查
- 在 `CheckAndStartMode()` 中添加了 mode 的 null 检查
- 在 `GetMode()` 中优化了工厂创建逻辑

### 2. 对象池类型错误

**问题描述：** 对象池使用错误的类型进行缓存

**修复内容：**
- 移除了错误的对象池类型检查
- 直接使用工厂方法创建新实例

### 3. 历史记录栈溢出

**问题描述：** 使用 LINQ 操作栈可能导致性能问题

**修复内容：**
- 使用数组临时存储替代 LINQ 操作
- 优化历史记录限制逻辑

### 4. 状态ID管理问题

**问题描述：** 创建状态时没有正确更新 m_size

**修复内容：**
- 在 `Create()` 方法中自动更新 m_size
- 添加状态ID冲突检查

## 使用建议

### 1. 正确的初始化流程

```csharp
// 创建状态管理器
var manager = new Modes<YourParentType>(parentInstance);

// 注册延迟加载状态（推荐）
int idleStateId = manager.RegisterLazy<IdleState>(() => new IdleState());
int moveStateId = manager.RegisterLazy<MoveState>(() => new MoveState());

// 或者直接创建状态（立即加载）
manager.Create<IdleState>(0);
manager.Create<MoveState>(1);

// 配置状态参数
manager.ConfigureMode(idleStateId).DefaultParameters["speed"] = 0f;

// 启动初始状态
manager.Select(idleStateId);
```

### 2. 状态类的最佳实践

```csharp
public class ExampleState : ModeBase<YourParentType>
{
    public override void Enter(Dictionary<string, object> parameters = null)
    {
        // 确保调用基类方法或进行必要的初始化
        base.Enter(parameters);
        
        // 状态进入逻辑
        if (Parent != null)
        {
            // 安全访问父对象
        }
    }

    public override bool CanEnter(int fromMode, Dictionary<string, object> parameters = null)
    {
        // 添加状态转换验证逻辑
        return base.CanEnter(fromMode, parameters);
    }
}
```

### 3. 错误处理和调试

```csharp
// 启用详细日志记录
StatusDebugger.EnableVerboseLogging = true;

// 使用安全的状态切换
manager.SafeSelect(targetStateId, "从UI触发");

// 定期验证状态管理器
if (!StatusDebugger.ValidateStateManager(manager))
{
    Debug.LogError("状态管理器验证失败");
}

// 获取状态报告
Debug.Log(StatusDebugger.GetStatusReport(manager));
```

### 4. 性能优化建议

- 使用延迟加载 (`RegisterLazy`) 而不是立即创建状态
- 定期调用 `UnloadInactiveModes()` 清理未使用的状态
- 避免在Update中频繁切换状态
- 使用状态池来减少GC压力

### 5. 常见错误及解决方法

#### 错误：状态切换失败
- **检查：** 状态ID是否有效
- **检查：** 是否正确注册了状态
- **检查：** `CanEnter/CanExit` 是否返回 true

#### 错误：Update方法不执行
- **检查：** 是否调用了 `manager.Update()`
- **检查：** 当前状态是否正确设置
- **检查：** 状态是否已正确加载

#### 错误：参数传递失败
- **检查：** 参数字典的键值类型
- **检查：** 是否正确配置了默认参数

### 6. 事件处理

```csharp
// 监听状态转换事件
manager.BeforeModeChange += (sender, args) => {
    Debug.Log($"即将从状态 {args.FromMode} 切换到 {args.ToMode}");
    // 可以设置 args.Cancel = true 来取消转换
};

manager.AfterModeChange += (sender, args) => {
    Debug.Log($"已从状态 {args.FromMode} 切换到 {args.ToMode}");
};

manager.ModeChangeRejected += (sender, args) => {
    Debug.LogWarning($"状态转换被拒绝: {args.FromMode} -> {args.ToMode}");
};
```

## 调试工具

### 使用 StatusDebugger 类
- `StatusDebugger.EnableVerboseLogging = true` - 启用详细日志
- `StatusDebugger.ValidateStateManager(manager)` - 验证状态管理器
- `StatusDebugger.GetStatusReport(manager)` - 获取状态报告

### 使用 StatusPerformanceMonitor 组件
将此组件添加到游戏对象上以监控性能：
- 监控状态转换频率
- 记录错误次数
- 生成性能报告

### Editor模式下的调试信息
在示例脚本中，Editor模式下会显示详细的调试信息窗口。
