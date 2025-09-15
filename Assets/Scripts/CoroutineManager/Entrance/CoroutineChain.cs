using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 协程链接口 - 支持流畅的链式调用
/// </summary>
public interface ICoroutineChain
{
    /// <summary>
    /// 然后延迟执行
    /// </summary>
    ICoroutineChain ThenDelay(float seconds);

    /// <summary>
    /// 然后执行动作
    /// </summary>
    ICoroutineChain ThenDo(Action action);

    /// <summary>
    /// 然后执行插值
    /// </summary>
    ICoroutineChain ThenLerpValue(float from, float to, float duration, Action<float> onUpdate,
        Func<float, float> easing = null);

    /// <summary>
    /// 然后执行Vector3插值
    /// </summary>
    ICoroutineChain ThenLerpVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
        Func<float, float> easing = null);

    /// <summary>
    /// 然后震动
    /// </summary>
    ICoroutineChain ThenShake(Transform target, float duration, float magnitude = 1f, float frequency = 25f);

    /// <summary>
    /// 然后等待条件
    /// </summary>
    ICoroutineChain ThenWaitUntil(Func<bool> condition, float timeout = 0f);

    /// <summary>
    /// 然后执行自定义协程
    /// </summary>
    ICoroutineChain ThenCustom(IEnumerator routine);

    /// <summary>
    /// 然后执行自定义策略
    /// </summary>
    ICoroutineChain ThenStrategy(ICoroutineStrategy strategy);

    /// <summary>
    /// 并行执行（与当前步骤同时进行）
    /// </summary>
    ICoroutineChain Parallel(Action<ICoroutineChain> parallelChain);

    /// <summary>
    /// 重复整个链条
    /// </summary>
    ICoroutineChain Repeat(int count);

    /// <summary>
    /// 开始执行链条
    /// </summary>
    Coroutine Start(string chainName = null);
}

/// <summary>
/// 协程链步骤类型
/// </summary>
public enum ChainStepType
{
    Delay,
    Action,
    LerpValue,
    LerpVector3,
    Shake,
    WaitUntil,
    Custom,
    Strategy,
    Parallel
}

/// <summary>
/// 协程链步骤
/// </summary>
public class ChainStep
{
    public ChainStepType Type { get; set; }
    public object Data { get; set; }
    public string Description { get; set; }
}

/// <summary>
/// 插值数据结构
/// </summary>
public struct LerpValueData
{
    public float from;
    public float to;
    public float duration;
    public Action<float> onUpdate;
    public Func<float, float> easing;
}

/// <summary>
/// Vector3插值数据结构
/// </summary>
public struct LerpVector3Data
{
    public Vector3 from;
    public Vector3 to;
    public float duration;
    public Action<Vector3> onUpdate;
    public Func<float, float> easing;
}

/// <summary>
/// 震动数据结构
/// </summary>
public struct ShakeData
{
    public Transform target;
    public float duration;
    public float magnitude;
    public float frequency;
}

/// <summary>
/// 等待条件数据结构
/// </summary>
public struct WaitUntilData
{
    public Func<bool> condition;
    public float timeout;
}

/// <summary>
/// 协程链实现类
/// </summary>
public class CoroutineChain : ICoroutineChain
{
    private readonly List<ChainStep> _steps = new List<ChainStep>();
    private int _repeatCount = 1;
    private string _chainName;

    /// <summary>
    /// 获取步骤列表（用于内部访问）
    /// </summary>
    internal List<ChainStep> Steps => _steps;

    /// <summary>
    /// 创建协程链
    /// </summary>
    public static ICoroutineChain Create()
    {
        return new CoroutineChain();
    }

    /// <summary>
    /// 从延迟开始创建协程链
    /// </summary>
    public static ICoroutineChain StartWithDelay(float seconds)
    {
        return new CoroutineChain().ThenDelay(seconds);
    }

    /// <summary>
    /// 从动作开始创建协程链
    /// </summary>
    public static ICoroutineChain StartWithAction(Action action)
    {
        return new CoroutineChain().ThenDo(action);
    }

    public ICoroutineChain ThenDelay(float seconds)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Delay,
            Data = seconds,
            Description = $"延迟 {seconds} 秒"
        });
        return this;
    }

    public ICoroutineChain ThenDo(Action action)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Action,
            Data = action,
            Description = "执行动作"
        });
        return this;
    }

    public ICoroutineChain ThenLerpValue(float from, float to, float duration, Action<float> onUpdate,
        Func<float, float> easing = null)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.LerpValue,
            Data = new LerpValueData { from = from, to = to, duration = duration, onUpdate = onUpdate, easing = easing },
            Description = $"浮点插值 {from}→{to} ({duration}s)"
        });
        return this;
    }

    public ICoroutineChain ThenLerpVector3(Vector3 from, Vector3 to, float duration, Action<Vector3> onUpdate,
        Func<float, float> easing = null)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.LerpVector3,
            Data = new LerpVector3Data { from = from, to = to, duration = duration, onUpdate = onUpdate, easing = easing },
            Description = $"向量插值 ({duration}s)"
        });
        return this;
    }

    public ICoroutineChain ThenShake(Transform target, float duration, float magnitude = 1f, float frequency = 25f)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Shake,
            Data = new ShakeData { target = target, duration = duration, magnitude = magnitude, frequency = frequency },
            Description = $"震动 {target?.name} ({duration}s)"
        });
        return this;
    }

    public ICoroutineChain ThenWaitUntil(Func<bool> condition, float timeout = 0f)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.WaitUntil,
            Data = new WaitUntilData { condition = condition, timeout = timeout },
            Description = $"等待条件 (超时:{timeout}s)"
        });
        return this;
    }

    public ICoroutineChain ThenCustom(IEnumerator routine)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Custom,
            Data = routine,
            Description = "自定义协程"
        });
        return this;
    }

    public ICoroutineChain ThenStrategy(ICoroutineStrategy strategy)
    {
        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Strategy,
            Data = strategy,
            Description = $"策略: {strategy.GetCoroutineName()}"
        });
        return this;
    }

    public ICoroutineChain Parallel(Action<ICoroutineChain> parallelChain)
    {
        var parallel = new CoroutineChain();
        parallelChain(parallel);

        _steps.Add(new ChainStep
        {
            Type = ChainStepType.Parallel,
            Data = parallel,
            Description = $"并行执行 ({parallel._steps.Count} 步骤)"
        });
        return this;
    }

    public ICoroutineChain Repeat(int count)
    {
        _repeatCount = Mathf.Max(1, count);
        return this;
    }

    public Coroutine Start(string chainName = null)
    {
        _chainName = chainName ?? $"Chain_{DateTime.Now.Ticks}";
        var chainStrategy = new ChainExecutionStrategy(_steps, _repeatCount, _chainName);
        return CoroutineManager.StartCoroutine(chainStrategy);
    }

    /// <summary>
    /// 获取链条描述
    /// </summary>
    public string GetDescription()
    {
        var descriptions = new List<string>();
        for (int i = 0; i < _steps.Count; i++)
        {
            descriptions.Add($"{i + 1}. {_steps[i].Description}");
        }

        var result = string.Join("\n", descriptions);
        if (_repeatCount > 1)
        {
            result += $"\n重复 {_repeatCount} 次";
        }

        return result;
    }
}
