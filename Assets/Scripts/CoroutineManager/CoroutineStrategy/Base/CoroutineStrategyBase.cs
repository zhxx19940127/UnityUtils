using System.Collections;



/// <summary>
/// 抽象协程策略基类
/// </summary>
public abstract class CoroutineStrategyBase : ICoroutineStrategy
{
    protected string _name;
    protected string _category;
    protected bool _usePool;

    protected CoroutineStrategyBase(string name = null, string category = "Default", bool usePool = true)
    {
        _name = name;
        _category = category;
        _usePool = usePool;
    }

    public abstract IEnumerator CreateCoroutine();

    public virtual string GetCoroutineName()
    {
        return _name ?? GenerateDefaultName();
    }

    public virtual string GetCategory()
    {
        return _category;
    }

    public virtual bool UsePool => _usePool;

    protected abstract string GenerateDefaultName();
}