using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 状态管理器（核心类）
/// </summary>
/// <typeparam name="ParentT">父级对象类型</typeparam>
public class Modes<ParentT> : IDisposable where ParentT : new()
{
    #region 内部类和结构

    /// <summary>
    /// 状态配置器（Builder模式）
    /// </summary>
    public class ModeConfigurator
    {
        /// <summary>
        /// 默认参数（每次进入状态时自动合并）
        /// </summary>
        public Dictionary<string, object> DefaultParameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// 进入条件（返回false将阻止进入）
        /// </summary>
        public Func<bool> EnterCondition { get; set; }

        /// <summary>
        /// 退出条件（返回false将阻止退出）
        /// </summary>
        public Func<bool> ExitCondition { get; set; }

        /// <summary>
        /// 状态优先级（影响自动切换逻辑）
        /// </summary>
        public int Priority { get; set; }
    }

    /// <summary>
    /// 状态对象池（减少GC）
    /// </summary>
    private class ModePool
    {
        private readonly Dictionary<Type, Queue<IMode<ParentT>>> _pool = new Dictionary<Type, Queue<IMode<ParentT>>>();

        /// <summary>
        /// 从池中获取状态对象
        /// </summary>
        public IMode<ParentT> Get(Type modeType, Func<IMode<ParentT>> factory)
        {
            if (_pool.TryGetValue(modeType, out var queue) && queue.Count > 0)
            {
                var mode = queue.Dequeue();
                mode.ResetState();
                return mode;
            }

            return factory();
        }

        /// <summary>
        /// 将状态对象返回到池中
        /// </summary>
        public void Return(IMode<ParentT> mode)
        {
            if (mode == null) return;

            var type = mode.GetType();
            if (!_pool.ContainsKey(type))
            {
                _pool[type] = new Queue<IMode<ParentT>>();
            }

            _pool[type].Enqueue(mode);
        }

        /// <summary>
        /// 清空对象池
        /// </summary>
        public void Clear()
        {
            foreach (var queue in _pool.Values)
            {
                while (queue.Count > 0)
                {
                    var mode = queue.Dequeue();
                    mode.Dispose();
                }
            }

            _pool.Clear();
        }
    }


    // 使用一个辅助类来存储工厂方法
    private class ModeFactoryParent
    {
        public Func<IMode<ParentT>> Factory { get; }

        public ModeFactoryParent(Func<IMode<ParentT>> factory)
        {
            Factory = factory;
        }
    }

    #endregion

    #region 字段和属性

    // 基础字段
    private readonly ParentT m_parent;

    private Dictionary<int, IMode<ParentT>> m_modes = new Dictionary<int, IMode<ParentT>>();
    private int m_size = 0;
    private int m_previous = -1;
    private int m_active = -1;
    private int m_started = -1;

    // 历史记录
    private readonly Stack<int> m_history = new Stack<int>();
    private readonly int m_historyLimit;

    // 延迟加载
    private readonly Dictionary<int, ModeFactoryParent> m_lazyModeFactories = new Dictionary<int, ModeFactoryParent>();

    // 状态配置
    private readonly Dictionary<int, ModeConfigurator> m_modeConfigs = new Dictionary<int, ModeConfigurator>();

    // 对象池
    private readonly ModePool m_modePool = new ModePool();

    // 服务提供者（用于依赖注入）
    private readonly IServiceProvider m_serviceProvider;

    /// <summary>
    /// 索引器访问状态
    /// </summary>
    public IMode<ParentT> this[int mode] => GetMode(mode);

    /// <summary>
    /// 上一个活跃状态的ID
    /// </summary>
    public int PreviousMode => m_previous;

    /// <summary>
    /// 当前活跃状态的ID
    /// </summary>
    public int ActiveMode => m_active;

    /// <summary>
    /// 历史记录数量
    /// </summary>
    public int HistoryCount => m_history.Count;

    /// <summary>
    /// 父级对象访问
    /// </summary>
    public ParentT Parent => m_parent;

    public Dictionary<int, IMode<ParentT>> ModesList => m_modes;

// 事件定义
    /// <summary>
    /// 状态转换前触发（可取消）
    /// </summary>
    public event EventHandler<ModeTransitionEventArgs<ParentT>> BeforeModeChange;

