using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 延迟执行策略
/// </summary>
public class DelayedActionStrategy : CoroutineStrategyBase
{
    private readonly Action _action;
    private readonly float _delaySeconds;

    public DelayedActionStrategy(Action action, float delaySeconds, string name = null, bool usePool = true)
        : base(name, "Delay", usePool)
    {
        _action = action;
        _delaySeconds = delaySeconds;
    }

    public override IEnumerator CreateCoroutine()
    {
        yield return new WaitForSeconds(_delaySeconds);
        _action?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"DelayedAction_{_delaySeconds}s";
    }
}