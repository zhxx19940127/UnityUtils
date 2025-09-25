using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReflectionToolkit.Core;

namespace ReflectionToolkit.Modules
{
    /// <summary>
    /// 类型缓存模块 - 基于现有TypeCache重构的高性能类型信息缓存
    /// 类型缓存模块，负责高性能缓存类型、方法、属性、字段、特性等反射信息。
    /// </summary>
    public class TypeCacheModule : BaseCacheModule
    {
        #region 私有字段

        // 类型缓存
        // 类型名到 Type 的缓存
        private readonly ConcurrentDictionary<string, Type> _typeCache = new ConcurrentDictionary<string, Type>();

        // 方法缓存（旧：不区分参数签名，保留兼容）
        // (Type, 方法名+BindingFlags) 到 MethodInfo 的缓存（兼容旧逻辑，不区分参数签名）
        private readonly ConcurrentDictionary<(Type, string), MethodInfo> _methodCache =
            new ConcurrentDictionary<(Type, string), MethodInfo>();

        // 方法重载缓存（新）：按 (Type, MethodName, BindingFlags, ParamSignature) 精确区分
        // ParamSignature 采用 FullName 以 '|' 连接；无参数时为 "__EMPTY__"
        // (Type, 方法名, BindingFlags, 参数签名) 到 MethodInfo 的缓存（支持重载精确匹配）
        private readonly ConcurrentDictionary<(Type, string, BindingFlags, string), MethodInfo> _methodOverloadCache =
            new ConcurrentDictionary<(Type, string, BindingFlags, string), MethodInfo>();

        // 属性缓存
        // (Type, 属性名+BindingFlags) 到 PropertyInfo 的缓存
        private readonly ConcurrentDictionary<(Type, string), PropertyInfo> _propertyCache =
            new ConcurrentDictionary<(Type, string), PropertyInfo>();

        // 字段缓存
        // (Type, 字段名+BindingFlags) 到 FieldInfo 的缓存
        private readonly ConcurrentDictionary<(Type, string), FieldInfo> _fieldCache =
            new ConcurrentDictionary<(Type, string), FieldInfo>();

        // 特性缓存
        // Type 到特性数组的缓存
        private readonly ConcurrentDictionary<Type, object[]> _attributeCache =
            new ConcurrentDictionary<Type, object[]>();

        // 方法特性缓存
        // MethodInfo 到特性数组的缓存
        private readonly ConcurrentDictionary<MethodInfo, object[]> _methodAttributeCache =
            new ConcurrentDictionary<MethodInfo, object[]>();

        // 字段特性缓存
        // FieldInfo 到特性数组的缓存
        private readonly ConcurrentDictionary<FieldInfo, object[]> _fieldAttributeCache =
            new ConcurrentDictionary<FieldInfo, object[]>();

        // 按特性筛选的方法列表缓存 key: (Type, AttributeType)
        // (Type, AttributeType) 到拥有该特性的方法数组的缓存
        private readonly ConcurrentDictionary<(Type, Type), MethodInfo[]> _methodsWithAttributeCache =
            new ConcurrentDictionary<(Type, Type), MethodInfo[]>();

        // 构造函数缓存
        // (Type, 参数签名) 到 ConstructorInfo 的缓存
        private readonly ConcurrentDictionary<(Type, string), ConstructorInfo> _constructorCache =
            new ConcurrentDictionary<(Type, string), ConstructorInfo>();

        // 使用频率追踪
        // 缓存项使用频率计数器
        private readonly ConcurrentDictionary<object, int> _usageCounter = new ConcurrentDictionary<object, int>();

        // 缓存项最后一次访问时间
        private readonly ConcurrentDictionary<object, DateTime> _lastUsedTime =
            new ConcurrentDictionary<object, DateTime>();

        #endregion

        #region 属性

        /// <summary>
        /// 模块名称
        /// </summary>
        public override string ModuleName => "TypeCache";

        #endregion

        #region 公共方法

        /// <summary>
        /// 尝试获取类型
        /// 尝试获取类型（支持自动查找所有已加载程序集）
        /// </summary>
        /// <param name="typeName">类型全名</param>
        /// <param name="type">输出类型</param>
        /// <returns>是否找到</returns>
        public bool TryGetType(string typeName, out Type type)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                type = null;
                RecordCacheMiss();
                return false;
            }