    /// <summary>
    /// 状态转换后触发
    /// </summary>
    public event EventHandler<ModeTransitionEventArgs<ParentT>> AfterModeChange;

    /// <summary>
    /// 状态转换被拒绝时触发
    /// </summary>
    public event EventHandler<ModeTransitionEventArgs<ParentT>> ModeChangeRejected;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="parent">父级对象</param>
    /// <param name="historyLimit">历史记录限制</param>
    /// <param name="serviceProvider">依赖注入容器</param>
    public Modes(ParentT parent = default, int historyLimit = 10, IServiceProvider serviceProvider = null)
    {
        m_parent = parent ?? new ParentT();
        m_historyLimit = historyLimit;
        m_serviceProvider = serviceProvider;
    }

    #endregion

    #region 公共API - 状态管理

    /// <summary>
    /// 创建状态实例（使用泛型）
    /// </summary>
    /// <typeparam name="ModeT">状态类型</typeparam>
    /// <returns>创建的状态实例</returns>
    public ModeT Create<ModeT>(int id) where ModeT : IMode<ParentT>, new()
    {
        return Create<ModeT>(id, () => new ModeT());
    }

    /// <summary>
    /// 创建状态实例（使用工厂方法）
    /// </summary>
    /// <typeparam name="ModeT">状态类型</typeparam>
    /// <param name="factory">工厂方法</param>
    /// <returns>创建的状态实例</returns>
    public ModeT Create<ModeT>(int id, Func<ModeT> factory) where ModeT : IMode<ParentT>
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (m_modes.ContainsKey(id))
            throw new InvalidOperationException($"状态ID {id} 已存在");

        // 通过DI容器或工厂创建实例
        ModeT newMode = m_serviceProvider != null
            ? (ModeT)m_serviceProvider.GetService(typeof(ModeT)) ?? factory()
            : factory();

        if (newMode == null)
            throw new InvalidOperationException($"无法创建状态类型 {typeof(ModeT).Name}");

        // 初始化状态
        m_modes.Add(id, newMode);
        newMode.SetParent(m_parent);
        newMode.Init();

        // 更新大小
        if (id >= m_size)
            m_size = id + 1;

