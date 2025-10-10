using System;
using System.Collections.Generic;
using System.Threading;
    /// <summary>
    /// 上下文容器
    /// 用于管理应用程序中的全局对象和资源
    /// 支持类型键和字符串键两种存储方式
    /// </summary>
    public partial class Context : IDisposable
    {
        private readonly Dictionary<Type, object> _typeAttributes;
        private readonly Dictionary<string, object> _nameAttributes;
        private readonly Context _contextBase;
        private readonly ReaderWriterLockSlim _lock;
        private bool _disposed;

        /// <summary>
        /// 初始化上下文(无父级上下文)
        /// </summary>
        public Context() : this(null)
        {
        }

        /// <summary>
        /// 初始化上下文
        /// </summary>
        /// <param name="contextBase">父级上下文,支持级联查找</param>
        public Context(Context contextBase)
        {
            _typeAttributes = new Dictionary<Type, object>();
            _nameAttributes = new Dictionary<string, object>();
            _contextBase = contextBase;
            _lock = new ReaderWriterLockSlim();
            _disposed = false;
        }

        /// <summary>
        /// 获取资源管理器
        /// </summary>

        /// <summary>
        /// 检查对象是否已释放,如果已释放则抛出异常
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
