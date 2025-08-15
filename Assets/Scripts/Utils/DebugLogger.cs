using System.Collections.Generic;

namespace GameObjectToolkit
{
    using UnityEngine;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;

    /// <summary>
    /// 日志调试工具类
    /// 提供全面的日志记录和调试功能
    /// </summary>
    public static class DebugLogger
    {
        #region 配置项

        /// <summary>
        /// 日志级别枚举
        /// </summary>
        public enum LogLevel
        {
            Verbose, // 最详细的日志
            Debug, // 调试信息
            Info, // 普通信息
            Warning, // 警告
            Error, // 错误
            Critical, // 严重错误
            None // 不记录任何日志
        }

        // 当前日志级别
        private static LogLevel _currentLogLevel = LogLevel.Debug;

        // 是否启用堆栈跟踪
        private static bool _enableStackTrace = true;

        // 日志文件路径
        private static string _logFilePath = Application.persistentDataPath + "/game_log.txt";

        // 是否写入文件
        private static bool _enableFileLogging = false;

        // 是否显示时间戳
        private static bool _showTimestamp = true;

        // 是否显示日志级别
        private static bool _showLogLevel = true;

        // 是否启用颜色标记
        private static bool _enableColor = true;

        #endregion

        #region 公共配置方法

        /// <summary>
        /// 设置日志级别
        /// </summary>
        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
        }

        /// <summary>
        /// 启用/禁用堆栈跟踪
        /// </summary>
        public static void EnableStackTrace(bool enable)
        {
            _enableStackTrace = enable;
        }

        /// <summary>
        /// 设置日志文件路径
        /// </summary>
        public static void SetLogFilePath(string path)
        {
            _logFilePath = path;
        }

        /// <summary>
        /// 启用/禁用文件日志
        /// </summary>
        public static void EnableFileLogging(bool enable)
        {
            _enableFileLogging = enable;
            if (enable)
            {
                InitializeLogFile();
            }
        }

        /// <summary>
        /// 启用/禁用时间戳显示
        /// </summary>
        public static void ShowTimestamp(bool show)
        {
            _showTimestamp = show;
        }

        /// <summary>
        /// 启用/禁用日志级别显示
        /// </summary>
        public static void ShowLogLevel(bool show)
        {
            _showLogLevel = show;
        }

        /// <summary>
        /// 启用/禁用颜色标记
        /// </summary>
        public static void EnableColor(bool enable)
        {
            _enableColor = enable;
        }

        #endregion

        #region 核心日志方法

