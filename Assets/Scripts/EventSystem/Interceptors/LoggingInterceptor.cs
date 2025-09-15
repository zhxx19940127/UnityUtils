using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using EventSystem.Core;
using EventSystem.Components;

namespace EventSystem.Interceptors
{
    /// <summary>
    /// 日志拦截器 - 记录所有通过的消息，支持优先级控制
    /// 提供多种日志级别和输出方式，可用于调试、监控和审计
    /// </summary>
    public class LoggingInterceptor : IMessageInterceptor, InterceptorManager.IPriorityInterceptor
    {
        #region 配置和状态

        /// <summary>
        /// 拦截器优先级（低优先级，最后执行日志记录）
        /// </summary>
        public int Priority => 1;

        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            None = 0, // 不记录
            Error = 1, // 仅错误
            Warning = 2, // 错误和警告
            Info = 3, // 信息、警告和错误
            Debug = 4, // 所有日志（包括调试）
            Verbose = 5 // 详细日志（包括参数内容）
        }

        /// <summary>
        /// 当前日志级别
        /// </summary>
        public LogLevel CurrentLogLevel { get; set; } = LogLevel.Info;

        /// <summary>
        /// 是否启用文件日志
        /// </summary>
        public bool EnableFileLogging { get; set; } = false;

        /// <summary>
        /// 是否启用控制台日志
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = true;

        /// <summary>
        /// 日志文件路径
        /// </summary>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// 最大日志文件大小（字节）
        /// </summary>
        public long MaxLogFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

        /// <summary>
        /// 消息统计信息
        /// </summary>
        private readonly Dictionary<string, MessageStats> _messageStats;

        /// <summary>
        /// 消息过滤器（只记录匹配的消息）
        /// </summary>
        private readonly HashSet<string> _messageFilters;

        /// <summary>
        /// 忽略的消息（不记录）
        /// </summary>
        private readonly HashSet<string> _ignoredMessages;

        /// <summary>
        /// 线程安全锁
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// 日志缓冲区
        /// </summary>
        private readonly StringBuilder _logBuffer;

        /// <summary>
        /// 最后一次刷新缓冲区的时间
        /// </summary>
        private DateTime _lastFlushTime;

        /// <summary>
        /// 缓冲区刷新间隔（秒）
        /// </summary>
        public float BufferFlushInterval { get; set; } = 5f;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logLevel">日志级别</param>
        /// <param name="enableFileLogging">是否启用文件日志</param>
        public LoggingInterceptor(LogLevel logLevel = LogLevel.Info, bool enableFileLogging = false)
        {
            CurrentLogLevel = logLevel;
            EnableFileLogging = enableFileLogging;

            _messageStats = new Dictionary<string, MessageStats>();
            _messageFilters = new HashSet<string>();
            _ignoredMessages = new HashSet<string>();
            _logBuffer = new StringBuilder(4096);
            _lastFlushTime = DateTime.Now;

            if (EnableFileLogging)
            {
                InitializeFileLogging();
            }

            SetupDefaultConfiguration();
        }

        #endregion

        #region 配置方法

        /// <summary>
        /// 初始化文件日志
        /// </summary>
        private void InitializeFileLogging()
        {
            try
            {
                // 使用线程安全的方式获取日志目录
                var logDirectory = GetLogDirectory();
                Directory.CreateDirectory(logDirectory);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                LogFilePath = Path.Combine(logDirectory, $"EventLog_{timestamp}.txt");

                // 写入日志文件头
                WriteToFile($"=== Event System Log Started at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");

                SafeDebugLog($"[LoggingInterceptor] 文件日志已初始化: {LogFilePath}");
            }
            catch (Exception ex)
            {
                SafeDebugLogError($"[LoggingInterceptor] 初始化文件日志失败: {ex.Message}");
                EnableFileLogging = false;
            }
        }

        /// <summary>
        /// 获取日志目录（线程安全）
        /// </summary>
        /// <returns>日志目录路径</returns>
        private string GetLogDirectory()
        {
            try
            {
                // 尝试获取 Unity 的持久化数据路径
                return Path.Combine(Application.persistentDataPath, "Logs");
            }
            catch
            {
                // 如果不在 Unity 主线程中，使用临时目录
                var tempPath = Path.GetTempPath();
                return Path.Combine(tempPath, "UnityEventLogs");
            }
        }

