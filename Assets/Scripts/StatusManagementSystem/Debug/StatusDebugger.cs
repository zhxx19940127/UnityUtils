using UnityEngine;
using System.Text;

/// <summary>
/// 状态管理系统调试工具
/// </summary>
public static class StatusDebugger
{
    /// <summary>
    /// 启用详细日志
    /// </summary>
    public static bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// 日志状态转换
    /// </summary>
    public static void LogStateTransition(int fromState, int toState, string reason = "")
    {
        if (EnableVerboseLogging)
        {
            Debug.Log($"[状态转换] {fromState} -> {toState} {reason}");
        }
    }

    /// <summary>
    /// 日志状态转换失败
    /// </summary>
    public static void LogStateTransitionFailed(int fromState, int toState, string reason)
    {
        Debug.LogWarning($"[状态转换失败] {fromState} -> {toState}. 原因: {reason}");
    }

    /// <summary>
    /// 日志状态生命周期事件
    /// </summary>
    public static void LogStateLifecycle(string stateName, string lifecycleEvent, string details = "")
    {
        if (EnableVerboseLogging)
        {
            Debug.Log($"[状态生命周期] {stateName}.{lifecycleEvent} {details}");
        }
    }

    /// <summary>
    /// 日志错误
    /// </summary>
    public static void LogError(string context, System.Exception ex)
    {
        Debug.LogError($"[状态管理错误] {context}: {ex.Message}\n{ex.StackTrace}");
    }

    /// <summary>
    /// 验证状态管理器的健康状态
    /// </summary>
    public static bool ValidateStateManager<T>(Modes<T> manager) where T : new()
    {
        if (manager == null)
        {
            Debug.LogError("[状态验证] 状态管理器为null");
            return false;
        }

        var activeMode = manager.ActiveMode;
        if (activeMode >= 0 && !manager.HasMode(activeMode))
        {
            Debug.LogError($"[状态验证] 活动状态ID {activeMode} 不存在");
            return false;
        }

        if (activeMode >= 0 && !manager.IsModeLoaded(activeMode))
        {
            Debug.LogWarning($"[状态验证] 活动状态ID {activeMode} 未加载");
        }

        return true;
    }

    /// <summary>
    /// 获取状态管理器的状态报告
    /// </summary>
    public static string GetStatusReport<T>(Modes<T> manager) where T : new()
    {
        if (manager == null)
            return "状态管理器为null";

        var sb = new StringBuilder();
        sb.AppendLine("=== 状态管理器报告 ===");
        sb.AppendLine($"当前状态: {manager.ActiveMode}");
        sb.AppendLine($"上一状态: {manager.PreviousMode}");
        sb.AppendLine($"历史记录数: {manager.HistoryCount}");
        
        // 检查所有已注册的状态
        sb.AppendLine("已加载状态:");
        foreach (var kvp in manager.ModesList)
        {
            var status = kvp.Value != null ? "已加载" : "未加载";
            sb.AppendLine($"  [{kvp.Key}] {status}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// 状态管理系统的性能监控器
/// </summary>
public class StatusPerformanceMonitor : MonoBehaviour
{
    [Header("监控设置")]
    public bool enableMonitoring = true;
    public float reportInterval = 5f; // 报告间隔（秒）

    private float lastReportTime;
    private int stateTransitionCount;
    private int errorCount;

    void Update()
    {
        if (!enableMonitoring) return;

        if (Time.time - lastReportTime >= reportInterval)
        {
            GeneratePerformanceReport();
            lastReportTime = Time.time;
        }
    }

    private void GeneratePerformanceReport()
    {
        Debug.Log($"[性能报告] 过去{reportInterval}秒内:");
        Debug.Log($"  状态转换次数: {stateTransitionCount}");
        Debug.Log($"  错误次数: {errorCount}");

        // 重置计数器
        stateTransitionCount = 0;
        errorCount = 0;
    }

    public void RecordStateTransition()
    {
        if (enableMonitoring)
            stateTransitionCount++;
    }

    public void RecordError()
    {
        if (enableMonitoring)
            errorCount++;
    }
}

/// <summary>
/// 扩展方法，为状态管理器添加调试功能
/// </summary>
public static class ModesDebugExtensions
{
    /// <summary>
    /// 安全的状态选择，带有详细的错误报告
    /// </summary>
    public static bool SafeSelect<T>(this Modes<T> manager, int targetState, string context = "") where T : new()
    {
        try
        {
            if (!StatusDebugger.ValidateStateManager(manager))
                return false;

            StatusDebugger.LogStateTransition(manager.ActiveMode, targetState, context);
            
            bool result = manager.Select(targetState);
            
            if (!result)
            {
                StatusDebugger.LogStateTransitionFailed(manager.ActiveMode, targetState, "Select返回false");
            }
            
            return result;
        }
        catch (System.Exception ex)
        {
            StatusDebugger.LogError($"SafeSelect失败，context: {context}", ex);
            return false;
        }
    }

    /// <summary>
    /// 打印当前状态信息
    /// </summary>
    public static void PrintStatus<T>(this Modes<T> manager) where T : new()
    {
        Debug.Log(StatusDebugger.GetStatusReport(manager));
    }
}
