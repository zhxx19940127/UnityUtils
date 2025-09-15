using System.Collections;

/// <summary>
/// 协程策略接口
/// </summary>
public interface ICoroutineStrategy
{
    /// <summary>
    /// 创建协程
    /// </summary>
    /// <returns>协程枚举器</returns>
    IEnumerator CreateCoroutine();
    
    /// <summary>
    /// 获取协程名称
    /// </summary>
    string GetCoroutineName();
    
    /// <summary>
    /// 获取协程分类
    /// </summary>
    string GetCategory();
    
    /// <summary>
    /// 是否使用协程池
    /// </summary>
    bool UsePool { get; }
}