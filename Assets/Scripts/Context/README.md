# Context 系统架构说明

## 📁 文件结构

Context 系统采用 **Partial Class(部分类)** 架构,将一个大类拆分成多个文件,每个文件负责特定的功能模块。

```
Context/
├── Context.Core.cs         # 核心定义 (字段、构造函数、属性)
├── Context.Dispose.cs      # 资源释放
├── Context.Contains.cs     # 检查对象是否存在
├── Context.Get.cs          # 获取对象 (Get & TryGet)
├── Context.Set.cs          # 设置对象
├── Context.Remove.cs       # 移除对象 (Remove & TryRemove)
├── Context.Static.cs       # 静态成员和全局上下文管理
└── ApplicationContext.cs   # 应用上下文(扩展类)
```

---

## 📋 各文件职责

### 1️⃣ **Context.Core.cs** (核心定义)
**职责**: 定义类的基础结构
- 字段定义 (`_typeAttributes`, `_nameAttributes`, `_lock` 等)
- 构造函数
- 核心属性 (`Res`)
- 通用方法 (`ThrowIfDisposed`)

**代码量**: ~55 行

---

### 2️⃣ **Context.Dispose.cs** (资源释放)
**职责**: 负责资源的正确释放
- `Dispose()` 方法实现
- 自动释放所有 `IDisposable` 对象
- 清理字典和锁资源

**代码量**: ~50 行

---

### 3️⃣ **Context.Contains.cs** (检查方法)
**职责**: 检查对象是否存在
- `Contains(string name)` - 按名称检查
- `Contains<T>()` - 按类型检查
- `Contains(Type type)` - 按 Type 检查
- 支持级联查找父上下文

**代码量**: ~85 行

---

### 4️⃣ **Context.Get.cs** (获取方法)
**职责**: 从上下文获取对象
- **Get 系列** (4个方法)
  - `Get(string name)` - 按名称获取
  - `Get<T>()` - 按类型获取
  - `Get<T>(string name)` - 按名称+类型获取
  - `Get(Type type)` - 按 Type 获取

- **TryGet 系列** (4个方法)
  - `TryGet(string name, out object value)` - 安全获取
  - `TryGet<T>(out T value)` - 类型安全获取
  - `TryGet<T>(string name, out T value)` - 名称+类型安全获取
  - `TryGet(Type type, out object value)` - Type 安全获取

**代码量**: ~265 行

---

### 5️⃣ **Context.Set.cs** (设置方法)
**职责**: 向上下文设置对象
- `Set(string name, object value)` - 按名称设置
- `Set<T>(T value)` - 按类型设置
- `Set(Type type, object value)` - 按 Type 设置

**代码量**: ~70 行

---

### 6️⃣ **Context.Remove.cs** (移除方法)
**职责**: 从上下文移除对象
- **Remove 系列** (4个方法)
  - `Remove(string name)` - 按名称移除
  - `Remove<T>()` - 按类型移除
  - `Remove<T>(string name)` - 按名称+类型移除
  - `Remove(Type type)` - 按 Type 移除

- **TryRemove 系列** (4个方法)
  - `TryRemove(string name, out object value)` - 安全移除
  - `TryRemove<T>(out T value)` - 类型安全移除
  - `TryRemove<T>(string name, out T value)` - 名称+类型安全移除
  - `TryRemove(Type type, out object value)` - Type 安全移除

**代码量**: ~245 行

---

### 7️⃣ **Context.Static.cs** (静态管理)
**职责**: 管理全局上下文
- **应用上下文管理**
  - `GetApplicationContext()` - 获取全局应用上下文
  - `SetApplicationContext()` - 设置全局应用上下文

- **命名上下文管理**
  - `GetContext()` / `TryGetContext()` - 获取命名上下文
  - `AddContext()` / `TryAddContext()` / `AddOrUpdateContext()` - 添加命名上下文
  - `RemoveContext()` / `TryRemoveContext()` - 移除命名上下文
  
- **批量管理**
  - `ClearAllContexts()` - 清空所有上下文
  - `GetContextCount()` - 获取上下文数量
  - `GetAllContextKeys()` - 获取所有键

**代码量**: ~290 行

---
