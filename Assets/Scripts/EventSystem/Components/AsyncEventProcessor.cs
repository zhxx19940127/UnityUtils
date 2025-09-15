using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Components
{
    /// <summary>
    /// 异步事件处理器实现 - 负责异步消息的处理和队列管理
    /// 支持线程安全的异步消息分发和处理
    /// </summary>
    public class AsyncEventProcessor : IAsyncProcessor, IDisposable
    {
        #region 私有字段

        /// <summary>
        /// 异步消息队列，线程安全
        /// </summary>
        private readonly ConcurrentQueue<AsyncMessage> _asyncMessageQueue = new ConcurrentQueue<AsyncMessage>();

        /// <summary>
        /// 事件总线引用，用于实际的消息分发
        /// </summary>
        private readonly IEventBus _eventBus;

        /// <summary>
        /// 处理状态标志
        /// </summary>
        private volatile bool _isProcessing = false;

        /// <summary>
        /// 取消令牌源，用于停止异步处理
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// 处理任务
        /// </summary>
        private Task _processingTask;

        /// <summary>
        /// 处理间隔（毫秒）
        /// </summary>
        private const int PROCESSING_INTERVAL_MS = 10;

        /// <summary>
        /// 批处理大小
        /// </summary>
        private const int BATCH_SIZE = 50;

        /// <summary>
        /// 队列最大容量
        /// </summary>
        private const int MAX_QUEUE_SIZE = 10000;

        /// <summary>
        /// 性能统计
        /// </summary>
        private readonly AsyncProcessorStats _stats = new AsyncProcessorStats();

        #endregion

        #region 属性

        /// <summary>
        /// 检查是否正在处理异步消息
        /// </summary>
        public bool IsProcessing => _isProcessing;

        /// <summary>
        /// 获取队列中待处理的消息数量
        /// </summary>
        public int QueueCount => _asyncMessageQueue.Count;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="eventBus">事件总线引用</param>
        public AsyncEventProcessor(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region IAsyncProcessor 实现

        /// <summary>
        /// 添加异步消息到队列
        /// </summary>
        public void EnqueueAsyncMessage(string tag, object[] parameters)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogWarning("[AsyncEventProcessor] 尝试添加空标签的消息");
                return;
            }

            // 检查队列容量
            if (_asyncMessageQueue.Count >= MAX_QUEUE_SIZE)
            {
                Debug.LogWarning($"[AsyncEventProcessor] 异步消息队列已满 (>{MAX_QUEUE_SIZE})，丢弃消息: {tag}");
                _stats.DroppedMessages++;
                return;
            }

            var message = new AsyncMessage
            {
                Tag = tag,
                Parameters = parameters,
                EnqueueTime = DateTime.Now
            };

            _asyncMessageQueue.Enqueue(message);
            _stats.EnqueuedMessages++;

            // 如果没有在处理，启动处理
            if (!_isProcessing)
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// 启动异步处理
        /// </summary>
        public void StartProcessing()
        {
            if (_isProcessing) return;

            _isProcessing = true;
            _processingTask = Task.Run(ProcessAsyncMessagesAsync, _cancellationTokenSource.Token);

#if UNITY_EDITOR
            Debug.Log("[AsyncEventProcessor] 异步处理已启动");
#endif
        }

        /// <summary>
        /// 停止异步处理
        /// </summary>
        public void StopProcessing()
        {
            if (!_isProcessing) return;

            _cancellationTokenSource?.Cancel();
            _isProcessing = false;

            try
            {
                _processingTask?.Wait(1000); // 等待最多1秒
            }
            catch (OperationCanceledException)
            {
                // 正常的取消操作
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncEventProcessor] 停止处理时出错: {ex}");
            }

#if UNITY_EDITOR
            Debug.Log("[AsyncEventProcessor] 异步处理已停止");
#endif
        }

        /// <summary>
        /// 清空异步消息队列
        /// </summary>
        public void ClearQueue()
        {
            int clearCount = 0;
            while (_asyncMessageQueue.TryDequeue(out var _))
            {
                clearCount++;
            }

            _stats.ClearedMessages += clearCount;

#if UNITY_EDITOR
            if (clearCount > 0)
            {
                Debug.Log($"[AsyncEventProcessor] 清空了 {clearCount} 条异步消息");
            }
#endif
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 异步消息处理主循环
        /// </summary>
        private async Task ProcessAsyncMessagesAsync()
        {
            var token = _cancellationTokenSource.Token;

            try
            {
                while (!token.IsCancellationRequested)
                {
                    var processedCount = ProcessBatch();

                    if (processedCount == 0)
                    {
                        // 如果没有消息要处理，检查是否可以停止
                        if (_asyncMessageQueue.IsEmpty)
                        {
                            _isProcessing = false;
                            break;
                        }

                        // 短暂等待，避免空转
                        await Task.Delay(PROCESSING_INTERVAL_MS, token);
                    }
                    else
                    {
                        // 处理了消息，让出控制权给其他任务
                        await Task.Yield();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 正常的取消操作
#if UNITY_EDITOR
                Debug.Log("[AsyncEventProcessor] 异步处理被取消");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AsyncEventProcessor] 异步处理出错: {ex}");
                _stats.ProcessingErrors++;
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 批处理消息
        /// </summary>
        /// <returns>处理的消息数量</returns>
        private int ProcessBatch()
        {
            int processedCount = 0;
            int batchCount = 0;

            while (batchCount < BATCH_SIZE && _asyncMessageQueue.TryDequeue(out var message))
            {
                try
                {
                    ProcessSingleMessage(message);
                    processedCount++;
                    _stats.ProcessedMessages++;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AsyncEventProcessor] 处理消息时出错 - Tag: {message.Tag}, Error: {ex}");
                    _stats.ProcessingErrors++;
                }

                batchCount++;
            }

            return processedCount;
        }

        /// <summary>
        /// 处理单个消息
        /// </summary>
        /// <param name="message">要处理的消息</param>
        private void ProcessSingleMessage(AsyncMessage message)
        {
            // 计算消息延迟
            var delay = DateTime.Now - message.EnqueueTime;
            _stats.UpdateAverageDelay(delay);

            // 分发消息到事件总线
            _eventBus.Post(message.Tag, message.Parameters);

#if UNITY_EDITOR && DEBUG_ASYNC_MESSAGES
            Debug.Log($"[AsyncEventProcessor] 处理异步消息: {message.Tag}, 延迟: {delay.TotalMilliseconds:F2}ms");
#endif
        }

        #endregion

        #region 调试和统计

#if UNITY_EDITOR
        /// <summary>
        /// 获取异步处理器统计信息（仅编辑器模式）
        /// </summary>
        /// <returns>统计信息</returns>
        public AsyncProcessorStats GetStats()
        {
            return _stats.Clone();
        }

        /// <summary>
        /// 重置统计信息（仅编辑器模式）
        /// </summary>
        public void ResetStats()
        {
            _stats.Reset();
            Debug.Log("[AsyncEventProcessor] 统计信息已重置");
        }

        /// <summary>
        /// 获取处理器状态信息（仅编辑器模式）
        /// </summary>
        /// <returns>状态信息字符串</returns>
        public string GetStatusInfo()
        {
            return $"AsyncEventProcessor Status:\n" +
                   $"- Is Processing: {IsProcessing}\n" +
                   $"- Queue Count: {QueueCount}\n" +
                   $"- Task Status: {_processingTask?.Status ?? TaskStatus.RanToCompletion}\n" +
                   $"- Stats: {_stats}";
        }
#endif

        #endregion

        #region IDisposable 实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            StopProcessing();
            _cancellationTokenSource?.Dispose();
            ClearQueue();

#if UNITY_EDITOR
            Debug.Log("[AsyncEventProcessor] 已释放资源");
#endif
        }

        #endregion

        #region 内部类型

        /// <summary>
        /// 异步消息结构
        /// </summary>
        private struct AsyncMessage
        {
            public string Tag;
            public object[] Parameters;
            public DateTime EnqueueTime;
        }

        /// <summary>
        /// 异步处理器统计信息
        /// </summary>
        public class AsyncProcessorStats
        {
            public int EnqueuedMessages { get; set; }
            public int ProcessedMessages { get; set; }
            public int DroppedMessages { get; set; }
            public int ClearedMessages { get; set; }
            public int ProcessingErrors { get; set; }
            public TimeSpan AverageDelay { get; private set; }

            private readonly object _lock = new object();
            private TimeSpan _totalDelay = TimeSpan.Zero;
            private int _delayCount = 0;

            /// <summary>
            /// 更新平均延迟
            /// </summary>
            /// <param name="delay">新的延迟时间</param>
            internal void UpdateAverageDelay(TimeSpan delay)
            {
                lock (_lock)
                {
                    _totalDelay = _totalDelay.Add(delay);
                    _delayCount++;
                    AverageDelay = new TimeSpan(_totalDelay.Ticks / _delayCount);
                }
            }

            /// <summary>
            /// 重置统计信息
            /// </summary>
            internal void Reset()
            {
                lock (_lock)
                {
                    EnqueuedMessages = 0;
                    ProcessedMessages = 0;
                    DroppedMessages = 0;
                    ClearedMessages = 0;
                    ProcessingErrors = 0;
                    AverageDelay = TimeSpan.Zero;
                    _totalDelay = TimeSpan.Zero;
                    _delayCount = 0;
                }
            }

            /// <summary>
            /// 克隆统计信息
            /// </summary>
            /// <returns>统计信息副本</returns>
            internal AsyncProcessorStats Clone()
            {
                lock (_lock)
                {
                    return new AsyncProcessorStats
                    {
                        EnqueuedMessages = this.EnqueuedMessages,
                        ProcessedMessages = this.ProcessedMessages,
                        DroppedMessages = this.DroppedMessages,
                        ClearedMessages = this.ClearedMessages,
                        ProcessingErrors = this.ProcessingErrors,
                        AverageDelay = this.AverageDelay,
                        _totalDelay = this._totalDelay,
                        _delayCount = this._delayCount
                    };
                }
            }

            /// <summary>
            /// 转换为字符串
            /// </summary>
            public override string ToString()
            {
                return $"Enqueued: {EnqueuedMessages}, Processed: {ProcessedMessages}, " +
                       $"Dropped: {DroppedMessages}, Errors: {ProcessingErrors}, " +
                       $"Avg Delay: {AverageDelay.TotalMilliseconds:F2}ms";
            }
        }

        #endregion
    }
}