        return newMode;
    }

    /// <summary>
    /// 注册延迟加载的状态
    /// </summary>
    /// <typeparam name="ModeT">状态类型</typeparam>
    /// <param name="factory">工厂方法</param>
    /// <returns>分配的状态ID</returns>
    public int RegisterLazy<ModeT>(Func<ModeT> factory) where ModeT : IMode<ParentT>
    {
        int index = m_size;
        // 包装工厂方法
        m_lazyModeFactories[index] = new ModeFactoryParent(() => factory());
        m_size++;
        return index;
    }

    /// <summary>
    /// 配置指定状态的参数和行为
    /// </summary>
    /// <param name="modeIndex">状态ID</param>
    /// <returns>配置器实例</returns>
    public ModeConfigurator ConfigureMode(int modeIndex)
    {
        if (!m_modeConfigs.TryGetValue(modeIndex, out var config))
        {
            config = new ModeConfigurator();
            m_modeConfigs[modeIndex] = config;
        }

        return config;
    }

    #endregion

    #region 公共API - 状态控制

    /// <summary>
    /// 切换到指定状态（同步）
    /// </summary>
    /// <param name="selection">目标状态ID</param>
    /// <param name="parameters">转换参数</param>
    /// <param name="addToHistory">是否添加到历史记录</param>
    /// <returns>是否切换成功</returns>
    public bool Select(int selection, Dictionary<string, object> parameters = null, bool addToHistory = true)
    {
        // 边界检查
        if (selection < 0 || selection >= m_size)
        {
            OnModeChangeRejected(m_active, selection, parameters, "Invalid mode index");
            return false;
        }

        // 重复检查
        if (selection == m_active)
            return true;

        // 获取状态实例
        var currentMode = GetMode(m_active, false);
        var requestedMode = GetMode(selection);
        if (requestedMode == null)
        {
            OnModeChangeRejected(m_active, selection, parameters, "Requested mode is null");
            return false;
        }

        // 创建事件参数
        var args = new ModeTransitionEventArgs<ParentT>(m_active, selection, parameters, m_parent);

        // 触发前置事件
        BeforeModeChange?.Invoke(this, args);
        if (args.Cancel)
        {
            OnModeChangeRejected(m_active, selection, parameters, "Cancelled by BeforeModeChange");
            return false;
        }

        // 检查状态配置
        if (m_modeConfigs.TryGetValue(selection, out var config))
        {
            // 合并默认参数
            parameters = MergeParameters(config.DefaultParameters, parameters);

            // 检查进入条件
            if (config.EnterCondition != null && !config.EnterCondition())
            {
                OnModeChangeRejected(m_active, selection, parameters, "Failed EnterCondition check");
                return false;
            }
        }

        // 验证状态转换
        if (currentMode != null && !currentMode.CanExit(selection, parameters))
        {
            OnModeChangeRejected(m_active, selection, parameters, "CanExit returned false");
            return false;
        }

        if (!requestedMode.CanEnter(m_active, parameters))
        {
            OnModeChangeRejected(m_active, selection, parameters, "CanEnter returned false");
            return false;
        }

        // 执行状态退出
        currentMode?.Exit();

        // 更新状态记录
        m_previous = m_active;
        m_active = selection;
        m_started = -1;

        // 更新历史记录
        if (addToHistory && m_previous >= 0)
        {
            m_history.Push(m_previous);
            if (m_history.Count > m_historyLimit)
            {
                // 保持历史记录不超过限制 - 创建新的栈来移除最旧的元素
                var tempArray = new int[m_historyLimit];
                for (int i = 0; i < m_historyLimit && m_history.Count > 0; i++)
                {
                    tempArray[i] = m_history.Pop();
                }
                
                m_history.Clear();
                for (int i = m_historyLimit - 1; i >= 0; i--)
                {
                    m_history.Push(tempArray[i]);
                }
            }
        }

        // 执行状态进入
        requestedMode.Enter(parameters);

        // 触发后置事件
        AfterModeChange?.Invoke(this, args);
        return true;
    }

    /// <summary>
    /// 异步切换到指定状态
    /// </summary>
    /// <param name="selection">目标状态ID</param>
    /// <param name="parameters">转换参数</param>
    /// <param name="addToHistory">是否添加到历史记录</param>
    /// <returns>Task<bool> 表示异步操作，返回是否切换成功</returns>
    public async Task<bool> SelectAsync(int selection, Dictionary<string, object> parameters = null,
        bool addToHistory = true)
    {
        // 边界检查 - 无效状态ID
        if (selection < 0 || selection >= m_size)
        {
            var errorMsg = $"无效的状态ID: {selection} (有效范围: 0-{m_size - 1})";
            OnModeChangeRejected(m_active, selection, parameters, errorMsg);
            return false;
        }

        // 重复检查 - 已经是目标状态
        if (selection == m_active)
        {
            Debug.LogWarning($"已是当前状态，无需切换: {selection}");
            return true;
        }

        // 获取当前状态和目标状态实例
        var currentMode = GetMode(m_active, false);
        var requestedMode = GetMode(selection);

        // 检查目标状态是否有效
        if (requestedMode == null)
        {
            OnModeChangeRejected(m_active, selection, parameters, "请求的状态实例为null");
            return false;
        }

        // 创建事件参数
        var transitionArgs = new ModeTransitionEventArgs<ParentT>(m_active, selection, parameters, m_parent);

        try
        {
            // 触发前置事件（可取消）
            BeforeModeChange?.Invoke(this, transitionArgs);
            if (transitionArgs.Cancel)
            {
                OnModeChangeRejected(m_active, selection, parameters, "状态转换被BeforeModeChange事件取消");
                return false;
            }

            // 检查状态配置
            if (m_modeConfigs.TryGetValue(selection, out var config))
            {
                // 合并默认参数
                parameters = MergeParameters(config.DefaultParameters, parameters);

                // 检查进入条件
                if (config.EnterCondition != null && !config.EnterCondition())
                {
                    OnModeChangeRejected(m_active, selection, parameters, "不满足进入条件");
                    return false;
                }
            }

            // 验证状态转换条件
            if (currentMode != null && !currentMode.CanExit(selection, parameters))
            {
                OnModeChangeRejected(m_active, selection, parameters, "当前状态不允许退出");
                return false;
            }

            if (!requestedMode.CanEnter(m_active, parameters))
            {
                OnModeChangeRejected(m_active, selection, parameters, "目标状态不允许进入");
                return false;
            }

            // 异步执行状态退出
            if (currentMode != null)
            {
                try
                {
                    await currentMode.ExitAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"状态退出失败: {currentMode.GetType().Name}. Error: {ex}");
                    OnModeChangeRejected(m_active, selection, parameters, $"退出状态时发生异常: {ex.Message}");
                    return false;
                }
            }

            // 更新状态记录
            m_previous = m_active;
            m_active = selection;
            m_started = -1;

            // 更新历史记录
            if (addToHistory && m_previous >= 0)
            {
                m_history.Push(m_previous);

                // 限制历史记录数量
                if (m_history.Count > m_historyLimit)
                {
                    var tempArray = new int[m_historyLimit];
                    for (int i = 0; i < m_historyLimit && m_history.Count > 0; i++)
                    {
                        tempArray[i] = m_history.Pop();
                    }
                    
                    m_history.Clear();
                    for (int i = m_historyLimit - 1; i >= 0; i--)
                    {
                        m_history.Push(tempArray[i]);
                    }
                }
            }

            // 异步执行状态进入
            try
            {
                await requestedMode.EnterAsync(parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"状态进入失败: {requestedMode.GetType().Name}. Error: {ex}");

                // 尝试回滚到之前的状态
                if (currentMode != null)
                {
                    Debug.LogWarning("尝试回滚到之前的状态...");
                    await currentMode.EnterAsync(parameters).ConfigureAwait(false);
                    m_active = m_previous;
                    m_previous = -1;
                }

                OnModeChangeRejected(m_active, selection, parameters, $"进入状态时发生异常: {ex.Message}");
                return false;
            }

            // 触发后置事件
            AfterModeChange?.Invoke(this, transitionArgs);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"状态切换过程中发生未处理异常: {ex}");
            OnModeChangeRejected(m_active, selection, parameters, $"未处理的异常: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 返回到上一个状态
    /// </summary>
    /// <param name="parameters">转换参数</param>
    /// <returns>是否回退成功</returns>
    public bool GoBack(Dictionary<string, object> parameters = null)
    {
        if (m_history.Count == 0)
            return false;

        int previous = m_history.Pop();
        return Select(previous, parameters, false);
    }

    /// <summary>
    /// 预加载指定状态
    /// </summary>
    /// <param name="mode">状态ID</param>
    public void PreloadMode(int mode)
    {
        if (HasMode(mode))
        {
            GetMode(mode);
        }
    }

    /// <summary>
    /// 卸载非活跃状态
    /// </summary>
    /// <param name="keepHistory">是否保留历史记录中的状态</param>
    public void UnloadInactiveModes(bool keepHistory = true)
    {
        for (int i = 0; i < m_size; i++)
        {
            if (i != m_active && (!keepHistory || !m_history.Contains(i)))
            {
                if (m_modes.TryGetValue(i, out var mode) && mode != null && m_lazyModeFactories.ContainsKey(i))
                {
                    m_modePool.Return(mode);
                    m_modes[i] = null;
                }
            }
        }
    }

    #endregion

    #region 公共API - 更新方法

    /// <summary>
    /// 更新当前活跃状态
    /// </summary>
    public void Update()
    {
        if (!IsValidActiveMode()) return;

        var active = GetMode(m_active);
        if (active == null) return;

        // 检查是否需要调用Start
        CheckAndStartMode(active);

        // 调用更新
        active.Update();
    }

    /// <summary>
    /// 延迟更新当前活跃状态
    /// </summary>
    public void LateUpdate()
    {
        if (!IsValidActiveMode()) return;

        var active = GetMode(m_active);
        if (active == null) return;

        CheckAndStartMode(active);
        active.LateUpdate();
    }

    /// <summary>
    /// 固定更新当前活跃状态
    /// </summary>
    public void FixedUpdate()
    {
        if (!IsValidActiveMode()) return;

        var active = GetMode(m_active);
        if (active == null) return;

        CheckAndStartMode(active);
        active.FixedUpdate();
    }

    /// <summary>
    /// GUI渲染当前活跃状态
    /// </summary>
    public void OnGUI()
    {
        if (!IsValidActiveMode()) return;

        var active = GetMode(m_active);
        if (active == null) return;

        CheckAndStartMode(active);
        active.OnGUI();
    }

    #endregion

    #region 公共API - 工具方法

    /// <summary>
    /// 检查状态是否存在
    /// </summary>
    public bool HasMode(int mode) => mode >= 0 && mode < m_size;

    /// <summary>
    /// 检查状态是否已加载
    /// </summary>
    public bool IsModeLoaded(int mode) => HasMode(mode) && GetMode(mode, false) != null;

    /// <summary>
    /// 销毁所有状态并清理资源
    /// </summary>
    public void Destroy()
    {
        for (int mode = 0; mode < m_size; ++mode)
        {
            var modeInstance = GetMode(mode, false);
            if (modeInstance != null)
            {
                if (mode == m_active)
                {
                    modeInstance.Exit();
                }

                modeInstance.Destroy();

                if (!m_lazyModeFactories.ContainsKey(mode))
                {
                    m_modes[mode] = null;
                }
            }
        }

        m_lazyModeFactories.Clear();
        m_modeConfigs.Clear();
        m_history.Clear();
        m_modePool.Clear();
        m_modes.Clear();

        m_previous = m_active = -1;
    }

    /// <summary>
    /// 实现IDisposable接口
    /// </summary>
    public void Dispose()
    {
        Destroy();
    }

    #endregion

    #region 调试支持

#if UNITY_EDITOR
    /// <summary>
    /// 获取调试信息（仅Editor下可用）
    /// </summary>
    public string GetDebugInfo()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Current Mode: {m_active}");
        sb.AppendLine($"Previous Mode: {m_previous}");
        sb.AppendLine("Mode History:");
        foreach (var mode in m_history)
        {
            sb.AppendLine($"- {mode}");
        }

        sb.AppendLine("Registered Modes:");
        for (int i = 0; i < m_size; i++)
        {
            var mode = GetMode(i, false);
            sb.AppendLine($"[{i}] {(mode != null ? mode.GetType().Name : "Not Loaded")}");
        }

        return sb.ToString();
    }