        /// <summary>
        /// 线程安全的 Debug.Log
        /// </summary>
        /// <param name="message">日志消息</param>
        private void SafeDebugLog(string message)
        {
            try
            {
                Debug.Log(message);
            }
            catch
            {
                // 如果不在主线程中，输出到控制台
                Console.WriteLine(message);
            }
        }

        /// <summary>
        /// 线程安全的 Debug.LogError
        /// </summary>
        /// <param name="message">错误消息</param>
        private void SafeDebugLogError(string message)
        {
            try
            {
                Debug.LogError(message);
            }
            catch
            {
                // 如果不在主线程中，输出到控制台
                Console.WriteLine($"ERROR: {message}");
            }
        }

        /// <summary>
        /// 线程安全的 Debug.LogWarning
        /// </summary>
        /// <param name="message">警告消息</param>
        private void SafeDebugLogWarning(string message)
        {
            try
            {
                Debug.LogWarning(message);
            }
            catch
            {
                // 如果不在主线程中，输出到控制台
                Console.WriteLine($"WARNING: {message}");
            }
        }

        /// <summary>
        /// 添加消息过滤器（只记录匹配的消息）
        /// </summary>
        /// <param name="messageTag">消息标签或模式</param>
        public void AddMessageFilter(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _messageFilters.Add(messageTag);
                SafeDebugLog($"[LoggingInterceptor] 添加消息过滤器: {messageTag}");
            }
        }

        /// <summary>
        /// 移除消息过滤器
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveMessageFilter(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _messageFilters.Remove(messageTag);
            }
        }

        /// <summary>
        /// 添加忽略的消息
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void AddIgnoredMessage(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _ignoredMessages.Add(messageTag);
            }
        }

        /// <summary>
        /// 移除忽略的消息
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemoveIgnoredMessage(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _ignoredMessages.Remove(messageTag);
            }
        }

        /// <summary>
        /// 清除所有过滤器和配置
        /// </summary>
        public void ClearAllFilters()
        {
            lock (_lock)
            {
                _messageFilters.Clear();
                _ignoredMessages.Clear();
                _messageStats.Clear();
            }

            SafeDebugLog("[LoggingInterceptor] 已清除所有过滤器");
        }

        #endregion

        #region IMessageInterceptor 实现

        /// <summary>
        /// 判断是否应该处理该消息（始终返回true，不拦截任何消息）
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>总是返回true，允许所有消息通过</returns>
        public bool ShouldProcess(string tag, object[] parameters)
        {
            try
            {
                // 记录消息日志
                LogMessage(tag, parameters);

                // 始终允许消息通过（日志拦截器不阻止消息）
                return true;
            }
            catch (Exception ex)
            {
                SafeDebugLogError($"[LoggingInterceptor] 日志记录过程中发生异常: {ex.Message}");
                return true; // 即使日志记录失败，也允许消息通过
            }
        }

        #endregion

        #region 日志记录方法

        /// <summary>
        /// 记录消息日志
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        private void LogMessage(string tag, object[] parameters)
        {
            if (CurrentLogLevel == LogLevel.None || string.IsNullOrEmpty(tag))
                return;

            // 检查是否应该忽略此消息
            if (ShouldIgnoreMessage(tag))
                return;

            // 检查是否通过过滤器
            if (!PassesMessageFilter(tag))
                return;

            lock (_lock)
            {
                // 更新统计信息
                UpdateMessageStats(tag);

                // 生成日志内容
                var logEntry = GenerateLogEntry(tag, parameters);

                // 输出到控制台
                if (EnableConsoleLogging)
                {
                    OutputToConsole(tag, logEntry);
                }

                // 输出到文件
                if (EnableFileLogging)
                {
                    AppendToBuffer(logEntry);

                    // 检查是否需要刷新缓冲区
                    if (ShouldFlushBuffer())
                    {
                        FlushBuffer();
                    }
                }
            }
        }

        /// <summary>
        /// 生成日志条目
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>日志条目字符串</returns>
        private string GenerateLogEntry(string tag, object[] parameters)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            
            // 检查是否在主线程中，如果不是则使用线程ID作为替代
            var frameInfo = GetFrameInfo();

            var logBuilder = new StringBuilder(256);
            logBuilder.Append($"[{timestamp}] {frameInfo} | {tag}");

            // 根据日志级别决定记录的详细程度
            if (CurrentLogLevel >= LogLevel.Debug)
            {
                var paramCount = parameters?.Length ?? 0;
                logBuilder.Append($" | Params:{paramCount}");

                if (CurrentLogLevel >= LogLevel.Verbose && parameters != null && parameters.Length > 0)
                {
                    logBuilder.Append(" | Data:[");
                    for (int i = 0; i < Math.Min(parameters.Length, 5); i++) // 最多显示5个参数
                    {
                        if (i > 0) logBuilder.Append(", ");

                        var param = parameters[i];
                        if (param == null)
                        {
                            logBuilder.Append("null");
                        }
                        else
                        {
                            var paramStr = param.ToString();
                            if (paramStr.Length > 50)
                            {
                                paramStr = paramStr.Substring(0, 47) + "...";
                            }

                            logBuilder.Append($"{param.GetType().Name}:{paramStr}");
                        }
                    }

                    if (parameters.Length > 5)
                    {
                        logBuilder.Append($", ...{parameters.Length - 5} more");
                    }

                    logBuilder.Append("]");
                }
            }

            logBuilder.AppendLine();
            return logBuilder.ToString();
        }

        /// <summary>
        /// 获取帧信息（线程安全）
        /// </summary>
        /// <returns>帧信息字符串</returns>
        private string GetFrameInfo()
        {
            // 使用时间戳和线程信息作为安全的替代方案
            var threadId = Thread.CurrentThread.ManagedThreadId;
            var isMainThread = threadId == 1; // 通常主线程ID为1
            
            if (isMainThread)
            {
                try
                {
                    // 尝试获取帧计数，如果在主线程中应该能成功
                    return $"Frame:{Time.frameCount}";
                }
                catch
                {
                    // 如果失败，可能不在主线程或Unity上下文不可用
                }
            }

            // 使用线程信息作为替代
            var threadName = !string.IsNullOrEmpty(Thread.CurrentThread.Name) 
                ? Thread.CurrentThread.Name 
                : (isMainThread ? "Main" : "Worker");
            return $"Thread:{threadId}({threadName})";
        }

        /// <summary>
        /// 输出到控制台
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="logEntry">日志条目</param>
        private void OutputToConsole(string tag, string logEntry)
        {
            // 根据消息类型选择不同的日志级别
            if (tag.Contains("Error") || tag.StartsWith("Err"))
            {
                SafeDebugLogError($"[EventLog] {logEntry.TrimEnd()}");
            }
            else if (tag.Contains("Warning") || tag.Contains("Warn"))
            {
                SafeDebugLogWarning($"[EventLog] {logEntry.TrimEnd()}");
            }
            else
            {
                SafeDebugLog($"[EventLog] {logEntry.TrimEnd()}");
            }
        }

        /// <summary>
        /// 添加到缓冲区
        /// </summary>
        /// <param name="logEntry">日志条目</param>
        private void AppendToBuffer(string logEntry)
        {
            _logBuffer.Append(logEntry);
        }

        /// <summary>
        /// 检查是否应该刷新缓冲区
        /// </summary>
        /// <returns>是否应该刷新</returns>
        private bool ShouldFlushBuffer()
        {
            return _logBuffer.Length > 8192 || // 缓冲区超过8KB
                   (DateTime.Now - _lastFlushTime).TotalSeconds > BufferFlushInterval; // 超过刷新间隔
        }

        /// <summary>
        /// 刷新缓冲区到文件
        /// </summary>
        private void FlushBuffer()
        {
            if (_logBuffer.Length == 0 || !EnableFileLogging)
                return;

            try
            {
                WriteToFile(_logBuffer.ToString());
                _logBuffer.Clear();
                _lastFlushTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                SafeDebugLogError($"[LoggingInterceptor] 刷新日志缓冲区失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="content">内容</param>
        private void WriteToFile(string content)
        {
            if (string.IsNullOrEmpty(LogFilePath))
                return;

            // 检查文件大小
            if (File.Exists(LogFilePath) && new FileInfo(LogFilePath).Length > MaxLogFileSize)
            {
                RotateLogFile();
            }

            File.AppendAllText(LogFilePath, content, Encoding.UTF8);
        }

        /// <summary>
        /// 轮转日志文件
        /// </summary>
        private void RotateLogFile()
        {
            try
            {
                var directory = Path.GetDirectoryName(LogFilePath);
                var fileName = Path.GetFileNameWithoutExtension(LogFilePath);
                var extension = Path.GetExtension(LogFilePath);

                var backupPath = Path.Combine(directory,
                    $"{fileName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");
                File.Move(LogFilePath, backupPath);

                SafeDebugLog($"[LoggingInterceptor] 日志文件已轮转: {backupPath}");
            }
            catch (Exception ex)
            {
                SafeDebugLogError($"[LoggingInterceptor] 日志文件轮转失败: {ex.Message}");
            }
        }

        #endregion

        #region 过滤和统计

        /// <summary>
        /// 检查是否应该忽略消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否忽略</returns>
        private bool ShouldIgnoreMessage(string tag)
        {
            return _ignoredMessages.Contains(tag);
        }

        /// <summary>
        /// 检查是否通过消息过滤器
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否通过</returns>
        private bool PassesMessageFilter(string tag)
        {
            if (_messageFilters.Count == 0)
                return true; // 没有过滤器时，允许所有消息

            foreach (var filter in _messageFilters)
            {
                if (tag.Contains(filter) || tag.StartsWith(filter))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 更新消息统计
        /// </summary>
        /// <param name="tag">消息标签</param>
        private void UpdateMessageStats(string tag)
        {
            if (!_messageStats.TryGetValue(tag, out var stats))
            {
                stats = new MessageStats();
                _messageStats[tag] = stats;
            }

            stats.Count++;
            stats.LastSeen = DateTime.Now;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 手动刷新缓冲区
        /// </summary>
        public void FlushNow()
        {
            lock (_lock)
            {
                FlushBuffer();
            }
        }

        /// <summary>
        /// 获取消息统计
        /// </summary>
        /// <returns>消息统计字典</returns>
        public Dictionary<string, MessageStats> GetMessageStatistics()
        {
            lock (_lock)
            {
                return new Dictionary<string, MessageStats>(_messageStats);
            }
        }

        /// <summary>
        /// 打印统计报告
        /// </summary>
        public void PrintStatisticsReport()
        {
            lock (_lock)
            {
                SafeDebugLog("=== LoggingInterceptor 统计报告 ===");
                SafeDebugLog($"当前日志级别: {CurrentLogLevel}");
                SafeDebugLog($"文件日志: {(EnableFileLogging ? "启用" : "禁用")}");
                SafeDebugLog($"控制台日志: {(EnableConsoleLogging ? "启用" : "禁用")}");
                SafeDebugLog($"已记录消息类型数量: {_messageStats.Count}");
                SafeDebugLog($"消息过滤器数量: {_messageFilters.Count}");
                SafeDebugLog($"忽略消息数量: {_ignoredMessages.Count}");

                if (_messageStats.Count > 0)
                {
                    SafeDebugLog("--- 消息统计 (Top 10) ---");
                    var sortedStats = _messageStats.OrderByDescending(kvp => kvp.Value.Count).Take(10);

                    foreach (var stat in sortedStats)
                    {
                        SafeDebugLog($"  {stat.Key}: {stat.Value.Count}次 (最后: {stat.Value.LastSeen:HH:mm:ss})");
                    }
                }
            }
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                FlushBuffer();
            }
        }

        #endregion

        #region 默认配置

        /// <summary>
        /// 设置默认配置
        /// </summary>
        private void SetupDefaultConfiguration()
        {
            // 默认忽略高频消息
            AddIgnoredMessage("Update");
            AddIgnoredMessage("FixedUpdate");
            AddIgnoredMessage("LateUpdate");
            AddIgnoredMessage("OnGUI");

            // 默认过滤器（如果需要的话）
            // AddMessageFilter("Game");
            // AddMessageFilter("UI");
        }

        #endregion

        #region 内部类

        /// <summary>
        /// 消息统计信息
        /// </summary>
        public class MessageStats
        {
            /// <summary>
            /// 调用次数
            /// </summary>
            public int Count { get; set; } = 0;

            /// <summary>
            /// 最后调用时间
            /// </summary>
            public DateTime LastSeen { get; set; } = DateTime.Now;
        }

        #endregion
    }
}