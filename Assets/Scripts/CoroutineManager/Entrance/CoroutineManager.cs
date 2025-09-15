using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

/// <summary>
/// 高级协程管理器
/// 使用策略模式统一管理所有协程
/// </summary>
public static class CoroutineManager
{
    #region 核心管理系统

    private static MonoBehaviour _coroutineRunner;
    private static Dictionary<string, Coroutine> _namedCoroutines = new Dictionary<string, Coroutine>();
    private static Dictionary<Coroutine, string> _coroutineNames = new Dictionary<Coroutine, string>();
    private static Dictionary<Coroutine, CoroutineInfo> _coroutineInfos = new Dictionary<Coroutine, CoroutineInfo>();

    // 协程池
    private static Queue<PooledCoroutineWrapper> _coroutinePool = new Queue<PooledCoroutineWrapper>();
    private static HashSet<PooledCoroutineWrapper> _activePooledCoroutines = new HashSet<PooledCoroutineWrapper>();
    private const int MAX_POOL_SIZE = 200;

    // 统计信息
    private static int _totalCoroutinesCreated = 0;
    private static int _poolHitCount = 0;

    // 线程安全锁
    private static readonly object _lock = new object();

    /// <summary>
    /// 协程信息
    /// </summary>
    private class CoroutineInfo
    {
        public string Name;
        public DateTime StartTime;
        public bool IsPooled;
        public string Category;

        public CoroutineInfo(string name, bool isPooled, string category = "Default")
        {
            Name = name;
            StartTime = DateTime.Now;
            IsPooled = isPooled;
            Category = category;
        }
    }

