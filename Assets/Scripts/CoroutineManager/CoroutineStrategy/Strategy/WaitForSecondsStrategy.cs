using System;
using System.Collections;
using System.Threading;
using UnityEngine;

/// <summary>
/// 等待时间策略
/// </summary>
public class WaitForSecondsStrategy : CoroutineStrategyBase
{
    private readonly float _seconds;
    private readonly Action _onComplete;
    private readonly CancellationToken _cancellationToken;

    public WaitForSecondsStrategy(float seconds, Action onComplete = null, 
        CancellationToken cancellationToken = default, string name = null, bool usePool = true)
        : base(name, "Wait", usePool)
    {
        _seconds = seconds;
        _onComplete = onComplete;
        _cancellationToken = cancellationToken;
    }

    public override IEnumerator CreateCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _seconds)
        {
            if (_cancellationToken.IsCancellationRequested)
                yield break;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 等待完成后执行回调
        _onComplete?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"WaitForSeconds_{_seconds}s";
    }
}