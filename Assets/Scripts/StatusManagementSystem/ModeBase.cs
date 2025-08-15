using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// 状态基类（可选实现）
/// </summary>
/// <typeparam name="ParentT">父级对象类型</typeparam>
public abstract class ModeBase<ParentT> : IMode<ParentT>
{
    #region 字段和属性

    /// <summary>
    /// 父级对象引用
    /// </summary>
    protected ParentT m_parent;

    /// <summary>
    /// 父级对象访问器
    /// </summary>
    public ParentT Parent => m_parent;

    #endregion

    #region 基础方法

    /// <summary>
    /// 设置父级对象
    /// </summary>
    public void SetParent(ParentT parent)
    {
        if (m_parent == null)
        {
            m_parent = parent;
        }
    }

    #endregion

    #region 生命周期方法（同步）

    /// <summary>
    /// 初始化状态
    /// </summary>
    public virtual void Init(Dictionary<string, object> parameters = null)
    {
    }

    /// <summary>
    /// 进入状态
    /// </summary>
    public virtual void Enter(Dictionary<string, object> parameters = null)
    {
    }

    /// <summary>
    /// 退出状态
    /// </summary>
    public virtual void Exit()
    {
    }

    /// <summary>
    /// 状态首次激活时调用
    /// </summary>
    public virtual void Start(Dictionary<string, object> parameters = null)
    {
    }

    #endregion

    #region 生命周期方法（异步）

    /// <summary>
    /// 异步初始化状态
    /// </summary>
    public virtual Task InitAsync(Dictionary<string, object> parameters = null) => Task.CompletedTask;

    /// <summary>
    /// 异步进入状态
    /// </summary>
    public virtual Task EnterAsync(Dictionary<string, object> parameters = null) => Task.CompletedTask;

    /// <summary>
    /// 异步退出状态
    /// </summary>
    public virtual Task ExitAsync() => Task.CompletedTask;

    /// <summary>
    /// 异步启动状态
    /// </summary>
    public virtual Task StartAsync(Dictionary<string, object> parameters = null) => Task.CompletedTask;

    #endregion

    #region 更新方法

    /// <summary>
    /// 每帧更新
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// 延迟更新
    /// </summary>
    public virtual void LateUpdate()
    {
    }

    /// <summary>
    /// 固定更新
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// GUI渲染
    /// </summary>
    public virtual void OnGUI()
    {
    }

    #endregion

    #region 状态转换验证

    /// <summary>
    /// 检查是否可以进入该状态
    /// </summary>
    public virtual bool CanEnter(int fromMode, Dictionary<string, object> parameters = null) => true;

    /// <summary>
    /// 检查是否可以退出当前状态
    /// </summary>
    public virtual bool CanExit(int toMode, Dictionary<string, object> parameters = null) => true;

    #endregion

    #region 其他方法

    /// <summary>
    /// 重置状态（对象被回收时调用）
    /// </summary>
    public virtual void ResetState()
    {
    }

    /// <summary>
    /// 销毁状态
    /// </summary>
    public virtual void Destroy()
    {
    }

    /// <summary>
    /// 实现IDisposable接口
    /// </summary>
    public virtual void Dispose()
    {
        Destroy();
    }

    #endregion
}