    /// <summary>
    /// 协程运行器
    /// </summary>
    private static MonoBehaviour CoroutineRunner
    {
        get
        {
            if (_coroutineRunner == null)
            {
                var go = new GameObject("CoroutineManager");
                _coroutineRunner = go.AddComponent<CoroutineRunnerComponent>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            return _coroutineRunner;
        }
    }

    #endregion

    #region 内部日志系统

    private static bool IsDebug = false;

    /// <summary>
    /// 内部调试日志方法
    /// </summary>
    /// <param name="message">日志消息</param>
    private static void DebugLog(string message)
    {
        if (IsDebug)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// 内部警告日志方法
    /// </summary>
    /// <param name="message">警告消息</param>
    private static void DebugLogWarning(string message)
    {
        if (IsDebug)
        {
            Debug.LogWarning(message);
        }
    }

    /// <summary>
    /// 内部错误日志方法
    /// </summary>
    /// <param name="message">错误消息</param>
    private static void DebugLogError(string message)
    {
        if (IsDebug)
        {
            Debug.LogError(message);
        }
    }

    /// <summary>
    /// 设置调试模式
    /// </summary>
    /// <param name="enabled">是否启用调试</param>
    public static void SetDebugMode(bool enabled)
    {
        IsDebug = enabled;
        if (enabled)
        {
            Debug.Log("[协程管理器] 调试模式已启用");
        }
    }

    #endregion

    #region 协程池系统

    /// <summary>
    /// 池化协程包装器
    /// </summary>
    private class PooledCoroutineWrapper : IEnumerator
    {
        private IEnumerator _routine;
        private string _name;
        private bool _isComplete;
        private string _category;

        public object Current
        {
            get
            {
                // 确保返回合适的值，避免null导致的问题
                if (_routine != null)
                    return _routine.Current;
                return null;
            }
        }

        public string Name => _name;
        public string Category => _category;

        public void Initialize(IEnumerator routine, string name, string category = "Default")
        {
            _routine = routine;
            _name = name;
            _category = category;
            _isComplete = false;

            DebugLog($"[协程池] 初始化包装器: {name} (分类: {category})");
        }

        public bool MoveNext()
        {
            if (_isComplete || _routine == null)
            {
                if (_isComplete)
                    DebugLog($"[协程池] 协程已完成: {_name}");
                return false;
            }

            bool hasNext = _routine.MoveNext();
            if (!hasNext)
            {
                _isComplete = true;
                DebugLog($"[协程池] 协程执行完毕，准备回收: {_name}");
                ReturnToPool();
            }

            return hasNext;
        }

        public void Reset()
        {
            // 注意：Unity协程不支持Reset，这里只重置我们的状态
            try
            {
                _routine?.Reset();
            }
            catch (NotSupportedException)
            {
                // Unity协程不支持Reset，忽略此异常
            }

            _isComplete = false;
        }

        /// <summary>
        /// 强制停止并回收（用于外部停止协程时）
        /// 注意：这个方法由CleanupCoroutine调用，不处理命名协程记录清理
        /// </summary>
        public void ForceStop()
        {
            if (_isComplete) return; // 避免重复处理

            DebugLog($"[协程池] 强制停止协程: {_name}");
            _isComplete = true;

            lock (_lock)
            {
                // 回收到池中
                if (_coroutinePool.Count < MAX_POOL_SIZE)
                {
                    DebugLog($"[协程池] 强制回收到池中: {_name} (池大小: {_coroutinePool.Count + 1}/{MAX_POOL_SIZE})");
                    // 清理状态，准备下次使用
                    _routine = null;
                    _name = null;
                    _category = null;
                    _isComplete = false; // 重置完成状态
                    _coroutinePool.Enqueue(this);
                }
                else
                {
                    DebugLog($"[协程池] 池已满，丢弃包装器: {_name} (池大小: {_coroutinePool.Count}/{MAX_POOL_SIZE})");
                }
            }
        }

        private void ReturnToPool()
        {
            DebugLog($"[协程池] 开始回收协程: {_name}");

            lock (_lock)
            {
                // 首先从活跃列表中移除
                _activePooledCoroutines.Remove(this);

                // 清理命名协程记录
                if (!string.IsNullOrEmpty(_name) && _namedCoroutines.ContainsKey(_name))
                {
                    var coroutine = _namedCoroutines[_name];
                    _namedCoroutines.Remove(_name);
                    _coroutineNames.Remove(coroutine);
                    _coroutineInfos.Remove(coroutine);
                    DebugLog($"[协程池] 清理协程记录: {_name}");
                }

                // 回收到池中
                if (_coroutinePool.Count < MAX_POOL_SIZE)
                {
                    DebugLog(
                        $"[协程池] 成功回收到池: {_name} (池大小: {_coroutinePool.Count + 1}/{MAX_POOL_SIZE}, 活跃数: {_activePooledCoroutines.Count})");
                    // 清理状态，准备下次使用
                    _routine = null;
                    _name = null;
                    _category = null;
                    _isComplete = false; // 重置完成状态
                    _coroutinePool.Enqueue(this);
                }
                else
                {
                    DebugLog($"[协程池] 池已满，丢弃包装器: {_name} (池大小: {_coroutinePool.Count}/{MAX_POOL_SIZE})");
                }
            }
        }
    }

    #endregion

    #region 策略模式核心接口

    /// <summary>
    /// 使用策略启动协程（主要接口）
    /// </summary>
    /// <param name="strategy">协程策略</param>
    /// <returns>协程对象</returns>
    public static Coroutine StartCoroutine(ICoroutineStrategy strategy)
    {
        if (strategy == null)
        {
            Debug.LogError("协程策略不能为空");
            return null;
        }

        var routine = strategy.CreateCoroutine();
        var name = strategy.GetCoroutineName();
        var category = strategy.GetCategory();
        var usePool = strategy.UsePool;

        return StartManagedCoroutine(routine, name, usePool, category);
    }

    /// <summary>
    /// 批量启动协程
    /// </summary>
    /// <param name="strategies">协程策略集合</param>
    /// <returns>协程对象列表</returns>
    public static List<Coroutine> StartCoroutines(params ICoroutineStrategy[] strategies)
    {
        var coroutines = new List<Coroutine>();
        foreach (var strategy in strategies)
        {
            var coroutine = StartCoroutine(strategy);
            if (coroutine != null)
            {
                coroutines.Add(coroutine);
            }
        }

        return coroutines;
    }

    #endregion

    #region 便捷快捷方法

    /// <summary>
    /// 延迟执行（快捷方法）
    /// </summary>
    public static Coroutine DelayedAction(Action action, float delaySeconds, string name = null, bool usePool = true)
    {
        return StartCoroutine(new DelayedActionStrategy(action, delaySeconds, name, usePool));
    }

    /// <summary>
    /// 延迟N帧执行（快捷方法）
    /// </summary>
    public static Coroutine DelayedFrameAction(Action action, int frameCount = 1, string name = null,
        bool usePool = true)
    {
        return StartCoroutine(new DelayedFrameActionStrategy(action, frameCount, name, usePool));
    }

    /// <summary>
    /// 重复执行（快捷方法）
    /// </summary>
    public static Coroutine RepeatAction(Action action, float intervalSeconds, int count = 0, string name = null,
        bool usePool = true)
    {
        return StartCoroutine(new RepeatActionStrategy(action, intervalSeconds, count, name, usePool));
    }

    /// <summary>
    /// 浮点数插值（快捷方法）
    /// </summary>
    public static Coroutine LerpValue(float from, float to, float duration, Action<float> onUpdate,
        Func<float, float> easing = null, string name = null, bool usePool = true)
    {
        return StartCoroutine(new LerpValueStrategy(from, to, duration, onUpdate, easing, name, usePool));
    }

    /// <summary>
    /// Vector3插值（快捷方法）
    /// </summary>
    public static Coroutine LerpVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
        Func<float, float> easing = null, string name = null, bool usePool = true)
    {
        return StartCoroutine(
            new LerpVector3Strategy(from, to, duration, onUpdate, easing, name, usePool));
    }

    /// <summary>
    /// 震动效果（快捷方法）
    /// </summary>
    public static Coroutine Shake(Transform target, float duration, float magnitude = 1f, float frequency = 25f,
        string name = null, bool usePool = true)
    {
        return StartCoroutine(new ShakeStrategy(target, duration, magnitude, frequency, name, usePool));
    }

    /// <summary>
    /// 条件执行（快捷方法）
    /// </summary>
    public static Coroutine ExecuteWhen(Action action, Func<bool> condition, float timeout = 0f,
        string name = null, bool usePool = true)
    {
        return StartCoroutine(new ExecuteWhenStrategy(action, condition, timeout, name, usePool));
    }

    /// <summary>
    /// 等待时间（快捷方法）
    /// </summary>
    public static Coroutine WaitForSeconds(float seconds, Action onComplete = null,
        CancellationToken cancellationToken = default, string name = null, bool usePool = true)
    {
        return StartCoroutine(new WaitForSecondsStrategy(seconds, onComplete, cancellationToken, name, usePool));
    }

    /// <summary>
    /// 等待条件（快捷方法）
    /// </summary>
    public static Coroutine WaitUntil(Func<bool> condition, Action onComplete = null, float checkInterval = 0f,
        CancellationToken cancellationToken = default, string name = null, bool usePool = true)
    {
        return StartCoroutine(new WaitUntilStrategy(condition, onComplete, checkInterval, cancellationToken, name, usePool));
    }

    /// <summary>
    /// 分帧处理（快捷方法）
    /// </summary>
    public static Coroutine ProcessOverFrames<T>(IEnumerable<T> collection, Action<T> action,
        int itemsPerFrame = 10, Action onComplete = null, string name = null, bool usePool = true)
    {
        return StartCoroutine(new ProcessOverFramesStrategy<T>(collection, action, itemsPerFrame, onComplete,
            name, usePool));
    }

    /// <summary>
    /// 自定义协程（快捷方法）
    /// </summary>
    public static Coroutine Custom(IEnumerator routine, string name = null, string category = "Custom",
        bool usePool = true)
    {
        return StartCoroutine(new CustomCoroutineStrategy(routine, name, category, usePool));
    }

    #endregion

    #region 核心启动方法

    /// <summary>
    /// 启动协程（统一入口，支持命名和池化）
    /// </summary>
    /// <param name="routine">协程方法</param>
    /// <param name="name">协程名称（为空则自动生成）</param>
    /// <param name="usePool">是否使用协程池</param>
    /// <param name="category">协程分类</param>
    /// <param name="stopExisting">是否停止同名协程</param>
    /// <returns>协程对象</returns>
    public static Coroutine StartManagedCoroutine(IEnumerator routine, string name = null,
        bool usePool = true, string category = "Default", bool stopExisting = true)
    {
        // 生成名称
        if (string.IsNullOrEmpty(name))
        {
            name = $"Coroutine_{_totalCoroutinesCreated}_{DateTime.Now.Ticks}";
        }

        // 停止同名协程
        if (stopExisting && _namedCoroutines.ContainsKey(name))
        {
            StopManagedCoroutine(name);
        }

        Coroutine coroutine;

        if (usePool)
        {
            coroutine = StartPooledCoroutine(routine, name, category);
        }
        else
        {
            coroutine = CoroutineRunner.StartCoroutine(routine);
            RegisterCoroutine(coroutine, name, false, category);
            _totalCoroutinesCreated++; // 普通协程在这里增加计数
        }

        return coroutine;
    }

    /// <summary>
    /// 从池中启动协程
    /// </summary>
    private static Coroutine StartPooledCoroutine(IEnumerator routine, string name, string category)
    {
        PooledCoroutineWrapper wrapper;

        lock (_lock)
        {
            if (_coroutinePool.Count > 0)
            {
                wrapper = _coroutinePool.Dequeue();
                _poolHitCount++;
                DebugLog($"[协程池] 从池中复用包装器: {name} (池剩余: {_coroutinePool.Count})");
            }
            else
            {
                wrapper = new PooledCoroutineWrapper();
                DebugLog($"[协程池] 创建新包装器: {name} (池为空)");
            }

            _activePooledCoroutines.Add(wrapper);
            _totalCoroutinesCreated++;
        }

        wrapper.Initialize(routine, name, category);

        var coroutine = CoroutineRunner.StartCoroutine(wrapper);
        RegisterCoroutine(coroutine, name, true, category);

        // 在锁外计算命中率
        var hitRate = _totalCoroutinesCreated > 0 ? (float)_poolHitCount / _totalCoroutinesCreated : 0f;

        DebugLog(
            $"[协程池] 启动池化协程: {name} (活跃数: {_activePooledCoroutines.Count}, 池可用: {_coroutinePool.Count}, 命中率: {hitRate:P1})");
        return coroutine;
    }

    /// <summary>
    /// 注册协程信息
    /// </summary>
    private static void RegisterCoroutine(Coroutine coroutine, string name, bool isPooled, string category)
    {
        if (string.IsNullOrEmpty(name))
            name = $"Coroutine_{_totalCoroutinesCreated}_{DateTime.Now.Ticks}";
        if (coroutine == null) return; // 防止协程对象为 null

        lock (_lock)
        {
            _namedCoroutines[name] = coroutine;
            _coroutineNames[coroutine] = name;
            _coroutineInfos[coroutine] = new CoroutineInfo(name, isPooled, category);
        }
    }

    #endregion

    #region 协程控制

    /// <summary>
    /// 停止命名协程
    /// </summary>
    /// <param name="name">协程名称</param>
    /// <returns>是否成功停止</returns>
    public static bool StopManagedCoroutine(string name)
    {
        if (_namedCoroutines.TryGetValue(name, out var coroutine))
        {
            CoroutineRunner.StopCoroutine(coroutine);
            CleanupCoroutine(coroutine);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 停止指定协程
    /// </summary>
    /// <param name="coroutine">协程对象</param>
    /// <returns>是否成功停止</returns>
    public static bool StopManagedCoroutine(Coroutine coroutine)
    {
        if (coroutine != null && _coroutineNames.ContainsKey(coroutine))
        {
            CoroutineRunner.StopCoroutine(coroutine);
            CleanupCoroutine(coroutine);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 停止分类中的所有协程
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>停止的协程数量</returns>
    public static int StopCoroutinesByCategory(string category)
    {
        var toStop = new List<Coroutine>();
        foreach (var info in _coroutineInfos)
        {
            if (info.Value.Category == category)
            {
                toStop.Add(info.Key);
            }
        }

        foreach (var coroutine in toStop)
        {
            StopManagedCoroutine(coroutine);
        }

        return toStop.Count;
    }

    /// <summary>
    /// 停止所有托管协程
    /// </summary>
    /// <returns>停止的协程数量</returns>
    public static int StopAllManagedCoroutines()
    {
        var count = _namedCoroutines.Count;
        var coroutines = new List<Coroutine>(_namedCoroutines.Values);

        foreach (var coroutine in coroutines)
        {
            CoroutineRunner.StopCoroutine(coroutine);
            CleanupCoroutine(coroutine);
        }

        // 确保清理完成
        _namedCoroutines.Clear();
        _coroutineNames.Clear();
        _coroutineInfos.Clear();
        _activePooledCoroutines.Clear();

        return count;
    }

    /// <summary>
    /// 清理协程记录
    /// </summary>
    private static void CleanupCoroutine(Coroutine coroutine)
    {
        string name = null;
        PooledCoroutineWrapper pooledWrapper = null;

        lock (_lock)
        {
            if (_coroutineNames.TryGetValue(coroutine, out name))
            {
                // 检查是否是池化协程
                pooledWrapper = _activePooledCoroutines.FirstOrDefault(w => w.Name == name);
                if (pooledWrapper != null)
                {
                    DebugLog($"[协程池] 清理池化协程: {name} (活跃数: {_activePooledCoroutines.Count})");
                    // 直接移除，避免重复清理
                    _activePooledCoroutines.Remove(pooledWrapper);
                }
                else
                {
                    DebugLog($"[协程池] 清理普通协程: {name}");
                }

                _namedCoroutines.Remove(name);
                _coroutineNames.Remove(coroutine);
                _coroutineInfos.Remove(coroutine);
            }
        }

        // 在锁外调用ForceStop，避免死锁
        if (pooledWrapper != null)
        {
            pooledWrapper.ForceStop();
        }
    }

    #endregion

    #region 查询和统计

    /// <summary>
    /// 检查命名协程是否存在
    /// </summary>
    /// <param name="name">协程名称</param>
    /// <returns>是否存在</returns>
    public static bool HasCoroutine(string name)
    {
        return _namedCoroutines.ContainsKey(name);
    }

    /// <summary>
    /// 获取协程名称
    /// </summary>
    /// <param name="coroutine">协程对象</param>
    /// <returns>协程名称</returns>
    public static string GetCoroutineName(Coroutine coroutine)
    {
        return _coroutineNames.TryGetValue(coroutine, out var name) ? name : null;
    }

    /// <summary>
    /// 获取所有运行中的协程名称
    /// </summary>
    /// <returns>协程名称列表</returns>
    public static List<string> GetRunningCoroutineNames()
    {
        return new List<string>(_namedCoroutines.Keys);
    }

    /// <summary>
    /// 按分类获取协程名称
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>协程名称列表</returns>
    public static List<string> GetCoroutineNamesByCategory(string category)
    {
        var names = new List<string>();
        foreach (var info in _coroutineInfos.Values)
        {
            if (info.Category == category)
            {
                names.Add(info.Name);
            }
        }

        return names;
    }

    /// <summary>
    /// 获取协程统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    public static CoroutineStats GetCoroutineStats()
    {
        var categoryCount = new Dictionary<string, int>();
        int pooledCount = 0;
        int normalCount = 0;

        foreach (var info in _coroutineInfos.Values)
        {
            if (categoryCount.ContainsKey(info.Category))
                categoryCount[info.Category]++;
            else
                categoryCount[info.Category] = 1;

            if (info.IsPooled)
                pooledCount++;
            else
                normalCount++;
        }

        return new CoroutineStats
        {
            TotalRunning = _namedCoroutines.Count,
            PooledRunning = pooledCount,
            NormalRunning = normalCount,
            PoolAvailable = _coroutinePool.Count,
            PoolActive = _activePooledCoroutines.Count,
            TotalCreated = _totalCoroutinesCreated,
            PoolHitRate = _totalCoroutinesCreated > 0 ? (float)_poolHitCount / _totalCoroutinesCreated : 0f,
            CategoryCounts = categoryCount
        };
    }

    /// <summary>
    /// 协程统计信息
    /// </summary>
    public class CoroutineStats
    {
        public int TotalRunning;
        public int PooledRunning;
        public int NormalRunning;
        public int PoolAvailable;
        public int PoolActive;
        public int TotalCreated;
        public float PoolHitRate;
        public Dictionary<string, int> CategoryCounts;

        public override string ToString()
        {
            var categories = string.Join(", ", CategoryCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
            return $"协程统计 - 运行中:{TotalRunning}(池化:{PooledRunning}  普通:{NormalRunning}), " +
                   $"池状态:{PoolActive}活跃/{PoolAvailable}可用, " +
                   $"总创建:{TotalCreated}, 池命中率:{PoolHitRate:P1}, " +
                   $"分类:[{categories}]";
        }
    }

    #endregion

    #region 管理功能

    /// <summary>
    /// 清理协程池
    /// </summary>
    public static void ClearCoroutinePool()
    {
        var poolCount = _coroutinePool.Count;
        var activeCount = _activePooledCoroutines.Count;

        _coroutinePool.Clear();
        _activePooledCoroutines.Clear();

        DebugLog($"[协程池] 清理协程池完成 - 清理池中包装器: {poolCount}, 清理活跃包装器: {activeCount}");
    }

    /// <summary>
    /// 重置管理器
    /// </summary>
    public static void ResetManager()
    {
        var totalRunning = _namedCoroutines.Count;
        var poolSize = _coroutinePool.Count;
        var activePooled = _activePooledCoroutines.Count;

        DebugLog($"[协程池] 重置管理器开始 - 运行中: {totalRunning}, 池大小: {poolSize}, 活跃池化: {activePooled}");

        StopAllManagedCoroutines();
        ClearCoroutinePool();
        _totalCoroutinesCreated = 0;
        _poolHitCount = 0;

        DebugLog("[协程池] 重置管理器完成 - 所有统计数据已重置");
    }

    /// <summary>
    /// 打印协程池详细状态（用于调试）
    /// </summary>
    public static void LogPoolStatus()
    {
        var stats = GetCoroutineStats();
        DebugLog($"[协程池状态] {stats}");

        // 详细的活跃协程信息
        if (_activePooledCoroutines.Count > 0)
        {
            DebugLog($"[协程池] 活跃协程详情:");
            foreach (var wrapper in _activePooledCoroutines)
            {
                DebugLog($"  - {wrapper.Name} (分类: {wrapper.Category})");
            }
        }

        // 池中可用包装器数量
        DebugLog($"[协程池] 可复用包装器: {_coroutinePool.Count}/{MAX_POOL_SIZE}");
    }

    #endregion

    #region 缓动函数

    /// <summary>
    /// 常用缓动函数
    /// </summary>
    public static class Easing
    {
        public static float Linear(float t) => t;
        public static float EaseInQuad(float t) => t * t;
        public static float EaseOutQuad(float t) => 1 - (1 - t) * (1 - t);
        public static float EaseInOutQuad(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        public static float EaseInCubic(float t) => t * t * t;
        public static float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
        public static float EaseInOutCubic(float t) => t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
        public static float EaseInSine(float t) => 1 - Mathf.Cos(t * Mathf.PI / 2);
        public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI / 2);
        public static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

        public static float EaseOutBounce(float t)
        {
            if (t < 1 / 2.75f)
                return 7.5625f * t * t;
            else if (t < 2 / 2.75f)
                return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
            else if (t < 2.5 / 2.75f)
                return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
            else
                return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
        }
    }

    #endregion

    #region 内部组件

    private class CoroutineRunnerComponent : MonoBehaviour
    {
        private void OnDestroy()
        {
            ResetManager();
        }
    }

    #endregion
}