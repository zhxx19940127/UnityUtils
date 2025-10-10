using System;
    /// <summary>
    /// Context - Set 方法部分
    /// </summary>
    public partial class Context
    {
        /// <summary>
        /// 设置指定名称的对象
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="value">对象值</param>
        public virtual void Set(string name, object value)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _nameAttributes[name] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 设置指定类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="value">对象值</param>
        public virtual void Set<T>(T value)
        {
            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _typeAttributes[typeof(T)] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// 设置指定类型的对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="value">对象值</param>
        public virtual void Set(Type type, object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterWriteLock();
            try
            {
                _typeAttributes[type] = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
