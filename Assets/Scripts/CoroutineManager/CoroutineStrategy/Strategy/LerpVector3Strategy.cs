using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Vector3 插值策略
/// </summary>
public class LerpVector3Strategy : CoroutineStrategyBase
{
    private readonly Vector3 _from;
    private readonly Vector3 _to;
    private readonly float _duration;
    private readonly Action<Vector3> _onUpdate;
    private readonly Func<float, float> _easing;

    public LerpVector3Strategy(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
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
            _onUpdate?.Invoke(Vector3.Lerp(_from, _to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }
        _onUpdate?.Invoke(_to);
    }

    protected override string GenerateDefaultName()
    {
        return $"LerpVector3_{_duration}s";
    }
}