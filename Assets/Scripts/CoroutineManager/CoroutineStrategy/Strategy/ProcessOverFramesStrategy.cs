using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 分帧处理策略
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ProcessOverFramesStrategy<T> : CoroutineStrategyBase
{
    private readonly IEnumerable<T> _collection;
    private readonly Action<T> _action;
    private readonly int _itemsPerFrame;
    private readonly Action _onComplete;

    public ProcessOverFramesStrategy(IEnumerable<T> collection, Action<T> action, int itemsPerFrame = 10,
        Action onComplete = null, string name = null, bool usePool = true)
        : base(name, "Performance", usePool)
    {
        _collection = collection;
        _action = action;
        _itemsPerFrame = itemsPerFrame;
        _onComplete = onComplete;
    }

    public override IEnumerator CreateCoroutine()
    {
        int processed = 0;
        foreach (var item in _collection)
        {
            _action?.Invoke(item);
            processed++;

            if (processed >= _itemsPerFrame)
            {
                processed = 0;
                yield return null;
            }
        }
        _onComplete?.Invoke();
    }

    protected override string GenerateDefaultName()
    {
        return $"ProcessOverFrames_{_itemsPerFrame}items";
    }
}