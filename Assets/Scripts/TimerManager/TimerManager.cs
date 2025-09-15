using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#region 依赖的接口和枚举

/// <summary>
/// 定时器回调上下文接口
/// </summary>
public interface ITimerContext
{
    void Execute();
}

/// <summary>
/// 定时器工作模式枚举
/// </summary>
public enum TimerMode
{
    /// <summary>单次定时器</summary>
    Normal,

    /// <summary>重复执行的定时器</summary>
    Repeat,

    /// <summary>真实时间定时器(不受Time.timeScale影响)</summary>
    Realtime,

    /// <summary>基于帧数的定时器</summary>
    FrameBased
}

#endregion


/// <summary>
/// 定时器管理系统(单例)
/// </summary>
public class TimerManager : MonoBehaviour
{
    // ========== 私有字段 ==========

    #region 内部数据

    /// <summary> 当前活动的定时器字典 </summary>
    private readonly Dictionary<string, Timer> _mTimerList = new Dictionary<string, Timer>();

    /// <summary> 等待添加的定时器队列 </summary>
    private readonly Dictionary<string, Timer> _mAddTimerList = new Dictionary<string, Timer>();

    /// <summary> 等待销毁的定时器队列(使用HashSet提高效率) </summary>
    private readonly HashSet<string> _mDestroyTimerList = new HashSet<string>();

    /// <summary> 帧计数器(用于帧定时器) </summary>
    private int _mFrameCount;

    /// <summary> 单例实例 </summary>
    private static TimerManager _instance;

    #endregion

    // ========== 公有属性 ==========

    #region 配置属性

    /// <summary>
    /// 全局时间缩放系数(影响所有受缩放影响的定时器)
    /// </summary>
    public float TimeScale { get; set; } = 1.0f;

