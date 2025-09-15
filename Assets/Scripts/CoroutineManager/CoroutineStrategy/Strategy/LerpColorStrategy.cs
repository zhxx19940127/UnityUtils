using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Color 插值策略
/// </summary>
public class LerpColorStrategy : CoroutineStrategyBase
{
    private readonly Color _from;
    private readonly Color _to;
    private readonly float _duration;
    private readonly Action<Color> _onUpdate;
    private readonly Func<float, float> _easing;

    public LerpColorStrategy(Color from, Color to, float duration, Action<Color> onUpdate,
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
            _onUpdate?.Invoke(Color.Lerp(_from, _to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        _onUpdate?.Invoke(_to);
    }

    protected override string GenerateDefaultName()
    {
        return $"LerpColor_{_duration}s";
    }
}