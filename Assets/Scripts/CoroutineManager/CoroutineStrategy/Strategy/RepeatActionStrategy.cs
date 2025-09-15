using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 重复执行策略
/// </summary>
public class RepeatActionStrategy : CoroutineStrategyBase
{
    private readonly Action _action;
    private readonly float _intervalSeconds;
    private readonly int _count;

    public RepeatActionStrategy(Action action, float intervalSeconds, int count = 0,
        string name = null, bool usePool = true)
        : base(name, "Repeat", usePool)
    {
        _action = action;
        _intervalSeconds = intervalSeconds;
        _count = count;
    }

    public override IEnumerator CreateCoroutine()
    {
        if (_count == 0)
        {
            while (true)
            {
                _action?.Invoke();
                yield return new WaitForSeconds(_intervalSeconds);
            }
        }
        else
        {
            for (int i = 0; i < _count; i++)
            {
                _action?.Invoke();
                yield return new WaitForSeconds(_intervalSeconds);
            }
        }
    }

    protected override string GenerateDefaultName()
    {
        return $"RepeatAction_{_intervalSeconds}s_x{_count}";
    }
}