#endif

    #endregion

    #region 私有方法

    /// <summary>
    /// 获取状态实例
    /// </summary>
    private IMode<ParentT> GetMode(int index, bool createIfLazy = true)
    {
        if (index < 0 || index >= m_size)
            return null;

        // 如果已加载则直接返回
        if (m_modes.TryGetValue(index, out var mode1))
            return mode1;

        // 延迟加载处理
        if (m_lazyModeFactories.TryGetValue(index, out var modeFactoryParent) && createIfLazy)
        {
            var factory = modeFactoryParent.Factory;
            var newMode = factory();
            
            if (newMode != null)
            {
                newMode.SetParent(m_parent);
                newMode.Init();
                m_modes[index] = newMode;
            }
            
            return newMode;
        }

        return null;
    }

    /// <summary>
    /// 检查并调用状态的Start方法
    /// </summary>
    private void CheckAndStartMode(IMode<ParentT> mode)
    {
        if (mode != null && m_started != m_active)
        {
            m_started = m_active;
            mode.Start();
        }
    }

    /// <summary>
    /// 检查当前活跃状态是否有效
    /// </summary>
    private bool IsValidActiveMode() => m_active >= 0 && m_active < m_size;

    /// <summary>
    /// 合并参数字典
    /// </summary>
    private Dictionary<string, object> MergeParameters(
        Dictionary<string, object> defaults,
        Dictionary<string, object> overrides)
    {
        if (defaults == null && overrides == null) return null;
        if (defaults == null) return new Dictionary<string, object>(overrides);
        if (overrides == null) return new Dictionary<string, object>(defaults);

        var merged = new Dictionary<string, object>(defaults);
        foreach (var kvp in overrides)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }

    /// <summary>
    /// 状态转换被拒绝时的处理
    /// </summary>
    private void OnModeChangeRejected(int fromMode, int toMode, Dictionary<string, object> parameters, string reason)
    {
        var args = new ModeTransitionEventArgs<ParentT>(fromMode, toMode, parameters, m_parent);
        ModeChangeRejected?.Invoke(this, args);

#if UNITY_EDITOR
        UnityEngine.Debug.LogWarning($"Mode change rejected from {fromMode} to {toMode}. Reason: {reason}");
#endif
    }

    #endregion
}