        /// <summary>
        /// 记录详细日志（Verbose级别）
        /// </summary>
        public static void Verbose(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Verbose, message, context);
        }

        /// <summary>
        /// 记录调试日志（Debug级别）
        /// </summary>
        public static void Debug(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Debug, message, context);
        }

        /// <summary>
        /// 记录信息日志（Info级别）
        /// </summary>
        public static void Info(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Info, message, context);
        }

        /// <summary>
        /// 记录警告日志（Warning级别）
        /// </summary>
        public static void Warning(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Warning, message, context);
        }

        /// <summary>
        /// 记录错误日志（Error级别）
        /// </summary>
        public static void Error(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Error, message, context);
        }

        /// <summary>
        /// 记录严重错误日志（Critical级别）
        /// </summary>
        public static void Critical(object message, UnityEngine.Object context = null)
        {
            LogInternal(LogLevel.Critical, message, context);
        }

        /// <summary>
        /// 条件日志（仅在条件为true时记录）
        /// </summary>
        public static void LogIf(bool condition, object message, LogLevel level = LogLevel.Debug,
            UnityEngine.Object context = null)
        {
            if (condition)
            {
                LogInternal(level, message, context);
            }
        }

        #endregion

        #region 高级日志功能

        /// <summary>
        /// 记录带有堆栈跟踪的日志
        /// </summary>
        public static void LogWithStackTrace(object message, LogLevel level = LogLevel.Debug,
            UnityEngine.Object context = null)
        {
            string stackTrace = Environment.StackTrace;
            LogInternal(level, $"{message}\nStack Trace:\n{stackTrace}", context);
        }

        /// <summary>
        /// 记录对象的结构化信息
        /// </summary>
        public static void LogObject(object obj, LogLevel level = LogLevel.Debug, UnityEngine.Object context = null)
        {
            if (obj == null)
            {
                LogInternal(level, "[NULL]", context);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{obj.GetType().Name} Details:");

            foreach (var field in obj.GetType().GetFields())
            {
                sb.AppendLine($"  {field.Name}: {field.GetValue(obj)}");
            }

            foreach (var prop in obj.GetType().GetProperties())
            {
                if (prop.CanRead)
                {
                    sb.AppendLine($"  {prop.Name}: {prop.GetValue(obj, null)}");
                }
            }

            LogInternal(level, sb.ToString(), context);
        }

        /// <summary>
        /// 开始性能分析计时
        /// </summary>
        /// <param name="name">计时器名称</param>
        /// <returns>计时器ID</returns>
        public static string BeginProfile(string name)
        {
            string id = Guid.NewGuid().ToString();
            ProfileData data = new ProfileData
            {
                Name = name,
                StartTime = DateTime.Now,
                Stopwatch = Stopwatch.StartNew()
            };
            _profileData[id] = data;

            LogInternal(LogLevel.Debug, $"[Profiler] {name} started", null);
            return id;
        }

        /// <summary>
        /// 结束性能分析计时并记录结果
        /// </summary>
        /// <param name="id">计时器ID</param>
        public static void EndProfile(string id)
        {
            if (_profileData.TryGetValue(id, out ProfileData data))
            {
                data.Stopwatch.Stop();
                TimeSpan elapsed = DateTime.Now - data.StartTime;
                LogInternal(LogLevel.Debug, $"[Profiler] {data.Name} completed - " +
                                            $"CPU Time: {data.Stopwatch.ElapsedMilliseconds}ms, " +
                                            $"Real Time: {elapsed.TotalMilliseconds}ms", null);
                _profileData.Remove(id);
            }
            else
            {
                LogInternal(LogLevel.Warning, $"[Profiler] Invalid profile ID: {id}", null);
            }
        }

        private static Dictionary<string, ProfileData> _profileData = new Dictionary<string, ProfileData>();

        private struct ProfileData
        {
            public string Name;
            public DateTime StartTime;
            public Stopwatch Stopwatch;
        }

        #endregion

        #region 内部实现

        /// <summary>
        /// 内部日志实现
        /// </summary>
        private static void LogInternal(LogLevel level, object message, UnityEngine.Object context)
        {
            // 检查日志级别
            if (level < _currentLogLevel)
            {
                return;
            }

            // 构建日志消息
            string logMessage = BuildLogMessage(level, message);

            // 根据级别调用Unity的日志系统
            switch (level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(logMessage, context);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(logMessage, context);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    UnityEngine.Debug.LogError(logMessage, context);
                    break;
            }

            // 写入日志文件
            if (_enableFileLogging)
            {
                WriteToLogFile(logMessage);
            }
        }

        /// <summary>
        /// 构建日志消息
        /// </summary>
        private static string BuildLogMessage(LogLevel level, object message)
        {
            StringBuilder sb = new StringBuilder();

            // 添加颜色标记开始
            if (_enableColor)
            {
                sb.Append(GetColorTag(level));
            }

            // 添加时间戳
            if (_showTimestamp)
            {
                sb.Append($"[{DateTime.Now:HH:mm:ss.fff}] ");
            }

            // 添加日志级别
            if (_showLogLevel)
            {
                sb.Append($"[{level.ToString().ToUpper()}] ");
            }

            // 添加消息内容
            sb.Append(message);

            // 添加堆栈跟踪
            if (_enableStackTrace && (level >= LogLevel.Warning))
            {
                sb.Append("\n" + GetSimplifiedStackTrace());
            }

            // 添加颜色标记结束
            if (_enableColor)
            {
                sb.Append("</color>");
            }

            return sb.ToString();
        }

        /// <summary>
        /// 获取颜色标记
        /// </summary>
        private static string GetColorTag(LogLevel level)
        {
            if (!_enableColor) return "";

            switch (level)
            {
                case LogLevel.Verbose: return "<color=#888888>"; // 灰色
                case LogLevel.Debug: return "<color=#AAAAAA>"; // 浅灰色
                case LogLevel.Info: return "<color=#FFFFFF>"; // 白色
                case LogLevel.Warning: return "<color=#FFFF00>"; // 黄色
                case LogLevel.Error: return "<color=#FF0000>"; // 红色
                case LogLevel.Critical: return "<color=#FF00FF>"; // 紫色
                default: return "";
            }
        }

        /// <summary>
        /// 获取简化的堆栈跟踪
        /// </summary>
        private static string GetSimplifiedStackTrace()
        {
            string stackTrace = Environment.StackTrace;
            string[] lines = stackTrace.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 跳过前3行（它们属于日志系统本身）
            StringBuilder sb = new StringBuilder();
            for (int i = 3; i < lines.Length && i < 6; i++) // 最多显示3行堆栈
            {
                string line = lines[i].Trim();
                if (line.StartsWith("at "))
                {
                    line = line.Substring(3);
                }

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 初始化日志文件
        /// </summary>
        private static void InitializeLogFile()
        {
            try
            {
                // 创建目录（如果不存在）
                string directory = Path.GetDirectoryName(_logFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 写入初始化信息
                File.WriteAllText(_logFilePath, $"=== Log Started at {DateTime.Now} ===\n\n");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to initialize log file: {e.Message}");
                _enableFileLogging = false;
            }
        }

        /// <summary>
        /// 写入日志文件
        /// </summary>
        private static void WriteToLogFile(string message)
        {
            try
            {
                // 移除颜色标记
                string cleanMessage = System.Text.RegularExpressions.Regex.Replace(
                    message, @"<.*?>", string.Empty);

                File.AppendAllText(_logFilePath, cleanMessage + "\n");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Failed to write to log file: {e.Message}");
            }
        }

        #endregion
    }
}