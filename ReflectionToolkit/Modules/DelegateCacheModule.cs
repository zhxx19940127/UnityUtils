using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ReflectionToolkit.Core;

namespace ReflectionToolkit.Modules
{
    /// <summary>
    /// 委托缓存模块 - 将常用反射调用委托化，加速 Method / Property / Constructor 访问
    /// 并提供 “手动” 委托缓存功能（任意 key -> Action&lt;object, object[]&gt;）
    /// 委托缓存模块：将反射调用委托化，极大提升 Method/Property/Constructor 访问性能。
    /// 支持手动委托注册、工厂缓存、ref/out 回退、用量统计、智能清理等。
    /// </summary>
    public class DelegateCacheModule : BaseCacheModule
    {
        #region 字段

        /// <summary>无参方法委托缓存</summary>
        private readonly ConcurrentDictionary<MethodInfo, Action<object>> _actionCache =
            new ConcurrentDictionary<MethodInfo, Action<object>>();

        /// <summary>有参方法委托缓存</summary>
        private readonly ConcurrentDictionary<MethodInfo, Action<object, object[]>> _actionWithParamsCache =
            new ConcurrentDictionary<MethodInfo, Action<object, object[]>>();

        /// <summary>有返回值方法委托缓存</summary>
        private readonly ConcurrentDictionary<MethodInfo, Func<object, object[], object>> _funcCache =
            new ConcurrentDictionary<MethodInfo, Func<object, object[], object>>();

        /// <summary>属性 Getter 委托缓存</summary>
        private readonly ConcurrentDictionary<PropertyInfo, Func<object, object>> _getterCache =
            new ConcurrentDictionary<PropertyInfo, Func<object, object>>();

        /// <summary>属性 Setter 委托缓存</summary>
        private readonly ConcurrentDictionary<PropertyInfo, Action<object, object>> _setterCache =
            new ConcurrentDictionary<PropertyInfo, Action<object, object>>();

        /// <summary>构造函数委托缓存</summary>
        private readonly ConcurrentDictionary<ConstructorInfo, Func<object[], object>> _constructorCache =
            new ConcurrentDictionary<ConstructorInfo, Func<object[], object>>();

        // 手动委托缓存（任意 key）
        /// <summary>手动注册委托缓存（任意 key）</summary>
        private readonly ConcurrentDictionary<object, Action<object, object[]>> _manualDelegateCache =
            new ConcurrentDictionary<object, Action<object, object[]>>();

        /// <summary>手动委托工厂缓存（提升工厂复用效率）</summary>
        private readonly ConcurrentDictionary<object, Func<Action<object, object[]>>> _delegateFactoryCache =
            new ConcurrentDictionary<object, Func<Action<object, object[]>>>();

        // 使用追踪（统一用于所有类型 key）
        /// <summary>所有 key 的使用计数（方法/属性/构造/手动委托等）</summary>
        private readonly ConcurrentDictionary<object, int> _usageCounter = new ConcurrentDictionary<object, int>();

        /// <summary>所有 key 的最后使用时间</summary>
        private readonly ConcurrentDictionary<object, DateTime> _lastUsedTime =
            new ConcurrentDictionary<object, DateTime>();

        /// <summary>手动委托缓存配置</summary>
        private ManualDelegateCacheConfiguration _manualConfig = new ManualDelegateCacheConfiguration();

        // ref/out 回退警告一次性记录
        /// <summary>ref/out 回退警告记录（每方法仅提示一次）</summary>
        private readonly ConcurrentDictionary<MethodInfo, byte> _refOutFallbackNotified =
            new ConcurrentDictionary<MethodInfo, byte>();

        #endregion

        #region 公共属性

        /// <summary>模块名称</summary>
        public override string ModuleName => "DelegateCache";

        /// <summary>
        /// 手动委托键校验器（返回 false 则拒绝缓存）
        /// </summary>
        /// <summary>
        /// 手动委托键校验器（返回 false 则拒绝缓存，支持自定义 key 规则）。
        /// </summary>
        public Func<object, bool> ManualKeyValidator { get; set; }

