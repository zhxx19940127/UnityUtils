using System;
using System.Collections.Generic;
using System.Reflection;
using ReflectionToolkit.Modules;

namespace ReflectionToolkit
{
    /// <summary>
    /// 反射工具助手类 - 提供便捷的反射操作API
    /// </summary>
    public static class ReflectionHelper
    {
        #region 类型相关操作

        /// <summary>
        /// 尝试获取类型
        /// </summary>
        public static bool TryGetType(string typeName, out Type type)
        {
            type = null;

            if (string.IsNullOrEmpty(typeName))
                return false;

            // 优先使用类型缓存模块
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            if (module != null && module.TryGetType(typeName, out type))
            {
                return true;
            }

            // 回退：直接尝试通过 Type.GetType 获取
            try
            {
                type = Type.GetType(typeName, false);
                if (type != null) return true;

                // 遍历当前已加载程序集进行解析（避免重复解析可根据需要加简单缓存）
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(typeName, false);
                    if (type != null) return true;
                }
            }
            catch
            {
                type = null; // 保证 out 参数安全
            }

            return false;
        }

        /// <summary>
        /// 获取类型
        /// </summary>
        public static Type GetType(string typeName)
        {
            return TryGetType(typeName, out var type) ? type : null;
        }

        /// <summary>
        /// 获取泛型类型
        /// </summary>
        public static Type GetGenericType(string genericTypeName, params Type[] typeArguments)
        {
            var genericType = GetType(genericTypeName);
            if (genericType == null || !genericType.IsGenericTypeDefinition)
                return null;

            try
            {
                return genericType.MakeGenericType(typeArguments);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 检查类型是否继承自指定基类
        /// </summary>
        public static bool IsAssignableFrom<T>(Type type)
        {
            return type != null && typeof(T).IsAssignableFrom(type);
        }

        /// <summary>
        /// 检查类型是否实现了指定接口
        /// </summary>
        public static bool ImplementsInterface<T>(Type type) where T : class
        {
            return type != null && typeof(T).IsInterface && typeof(T).IsAssignableFrom(type);
        }

        #endregion

        #region 方法相关操作

        /// <summary>
        /// 获取方法信息
        /// </summary>
        public static MethodInfo GetMethod(Type type, string methodName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetMethod(type, methodName, flags);
        }

        /// <summary>
        /// 获取方法信息（泛型版本）
        /// </summary>
        public static MethodInfo GetMethod<T>(string methodName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            return GetMethod(typeof(T), methodName, flags);
        }

        /// <summary>
        /// 调用方法（无参数）
        /// </summary>
        public static object InvokeMethod(object instance, MethodInfo method)
        {
            if (method == null) return null;

            var delegateModule = ReflectionToolkit.GetModule<DelegateCacheModule>();
            if (delegateModule != null)
            {
                if (method.ReturnType == typeof(void))
                {
                    var action = delegateModule.GetAction(method);
                    action?.Invoke(instance);
                    return null;
                }
                else
                {
                    var func = delegateModule.GetFunc(method);
                    return func?.Invoke(instance, Array.Empty<object>());
                }
            }

            return method.Invoke(instance, null);
        }

        /// <summary>
        /// 调用方法（有参数）
        /// </summary>
        public static object InvokeMethod(object instance, MethodInfo method, params object[] parameters)
        {
            if (method == null) return null;

            var delegateModule = ReflectionToolkit.GetModule<DelegateCacheModule>();
            if (delegateModule != null)
            {
                if (method.ReturnType == typeof(void))
                {
                    var action = delegateModule.GetActionWithParams(method);
                    action?.Invoke(instance, parameters);
                    return null;
                }
                else
                {
                    var func = delegateModule.GetFunc(method);
                    return func?.Invoke(instance, parameters);
                }
            }

            return method.Invoke(instance, parameters);
        }

        /// <summary>
        /// 调用静态方法
        /// </summary>
        public static object InvokeStaticMethod(Type type, string methodName, params object[] parameters)
        {
            var method = GetMethod(type, methodName, BindingFlags.Public | BindingFlags.Static);
            return InvokeMethod(null, method, parameters);
        }

        /// <summary>
        /// 调用静态方法（泛型版本）
        /// </summary>
        public static object InvokeStaticMethod<T>(string methodName, params object[] parameters)
        {
            return InvokeStaticMethod(typeof(T), methodName, parameters);
        }

        #endregion

        #region 属性相关操作

        /// <summary>
        /// 获取属性信息
        /// </summary>
        public static PropertyInfo GetProperty(Type type, string propertyName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetProperty(type, propertyName, flags);
        }

        /// <summary>
        /// 获取属性信息（泛型版本）
        /// </summary>
        public static PropertyInfo GetProperty<T>(string propertyName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            return GetProperty(typeof(T), propertyName, flags);
        }

        /// <summary>
        /// 获取属性值
        /// </summary>
        public static object GetPropertyValue(object instance, PropertyInfo property)
        {
            if (property == null) return null;

            var delegateModule = ReflectionToolkit.GetModule<DelegateCacheModule>();
            var getter = delegateModule?.GetGetter(property);
            return getter?.Invoke(instance) ?? property.GetValue(instance);
        }

        /// <summary>
        /// 获取属性值（按名称）
        /// </summary>
        public static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null) return null;
            var property = GetProperty(instance.GetType(), propertyName);
            return GetPropertyValue(instance, property);
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        public static void SetPropertyValue(object instance, PropertyInfo property, object value)
        {
            if (property == null || !property.CanWrite) return;

            var delegateModule = ReflectionToolkit.GetModule<DelegateCacheModule>();
            var setter = delegateModule?.GetSetter(property);

            if (setter != null)
            {
                setter.Invoke(instance, value);
            }
            else
            {
                property.SetValue(instance, value);
            }
        }

        /// <summary>
        /// 设置属性值（按名称）
        /// </summary>
        public static void SetPropertyValue(object instance, string propertyName, object value)
        {
            if (instance == null) return;
            var property = GetProperty(instance.GetType(), propertyName);
            SetPropertyValue(instance, property, value);
        }

        #endregion

        #region 字段相关操作

        /// <summary>
        /// 获取字段信息
        /// </summary>
        public static FieldInfo GetField(Type type, string fieldName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetField(type, fieldName, flags);
        }

        /// <summary>
        /// 获取字段信息（泛型版本）
        /// </summary>
        public static FieldInfo GetField<T>(string fieldName,
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance)
        {
            return GetField(typeof(T), fieldName, flags);
        }

        /// <summary>
        /// 获取字段值
        /// </summary>
        public static object GetFieldValue(object instance, FieldInfo field)
        {
            return field?.GetValue(instance);
        }

        /// <summary>
        /// 获取字段值（按名称）
        /// </summary>
        public static object GetFieldValue(object instance, string fieldName)
        {
            if (instance == null) return null;
            var field = GetField(instance.GetType(), fieldName);
            return GetFieldValue(instance, field);
        }

        /// <summary>
        /// 设置字段值
        /// </summary>
        public static void SetFieldValue(object instance, FieldInfo field, object value)
        {
            field?.SetValue(instance, value);
        }

        /// <summary>
        /// 设置字段值（按名称）
        /// </summary>
        public static void SetFieldValue(object instance, string fieldName, object value)
        {
            if (instance == null) return;
            var field = GetField(instance.GetType(), fieldName);
            SetFieldValue(instance, field, value);
        }

        #endregion

        #region 特性相关操作

        /// <summary>
        /// 获取类型特性
        /// </summary>
        public static T[] GetCustomAttributes<T>(Type type, bool inherit = false) where T : Attribute
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetCustomAttributes<T>(type, inherit) ?? Array.Empty<T>();
        }

