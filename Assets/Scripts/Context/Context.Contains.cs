using System;

    /// <summary>
    /// Context - Contains 方法部分
    /// </summary>
    public partial class Context
    {
        /// <summary>
        /// 检查上下文中是否包含指定名称的对象
        /// </summary>
        /// <param name="name">对象名称</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果包含返回 true,否则返回 false</returns>
        public virtual bool Contains(string name, bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_nameAttributes.ContainsKey(name))
                    return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Contains(name, cascade);

            return false;
        }

        /// <summary>
        /// 检查上下文中是否包含指定类型的对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果包含返回 true,否则返回 false</returns>
        public virtual bool Contains<T>(bool cascade = true)
        {
            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.ContainsKey(typeof(T)))
                    return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Contains<T>(cascade);

            return false;
        }

        /// <summary>
        /// 检查上下文中是否包含指定类型的对象
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="cascade">是否级联查找父上下文</param>
        /// <returns>如果包含返回 true,否则返回 false</returns>
        public virtual bool Contains(Type type, bool cascade = true)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            ThrowIfDisposed();

            _lock.EnterReadLock();
            try
            {
                if (_typeAttributes.ContainsKey(type))
                    return true;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            if (cascade && _contextBase != null)
                return _contextBase.Contains(type, cascade);

            return false;
        }
    }
