using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 浮点数插值策略
/// </summary>
public class LerpValueStrategy : CoroutineStrategyBase
{
    private readonly float _from;
    private readonly float _to;
    private readonly float _duration;
    private readonly Action<float> _onUpdate;
    private readonly Func<float, float> _easing;

    public LerpValueStrategy(float from, float to, float duration, Action<float> onUpdate,
        Func<float, float> easing = null, string name = null, bool usePool = true)
        : base(name, "Animation", usePool)
    {
        _from = from;
        _to = to;
        _duration = duration;
        _onUpdate = onUpdate;
        _easing = easing;
    }

    public override IEnumerator CreateCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            float t = elapsed / _duration;
            t = _easing?.Invoke(t) ?? t;
            _onUpdate?.Invoke(Mathf.Lerp(_from, _to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        _onUpdate?.Invoke(_to);
    }

    protected override string GenerateDefaultName()
    {
        return $"LerpValue_{_from}to{_to}_{_duration}s";
    }
}