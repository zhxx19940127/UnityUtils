using System;

    /// <summary>
    /// Context - Get 和 TryGet 方法部分
    /// </summary>
    public partial class Context
    {
        // ==================== Get Methods ====================

        /// <summary>
        /// 获取指定名称的对象
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>对象实例,如果不存在返回 null</returns>
        public virtual object Get(string name, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var value))
                    return value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Get(name, cascade);

            return null;
        }

        /// <summary>
        /// 获取指定类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>对象实例,如果不存在返回 default(T)</returns>
        public virtual T Get<T>(bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.TryGetValue(typeof(T), out var value))
                    return (T)value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Get<T>(cascade);

            return default;
        }

        /// <summary>
        /// 获取指定名称和类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>对象实例,如果不存在返回 default(T)</returns>
        public virtual T Get<T>(string name, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var value))
                    return (T)value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Get<T>(name, cascade);

            return default;
        }

        /// <summary>
        /// 获取指定类型的对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>对象实例,如果不存在返回 null</returns>
        public virtual object Get(Type type, bool cascade = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.TryGetValue(type, out var value))
                    return value;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Get(type, cascade);

            return null;
        }

        // ==================== TryGet Methods ====================

        /// <summary>
        /// 尝试获取指定名称的对象(线程安全)
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="value">输出参数,获取到的对象</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public virtual bool TryGet(string name, out object value, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out value))
                    return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.TryGet(name, out value, cascade);

            value = null;
            return false;
        }

        /// <summary>
        /// 尝试获取指定类型的对象(线程安全)
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">输出参数,获取到的对象</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public virtual bool TryGet<T>(out T value, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.TryGetValue(typeof(T), out var obj))
                {
                    value = (T)obj;
                    return true;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.TryGet(out value, cascade);

            value = default;
            return false;
        }

        /// <summary>
        /// 尝试获取指定名称和类型的对象(线程安全)
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="name">对象名称</param>
        /// <param name="value">输出参数,获取到的对象</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public virtual bool TryGet<T>(string name, out T value, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_nameAttributes.TryGetValue(name, out var obj))
                {
                    value = (T)obj;
                    return true;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.TryGet(name, out value, cascade);

            value = default;
            return false;
        }

        /// <summary>
        /// 尝试获取指定类型的对象(线程安全)
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="value">输出参数,获取到的对象</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果获取成功返回 true,否则返回 false</returns>
        public virtual bool TryGet(Type type, out object value, bool cascade = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.TryGetValue(type, out value))
                    return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.TryGet(type, out value, cascade);

            value = null;
            return false;
        }
    }
