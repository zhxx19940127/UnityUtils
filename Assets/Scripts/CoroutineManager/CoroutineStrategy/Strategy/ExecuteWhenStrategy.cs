using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 条件执行策略
/// </summary>
public class ExecuteWhenStrategy : CoroutineStrategyBase
{
    private readonly Action _action;
    private readonly Func<bool> _condition;
    private readonly float _timeout;

    public ExecuteWhenStrategy(Action action, Func<bool> condition, float timeout = 0f,
        string name = null, bool usePool = true)
        : base(name, "Condition", usePool)
    {
        _action = action;
        _condition = condition;
        _timeout = timeout;
    }

    public override IEnumerator CreateCoroutine()
    {
        float startTime = Time.time;
        while (!_condition())
        {
            if (_timeout > 0 && Time.time - startTime >= _timeout)
                yield break;
            yield return null;
        }

        _action?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"ExecuteWhen_{_timeout}s";
    }
}