using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态管理系统使用示例
/// </summary>
public class StatusManagerExample : MonoBehaviour
{
    // 状态管理器实例
    private Modes<StatusManagerExample> modeManager;
    
    // 状态ID常量
    public const int STATE_IDLE = 0;
    public const int STATE_MOVING = 1;
    public const int STATE_ATTACKING = 2;

    void Start()
    {
        // 初始化状态管理器
        modeManager = new Modes<StatusManagerExample>(this);

        // 注册状态
        modeManager.RegisterLazy<IdleState>(() => new IdleState());
        modeManager.RegisterLazy<MovingState>(() => new MovingState());
        modeManager.RegisterLazy<AttackingState>(() => new AttackingState());

        // 配置状态参数
        modeManager.ConfigureMode(STATE_IDLE).DefaultParameters["speed"] = 0f;
        modeManager.ConfigureMode(STATE_MOVING).DefaultParameters["speed"] = 5f;
        modeManager.ConfigureMode(STATE_ATTACKING).DefaultParameters["damage"] = 10f;

        // 启动初始状态
        modeManager.Select(STATE_IDLE);

        Debug.Log("状态管理器初始化完成");
    }

    void Update()
    {
        // 更新当前状态
        modeManager?.Update();

        // 示例：简单的状态切换逻辑
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            modeManager.Select(STATE_IDLE);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            modeManager.Select(STATE_MOVING);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            modeManager.Select(STATE_ATTACKING);
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            modeManager.GoBack(); // 返回上一个状态
        }
    }

    void LateUpdate()
    {
        modeManager?.LateUpdate();
    }

    void FixedUpdate()
    {
        modeManager?.FixedUpdate();
    }

    void OnGUI()
    {
        modeManager?.OnGUI();

        // 显示调试信息
        GUI.Label(new Rect(10, 10, 300, 20), $"当前状态: {modeManager?.ActiveMode}");
        GUI.Label(new Rect(10, 30, 300, 20), $"上一个状态: {modeManager?.PreviousMode}");
        GUI.Label(new Rect(10, 50, 300, 20), "按键 1-3 切换状态，Backspace 返回");

#if UNITY_EDITOR
        // 在Editor模式下显示详细调试信息
        if (modeManager != null)
        {
            var debugInfo = modeManager.GetDebugInfo();
            GUI.TextArea(new Rect(10, 80, 400, 200), debugInfo);
        }
#endif
    }

    void OnDestroy()
    {
        // 清理资源
        modeManager?.Dispose();
    }

    // 公共方法供状态调用
    public void DoSomething(string message)
    {
        Debug.Log($"状态调用: {message}");
    }
}

// 示例状态实现
public class IdleState : ModeBase<StatusManagerExample>
{
    public override void Enter(Dictionary<string, object> parameters = null)
    {
        Debug.Log("进入空闲状态");
        Parent?.DoSomething("开始空闲");
    }

    public override void Update()
    {
        // 空闲状态的更新逻辑
    }

    public override void Exit()
    {
        Debug.Log("退出空闲状态");
    }
}

public class MovingState : ModeBase<StatusManagerExample>
{
    private float speed;

    public override void Enter(Dictionary<string, object> parameters = null)
    {
        Debug.Log("进入移动状态");
        
        // 获取参数
        if (parameters != null && parameters.TryGetValue("speed", out var speedObj))
        {
            speed = (float)speedObj;
        }
        
        Parent?.DoSomething($"开始移动，速度: {speed}");
    }

    public override void Update()
    {
        // 移动逻辑
        if (Parent != null)
        {
            // 这里可以添加实际的移动代码
        }
    }

    public override void Exit()
    {
        Debug.Log("退出移动状态");
    }
}

public class AttackingState : ModeBase<StatusManagerExample>
{
    private float damage;

    public override void Enter(Dictionary<string, object> parameters = null)
    {
        Debug.Log("进入攻击状态");
        
        // 获取参数
        if (parameters != null && parameters.TryGetValue("damage", out var damageObj))
        {
            damage = (float)damageObj;
        }
        
        Parent?.DoSomething($"开始攻击，伤害: {damage}");
    }

    public override void Update()
    {
        // 攻击逻辑
    }

    public override bool CanEnter(int fromMode, Dictionary<string, object> parameters = null)
    {
        // 只允许从空闲或移动状态进入攻击状态
        return fromMode == StatusManagerExample.STATE_IDLE || fromMode == StatusManagerExample.STATE_MOVING;
    }

    public override void Exit()
    {
        Debug.Log("退出攻击状态");
    }
}