        /// <summary>
        /// 获取或设置手动委托配置
        /// </summary>
        /// <summary>
        /// 手动委托缓存配置（支持最大数量、最小使用、内存上限等）。
        /// </summary>
        public ManualDelegateCacheConfiguration ManualConfiguration
        {
            get => _manualConfig;
            set => _manualConfig = value ?? new ManualDelegateCacheConfiguration();
        }

        #endregion

        #region 公共API - Method

        /// <summary>
        /// 获取或创建无参方法委托。
        /// </summary>
        public Action<object> GetAction(MethodInfo method)
        {
            if (method == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_actionCache.TryGetValue(method, out var cached))
            {
                TrackUsage(method);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var action = CreateAction(method);
                _actionCache.TryAdd(method, action);
                TrackUsage(method);
                RecordCacheHit();
                return action;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Action委托失败 {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        /// <summary>
        /// 获取或创建有参方法委托。
        /// </summary>
        public Action<object, object[]> GetActionWithParams(MethodInfo method)
        {
            if (method == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_actionWithParamsCache.TryGetValue(method, out var cached))
            {
                TrackUsage(method);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var action = CreateActionWithParams(method);
                _actionWithParamsCache.TryAdd(method, action);
                TrackUsage(method);
                RecordCacheHit();
                return action;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Action(含参数)委托失败 {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        /// <summary>
        /// 获取或创建有返回值方法委托。
        /// </summary>
        public Func<object, object[], object> GetFunc(MethodInfo method)
        {
            if (method == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_funcCache.TryGetValue(method, out var cached))
            {
                TrackUsage(method);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var func = CreateFunc(method);
                _funcCache.TryAdd(method, func);
                TrackUsage(method);
                RecordCacheHit();
                return func;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Func委托失败 {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        #endregion

        #region 公共API - Property

        /// <summary>
        /// 获取或创建属性 Getter 委托。
        /// </summary>
        public Func<object, object> GetGetter(PropertyInfo property)
        {
            if (property == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_getterCache.TryGetValue(property, out var cached))
            {
                TrackUsage(property);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var getter = CreateGetter(property);
                _getterCache.TryAdd(property, getter);
                TrackUsage(property);
                RecordCacheHit();
                return getter;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Getter委托失败 {property.DeclaringType?.Name}.{property.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        /// <summary>
        /// 获取或创建属性 Setter 委托。
        /// </summary>
        public Action<object, object> GetSetter(PropertyInfo property)
        {
            if (property == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_setterCache.TryGetValue(property, out var cached))
            {
                TrackUsage(property);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var setter = CreateSetter(property);
                _setterCache.TryAdd(property, setter);
                TrackUsage(property);
                RecordCacheHit();
                return setter;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Setter委托失败 {property.DeclaringType?.Name}.{property.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        #endregion

        #region 公共API - Constructor

        /// <summary>
        /// 获取或创建构造函数委托。
        /// </summary>
        public Func<object[], object> GetConstructor(ConstructorInfo constructor)
        {
            if (constructor == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (_constructorCache.TryGetValue(constructor, out var cached))
            {
                TrackUsage(constructor);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var ctor = CreateConstructor(constructor);
                _constructorCache.TryAdd(constructor, ctor);
                TrackUsage(constructor);
                RecordCacheHit();
                return ctor;
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建Constructor委托失败 {constructor.DeclaringType?.Name}: {ex.Message}");
                RecordCacheMiss();
                return null;
            }
        }

        #endregion

        #region 公共API - 手动委托缓存

        /// <summary>
        /// 获取或创建手动委托（支持工厂缓存与 key 校验）。
        /// </summary>
        public Action<object, object[]> GetOrCreateDelegate(object key, Func<Action<object, object[]>> factory)
        {
            if (key == null || factory == null)
            {
                RecordCacheMiss();
                return null;
            }

            if (ManualKeyValidator != null)
            {
                try
                {
                    if (!ManualKeyValidator(key))
                    {
                        RecordCacheMiss();
                        return null;
                    }
                }
                catch
                {
                    RecordCacheMiss();
                    return null;
                }
            }

            if (_manualDelegateCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                Action<object, object[]> created = null;
                if (_manualConfig.EnableFactoryCache)
                {
                    var cachedFactory = _delegateFactoryCache.GetOrAdd(key, factory);
                    created = cachedFactory();
                }
                else
                {
                    created = factory();
                }

                if (created != null)
                {
                    _manualDelegateCache.TryAdd(key, created);
                    TrackUsage(key);
                    RecordCacheHit();
                    return created;
                }
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"创建手动委托失败 {key}: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 预注册手动委托。
        /// </summary>
        public void PreRegisterDelegate(object key, Action<object, object[]> delegateAction)
        {
            if (key == null || delegateAction == null) return;
            if (ManualKeyValidator != null)
            {
                try
                {
                    if (!ManualKeyValidator(key)) return;
                }
                catch
                {
                    return;
                }
            }

            _manualDelegateCache.TryAdd(key, delegateAction);
            TrackUsage(key);
        }

        /// <summary>
        /// 批量预注册手动委托。
        /// </summary>
        public void PreRegisterDelegates(IEnumerable<KeyValuePair<object, Action<object, object[]>>> delegates)
        {
            if (delegates == null) return;
            foreach (var kvp in delegates) PreRegisterDelegate(kvp.Key, kvp.Value);
        }

        /// <summary>
        /// 移除手动委托。
        /// </summary>
        public bool RemoveDelegate(object key)
        {
            if (key == null) return false;
            var removed = _manualDelegateCache.TryRemove(key, out _);
            if (removed)
            {
                _usageCounter.TryRemove(key, out _);
                _lastUsedTime.TryRemove(key, out _);
                _delegateFactoryCache.TryRemove(key, out _);
            }

            return removed;
        }

        /// <summary>
        /// 判断是否包含指定手动委托。
        /// </summary>
        public bool ContainsDelegate(object key) => key != null && _manualDelegateCache.ContainsKey(key);

        /// <summary>
        /// 获取手动委托缓存统计。
        /// </summary>
        public ManualDelegateCacheStats GetDelegateStats()
        {
            var now = DateTime.UtcNow;
            int totalUsage = 0, recentlyUsed = 0, oldDelegates = 0;
            foreach (var key in _manualDelegateCache.Keys)
            {
                if (_usageCounter.TryGetValue(key, out var u)) totalUsage += u;
                if (_lastUsedTime.TryGetValue(key, out var t))
                {
                    if ((now - t).TotalMinutes < 30) recentlyUsed++;
                    if ((now - t).TotalHours > 1) oldDelegates++;
                }
            }

            var totalDelegates = _manualDelegateCache.Count;
            var average = totalDelegates > 0 ? (float)totalUsage / totalDelegates : 0f;
            return new ManualDelegateCacheStats
            {
                TotalDelegates = totalDelegates,
                TotalUsage = totalUsage,
                AverageUsage = average,
                RecentlyUsed = recentlyUsed,
                OldDelegates = oldDelegates,
                MemoryPressureLevel = DetermineMemoryPressureLevel()
            };
        }

        #endregion

        #region 创建委托实现

        // 无参 Action
        /// <summary>
        /// 创建无参方法委托（支持静态/实例，ref/out 自动回退）。
        /// </summary>
        private Action<object> CreateAction(MethodInfo method)
        {
            if (NeedRefOutFallback(method)) return inst => { method.Invoke(inst, null); };
            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var instanceCast = method.IsStatic ? null : Expression.Convert(instanceParam, method.DeclaringType);
                var call = method.IsStatic ? Expression.Call(method) : Expression.Call(instanceCast, method);
                return Expression.Lambda<Action<object>>(call, instanceParam).Compile();
            }
            catch
            {
                return inst => method.Invoke(inst, null);
            }
        }

        // 有参 Action
        /// <summary>
        /// 创建有参方法委托（支持静态/实例，ref/out 自动回退）。
        /// </summary>
        private Action<object, object[]> CreateActionWithParams(MethodInfo method)
        {
            if (NeedRefOutFallback(method)) return (inst, arr) => method.Invoke(inst, arr);
            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var paramsParam = Expression.Parameter(typeof(object[]), "parameters");
                var pInfos = method.GetParameters();
                var exprs = new Expression[pInfos.Length];
                for (int i = 0; i < pInfos.Length; i++)
                {
                    var idx = Expression.Constant(i);
                    var access = Expression.ArrayIndex(paramsParam, idx);
                    exprs[i] = Expression.Convert(access, pInfos[i].ParameterType);
                }

                var instanceCast = method.IsStatic ? null : Expression.Convert(instanceParam, method.DeclaringType);
                var call = method.IsStatic
                    ? Expression.Call(method, exprs)
                    : Expression.Call(instanceCast, method, exprs);
                return Expression.Lambda<Action<object, object[]>>(call, instanceParam, paramsParam).Compile();
            }
            catch
            {
                return (inst, arr) => method.Invoke(inst, arr);
            }
        }

        // Func
        /// <summary>
        /// 创建有返回值方法委托（支持静态/实例，ref/out 自动回退）。
        /// </summary>
        private Func<object, object[], object> CreateFunc(MethodInfo method)
        {
            if (NeedRefOutFallback(method)) return (inst, arr) => method.Invoke(inst, arr);
            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var paramsParam = Expression.Parameter(typeof(object[]), "parameters");
                var pInfos = method.GetParameters();
                var exprs = new Expression[pInfos.Length];
                for (int i = 0; i < pInfos.Length; i++)
                {
                    var idx = Expression.Constant(i);
                    var access = Expression.ArrayIndex(paramsParam, idx);
                    exprs[i] = Expression.Convert(access, pInfos[i].ParameterType);
                }

                var instanceCast = method.IsStatic ? null : Expression.Convert(instanceParam, method.DeclaringType);
                var call = method.IsStatic
                    ? Expression.Call(method, exprs)
                    : Expression.Call(instanceCast, method, exprs);
                var callCast = Expression.Convert(call, typeof(object));
                return Expression.Lambda<Func<object, object[], object>>(callCast, instanceParam, paramsParam)
                    .Compile();
            }
            catch
            {
                return (inst, arr) => method.Invoke(inst, arr);
            }
        }

        // Getter（支持静态）
        /// <summary>
        /// 创建属性 Getter 委托（支持静态/实例）。
        /// </summary>
        private Func<object, object> CreateGetter(PropertyInfo property)
        {
            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                Expression target = null;
                if (!property.GetMethod.IsStatic)
                    target = Expression.Convert(instanceParam, property.DeclaringType);
                var access = Expression.Property(property.GetMethod.IsStatic ? null : target, property);
                var cast = Expression.Convert(access, typeof(object));
                return Expression.Lambda<Func<object, object>>(cast, instanceParam).Compile();
            }
            catch
            {
                return inst =>
                    property.GetValue(property.GetMethod != null && property.GetMethod.IsStatic ? null : inst);
            }
        }

        // Setter（支持静态）
        /// <summary>
        /// 创建属性 Setter 委托（支持静态/实例）。
        /// </summary>
        private Action<object, object> CreateSetter(PropertyInfo property)
        {
            try
            {
                var instanceParam = Expression.Parameter(typeof(object), "instance");
                var valueParam = Expression.Parameter(typeof(object), "value");
                Expression target = null;
                if (!property.GetMethod.IsStatic)
                    target = Expression.Convert(instanceParam, property.DeclaringType);
                var valueCast = Expression.Convert(valueParam, property.PropertyType);
                var prop = Expression.Property(property.GetMethod.IsStatic ? null : target, property);
                var assign = Expression.Assign(prop, valueCast);
                return Expression.Lambda<Action<object, object>>(assign, instanceParam, valueParam).Compile();
            }
            catch
            {
                return (inst, val) =>
                    property.SetValue(property.GetMethod != null && property.GetMethod.IsStatic ? null : inst, val);
            }
        }

        // 构造函数
        /// <summary>
        /// 创建构造函数委托。
        /// </summary>
        private Func<object[], object> CreateConstructor(ConstructorInfo constructor)
        {
            try
            {
                var paramsParam = Expression.Parameter(typeof(object[]), "parameters");
                var pInfos = constructor.GetParameters();
                var exprs = new Expression[pInfos.Length];
                for (int i = 0; i < pInfos.Length; i++)
                {
                    var idx = Expression.Constant(i);
                    var access = Expression.ArrayIndex(paramsParam, idx);
                    exprs[i] = Expression.Convert(access, pInfos[i].ParameterType);
                }

                var newExpr = Expression.New(constructor, exprs);
                var cast = Expression.Convert(newExpr, typeof(object));
                return Expression.Lambda<Func<object[], object>>(cast, paramsParam).Compile();
            }
            catch
            {
                return arr => constructor.Invoke(arr);
            }
        }

        /// <summary>
        /// 判断方法是否包含 ref/out 参数，若有则回退反射调用。
        /// </summary>
        private bool NeedRefOutFallback(MethodInfo method)
        {
            var ps = method.GetParameters();
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].ParameterType.IsByRef)
                {
                    if (_refOutFallbackNotified.TryAdd(method, 1))
                    {
                         UnityEngine.Debug.LogWarning($"方法 {method.DeclaringType?.Name}.{method.Name} 含 ref/out 参数，委托化暂不支持，已回退反射调用。");
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 记录 key 的使用次数与最后使用时间。
        /// </summary>
        private void TrackUsage(object key)
        {
            _usageCounter.AddOrUpdate(key, 1, (_, c) => c + 1);
            _lastUsedTime[key] = DateTime.UtcNow;
        }

        #endregion

        #region Base 实现

        /// <summary>
        /// 清空所有缓存与统计。
        /// </summary>
        protected override void OnClearCache()
        {
            _actionCache.Clear();
            _actionWithParamsCache.Clear();
            _funcCache.Clear();
            _getterCache.Clear();
            _setterCache.Clear();
            _constructorCache.Clear();
            _manualDelegateCache.Clear();
            _delegateFactoryCache.Clear();
            _usageCounter.Clear();
            _lastUsedTime.Clear();
            _refOutFallbackNotified.Clear();
        }

        /// <summary>
        /// 初始化钩子（已无策略注册）。
        /// </summary>
        protected override void OnInitialize()
        {
            base.OnInitialize();
            // 模块级驱逐策略体系已统一到全局 ICleanupStrategy，不再在模块内注册
        }

        /// <summary>
        /// 智能清理：长时间未使用、低频率、内存压力三段式批量淘汰。
        /// </summary>
        protected override int OnSmartCleanup()
        {
            // 恢复原模块级智能清理（长时间未使用 + 低频率 + 内存压力近似）
            int removed = 0;
            try
            {
                var now = DateTime.UtcNow;
                const int InactiveMinutes = 30; // 未使用阈值
                const int LowUsageThreshold = 1; // 低使用计数阈值
                const int LowUsageIdleMinutes = 30; // 低使用 + 空闲窗口
                const long MemoryLimitBytes = 25L * 1024 * 1024; // 模块软内存上限
                const int TrimBatchSize = 500; // 单次内存压缩批量

                // 1) 长时间未使用驱逐
                if (_lastUsedTime.Count > 0)
                {
                    foreach (var kv in _lastUsedTime.ToArray())
                    {
                        if ((now - kv.Value).TotalMinutes > InactiveMinutes)
                        {
                            if (RemoveGeneralCacheKey(kv.Key))
                                removed++;
                        }
                    }
                }

                // 2) 低频率 + 空闲驱逐（控制最大批次，防止过度）
                if (_usageCounter.Count > 0 && removed < 800) // 设定一次清理总体上限 ~800
                {
                    int lowFreqRemoved = 0;
                    foreach (var kv in _usageCounter.ToArray())
                    {
                        if (lowFreqRemoved >= 300) break; // 低频段控制批次
                        _lastUsedTime.TryGetValue(kv.Key, out var last);
                        if (kv.Value <= LowUsageThreshold && (now - last).TotalMinutes >= LowUsageIdleMinutes)
                        {
                            if (RemoveGeneralCacheKey(kv.Key))
                            {
                                removed++;
                                lowFreqRemoved++;
                            }
                        }
                    }
                }

                // 3) 内存压力（模块自身估算）超过阈值 → LRU 近似裁剪
                if (GetMemoryUsage() > MemoryLimitBytes && _lastUsedTime.Count > 0)
                {
                    var ordered = _lastUsedTime.ToArray().OrderBy(k => k.Value).Take(TrimBatchSize).ToArray();
                    foreach (var kv in ordered)
                    {
                        if (RemoveGeneralCacheKey(kv.Key)) removed++;
                    }
                }
            }
            catch (Exception ex)
            {
                 UnityEngine.Debug.LogWarning($"DelegateCacheModule OnSmartCleanup 异常: {ex.Message}");
            }

            return removed;
        }

        /// <summary>
        /// 通用 key 移除（支持 MethodInfo/PropertyInfo/ConstructorInfo/手动 key）。
        /// </summary>
        private bool RemoveGeneralCacheKey(object key)
        {
            bool removed = false;
            if (key is MethodInfo m)
            {
                removed = _actionCache.TryRemove(m, out _) |
                          _actionWithParamsCache.TryRemove(m, out _) |
                          _funcCache.TryRemove(m, out _);
            }
            else if (key is PropertyInfo p)
            {
                removed = _getterCache.TryRemove(p, out _) | _setterCache.TryRemove(p, out _);
            }
            else if (key is ConstructorInfo c)
            {
                removed = _constructorCache.TryRemove(c, out _);
            }

            if (removed)
            {
                _usageCounter.TryRemove(key, out _);
                _lastUsedTime.TryRemove(key, out _);
                _delegateFactoryCache.TryRemove(key, out _);
            }

            return removed;
        }

        /// <summary>
        /// 获取所有缓存项总数。
        /// </summary>
        protected override int GetCacheItemCount()
        {
            return _actionCache.Count + _actionWithParamsCache.Count + _funcCache.Count +
                   _getterCache.Count + _setterCache.Count + _constructorCache.Count +
                   _manualDelegateCache.Count;
        }

        /// <summary>
        /// 估算内存使用量（每项约200字节）。
        /// </summary>
        public override long GetMemoryUsage()
        {
            // 粗略估算：每个委托 200 字节
            return GetCacheItemCount() * 200;
        }

        /// <summary>
        /// 获取扩展统计信息（含各类缓存数、手动委托统计等）。
        /// </summary>
        protected override Dictionary<string, object> GetExtendedStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                // 中文显示键
                ["Action缓存数"] = _actionCache.Count,
                ["Action(含参数)缓存数"] = _actionWithParamsCache.Count,
                ["Func缓存数"] = _funcCache.Count,
                ["Getter缓存数"] = _getterCache.Count,
                ["Setter缓存数"] = _setterCache.Count,
                ["构造函数缓存数"] = _constructorCache.Count,
                ["手动委托数量"] = _manualDelegateCache.Count,
                ["委托工厂缓存数"] = _delegateFactoryCache.Count,
                ["总使用次数"] = _usageCounter.Values.Sum()
            };

            var m = GetDelegateStats();
            // 中文扩展
            stats["手动委托-总使用次数"] = m.TotalUsage;
            stats["手动委托-平均使用"] = m.AverageUsage;
            stats["手动委托-近期使用数"] = m.RecentlyUsed;
            stats["手动委托-旧委托数"] = m.OldDelegates;
            stats["配置:手动委托最大数量"] = _manualConfig.MaxDelegateCount;
            stats["配置:未使用阈值(分钟)"] = _manualConfig.UnusedDelegateThresholdMinutes;
            stats["配置:最大内存(MB)"] = _manualConfig.MaxMemoryUsageMB;
            return stats;
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 手动委托缓存配置（支持最大数量、最小使用、内存上限、工厂缓存等）。
        /// </summary>
        public class ManualDelegateCacheConfiguration
        {
            public int UnusedDelegateThresholdMinutes { get; set; } = 60;
            public int MaxDelegateCount { get; set; } = 2000;
            public int MinUsageThreshold { get; set; } = 2;
            public int MaxMemoryUsageMB { get; set; } = 50;
            public bool EnableFactoryCache { get; set; } = true;
        }

        /// <summary>
        /// 手动委托缓存统计结构。
        /// </summary>
        public struct ManualDelegateCacheStats
        {
            public int TotalDelegates;
            public int TotalUsage;
            public float AverageUsage;
            public int RecentlyUsed;
            public int OldDelegates;
            public string MemoryPressureLevel;

            public override string ToString()
            {
                return "手动委托缓存统计:\n" +
                       $"总数: {TotalDelegates}, 使用次数: {TotalUsage}\n" +
                       $"平均使用: {AverageUsage:F2}, 近期使用: {RecentlyUsed}\n" +
                       $"旧委托: {OldDelegates}, 内存压力: {MemoryPressureLevel}";
            }
        }

        #endregion
    }
}