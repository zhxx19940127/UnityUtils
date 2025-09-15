using System.Collections;
using UnityEngine;

/// <summary>
/// 震动效果策略
/// </summary>
public class ShakeStrategy : CoroutineStrategyBase
{
    private readonly Transform _target;
    private readonly float _duration;
    private readonly float _magnitude;
    private readonly float _frequency;

    public ShakeStrategy(Transform target, float duration, float magnitude = 1f, float frequency = 25f,
        string name = null, bool usePool = true)
        : base(name, "Animation", usePool)
    {
        _target = target;
        _duration = duration;
        _magnitude = magnitude;
        _frequency = frequency;
    }

    public override IEnumerator CreateCoroutine()
    {
        Vector3 originalPosition = _target.localPosition;
        float elapsed = 0f;
        float frameInterval = 1f / _frequency;

        while (elapsed < _duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * _magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * _magnitude;
            float z = UnityEngine.Random.Range(-1f, 1f) * _magnitude;

            _target.localPosition = originalPosition + new Vector3(x, y, z);

            // 根据频率等待适当的时间
            if (_frequency > 0)
            {
                yield return new WaitForSeconds(frameInterval);
                elapsed += frameInterval;
            }
            else
            {
                // 如果频率为0或负数，每帧更新
                yield return null;
                elapsed += Time.deltaTime;
            }
        }

        _target.localPosition = originalPosition;
    }

    protected override string GenerateDefaultName()
    {
        return $"Shake_{_target.name}_{_duration}s";
    }
}