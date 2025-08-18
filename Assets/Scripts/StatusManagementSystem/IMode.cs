using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 状态模式接口
/// </summary>
/// <typeparam name="ParentT">父级对象类型</typeparam>
public interface IMode<ParentT> : IDisposable
{
    #region 核心属性

    /// <summary>
    /// 获取关联的父级对象
    /// </summary>
    ParentT Parent { get; }

    /// <summary>
    /// 设置父级对象（只能设置一次）
    /// </summary>
    /// <param name="parent">父级对象实例</param>
    void SetParent(ParentT parent);

    #endregion

    #region 生命周期方法（同步）

    /// <summary>
    /// 初始化状态（同步）
    /// </summary>
    /// <param name="parameters">初始化参数</param>
    void Init(Dictionary<string, object> parameters = null);

    /// <summary>
    /// 进入状态（同步）
    /// </summary>
    /// <param name="parameters">进入参数</param>
    void Enter(Dictionary<string, object> parameters = null);

    /// <summary>
    /// 退出状态（同步）
    /// </summary>
    void Exit();

    /// <summary>
    /// 状态首次激活时调用（同步）
    /// </summary>
    /// <param name="parameters">启动参数</param>
    void Start(Dictionary<string, object> parameters = null);

    #endregion

    #region 生命周期方法（异步）

    /// <summary>
    /// 初始化状态（异步）
    /// </summary>
    /// <param name="parameters">初始化参数</param>
    /// <returns>Task</returns>
    Task InitAsync(Dictionary<string, object> parameters = null);

    /// <summary>
    /// 进入状态（异步）
    /// </summary>
    /// <param name="parameters">进入参数</param>
    /// <returns>Task</returns>
    Task EnterAsync(Dictionary<string, object> parameters = null);

    /// <summary>
    /// 退出状态（异步）
    /// </summary>
    /// <returns>Task</returns>
    Task ExitAsync();

    /// <summary>
    /// 状态首次激活时调用（异步）
    /// </summary>
    /// <param name="parameters">启动参数</param>
    /// <returns>Task</returns>
    Task StartAsync(Dictionary<string, object> parameters = null);

    #endregion

    #region 更新方法

    /// <summary>
    /// 每帧更新
    /// </summary>
    void Update();

    /// <summary>
    /// 每帧延迟更新（在所有Update之后调用）
    /// </summary>
    void LateUpdate();

    /// <summary>
    /// 固定时间间隔更新（物理更新）
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// GUI渲染回调
    /// </summary>
    void OnGUI();

    #endregion

    #region 状态转换验证

    /// <summary>
    /// 检查是否可以进入该状态
    /// </summary>
    /// <param name="fromMode">来源状态ID</param>
    /// <param name="parameters">转换参数</param>
    /// <returns>是否允许进入</returns>
    bool CanEnter(int fromMode, Dictionary<string, object> parameters = null);

    /// <summary>
    /// 检查是否可以退出当前状态
    /// </summary>
    /// <param name="toMode">目标状态ID</param>
    /// <param name="parameters">转换参数</param>
    /// <returns>是否允许退出</returns>
    bool CanExit(int toMode, Dictionary<string, object> parameters = null);

    #endregion

    #region 其他方法

    /// <summary>
    /// 重置状态（当状态被回收时调用）
    /// </summary>
    void ResetState();

    #endregion

    /// <summary>
    /// 销毁状态（释放资源）
    /// </summary>
    void Destroy();
}