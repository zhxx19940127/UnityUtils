# 自动 UI 代码生成工具（UnityUtils/GeneratedUITool）

本工具用于基于 UI 预制体自动生成访问脚本，避免手写查找与拖拽，支持单文件可重生成、序列化引用赋值、命名空间与基类包装、批量工作流等。

更新时间：2025-09-30

---

## 功能概述

- 自动生成 UI 访问脚本（单文件，重复生成仅覆盖标记段，保留 using 与用户代码）
- 支持 UIMark 标记导出目标：组件/RectTransform/GameObject/Auto，支持忽略子级与组件索引
- 自动包含常用控件（Button/Toggle/Slider/InputField/TMP），可扩展包含 ScrollRect/Scrollbar/Dropdown
- 两种“生成与赋值”模式：
  - 方法赋值：生成 public void InitRefs()；把查找逻辑放入该方法，需你在合适时机手动调用
  - 序列化引用：生成 [SerializeField] 字段并在编辑器中写引用（无需运行时查找）
- 支持命名空间包装与自定义基类（默认 MonoBehaviour）
- 统一的命名与属性生成：_camelCase、组件前缀映射、属性名去前缀（可选）
- 编辑器窗口：
  - 列出预制体，提供搜索与筛选（只未生成/只未挂载/只异常）
  - 每项状态固定三段（已生成/已挂载/有无异常），三字文案，固定宽度，避免抖动
  - 操作按钮统一两字文案与宽度：生成/挂载/打开/检查（删除仅在“已生成”时显示）
  - 批量：为所选预制生成脚本、为所选已生成预制挂载脚本
  - 从资源或层级右键菜单触发生成
- 稳健的赋值与状态：
  - 序列化模式下生成/挂载后自动尝试赋值，并缓存最近一次统计用于“只异常”筛选
  - 删除会移除组件与脚本，并清空异常缓存

---

## 主要文件结构

- `Assets/GeneratedUITool/UIMark.cs`
  - 导出标记：fieldName、ignoreChildren、targetKind、componentTypeFullName、componentIndex
- `Assets/GeneratedUITool/Editor/AutoUICodeGenSettings.cs`
  - 全局配置脚本化资源（自动创建）：
    - 目录：`prefabFolder`、`scriptOutputFolder`
    - 模式：`initAssignMode`（方法赋值/序列化引用；兼容老 Awake/StartFind）
    - 命名：`requireUppercaseClassName`、`privateFieldUnderscoreCamelCase`、`useComponentPrefixForFields`、`stripPrefixInPropertyNames`
    - 属性：`generateReadOnlyProperties`
    - 前缀：`componentPrefixes`（可编辑/重置）
    - 类包装：`wrapNamespace`、`baseClassFullName`
- `Assets/GeneratedUITool/Editor/UICodeGenerator.cs`
  - 核心生成：收集字段 → 命名处理 → 生成/更新单文件（标记段：fields/props/assign）
  - 方法赋值：在 `// <auto-assign>` 段生成 `public void InitRefs()`
  - 序列化引用：字段加 `[SerializeField]` 并在生成后尝试赋值
  - 支持命名空间与基类；增量更新在类体内精准 Upsert 标记段
  - 对外辅助：`CollectFields()`（挂载后赋值使用）
- `Assets/GeneratedUITool/Editor/SerializedReferenceAssigner.cs`
  - 在序列化模式下写回引用；统计 success/total/missingPath/missingComponent
  - 支持命名空间解析；`TryGetLastStatsByGuid`、`ClearStatsForGuid`
- `Assets/GeneratedUITool/Editor/GeneratedScriptAttacher.cs`
  - 手动挂载队列（编译后处理）；可优先尝试立即挂载
  - 挂载成功且序列化模式则自动收集字段并赋值
- `Assets/GeneratedUITool/Editor/AutoUICodeGeneratorWindow.cs`
  - 工具窗口：路径与规则设置、预制体列表、状态/操作、批量处理、拖放设置目录

---

## 标记段与代码结构

生成脚本采用单文件、标记段的方式，仅覆盖以下区域：

- `// <auto-fields>` ... `// </auto-fields>`
- `// <auto-props>` ... `// </auto-props>`（可选）
- `// <auto-assign>` ... `// </auto-assign>`（方法赋值模式生成 InitRefs()；序列化模式为空）

用户代码请写在类中的“用户代码”区域（生成器不会修改该区域）。

---

## 使用步骤

