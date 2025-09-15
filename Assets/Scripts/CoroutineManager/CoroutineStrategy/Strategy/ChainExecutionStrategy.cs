
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 链式执行策略
/// </summary>
public class ChainExecutionStrategy : CoroutineStrategyBase
{
    private readonly List<ChainStep> _steps;
    private readonly int _repeatCount;

    public ChainExecutionStrategy(List<ChainStep> steps, int repeatCount, string name)
        : base(name, "Chain", true)
    {
        _steps = new List<ChainStep>(steps);
        _repeatCount = repeatCount;
    }

    public override IEnumerator CreateCoroutine()
    {
        Debug.Log($"开始执行协程链: {_name} ({_steps.Count} 步骤, 重复 {_repeatCount} 次)");

        for (int repeat = 0; repeat < _repeatCount; repeat++)
        {
            if (_repeatCount > 1)
            {
                Debug.Log($"协程链第 {repeat + 1}/{_repeatCount} 次执行");
            }

            foreach (var step in _steps)
            {
                yield return ExecuteStep(step);
            }
        }

        Debug.Log($"协程链执行完成: {_name}");
    }

    private IEnumerator ExecuteStep(ChainStep step)
    {
        switch (step.Type)
        {
            case ChainStepType.Delay:
                yield return new WaitForSeconds((float)step.Data);
                break;

            case ChainStepType.Action:
                ((Action)step.Data)?.Invoke();
                break;

            case ChainStepType.LerpValue:
                var lerpData = (LerpValueData)step.Data;
                yield return CreateLerpValueCoroutine(lerpData.from, lerpData.to, lerpData.duration,
                    lerpData.onUpdate, lerpData.easing);
                break;

            case ChainStepType.LerpVector3:
                var lerpV3Data = (LerpVector3Data)step.Data;
                yield return CreateLerpVector3Coroutine(lerpV3Data.from, lerpV3Data.to, lerpV3Data.duration,
                    lerpV3Data.onUpdate, lerpV3Data.easing);
                break;

            case ChainStepType.Shake:
                var shakeData = (ShakeData)step.Data;
                yield return CreateShakeCoroutine(shakeData.target, shakeData.duration,
                    shakeData.magnitude, shakeData.frequency);
                break;

            case ChainStepType.WaitUntil:
                var waitData = (WaitUntilData)step.Data;
                yield return CreateWaitUntilCoroutine(waitData.condition, waitData.timeout);
                break;

            case ChainStepType.Custom:
                yield return (IEnumerator)step.Data;
                break;

            case ChainStepType.Strategy:
                var strategy = (ICoroutineStrategy)step.Data;
                yield return strategy.CreateCoroutine();
                break;

            case ChainStepType.Parallel:
                var parallelChain = (CoroutineChain)step.Data;
                var parallelStrategy = new ChainExecutionStrategy(parallelChain.Steps, 1, $"{_name}_Parallel");
                CoroutineManager.StartCoroutine(parallelStrategy); // 并行执行，不等待完成
                break;
        }
    }

    // 辅助方法 - 创建各种类型的协程
    private IEnumerator CreateLerpValueCoroutine(float from, float to, float duration,
        Action<float> onUpdate, Func<float, float> easing)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = easing?.Invoke(t) ?? t;
            onUpdate?.Invoke(Mathf.Lerp(from, to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        onUpdate?.Invoke(to);
    }

    private IEnumerator CreateLerpVector3Coroutine(Vector3 from, Vector3 to, float duration,
        Action<Vector3> onUpdate, Func<float, float> easing)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = easing?.Invoke(t) ?? t;
            onUpdate?.Invoke(Vector3.Lerp(from, to, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        onUpdate?.Invoke(to);
    }

    private IEnumerator CreateShakeCoroutine(Transform target, float duration, float magnitude, float frequency)
    {
        Vector3 originalPosition = target.localPosition;
        float elapsed = 0f;
        float frameInterval = 1f / frequency;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float z = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            target.localPosition = originalPosition + new Vector3(x, y, z);

            yield return new WaitForSeconds(frameInterval);
            elapsed += frameInterval;
        }

        target.localPosition = originalPosition;
    }

    private IEnumerator CreateWaitUntilCoroutine(Func<bool> condition, float timeout)
    {
        float startTime = Time.time;
        while (!condition())
        {
            if (timeout > 0 && Time.time - startTime >= timeout)
                yield break;
            yield return null;
        }
    }

    protected override string GenerateDefaultName()
    {
        return $"ChainExecution_{_steps.Count}steps";
    }
}