using UnityEngine;

    /// <summary>
    /// Context 系统使用示例
    /// 演示拆分后的 Context 类如何使用(完全向后兼容)
    /// </summary>
    public class ContextUsageExample : MonoBehaviour
    {
        // 示例管理器类
        public class NetworkManager
        {
            public bool IsConnected { get; set; }
            public void Connect() => IsConnected = true;
        }

        public class AudioManager
        {
            public float Volume { get; set; } = 1.0f;
            public void PlaySound(string name) => Debug.Log($"Playing: {name}");
        }

        private void Start()
        {
            DemoBasicUsage();
            DemoTryPattern();
            DemoStaticContextManagement();
        }

        /// <summary>
        /// 基础使用示例
        /// </summary>
        private void DemoBasicUsage()
        {
            Debug.Log("========== 基础使用示例 ==========");

            // 获取全局应用上下文
            var ctx = Context.GetApplicationContext();

            // 1. 按类型存储和获取 (使用 Context.Set.cs 和 Context.Get.cs)
            ctx.Set(new NetworkManager());
            var netMgr = ctx.Get<NetworkManager>();
            Debug.Log($"NetworkManager 已获取: IsConnected = {netMgr.IsConnected}");

            // 2. 按名称存储和获取
            ctx.Set("PlayerName", "张三");
            string playerName = ctx.Get<string>("PlayerName");
            Debug.Log($"玩家名称: {playerName}");

            // 3. 检查是否存在 (使用 Context.Contains.cs)
            if (ctx.Contains<NetworkManager>())
            {
                Debug.Log("NetworkManager 存在于上下文中");
            }

            // 4. 移除对象 (使用 Context.Remove.cs)
            var removed = ctx.Remove<NetworkManager>();
            Debug.Log($"已移除 NetworkManager: IsConnected = {removed?.IsConnected}");
        }

        /// <summary>
        /// Try* 模式示例 (线程安全、无异常)
        /// </summary>
        private void DemoTryPattern()
        {
            Debug.Log("\n========== Try* 模式示例 ==========");

            var ctx = Context.GetApplicationContext();

            // 1. TryGet - 安全获取 (使用 Context.Get.cs)
            ctx.Set(new AudioManager());

            if (ctx.TryGet<AudioManager>(out var audioMgr))
            {
                Debug.Log($"成功获取 AudioManager: Volume = {audioMgr.Volume}");
                audioMgr.PlaySound("BGM_Menu");
            }
            else
            {
                Debug.Log("AudioManager 不存在");
            }

            // 2. TryRemove - 安全移除 (使用 Context.Remove.cs)
            if (ctx.TryRemove<AudioManager>(out var removedAudio))
            {
                Debug.Log($"成功移除 AudioManager: Volume = {removedAudio.Volume}");
            }

            // 3. 尝试获取不存在的对象 (不会抛出异常)
            if (!ctx.TryGet<NetworkManager>(out var netMgr))
            {
                Debug.Log("NetworkManager 不存在(安全返回)");
            }
        }

        /// <summary>
        /// 静态上下文管理示例
        /// </summary>
        private void DemoStaticContextManagement()
        {
            Debug.Log("\n========== 静态上下文管理示例 ==========");

            // 1. 创建命名上下文 (使用 Context.Static.cs)
            var level1Context = new Context();
            level1Context.Set("LevelName", "关卡1");
            level1Context.Set("EnemyCount", 10);

            // 2. 尝试添加上下文
            if (Context.TryAddContext("Level1", level1Context))
            {
                Debug.Log("Level1 上下文添加成功");
            }

            // 3. 获取命名上下文
            if (Context.TryGetContext("Level1", out var ctx))
            {
                string levelName = ctx.Get<string>("LevelName");
                int enemyCount = ctx.Get<int>("EnemyCount");
                Debug.Log($"关卡: {levelName}, 敌人数量: {enemyCount}");
            }

            // 4. 查询上下文信息
            int contextCount = Context.GetContextCount();
            string[] keys = Context.GetAllContextKeys();
            Debug.Log($"当前上下文数量: {contextCount}");
            Debug.Log($"上下文键: {string.Join(", ", keys)}");

            // 5. 添加或更新(覆盖已存在的)
            var newLevel1Context = new Context();
            newLevel1Context.Set("LevelName", "关卡1-更新");
            Context.AddOrUpdateContext("Level1", newLevel1Context);
            Debug.Log("Level1 上下文已更新");

            // 6. 清理
            Context.RemoveContext("Level1");
            Debug.Log("Level1 上下文已移除");
        }

        private void OnDestroy()
        {
            // 应用退出时清理所有上下文 (使用 Context.Static.cs)
            Context.ClearAllContexts();
            Debug.Log("所有上下文已清理");
        }
    }
