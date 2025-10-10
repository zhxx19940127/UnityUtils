using System;

/// <summary>
/// Context - 资源释放部分
/// </summary>
public partial class Context
{
    /// <summary>
    /// 释放上下文及其包含的所有资源
    /// 会自动释放所有实现 IDisposable 接口的对象
    /// </summary>
    public virtual void Dispose()
    {
        if (_disposed) return;

        _lock.EnterWriteLock();
        try
        {
            _disposed = true;

            // 释放类型字典中的所有 IDisposable 对象
            foreach (var value in _typeAttributes.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            // 释放名称字典中的所有 IDisposable 对象
            foreach (var value in _nameAttributes.Values)
            {
                if (value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _typeAttributes.Clear();
            _nameAttributes.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }


        // 释放读写锁
        _lock?.Dispose();
    }
}