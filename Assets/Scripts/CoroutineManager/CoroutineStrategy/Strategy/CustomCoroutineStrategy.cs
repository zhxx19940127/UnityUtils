using System;
using System.Collections;

/// <summary>
/// 自定义协程策略
/// </summary>
public class CustomCoroutineStrategy : CoroutineStrategyBase
{
    private readonly IEnumerator _routine;

    public CustomCoroutineStrategy(IEnumerator routine, string name = null,
        string category = "Custom", bool usePool = true)
        : base(name, category, usePool)
    {
        _routine = routine;
    }

    public override IEnumerator CreateCoroutine()
    {
        return _routine;
    }

    protected override string GenerateDefaultName()
    {
        return $"Custom_{DateTime.Now.Ticks}";
    }
}