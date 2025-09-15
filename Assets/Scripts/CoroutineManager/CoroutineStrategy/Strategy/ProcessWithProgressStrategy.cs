using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 带进度的分帧处理策略
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ProcessWithProgressStrategy<T> : CoroutineStrategyBase
{
    private readonly IList<T> _collection;
    private readonly Action<T> _action;
    private readonly Action<int, int> _onProgress;
    private readonly int _itemsPerFrame;
    private readonly Action _onComplete;

    public ProcessWithProgressStrategy(IList<T> collection, Action<T> action, Action<int, int> onProgress,
        int itemsPerFrame = 10, Action onComplete = null, string name = null, bool usePool = true)
        : base(name, "Performance", usePool)
    {
        _collection = collection;
        _action = action;
        _onProgress = onProgress;
        _itemsPerFrame = itemsPerFrame;
        _onComplete = onComplete;
    }

    public override IEnumerator CreateCoroutine()
    {
        int total = _collection.Count;
        int processed = 0;

        for (int i = 0; i < total; i++)
        {
            _action?.Invoke(_collection[i]);
            _onProgress?.Invoke(i + 1, total);
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
        return $"ProcessWithProgress_{_collection.Count}items";
    }
}