1) 打开窗口：菜单 `Tools/UI/自动UI代码生成器`
2) 设置路径：
   - UI 预制体路径（支持多级）
   - 脚本生成路径（建议放在非 Editor 下）
3) 生成设置：
   - 生成与赋值方式：
     - 方法赋值：生成 `public void InitRefs()`，需你在合适时机调用
     - 序列化引用：生成 `[SerializeField]` 并由编辑器赋值
   - 其它：
     - 自动包含常用控件 / 扩展控件
     - 命名规则、组件前缀映射与属性名去前缀
     - 类包装：命名空间、基类全名（可选）
4) 在列表中：
   - 生成：无论状态如何都可点击，生成脚本（序列化模式下生成后会尝试赋值）
   - 挂载：在“已生成且未挂载”时可点击，挂载脚本（序列化模式下挂载后会赋值）
   - 打开：在“已生成”时可点击，打开脚本
   - 检查：始终可点击，弹窗展示状态与匹配详情
   - 删除：在“已生成”时可点击，移除预制体组件并删除脚本，清空异常缓存
5) 批量：底部提供批量生成与批量挂载按钮
6) 右键：在资源/层级也可通过菜单触发“生成UI代码”

---

## UIMark 使用说明

- 为需要导出的节点添加 `UIMark`：
  - `targetKind`：Component/RectTransform/GameObject/Auto
  - `componentTypeFullName`：指定组件类型全名（用于 Component 模式）
  - `componentIndex`：当存在多个同类组件时选择索引
  - `fieldName`：自定义字段名（可空，默认用节点名）
  - `ignoreChildren`：勾选后，该节点的子节点不会被自动包含
- 自动包含与 UIMark 可叠加，生成器会对 (path,type,index) 去重

---

## 命名与属性

- 字段默认私有；可选 `_camelCase`
- 可选为每个字段生成只读属性；属性名可移除组件前缀（btn/tog/sld/input/txt/img/rt/go 或自定义）
- 字段名冲突时自动唯一化处理

---

## 赋值与状态

- 方法赋值模式：
  - 生成 `public void InitRefs()`（位于 `// <auto-assign>` 段），你在合适时机手动调用
- 序列化引用模式：
  - 生成 `[SerializeField]` 字段；生成/挂载后尝试写回引用
  - 窗口展示最近一次赋值统计（“有异常/无异常”）；支持“只异常”筛选

---

## 删除

- 在“已生成”状态下点击“删除”：
  - 从预制体移除对应脚本组件（优先用 MonoScript 精确匹配，兜底类型短名）
  - 删除生成的脚本文件（AssetDatabase.DeleteAsset）
  - 清空最近一次异常统计缓存

---

## 常见问题（FAQ）

- 想让生成类使用命名空间与基类？
  - 在窗口“生成设置/类包装”填写 `wrapNamespace` 和 `baseClassFullName` 即可
- 老脚本没有命名空间怎么办？
  - 生成器会在类体内更新标记段与类签名；如需整体迁入命名空间，可手动迁移或添加“强制重写外壳”的安全迁移步骤（可按需扩展）
- 序列化引用缺少或错误？
  - 用“检查”查看详情；必要时在 UIMark 指定 `componentTypeFullName` 与 `componentIndex`
- 方法赋值忘记调用 InitRefs()？
  - 建议在你的基类或外部管理器约定调用时机（如 Awake/Start/OnEnable 或实例化后）

---

## 变更摘要（近期）

- 新增：命名空间包装（wrapNamespace）与基类（baseClassFullName）
- 生成与赋值方式简化为两种：方法赋值（InitRefs）与序列化引用
- 窗口 UI：
  - 状态区固定三段（三字文案）：已生成/已挂载/有无异常
  - 按钮文案两字并统一宽度：生成/挂载/打开/检查/删除
  - 删除按钮仅在“已生成”时显示
- 序列化模式：挂载后自动尝试赋值；缓存最近一次统计

---

## 约定与建议

- 保持预制体名合法（默认要求首字母大写，可关闭）
- 脚本输出目录建议放在非 Editor 下，避免运行时代码受限
- 对字段命名与前缀有团队约定时，使用“组件前缀映射”统一生成风格

---

若需扩展：
- 一键迁移旧文件到命名空间与基类（带备份/预览）
- 生成后自动在 Awake/Start 调用 InitRefs 的可选开关
- 多方案模板（接口、局部类、Partial）

欢迎在窗口中直接试用：搜索/筛选、生成、挂载、检查、删除与批量操作。