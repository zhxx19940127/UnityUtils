using System;
using System.Collections.Generic;

/// <summary>
/// 状态转换事件参数
/// </summary>
/// <typeparam name="ParentT">父级对象类型</typeparam>
public class ModeTransitionEventArgs<ParentT> : EventArgs
{
    /// <summary>
    /// 来源状态ID
    /// </summary>
    public int FromMode { get; }

    /// <summary>
    /// 目标状态ID
    /// </summary>
    public int ToMode { get; }

    /// <summary>
    /// 转换参数
    /// </summary>
    public Dictionary<string, object> Parameters { get; }

    /// <summary>
    /// 是否取消转换
    /// </summary>
    public bool Cancel { get; set; }

    /// <summary>
    /// 关联的父级对象
    /// </summary>
    public ParentT Parent { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public ModeTransitionEventArgs(int fromMode, int toMode, Dictionary<string, object> parameters, ParentT parent)
    {
        FromMode = fromMode;
        ToMode = toMode;
        Parameters = parameters ?? new Dictionary<string, object>();
        Parent = parent;
    }
}