            if (_typeCache.TryGetValue(typeName, out type))
            {
                TrackUsage(typeName);
                RecordCacheHit();
                return true;
            }

            // 尝试解析类型
            try
            {
                type = Type.GetType(typeName, false);
                if (type == null)
                {
                    // 尝试从所有已加载程序集中查找
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(typeName, false);
                        if (type != null) break;
                    }
                }

                if (type != null)
                {
                    _typeCache.TryAdd(typeName, type);
                    TrackUsage(typeName);
                    RecordCacheHit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"解析类型 {typeName} 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return false;
        }

        /// <summary>
        /// 获取类型
        /// </summary>
        /// <summary>
        /// 获取类型（找不到返回 null）
        /// </summary>
        /// <param name="typeName">类型全名</param>
        /// <returns>Type 或 null</returns>
        public Type GetType(string typeName)
        {
            return TryGetType(typeName, out var type) ? type : null;
        }

        /// <summary>
        /// 获取方法（旧逻辑：不指定参数签名；若存在多个重载则由 CLR 的 GetMethod 内部解析第一匹配）
        /// 获取方法（不指定参数签名，兼容旧逻辑，若有重载取第一个匹配）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="flags">绑定标志</param>
        /// <returns>MethodInfo 或 null</returns>
        public MethodInfo GetMethod(Type type, string methodName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                RecordCacheMiss();
                return null;
            }