        /// <summary>
        /// 检查类型是否有指定特性
        /// </summary>
        public static bool HasAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            var attributes = GetCustomAttributes<T>(type, inherit);
            return attributes.Length > 0;
        }

        /// <summary>
        /// 获取第一个指定特性
        /// </summary>
        public static T GetFirstAttribute<T>(Type type, bool inherit = false) where T : Attribute
        {
            var attributes = GetCustomAttributes<T>(type, inherit);
            return attributes.Length > 0 ? attributes[0] : null;
        }

        #endregion

        #region 实例创建相关操作

        /// <summary>
        /// 创建实例（无参构造函数）
        /// </summary>
        public static object CreateInstance(Type type)
        {
            return CreateInstance(type, Array.Empty<object>());
        }

        /// <summary>
        /// 创建实例（有参构造函数）
        /// </summary>
        public static object CreateInstance(Type type, params object[] parameters)
        {
            if (type == null) return null;

            try
            {
                var parameterTypes = parameters?.Length > 0
                    ? Array.ConvertAll(parameters, p => p?.GetType() ?? typeof(object))
                    : Type.EmptyTypes;

                var module = ReflectionToolkit.GetModule<TypeCacheModule>();
                var constructor = module?.GetConstructor(type, parameterTypes);

                if (constructor != null)
                {
                    var delegateModule = ReflectionToolkit.GetModule<DelegateCacheModule>();
                    var constructorDelegate = delegateModule?.GetConstructor(constructor);

                    if (constructorDelegate != null)
                    {
                        return constructorDelegate.Invoke(parameters ?? Array.Empty<object>());
                    }

                    return constructor.Invoke(parameters);
                }

                // 尝试使用Activator.CreateInstance
                return Activator.CreateInstance(type, parameters);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"创建实例失败 {type.FullName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 创建实例（泛型版本）
        /// </summary>
        public static T CreateInstance<T>(params object[] parameters)
        {
            var instance = CreateInstance(typeof(T), parameters);
            return instance is T result ? result : default(T);
        }

        /// <summary>
        /// 创建实例（按类型名称）
        /// </summary>
        public static object CreateInstance(string typeName, params object[] parameters)
        {
            var type = GetType(typeName);
            return CreateInstance(type, parameters);
        }

        #endregion

        #region TypeCacheModule 便捷方法补充

        /// <summary>
        /// 获取方法自定义特性（带缓存）
        /// </summary>
        public static T[] GetMethodCustomAttributes<T>(MethodInfo method, bool inherit = false) where T : Attribute
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetMethodCustomAttributes<T>(method, inherit) ?? Array.Empty<T>();
        }

        /// <summary>
        /// 获取字段自定义特性（带缓存）
        /// </summary>
        public static T[] GetFieldCustomAttributes<T>(FieldInfo field, bool inherit = false) where T : Attribute
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetFieldCustomAttributes<T>(field, inherit) ?? Array.Empty<T>();
        }

        /// <summary>
        /// 获取带有指定特性的方法集合（带缓存）
        /// </summary>
        public static MethodInfo[] GetMethodsWithAttribute<T>(Type type,
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static) where T : Attribute
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetMethodsWithAttribute<T>(type, flags) ?? Array.Empty<MethodInfo>();
        }

        /// <summary>
        /// 获取构造函数信息（带缓存）
        /// </summary>
        public static ConstructorInfo GetConstructor(Type type, params Type[] parameterTypes)
        {
            var module = ReflectionToolkit.GetModule<TypeCacheModule>();
            return module?.GetConstructor(type, parameterTypes);
        }

        #endregion

        #region DelegateCacheModule 便捷方法补充

        /// <summary>
        /// 获取无参方法的委托
        /// </summary>
        public static Action<object> GetAction(MethodInfo method)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetAction(method);
        }

        /// <summary>
        /// 获取带参数方法的委托
        /// </summary>
        public static Action<object, object[]> GetActionWithParams(MethodInfo method)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetActionWithParams(method);
        }

        /// <summary>
        /// 获取带返回值方法的委托
        /// </summary>
        public static Func<object, object[], object> GetFunc(MethodInfo method)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetFunc(method);
        }

        /// <summary>
        /// 获取属性 Getter 委托
        /// </summary>
        public static Func<object, object> GetGetter(PropertyInfo property)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetGetter(property);
        }

        /// <summary>
        /// 获取属性 Setter 委托
        /// </summary>
        public static Action<object, object> GetSetter(PropertyInfo property)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetSetter(property);
        }

        /// <summary>
        /// 获取构造函数委托
        /// </summary>
        public static Func<object[], object> GetConstructorDelegate(ConstructorInfo constructor)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetConstructor(constructor);
        }

        /// <summary>
        /// 获取或创建手动注册的委托
        /// </summary>
        public static Action<object, object[]> GetOrCreateDelegate(object key, Func<Action<object, object[]>> factory)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module?.GetOrCreateDelegate(key, factory);
        }

        /// <summary>
        /// 预注册手动委托
        /// </summary>
        public static void PreRegisterDelegate(object key, Action<object, object[]> delegateAction)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            if (module != null) module.PreRegisterDelegate(key, delegateAction);
        }

        /// <summary>
        /// 批量预注册手动委托
        /// </summary>
        public static void PreRegisterDelegates(IEnumerable<KeyValuePair<object, Action<object, object[]>>> delegates)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            if (module != null) module.PreRegisterDelegates(delegates);
        }

        /// <summary>
        /// 移除手动委托
        /// </summary>
        public static bool RemoveDelegate(object key)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module != null && module.RemoveDelegate(key);
        }

        /// <summary>
        /// 获取手动委托缓存统计
        /// </summary>
        public static DelegateCacheModule.ManualDelegateCacheStats GetManualDelegateStats()
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            return module != null ? module.GetDelegateStats() : default(DelegateCacheModule.ManualDelegateCacheStats);
        }

        /// <summary>
        /// 配置手动委托缓存参数
        /// </summary>
        public static void ConfigureManualDelegateCache(
            Action<DelegateCacheModule.ManualDelegateCacheConfiguration> config)
        {
            var module = ReflectionToolkit.GetModule<DelegateCacheModule>();
            if (module == null || config == null) return;
            var cfg = module.ManualConfiguration;
            try
            {
                config(cfg);
            }
            catch
            {
            }

            module.ManualConfiguration = cfg;
        }

        #endregion


        #region 实用工具方法

        /// <summary>
        /// 深度复制对象（通过反射）
        /// </summary>
        public static T DeepCopy<T>(T source) where T : class, new()
        {
            if (source == null) return null;

            var target = new T();
            var type = typeof(T);

            // 复制属性
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.CanRead && property.CanWrite)
                {
                    var value = GetPropertyValue(source, property);
                    SetPropertyValue(target, property, value);
                }
            }

            // 复制字段
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var value = GetFieldValue(source, field);
                SetFieldValue(target, field, value);
            }

            return target;
        }

        /// <summary>
        /// 获取类型的所有公共成员名称
        /// </summary>
        public static string[] GetMemberNames(Type type, MemberTypes memberTypes = MemberTypes.All)
        {
            if (type == null) return Array.Empty<string>();

            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var names = new List<string>();

            foreach (var member in members)
            {
                if ((member.MemberType & memberTypes) != 0)
                {
                    names.Add(member.Name);
                }
            }

            return names.ToArray();
        }

        /// <summary>
        /// 检查两个类型是否兼容（可以互相赋值）
        /// </summary>
        public static bool AreTypesCompatible(Type sourceType, Type targetType)
        {
            if (sourceType == null || targetType == null)
                return false;

            return targetType.IsAssignableFrom(sourceType) ||
                   sourceType.IsAssignableFrom(targetType) ||
                   (sourceType.IsValueType && targetType.IsValueType &&
                    Nullable.GetUnderlyingType(targetType) == sourceType) ||
                   (targetType.IsValueType && sourceType.IsValueType &&
                    Nullable.GetUnderlyingType(sourceType) == targetType);
        }

        #endregion
    }
}