using System;
using System.Collections.Generic;

    /// <summary>
    /// Context - 静态成员和全局上下文管理部分
    /// </summary>
    public partial class Context
    {
        private static ApplicationContext context = new ApplicationContext();
        private static readonly Dictionary<string, Context> contexts = new Dictionary<string, Context>();
        private static readonly object _staticLock = new object();

        // ==================== 应用上下文管理 ====================

        /// <summary>
        /// 获取全局应用上下文
        /// </summary>
        /// <returns>应用上下文实例</returns>
        public static ApplicationContext GetApplicationContext()
        {
            return context;
        }

        /// <summary>
        /// 设置全局应用上下文
        /// </summary>
        /// <param name="context">新的应用上下文实例</param>
        public static void SetApplicationContext(ApplicationContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_staticLock)
            {
                Context.context = context;
            }
        }

        // ==================== 命名上下文管理 ====================

        /// <summary>
        /// 获取指定键的上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <returns>上下文实例,如果不存在返回 null</returns>
        public static Context GetContext(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            lock (_staticLock)
            {
                contexts.TryGetValue(key, out var result);
                return result;
            }
        }

        /// <summary>
        /// 获取指定键和类型的上下文
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        /// <param name="key">上下文键</param>
        /// <returns>上下文实例,如果不存在或类型不匹配返回 null</returns>
        public static T GetContext<T>(string key) where T : Context
        {
            return (T)GetContext(key);
        }

        /// <summary>
        /// 获取指定类型的上下文(使用类型全名作为键)
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        /// <returns>上下文实例,如果不存在返回 null</returns>
        public static T GetContext<T>() where T : Context
        {
            return GetContext<T>(typeof(T).FullName);
        }

        /// <summary>
        /// 尝试获取指定键的上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="context">输出参数,获取到的上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public static bool TryGetContext(string key, out Context context)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            lock (_staticLock)
            {
                return contexts.TryGetValue(key, out context);
            }
        }

        /// <summary>
        /// 尝试获取指定键和类型的上下文
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        /// <param name="key">上下文键</param>
        /// <param name="context">输出参数,获取到的上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public static bool TryGetContext<T>(string key, out T context) where T : Context
        {
            if (TryGetContext(key, out var ctx))
            {
                context = ctx as T;
                return context != null;
            }

            context = null;
            return false;
        }

        /// <summary>
        /// 尝试获取指定类型的上下文(使用类型全名作为键)
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        /// <param name="context">输出参数,获取到的上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public static bool TryGetContext<T>(out T context) where T : Context
        {
            return TryGetContext(typeof(T).FullName, out context);
        }

        // ==================== 添加上下文 ====================

        /// <summary>
        /// 添加命名上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="context">上下文实例</param>
        /// <exception cref="ArgumentException">如果键已存在则抛出异常</exception>
        public static void AddContext(string key, Context context)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_staticLock)
            {
                if (contexts.ContainsKey(key))
                    throw new ArgumentException($"Context with key '{key}' already exists. Use AddOrUpdateContext or TryAddContext instead.");

                contexts.Add(key, context);
            }
        }

        /// <summary>
        /// 尝试添加命名上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="context">上下文实例</param>
        /// <returns>如果添加成功返回 true,如果键已存在返回 false</returns>
        public static bool TryAddContext(string key, Context context)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_staticLock)
            {
                if (contexts.ContainsKey(key))
                    return false;

                contexts.Add(key, context);
                return true;
            }
        }

        /// <summary>
        /// 添加或更新命名上下文
        /// 如果键已存在,会先释放旧的上下文,再添加新的
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="context">上下文实例</param>
        public static void AddOrUpdateContext(string key, Context context)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            lock (_staticLock)
            {
                if (contexts.TryGetValue(key, out var existingContext))
                {
                    existingContext.Dispose();
                }

                contexts[key] = context;
            }
        }

        // ==================== 移除上下文 ====================

        /// <summary>
        /// 移除指定类型的上下文(使用类型全名作为键)
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        public static void RemoveContext<T>() where T : Context
        {
            RemoveContext(typeof(T).FullName);
        }

        /// <summary>
        /// 移除指定键的上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        public static void RemoveContext(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            lock (_staticLock)
            {
                if (contexts.TryGetValue(key, out var context))
                {
                    context.Dispose();
                    contexts.Remove(key);
                }
            }
        }

        /// <summary>
        /// 尝试移除指定键的上下文
        /// </summary>
        /// <param name="key">上下文键</param>
        /// <param name="context">输出参数,被移除的上下文</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public static bool TryRemoveContext(string key, out Context context)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            lock (_staticLock)
            {
                if (contexts.TryGetValue(key, out context))
                {
                    context.Dispose();
                    contexts.Remove(key);
                    return true;
                }
            }

            context = null;
            return false;
        }

        /// <summary>
        /// 尝试移除指定类型的上下文(使用类型全名作为键)
        /// </summary>
        /// <typeparam name="T">上下文类型</typeparam>
        /// <param name="context">输出参数,被移除的上下文</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public static bool TryRemoveContext<T>(out T context) where T : Context
        {
            if (TryRemoveContext(typeof(T).FullName, out var ctx))
            {
                context = ctx as T;
                return context != null;
            }

            context = null;
            return false;
        }

        // ==================== 批量管理 ====================

        /// <summary>
        /// 清空所有命名上下文
        /// 会自动释放所有上下文的资源
        /// </summary>
        public static void ClearAllContexts()
        {
            lock (_staticLock)
            {
                foreach (var ctx in contexts.Values)
                {
                    ctx?.Dispose();
                }

                contexts.Clear();
            }
        }

        /// <summary>
        /// 获取当前命名上下文的数量
        /// </summary>
        /// <returns>上下文数量</returns>
        public static int GetContextCount()
        {
            lock (_staticLock)
            {
                return contexts.Count;
            }
        }

        /// <summary>
        /// 获取所有命名上下文的键
        /// </summary>
        /// <returns>键数组</returns>
        public static string[] GetAllContextKeys()
        {
            lock (_staticLock)
            {
                var keys = new string[contexts.Count];
                contexts.Keys.CopyTo(keys, 0);
                return keys;
            }
        }
    }
