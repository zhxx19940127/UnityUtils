using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VSCodeEditor;

public class Character : MonoBehaviour
{
    public string Name { get; set; }
    public int Stamina { get; set; }
    public int Health { get; set; }

    internal class IdleState : ModeBase<Character>
    {
        public override async Task EnterAsync(Dictionary<string, object> parameters = null)
        {
            Debug.Log($"{Parent.Name} 进入空闲状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override async Task ExitAsync()
        {
            Debug.Log($"{Parent.Name} 退出空闲状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override void Update()
        {
            // 空闲状态的更新逻辑
        }
    }

    internal class WalkState : ModeBase<Character>
    {
        public override async Task EnterAsync(Dictionary<string, object> parameters = null)
        {
            Debug.Log($"{Parent.Name} 进入行走状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override async Task ExitAsync()
        {
            Debug.Log($"{Parent.Name} 退出行走状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override void Update()
        {
            // 行走状态的更新逻辑
        }
    }

    internal class AttackState : ModeBase<Character>
    {
        public override async Task EnterAsync(Dictionary<string, object> parameters = null)
        {
            if (parameters == null || !parameters.ContainsKey("target"))
            {
                Debug.LogError($"{Parent.Name} 攻击失败：缺少目标参数");
                return;
            }

            Debug.Log($"{Parent.Name} 攻击 {parameters["target"]}");
            await Task.Delay(100); // 模拟异步操作
        }

        public override async Task ExitAsync()
        {
            Debug.Log($"{Parent.Name} 退出攻击状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override void Update()
        {
            // 攻击状态的更新逻辑
        }
    }

    internal class DeadState : ModeBase<Character>
    {
        public override async Task EnterAsync(Dictionary<string, object> parameters = null)
        {
            Debug.Log($"{Parent.Name} 进入死亡状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override async Task ExitAsync()
        {
            Debug.Log($"{Parent.Name} 退出死亡状态");
            await Task.Delay(100); // 模拟异步操作
        }

        public override void Update()
        {
            // 死亡状态的更新逻辑
        }
    }

    private void Start()
    {
        test();
    }

    private async void test()
    {
        // 1. 创建角色和状态管理器
        var character = new Character { Name = "英雄" };
        var modes = new Modes<Character>(character, historyLimit: 5);

        // 2. 注册状态（两种方式）
        // 方式1：直接创建
        var idleState = modes.Create<IdleState>(1);
        int idleMode = 1; // 获取状态ID

        // 方式2：延迟注册
        int walkMode = modes.RegisterLazy(() => new WalkState());
        int attackMode = modes.RegisterLazy(() => new AttackState());
        int deadMode = modes.RegisterLazy(() => new DeadState());


        // 4. 添加事件监听
        modes.BeforeModeChange += (sender, e) =>
            Debug.Log($"准备从状态 {e.FromMode} 切换到 {e.ToMode}");
        modes.AfterModeChange += (sender, e) =>
            Debug.Log($"已从状态 {e.FromMode} 切换到 {e.ToMode}");

        modes.ModeChangeRejected += (sender, e) =>
            Debug.Log($"状态切换被拒绝: {e.FromMode} -> {e.ToMode}");
        // 5. 状态切换演示
        Debug.Log("\n--- 切换到空闲状态 ---");
        modes.Select(idleMode);

        Debug.Log("\n--- 切换到行走状态 ---");
        modes.Select(walkMode);

        // 6. 模拟游戏循环
        for (int i = 0; i < 5; i++)
        {
            modes.Update();
            await Task.Delay(500);
        }

        Debug.Log("\n--- 尝试攻击（不带参数，应失败） ---");
        bool success = modes.Select(attackMode);
        Debug.Log($"切换结果: {success}");

        Debug.Log("\n--- 正确方式攻击 ---");
        modes.Select(attackMode, new Dictionary<string, object> { ["target"] = "怪物" });

        for (int i = 0; i < 3; i++)
        {
            modes.Update();
            await Task.Delay(500);
        }

        Debug.Log("\n--- 强制耗尽体力 ---");
        character.Stamina = 5;

        Debug.Log("\n--- 尝试切换到行走状态（体力不足应失败） ---");
        modes.Select(walkMode);

        Debug.Log("\n--- 返回上一个状态 ---");
        modes.GoBack();

        Debug.Log("\n--- 模拟角色死亡 ---");
        character.Health = 0;
        modes.Select(deadMode);

        Debug.Log("\n--- 尝试从死亡状态切换（应失败） ---");
        modes.Select(idleMode);

        // 7. 状态管理操作
        Debug.Log("\n--- 状态管理操作 ---");
        Debug.Log($"当前状态: {modes.ActiveMode}");
        Debug.Log($"前一个状态: {modes.PreviousMode}");
        Debug.Log($"历史记录数量: {modes.HistoryCount}");

        Debug.Log("\n--- 预加载所有状态 ---");
        modes.PreloadMode(walkMode);
        modes.PreloadMode(attackMode);
        modes.PreloadMode(deadMode);

        Debug.Log("\n--- 卸载非活跃状态 ---");
        modes.UnloadInactiveModes();
        Debug.Log("\n--- 销毁状态管理器 ---");
        modes.Dispose();
    }
}