            var key = (type, $"{methodName}_{flags}");
            if (_methodCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var method = type.GetMethod(methodName, flags);
                if (method != null)
                {
                    _methodCache.TryAdd(key, method);
                    TrackUsage(key);
                    RecordCacheHit();
                    return method;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取方法 {type.Name}.{methodName} 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 获取指定参数签名的方法（新重载感知版本）
        /// 参数数组可为空/null 表示无参签名；完全匹配（含引用顺序）
        /// </summary>
        /// <summary>
        /// 获取指定参数签名的方法（支持重载精确匹配）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="methodName">方法名</param>
        /// <param name="parameterTypes">参数类型数组</param>
        /// <param name="flags">绑定标志</param>
        /// <returns>MethodInfo 或 null</returns>
        public MethodInfo GetMethod(Type type, string methodName, Type[] parameterTypes,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            if (type == null || string.IsNullOrEmpty(methodName))
            {
                RecordCacheMiss();
                return null;
            }

            parameterTypes = parameterTypes ?? Type.EmptyTypes;
            var signature = parameterTypes.Length == 0
                ? "__EMPTY__"
                : string.Join("|", parameterTypes.Select(t => t.FullName));
            var oKey = (type, methodName, flags, signature);
            if (_methodOverloadCache.TryGetValue(oKey, out var cached))
            {
                TrackUsage(oKey);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var method = type.GetMethod(methodName, flags, Type.DefaultBinder, parameterTypes, null);
                if (method != null)
                {
                    _methodOverloadCache.TryAdd(oKey, method);
                    TrackUsage(oKey);
                    RecordCacheHit();
                    return method;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取方法重载 {type.Name}.{methodName}({signature}) 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 获取属性（带缓存）
        /// </summary>
        /// <summary>
        /// 获取属性（带缓存）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="propertyName">属性名</param>
        /// <param name="flags">绑定标志</param>
        /// <returns>PropertyInfo 或 null</returns>
        public PropertyInfo GetProperty(Type type, string propertyName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            if (type == null || string.IsNullOrEmpty(propertyName))
            {
                RecordCacheMiss();
                return null;
            }

            var key = (type, $"{propertyName}_{flags}");

            if (_propertyCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var property = type.GetProperty(propertyName, flags);
                if (property != null)
                {
                    _propertyCache.TryAdd(key, property);
                    TrackUsage(key);
                    RecordCacheHit();
                    return property;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取属性 {type.Name}.{propertyName} 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 获取字段（带缓存）
        /// </summary>
        /// <summary>
        /// 获取字段（带缓存）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="fieldName">字段名</param>
        /// <param name="flags">绑定标志</param>
        /// <returns>FieldInfo 或 null</returns>
        public FieldInfo GetField(Type type, string fieldName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            if (type == null || string.IsNullOrEmpty(fieldName))
            {
                RecordCacheMiss();
                return null;
            }

            var key = (type, $"{fieldName}_{flags}");

            if (_fieldCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var field = type.GetField(fieldName, flags);
                if (field != null)
                {
                    _fieldCache.TryAdd(key, field);
                    TrackUsage(key);
                    RecordCacheHit();
                    return field;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取字段 {type.Name}.{fieldName} 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 获取类型特性（带缓存）
        /// </summary>
        /// <summary>
        /// 获取类型特性（带缓存）
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>特性数组</returns>
        public T[] GetCustomAttributes<T>(Type type, bool inherit = false) where T : Attribute
        {
            if (type == null)
            {
                RecordCacheMiss();
                return Array.Empty<T>();
            }

            var key = type;

            if (_attributeCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached.OfType<T>().ToArray();
            }

            try
            {
                var attributes = type.GetCustomAttributes(inherit);
                if (attributes != null)
                {
                    _attributeCache.TryAdd(key, attributes);
                    TrackUsage(key);
                    RecordCacheHit();
                    return attributes.OfType<T>().ToArray();
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取类型 {type.Name} 特性失败: {ex.Message}");
            }

            RecordCacheMiss();
            return Array.Empty<T>();
        }

        /// <summary>
        /// 获取构造函数（带缓存）
        /// </summary>
        /// <summary>
        /// 获取构造函数（带缓存）
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="parameterTypes">参数类型数组</param>
        /// <returns>ConstructorInfo 或 null</returns>
        public ConstructorInfo GetConstructor(Type type, params Type[] parameterTypes)
        {
            if (type == null)
            {
                RecordCacheMiss();
                return null;
            }

            var paramTypesKey = parameterTypes?.Length > 0
                ? string.Join(",", parameterTypes.Select(t => t.FullName))
                : "Empty";
            var key = (type, paramTypesKey);

            if (_constructorCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var constructor = type.GetConstructor(parameterTypes ?? Type.EmptyTypes);
                if (constructor != null)
                {
                    _constructorCache.TryAdd(key, constructor);
                    TrackUsage(key);
                    RecordCacheHit();
                    return constructor;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取构造函数 {type.Name}({paramTypesKey}) 失败: {ex.Message}");
            }

            RecordCacheMiss();
            return null;
        }

        /// <summary>
        /// 获取方法特性（带缓存）
        /// </summary>
        /// <summary>
        /// 获取方法特性（带缓存）
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="method">方法</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>特性数组</returns>
        public T[] GetMethodCustomAttributes<T>(MethodInfo method, bool inherit = false) where T : Attribute
        {
            if (method == null)
            {
                RecordCacheMiss();
                return Array.Empty<T>();
            }

            if (_methodAttributeCache.TryGetValue(method, out var cached))
            {
                TrackUsage(method);
                RecordCacheHit();
                return cached.OfType<T>().ToArray();
            }

            try
            {
                var attributes = method.GetCustomAttributes(inherit);
                _methodAttributeCache.TryAdd(method, attributes);
                TrackUsage(method);
                RecordCacheHit();
                return attributes.OfType<T>().ToArray();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取方法特性失败 {method.DeclaringType?.Name}.{method.Name}: {ex.Message}");
            }

            RecordCacheMiss();
            return Array.Empty<T>();
        }

        /// <summary>
        /// 获取具有指定特性的方法数组（带缓存）
        /// </summary>
        /// <summary>
        /// 获取具有指定特性的方法数组（带缓存）
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="flags">绑定标志</param>
        /// <returns>MethodInfo 数组</returns>
        public MethodInfo[] GetMethodsWithAttribute<T>(Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) where T : Attribute
        {
            if (type == null)
            {
                RecordCacheMiss();
                return Array.Empty<MethodInfo>();
            }

            var attrType = typeof(T);
            var key = (type, attrType);

            if (_methodsWithAttributeCache.TryGetValue(key, out var cached))
            {
                TrackUsage(key);
                RecordCacheHit();
                return cached;
            }

            try
            {
                var methods = type.GetMethods(flags);
                var result = new List<MethodInfo>();
                for (int i = 0; i < methods.Length; i++)
                {
                    var m = methods[i];
                    var attrs = GetMethodCustomAttributes<T>(m);
                    if (attrs.Length > 0) result.Add(m);
                }

                var arr = result.ToArray();
                _methodsWithAttributeCache.TryAdd(key, arr);
                TrackUsage(key);
                RecordCacheHit();
                return arr;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取带特性的方法失败 {type.Name} [{attrType.Name}]: {ex.Message}");
            }

            RecordCacheMiss();
            return Array.Empty<MethodInfo>();
        }

        /// <summary>
        /// 获取字段特性（带缓存）
        /// </summary>
        /// <summary>
        /// 获取字段特性（带缓存）
        /// </summary>
        /// <typeparam name="T">特性类型</typeparam>
        /// <param name="field">字段</param>
        /// <param name="inherit">是否继承</param>
        /// <returns>特性数组</returns>
        public T[] GetFieldCustomAttributes<T>(FieldInfo field, bool inherit = false) where T : Attribute
        {
            if (field == null)
            {
                RecordCacheMiss();
                return Array.Empty<T>();
            }

            if (_fieldAttributeCache.TryGetValue(field, out var cached))
            {
                TrackUsage(field);
                RecordCacheHit();
                return cached.OfType<T>().ToArray();
            }

            try
            {
                var attributes = field.GetCustomAttributes(inherit);
                _fieldAttributeCache.TryAdd(field, attributes);
                TrackUsage(field);
                RecordCacheHit();
                return attributes.OfType<T>().ToArray();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"获取字段特性失败 {field.DeclaringType?.Name}.{field.Name}: {ex.Message}");
            }

            RecordCacheMiss();
            return Array.Empty<T>();
        }

        #endregion

        #region 重写方法

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        protected override void OnClearCache()
        {
            _typeCache.Clear();
            _methodCache.Clear();
            _methodOverloadCache.Clear();
            _propertyCache.Clear();
            _fieldCache.Clear();
            _attributeCache.Clear();
            _methodAttributeCache.Clear();
            _fieldAttributeCache.Clear();
            _methodsWithAttributeCache.Clear();
            _constructorCache.Clear();
            _usageCounter.Clear();
            _lastUsedTime.Clear();
        }

        /// <summary>
        /// 智能清理缓存（按空闲时间、低频、内存压力等多维度裁剪）
        /// </summary>
        /// <returns>移除的缓存项数量</returns>
        protected override int OnSmartCleanup()
        {
            int removed = 0;
            try
            {
                var now = DateTime.UtcNow;
                const int InactiveMinutes = 30;
                const int LowUsageThreshold = 1;
                const int LowUsageIdleMinutes = 30;
                const long MemoryLimitBytes = 30L * 1024 * 1024;
                const int TrimBatchSize = 600;

                // 聚合可参与驱逐的时间字典（_lastUsedTime 已统一 key）
                if (_lastUsedTime.Count > 0)
                {
                    foreach (var kv in _lastUsedTime.ToArray())
                    {
                        if ((now - kv.Value).TotalMinutes > InactiveMinutes)
                        {
                            if (TryRemoveKeyFromAllCaches(kv.Key)) removed++;
                        }
                    }
                }

                // 低频率 + 空闲
                if (_usageCounter.Count > 0 && removed < 900)
                {
                    int lowFreqRemoved = 0;
                    foreach (var kv in _usageCounter.ToArray())
                    {
                        if (lowFreqRemoved >= 400) break;
                        _lastUsedTime.TryGetValue(kv.Key, out var last);
                        if (kv.Value <= LowUsageThreshold && (now - last).TotalMinutes >= LowUsageIdleMinutes)
                        {
                            if (TryRemoveKeyFromAllCaches(kv.Key))
                            {
                                removed++;
                                lowFreqRemoved++;
                            }
                        }
                    }
                }

                // 内存压力裁剪
                if (GetMemoryUsage() > MemoryLimitBytes && _lastUsedTime.Count > 0)
                {
                    var ordered = _lastUsedTime.ToArray().OrderBy(k => k.Value).Take(TrimBatchSize).ToArray();
                    foreach (var kv in ordered)
                    {
                        if (TryRemoveKeyFromAllCaches(kv.Key)) removed++;
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"TypeCacheModule OnSmartCleanup 异常: {ex.Message}");
            }

            return removed;
        }

        /// <summary>
        /// 尝试从所有缓存字典移除指定 key
        /// </summary>
        /// <param name="key">缓存项 key</param>
        /// <returns>是否有移除</returns>
        private bool TryRemoveKeyFromAllCaches(object key)
        {
            bool any = false;
            // 统一遍历所有 ConcurrentDictionary 字段尝试删除（性能可接受：仅清理路径）
            foreach (var f in GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                var ft = f.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(ConcurrentDictionary<,>))
                {
                    var dict = f.GetValue(this);
                    var genArgs = ft.GetGenericArguments();
                    var keyType = genArgs[0];
                    if (!keyType.IsInstanceOfType(key)) continue; // key类型不匹配
                    var tryRemove = ft.GetMethod("TryRemove", new[] { keyType, genArgs[1].MakeByRefType() });
                    if (tryRemove != null)
                    {
                        object[] args = new object[] { key, null };
                        try
                        {
                            var r = tryRemove.Invoke(dict, args);
                            if (r is bool b && b) any = true;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (any)
            {
                _usageCounter.TryRemove(key, out _);
                _lastUsedTime.TryRemove(key, out _);
            }

            return any;
        }

        /// <summary>
        /// 获取所有缓存项总数
        /// </summary>
        /// <returns>缓存项数量</returns>
        protected override int GetCacheItemCount()
        {
            return _typeCache.Count + _methodCache.Count + _methodOverloadCache.Count + _propertyCache.Count +
                   _fieldCache.Count + _attributeCache.Count + _methodAttributeCache.Count +
                   _fieldAttributeCache.Count + _methodsWithAttributeCache.Count + _constructorCache.Count;
        }

        /// <summary>
        /// 获取内存使用估算值
        /// </summary>
        /// <returns>估算字节数</returns>
        public override long GetMemoryUsage()
        {
            // 简化的内存使用估算
            return (GetCacheItemCount() * 100); // 假设每个缓存项平均100字节
        }

        /// <summary>
        /// 获取扩展统计信息（各类缓存项数量、总使用次数等）
        /// </summary>
        /// <returns>统计字典</returns>
        protected override Dictionary<string, object> GetExtendedStatistics()
        {
            return new Dictionary<string, object>
            {
                // 中文显示键
                ["类型缓存数"] = _typeCache.Count,
                ["方法缓存数"] = _methodCache.Count,
                ["方法重载缓存数"] = _methodOverloadCache.Count,
                ["属性缓存数"] = _propertyCache.Count,
                ["字段缓存数"] = _fieldCache.Count,
                ["类型特性缓存数"] = _attributeCache.Count,
                ["方法特性缓存数"] = _methodAttributeCache.Count,
                ["字段特性缓存数"] = _fieldAttributeCache.Count,
                ["带特性的方法缓存数"] = _methodsWithAttributeCache.Count,
                ["构造函数缓存数"] = _constructorCache.Count,
                ["总使用次数"] = _usageCounter.Values.Sum()
            };
        }

        /// <summary>
        /// 预热常用类型缓存
        /// </summary>
        protected override void OnWarmupCache()
        {
            // 预热常用类型
            var commonTypes = new[]
            {
                typeof(string), typeof(int), typeof(float), typeof(double),
                typeof(bool), typeof(DateTime), typeof(object), typeof(void)
            };

            foreach (var type in commonTypes)
            {
                _typeCache.TryAdd(type.FullName, type);
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 记录缓存项访问（更新使用次数和最后访问时间）
        /// </summary>
        /// <param name="key">缓存项 key</param>
        private void TrackUsage(object key)
        {
            _usageCounter.AddOrUpdate(key, 1, (k, count) => count + 1);
            _lastUsedTime[key] = DateTime.UtcNow;
        }

        #endregion
    }
}