    /// <summary>
    /// 单例访问器
    /// </summary>
    public static TimerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimerManager>();
                if (_instance == null)
                {
                    var obj = new GameObject("TimerManager");
                    _instance = obj.AddComponent<TimerManager>();
                    DontDestroyOnLoad(obj);
                }
            }

            return _instance;
        }
    }

    #endregion

    // ========== Unity生命周期 ==========

    #region Unity回调

    private void Awake()
    {
        // 单例初始化
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerable<string> completedTimers = new List<string>();

    private void Update()
    {
        completedTimers = Enumerable.Empty<string>();
        // 1. 清理已完成定时器
        completedTimers = _mTimerList
            .Where(kvp => kvp.Value.ShouldBeDestroyed)
            .Select(kvp => kvp.Key);
        foreach (var key in completedTimers) DestroyTimer(key);

        // 2. 处理销毁队列
        if (_mDestroyTimerList.Count > 0)
        {
            foreach (var key in _mDestroyTimerList) _mTimerList.Remove(key);
            _mDestroyTimerList.Clear();
        }

        // 3. 处理添加队列
        if (_mAddTimerList.Count > 0)
        {
            foreach (var kvp in _mAddTimerList.Where(kvp => kvp.Value != null))
            {
                _mTimerList[kvp.Key] = kvp.Value;
            }

            _mAddTimerList.Clear();
        }

        // 4. 更新帧计数器
        _mFrameCount++;

        // 5. 更新所有活跃定时器
        if (_mTimerList.Count > 0)
        {
            UpdateAllTimers();
        }
    }

    #endregion

    // ========== 核心功能 ==========

    #region 定时器管理

    /// <summary>
    /// 添加基本定时器
    /// </summary>
    /// <param name="key">定时器唯一标识</param>
    /// <param name="duration">持续时间(秒/帧)</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="mode">定时器模式</param>
    /// <param name="enableCompensation">是否启用时间补偿</param>
    /// <returns>是否添加成功</returns>
    public bool AddTimer(string key, float duration, Action onComplete,
        TimerMode mode = TimerMode.Normal, bool enableCompensation = true)
    {
        return InternalAddTimer(key, mode, duration, onComplete, null,
            enableCompensation);
    }

    /// <summary>
    /// 添加带参数的定时器
    /// </summary>
    public bool AddTimer<T>(string key, float duration, Action<T> onComplete, T param,
        TimerMode mode = TimerMode.Normal, bool enableCompensation = true)
    {
        return InternalAddTimer(key, mode, duration, () => onComplete?.Invoke(param), null,
            enableCompensation);
    }

    /// <summary>
    /// 添加上下文定时器(支持多参数)
    /// </summary>
    public bool AddTimer(string key, float duration, ITimerContext timerContext,
        TimerMode mode = TimerMode.Normal, bool enableCompensation = true)
    {
        if (timerContext == null)
        {
            Debug.LogError("TimerContext不能为null");
            return false;
        }

        return InternalAddTimer(key, mode, duration, timerContext.Execute, null,
            enableCompensation);
    }

    /// <summary>
    /// 添加帧定时器
    /// </summary>
    public bool AddFrameTimer(string key, int frameCount, Action onComplete)
    {
        return InternalAddTimer(key, TimerMode.FrameBased, frameCount, onComplete, null,
            false);
    }

    /// <summary>
    /// 添加带进度回调的定时器
    /// </summary>
    public bool AddTimerWithProgress(string key, float duration, Action onComplete,
        Action<float> onProgress, TimerMode mode = TimerMode.Normal,
        bool enableCompensation = true)
    {
        return InternalAddTimer(key, mode, duration, onComplete, onProgress,
            enableCompensation);
    }

    /// <summary>
    /// 创建定时器链构建器
    /// </summary>
    public TimerChainBuilder CreateChain()
    {
        return new TimerChainBuilder(this);
    }

    #endregion

    #region 定时器控制

    /// <summary>
    /// 暂停指定定时器
    /// </summary>
    public void PauseTimer(string key)
    {
        if (_mTimerList.TryGetValue(key, out var timer))
        {
            timer.Pause();
        }
    }

    /// <summary>
    /// 恢复指定定时器
    /// </summary>
    public void ResumeTimer(string key)
    {
        if (_mTimerList.TryGetValue(key, out var timer))
        {
            timer.Resume();
        }
    }

    /// <summary>
    /// 销毁指定定时器
    /// </summary>
    public bool DestroyTimer(string key)
    {
        // 从添加队列中移除
        if (_mAddTimerList.Remove(key))
        {
            return true;
        }

        // 添加到销毁队列
        if (_mTimerList.ContainsKey(key))
        {
            _mDestroyTimerList.Add(key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 暂停所有指定前缀的定时器
    /// </summary>
    public void PauseTimersWithPrefix(string prefix)
    {
        foreach (var timer in _mTimerList.Where(x => x.Key.StartsWith(prefix)).Select(x => x.Value))
        {
            timer.Pause();
        }
    }

    /// <summary>
    /// 恢复所有指定前缀的定时器
    /// </summary>
    public void ResumeTimersWithPrefix(string prefix)
    {
        foreach (var timer in _mTimerList.Where(x => x.Key.StartsWith(prefix)).Select(x => x.Value))
        {
            timer.Resume();
        }
    }

    /// <summary>
    /// 销毁所有指定前缀的定时器
    /// </summary>
    public void DestroyTimersWithPrefix(string prefix)
    {
        var keysToRemove = _mTimerList.Keys
            .Where(k => k.StartsWith(prefix))
            .ToList();

        foreach (var key in keysToRemove)
        {
            DestroyTimer(key);
        }
    }

    /// <summary>
    /// 销毁所有定时器
    /// </summary>
    public void ClearAllTimers()
    {
        _mTimerList.Clear();
        _mAddTimerList.Clear();
        _mDestroyTimerList.Clear();
    }

    #endregion

    #region 定时器查询

    /// <summary>
    /// 检查定时器是否存在
    /// </summary>
    public bool IsTimerActive(string key)
    {
        return _mTimerList.ContainsKey(key) || _mAddTimerList.ContainsKey(key);
    }

    /// <summary>
    /// 获取定时器剩余时间
    /// </summary>
    public float GetTimerRemaining(string key)
    {
        return _mTimerList.TryGetValue(key, out var timer) ? timer.Remaining : 0f;
    }

    /// <summary>
    /// 获取定时器当前进度(0-1)
    /// </summary>
    public float GetTimerProgress(string key)
    {
        return _mTimerList.TryGetValue(key, out var timer) ? timer.Progress : 0f;
    }

    /// <summary>
    /// 打印所有活动定时器信息(调试用)
    /// </summary>
    public void LogActiveTimers()
    {
        if (_mTimerList.Count == 0)
        {
            Debug.Log("没有活动的定时器");
            return;
        }

        var sb = new StringBuilder("活动定时器:\n");
        foreach (var kvp in _mTimerList)
        {
            sb.AppendLine($"- {kvp.Key}: 剩余{kvp.Value.Remaining:F2}秒 [模式: {kvp.Value.Mode}, 暂停: {kvp.Value.IsPaused}]");
        }

        Debug.Log(sb.ToString());
    }

    #endregion

    // ========== 内部实现 ==========

    #region 内部方法

    /// <summary>
    /// 更新所有定时器
    /// </summary>
    private void UpdateAllTimers()
    {
        // 使用副本避免修改冲突
        var timers = _mTimerList.Values.ToList();

        // 计算时间增量
        var scaledDeltaTime = Time.deltaTime * TimeScale;
        var unscaledDeltaTime = Time.unscaledDeltaTime;

        foreach (var timer in timers)
        {
            if (timer == null || !_mTimerList.ContainsKey(timer.Name))
            {
                continue;
            }

            if (timer.IsPaused)
            {
                timer.UpdateGlobalPauseData(scaledDeltaTime);
                continue;
            }

            // 根据模式计算增量
            float delta;
            switch (timer.Mode)
            {
                case TimerMode.Normal:
                case TimerMode.Repeat:
                    delta = scaledDeltaTime;
                    break;
                case TimerMode.Realtime:
                    delta = unscaledDeltaTime;
                    break;
                case TimerMode.FrameBased:
                    delta = 1f;
                    break;
                default:
                    delta = 0f;
                    break;
            }

            timer.Update(delta);
        }
    }

    /// <summary>
    /// 内部添加定时器(基础实现)
    /// </summary>
    private bool InternalAddTimer(string key, TimerMode mode, float duration,
        Action callback, Action<float> onProgress, bool enableCompensation)
    {
        // 参数验证
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("定时器key不能为空");
            return false;
        }

        if (duration <= 0)
        {
            Debug.LogError($"无效的持续时间: {duration}。必须大于0");
            return false;
        }

        if (callback == null && onProgress == null)
        {
            Debug.LogError("定时器必须至少有一个回调(onComplete或onProgress)");
            return false;
        }

        // 从销毁队列中移除(如果存在)
        _mDestroyTimerList.Remove(key);

        // 检查key是否已存在
        if (_mTimerList.ContainsKey(key) || _mAddTimerList.ContainsKey(key))
        {
            Debug.LogWarning($"定时器 '{key}' 已存在。将被覆盖...");
        }

        // 创建并添加定时器
        _mAddTimerList[key] = new Timer(key, mode, duration, callback, onProgress,
            enableCompensation);
        return true;
    }

    #endregion

    // ========== 内部类 ==========

    #region Timer内部类

    /// <summary>
    /// 定时器内部实现类
    /// </summary>
    private class Timer
    {
        // ===== 私有字段 =====
        private readonly bool _enableCompensation; // 是否启用时间补偿
        private readonly Action _callback; // 完成回调
        private readonly Action<float> _progressCallback; // 进度回调
        private readonly float _initialDuration; // 初始持续时间(用于重复定时器)
        private float _pausedDuration; // 累计暂停时间(用于补偿)

        // ===== 公有属性 =====
        public string Name { get; } // 定时器名称
        public TimerMode Mode { get; } // 定时器模式
        public bool IsPaused { get; private set; } // 是否暂停
        public float Remaining { get; private set; } // 剩余时间
        public bool ShouldBeDestroyed { get; private set; } // 销毁标记

        /// <summary>
        /// 当前进度(0-1)
        /// </summary>
        public float Progress
        {
            get
            {
                if (_initialDuration <= 0) return 1f;
                return Mathf.Clamp01(1f - (Remaining / _initialDuration));
            }
        }

        // ===== 构造函数 =====
        public Timer(string name, TimerMode mode, float duration,
            Action callback, Action<float> progressCallback,
            bool enableCompensation)
        {
            Name = name;
            Mode = mode;
            Remaining = duration;
            _initialDuration = duration;
            _callback = callback;
            _progressCallback = progressCallback;
            _enableCompensation = enableCompensation;
        }

        // ===== 公有方法 =====
        /// <summary>
        /// 更新定时器
        /// </summary>
        public void Update(float delta)
        {
            if (IsPaused) return;

            Remaining -= delta;

            // 调用进度回调

            // 调用进度回调
            _progressCallback?.Invoke(Progress);

            // 检查是否完成
            if (Remaining <= 0)
            {
                _callback?.Invoke();

                if (Mode == TimerMode.Repeat)
                {
                    Remaining += _initialDuration; // 重置为初始持续时间
                }
                else
                {
                    ShouldBeDestroyed = true; // 标记为待销毁
                }
            }
        }

        /// <summary>
        /// 更新暂停状态数据(用于时间补偿)
        /// </summary>
        public void UpdateGlobalPauseData(float globalPauseData)
        {
            if (IsPaused && _enableCompensation &&
                Mode != TimerMode.Realtime && Mode != TimerMode.FrameBased)
            {
                _pausedDuration += globalPauseData;
            }
        }

        /// <summary>
        /// 暂停定时器
        /// </summary>
        public void Pause()
        {
            if (Mode == TimerMode.Realtime || IsPaused) return;
            IsPaused = true;
        }

        /// <summary>
        /// 恢复定时器
        /// </summary>
        public void Resume()
        {
            if (!IsPaused || Mode == TimerMode.Realtime) return;

            IsPaused = false;

            if (_enableCompensation && Mode != TimerMode.FrameBased)
            {
                Remaining -= _pausedDuration; // 应用时间补偿
                _pausedDuration = 0f; // 重置暂停时间

                if (Remaining < 0) Remaining = 0;
            }
        }
    }

    #endregion

    #region TimerChainBuilder 内部类

    /// <summary>
    /// 链式定时器构建器（支持条件终止和错误处理）
    /// </summary>
    public class TimerChainBuilder
    {
        private class TimerTask
        {
            public string Key { get; set; }
            public float Duration { get; set; }
            public Action Callback { get; set; }
            public TimerMode Mode { get; set; } = TimerMode.Normal;
            public bool EnableCompensation { get; set; } = true;
            public Func<bool> Condition { get; set; } = () => true;
        }

        private readonly List<TimerTask> _tasks = new List<TimerTask>();
        private readonly TimerManager _manager;
        private Action<string> _errorHandler;

        internal TimerChainBuilder(TimerManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        #region 核心链式方法

        /// <summary>
        /// 添加起始定时任务
        /// </summary>
        public TimerChainBuilder AddFirst(string key, float duration, Action callback)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration));

            _tasks.Add(new TimerTask
            {
                Key = key,
                Duration = duration,
                Callback = callback ?? throw new ArgumentNullException(nameof(callback))
            });
            return this;
        }

        /// <summary>
        /// 添加后续定时任务
        /// </summary>
        public TimerChainBuilder Then(string key, float duration, Action callback)
        {
            return AddFirst(key, duration, callback);
        }

        /// <summary>
        /// 添加条件性定时任务（当condition返回true时执行）
        /// </summary>
        public TimerChainBuilder ThenIf(Func<bool> condition, string key, float duration, Action callback)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            var lastTask = _tasks.LastOrDefault();
            if (lastTask == null)
            {
                throw new InvalidOperationException("必须先调用AddFirst或Then添加初始任务");
            }

            lastTask.Condition = condition;
            return Then(key, duration, callback);
        }

        #endregion

        #region 配置方法

        /// <summary>
        /// 设置定时器模式
        /// </summary>
        public TimerChainBuilder WithMode(TimerMode mode)
        {
            if (_tasks.Count == 0)
            {
                throw new InvalidOperationException("没有可配置的任务");
            }

            _tasks.Last().Mode = mode;
            return this;
        }

        /// <summary>
        /// 设置时间补偿
        /// </summary>
        public TimerChainBuilder WithCompensation(bool enable)
        {
            if (_tasks.Count == 0)
            {
                throw new InvalidOperationException("没有可配置的任务");
            }

            _tasks.Last().EnableCompensation = enable;
            return this;
        }

        /// <summary>
        /// 设置错误处理器
        /// </summary>
        public TimerChainBuilder OnError(Action<string> errorHandler)
        {
            _errorHandler = errorHandler;
            return this;
        }

        #endregion

        #region 执行控制

        /// <summary>
        /// 执行定时器链
        /// </summary>
        public void Run()
        {
            if (_tasks.Count == 0)
            {
                _errorHandler?.Invoke("任务链为空");
                return;
            }

            ExecuteTask(0);
        }

        private void ExecuteTask(int index)
        {
            if (index >= _tasks.Count) return;

            var currentTask = _tasks[index];

            // 检查前置条件
            if (index > 0 && !_tasks[index - 1].Condition())
            {
                _errorHandler?.Invoke($"任务 {currentTask.Key} 的前置条件不满足");
                return;
            }

            if (!_manager.AddTimer(
                    currentTask.Key,
                    currentTask.Duration,
                    () =>
                    {
                        try
                        {
                            currentTask.Callback?.Invoke();
                            ExecuteTask(index + 1); // 执行下一个任务
                        }
                        catch (Exception ex)
                        {
                            _errorHandler?.Invoke($"执行任务 {currentTask.Key} 时出错: {ex.Message}");
                        }
                    },
                    currentTask.Mode,
                    currentTask.EnableCompensation))
            {
                _errorHandler?.Invoke($"添加定时器失败: {currentTask.Key}");
            }
        }

        #endregion

        #region 扩展方法（实用工具）

        /// <summary>
        /// 添加延迟执行的简单任务
        /// </summary>
        public TimerChainBuilder Delay(float seconds, Action action)
        {
            return Then(Guid.NewGuid().ToString(), seconds, action);
        }

        /// <summary>
        /// 添加帧延迟任务
        /// </summary>
        public TimerChainBuilder DelayFrames(int frames, Action action)
        {
            if (frames <= 0) throw new ArgumentOutOfRangeException(nameof(frames));

            return Then(Guid.NewGuid().ToString(), frames, action)
                .WithMode(TimerMode.FrameBased)
                .WithCompensation(false);
        }

        #endregion
    }

    #endregion
}