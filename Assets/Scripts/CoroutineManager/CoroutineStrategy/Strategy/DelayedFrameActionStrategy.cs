using System;
using System.Collections;

/// <summary>
/// 延迟帧执行策略
/// </summary>
public class DelayedFrameActionStrategy : CoroutineStrategyBase
{
    private readonly Action _action;
    private readonly int _frameCount;

    public DelayedFrameActionStrategy(Action action, int frameCount = 1, string name = null, bool usePool = true)
        : base(name, "Delay", usePool)
    {
        _action = action;
        _frameCount = frameCount;
    }

    public override IEnumerator CreateCoroutine()
    {
        for (int i = 0; i < _frameCount; i++)
        {
            yield return null;
        }

        _action?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"DelayedFrameAction_{_frameCount}frames";
    }
}