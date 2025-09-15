using System;
using System.Collections;
using System.Threading;
using UnityEngine;

/// <summary>
/// 等待条件策略
/// </summary>
public class WaitUntilStrategy : CoroutineStrategyBase
{
    private readonly Func<bool> _condition;
    private readonly Action _onComplete;
    private readonly float _checkInterval;
    private readonly CancellationToken _cancellationToken;

    public WaitUntilStrategy(Func<bool> condition, Action onComplete = null, float checkInterval = 0f,
        CancellationToken cancellationToken = default, string name = null, bool usePool = true)
        : base(name, "Wait", usePool)
    {
        _condition = condition;
        _onComplete = onComplete;
        _checkInterval = checkInterval;
        _cancellationToken = cancellationToken;
    }

    public override IEnumerator CreateCoroutine()
    {
        while (!_condition())
        {
            if (_cancellationToken.IsCancellationRequested)
                yield break;

            if (_checkInterval > 0)
                yield return new WaitForSeconds(_checkInterval);
            else
                yield return null;
        }
        
        // 条件满足后执行回调
        _onComplete?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"WaitUntil_{_checkInterval}s";
    }
}