using System;

    /// <summary>
    /// Context - Remove 和 TryRemove 方法部分
    /// </summary>
    public partial class Context
    {
        // ==================== Remove Methods ====================

        /// <summary>
        /// 移除指定名称的对象
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <returns>被移除的对象,如果不存在返回 null</returns>
        public virtual object Remove(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var value))
                {
                    _nameAttributes.Remove(name);
                    return value;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return null;
        }

        /// <summary>
        /// 移除指定类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <returns>被移除的对象,如果不存在返回 default(T)</returns>
        public virtual T Remove<T>()
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_typeAttributes.TryGetValue(typeof(T), out var value))
                {
                    _typeAttributes.Remove(typeof(T));
                    return (T)value;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return default;
        }

        /// <summary>
        /// 移除指定名称和类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <returns>被移除的对象,如果不存在返回 default(T)</returns>
        public virtual T Remove<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var value))
                {
                    _nameAttributes.Remove(name);
                    return (T)value;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return default;
        }

        /// <summary>
        /// 移除指定类型的对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <returns>被移除的对象,如果不存在返回 null</returns>
        public virtual object Remove(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_typeAttributes.TryGetValue(type, out var value))
                {
                    _typeAttributes.Remove(type);
                    return value;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return null;
        }

        // ==================== TryRemove Methods ====================

        /// <summary>
        /// 尝试移除指定名称的对象(线程安全)
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="value">输出参数,被移除的对象</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public virtual bool TryRemove(string name, out object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out value))
                {
                    _nameAttributes.Remove(name);
                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            value = null;
            return false;
        }

        /// <summary>
        /// 尝试移除指定类型的对象(线程安全)
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">输出参数,被移除的对象</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public virtual bool TryRemove<T>(out T value)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_typeAttributes.TryGetValue(typeof(T), out var obj))
                {
                    _typeAttributes.Remove(typeof(T));
                    value = (T)obj;
                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 尝试移除指定名称和类型的对象(线程安全)
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <param name="value">输出参数,被移除的对象</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public virtual bool TryRemove<T>(string name, out T value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var obj))
                {
                    _nameAttributes.Remove(name);
                    value = (T)obj;
                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            value = default;
            return false;
        }

        /// <summary>
        /// 尝试移除指定类型的对象(线程安全)
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="value">输出参数,被移除的对象</param>
        /// <returns>如果移除成功返回 true,否则返回 false</returns>
        public virtual bool TryRemove(Type type, out object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                if (_typeAttributes.TryGetValue(type, out value))
                {
                    _typeAttributes.Remove(type);
                    return true;
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            value = null;
            return false;
        }
    }
