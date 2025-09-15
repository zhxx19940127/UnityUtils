using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystem.Core;
using EventSystem.Interceptors;

namespace EventSystem.Utils
{
    /// <summary>
    /// 拦截器设置辅助类 - 提供便捷的拦截器配置和管理功能
    /// 封装了常用的拦截器组合配置，简化拦截器的使用和管理
    /// </summary>
    public class InterceptorSetup : MonoBehaviour
    {
        #region 预设配置

        [Header("拦截器配置")] [SerializeField] private bool _useAuthenticationInterceptor = true;
        [SerializeField] private bool _useParameterValidationInterceptor = true;
        [SerializeField] private bool _useRateLimitInterceptor = true;
        [SerializeField] private bool _useConditionalInterceptor = false;
        [SerializeField] private bool _useLoggingInterceptor = true;

        [Header("日志配置")] [SerializeField]
        private LoggingInterceptor.LogLevel _logLevel = LoggingInterceptor.LogLevel.Info;

        [SerializeField] private bool _enableFileLogging = false;
        [SerializeField] private bool _enableVerboseLogging = false;

        [Header("频率限制配置")] [SerializeField] private int _defaultRateLimitMs = 100;
        [SerializeField] private bool _enableRateLimitLogging = false;

        [Header("用户权限配置")] [SerializeField] private bool _defaultUserIsAdmin = false;
        [SerializeField] private bool _defaultUserIsLoggedIn = true;
        [SerializeField] private string _defaultUsername = "DefaultUser";

        #endregion

        #region 拦截器实例

        private AuthenticationInterceptor _authInterceptor;
        private ParameterValidationInterceptor _paramValidationInterceptor;
        private RateLimitInterceptor _rateLimitInterceptor;
        private ConditionalInterceptor _conditionalInterceptor;
        private LoggingInterceptor _loggingInterceptor;

        #endregion

        #region Unity 生命周期

        /// <summary>
        /// 启动时自动配置拦截器
        /// </summary>
        void Start()
        {
            SetupInterceptors();
        }

        /// <summary>
        /// 销毁时清理拦截器
        /// </summary>
        void OnDestroy()
        {
            CleanupInterceptors();
        }

        #endregion

        #region 主要配置方法

        /// <summary>
        /// 设置拦截器（根据配置）
        /// </summary>
        public void SetupInterceptors()
        {
            var eventBus = Message.DefaultEvent;
            if (eventBus == null)
            {
                Debug.LogError("[InterceptorSetup] Message.Instance 为 null，无法设置拦截器");
                return;
            }

            Debug.Log("[InterceptorSetup] 开始设置拦截器...");

            // 按优先级顺序添加拦截器

            // 1. 权限检查拦截器（最高优先级：100）
            if (_useAuthenticationInterceptor)
            {
                SetupAuthenticationInterceptor(eventBus);
            }

            // 2. 参数验证拦截器（默认优先级：0）
            if (_useParameterValidationInterceptor)
            {
                SetupParameterValidationInterceptor(eventBus);
            }

            // 3. 频率限制拦截器（默认优先级：0）
            if (_useRateLimitInterceptor)
            {
                SetupRateLimitInterceptor(eventBus);
            }

            // 4. 条件拦截器（默认优先级：0）
            if (_useConditionalInterceptor)
            {
                SetupConditionalInterceptor(eventBus);
            }

            // 5. 日志拦截器（最低优先级：1）
            if (_useLoggingInterceptor)
            {
                SetupLoggingInterceptor(eventBus);
            }

            Debug.Log("[InterceptorSetup] 拦截器设置完成");

            // 打印设置报告
            if (_enableVerboseLogging)
            {
                PrintSetupReport();
            }
        }

        /// <summary>
        /// 清理所有拦截器
        /// </summary>
        public void CleanupInterceptors()
        {
            var eventBus = Message.DefaultEvent;
            if (eventBus == null) return;

            Debug.Log("[InterceptorSetup] 清理拦截器...");

            // 移除所有拦截器
            if (_authInterceptor != null)
                eventBus.RemoveInterceptor(_authInterceptor);

            if (_paramValidationInterceptor != null)
                eventBus.RemoveInterceptor(_paramValidationInterceptor);

            if (_rateLimitInterceptor != null)
                eventBus.RemoveInterceptor(_rateLimitInterceptor);

            if (_conditionalInterceptor != null)
                eventBus.RemoveInterceptor(_conditionalInterceptor);

            if (_loggingInterceptor != null)
            {
                _loggingInterceptor.FlushNow(); // 确保日志被写入
                eventBus.RemoveInterceptor(_loggingInterceptor);
            }

            Debug.Log("[InterceptorSetup] 拦截器清理完成");
        }

        #endregion

        #region 具体拦截器设置

        /// <summary>
        /// 设置权限检查拦截器
        /// </summary>
        private void SetupAuthenticationInterceptor(IEventBus eventBus)
        {
            var permissionProvider = new DefaultUserPermissionProvider
            {
                IsAdmin = _defaultUserIsAdmin,
                IsLoggedIn = _defaultUserIsLoggedIn,
                Username = _defaultUsername
            };

            // 添加一些默认权限
            if (_defaultUserIsLoggedIn)
            {
                permissionProvider.UserPermissions.Add("BasicAccess");
                permissionProvider.UserPermissions.Add("ChatAccess");
            }

            if (_defaultUserIsAdmin)
            {
                permissionProvider.UserPermissions.Add("SystemAccess");
                permissionProvider.UserPermissions.Add("DatabaseAccess");
                permissionProvider.UserPermissions.Add("NetworkConfig");
                permissionProvider.UserPermissions.Add("AnalyticsAccess");
            }

            _authInterceptor = new AuthenticationInterceptor(permissionProvider)
            {
                EnableVerboseLogging = _enableVerboseLogging
            };

            eventBus.AddInterceptor(_authInterceptor);
            Debug.Log($"[InterceptorSetup] 权限检查拦截器已添加 (用户: {_defaultUsername}, 管理员: {_defaultUserIsAdmin})");
        }

        /// <summary>
        /// 设置参数验证拦截器
        /// </summary>
        private void SetupParameterValidationInterceptor(IEventBus eventBus)
        {
            _paramValidationInterceptor = new ParameterValidationInterceptor();

            eventBus.AddInterceptor(_paramValidationInterceptor);
            Debug.Log("[InterceptorSetup] 参数验证拦截器已添加");
        }

        /// <summary>
        /// 设置频率限制拦截器
        /// </summary>
        private void SetupRateLimitInterceptor(IEventBus eventBus)
        {
            _rateLimitInterceptor = new RateLimitInterceptor(_defaultRateLimitMs)
            {
                EnableVerboseLogging = _enableRateLimitLogging
            };

            // 添加一些自定义频率规则
            _rateLimitInterceptor.SetCustomInterval("HighFrequencyUpdate", 16); // 60fps
            _rateLimitInterceptor.SetCustomInterval("SaveProgress", 5000); // 5秒
            _rateLimitInterceptor.SetCustomInterval("NetworkSync", 1000); // 1秒

            eventBus.AddInterceptor(_rateLimitInterceptor);
            Debug.Log($"[InterceptorSetup] 频率限制拦截器已添加 (默认间隔: {_defaultRateLimitMs}ms)");
        }

        /// <summary>
        /// 设置条件拦截器
        /// </summary>
        private void SetupConditionalInterceptor(IEventBus eventBus)
        {
            _conditionalInterceptor = new ConditionalInterceptor()
            {
                EnableVerboseLogging = _enableVerboseLogging
            };

            // 添加一些常用的条件配置
            _conditionalInterceptor.AddToBlocklist("TestMessage");
            _conditionalInterceptor.AddToBlocklist("DebugMessage");

            // 添加自定义条件示例
            _conditionalInterceptor.AddCustomCondition("PerformanceIntensive", (parameters) =>
            {
                // 当帧率低于30fps时拦截性能密集型操作
                return 1f / Time.deltaTime < 30f;
            });

            eventBus.AddInterceptor(_conditionalInterceptor);
            Debug.Log("[InterceptorSetup] 条件拦截器已添加");
        }

        /// <summary>
        /// 设置日志拦截器
        /// </summary>
        private void SetupLoggingInterceptor(IEventBus eventBus)
        {
            _loggingInterceptor = new LoggingInterceptor(_logLevel, _enableFileLogging)
            {
                EnableConsoleLogging = true,
                BufferFlushInterval = 5f
            };

            // 添加一些日志过滤器
            if (_logLevel >= LoggingInterceptor.LogLevel.Debug)
            {
                _loggingInterceptor.AddMessageFilter("Game");
                _loggingInterceptor.AddMessageFilter("UI");
                _loggingInterceptor.AddMessageFilter("Network");
            }

            // 忽略高频消息以减少日志噪音
            _loggingInterceptor.AddIgnoredMessage("MouseMove");
            _loggingInterceptor.AddIgnoredMessage("MouseHover");

            eventBus.AddInterceptor(_loggingInterceptor);
            Debug.Log($"[InterceptorSetup] 日志拦截器已添加 (级别: {_logLevel}, 文件日志: {_enableFileLogging})");
        }

        #endregion

        #region 预设配置方法

        /// <summary>
        /// 应用开发模式配置
        /// </summary>
        [ContextMenu("应用开发模式配置")]
        public void ApplyDevelopmentConfiguration()
        {
            _useAuthenticationInterceptor = false; // 开发时不需要权限检查
            _useParameterValidationInterceptor = true;
            _useRateLimitInterceptor = false; // 开发时不限制频率
            _useConditionalInterceptor = false;
            _useLoggingInterceptor = true;

            _logLevel = LoggingInterceptor.LogLevel.Verbose;
            _enableFileLogging = true;
            _enableVerboseLogging = true;

            Debug.Log("[InterceptorSetup] 已应用开发模式配置");

            if (Application.isPlaying)
            {
                CleanupInterceptors();
                SetupInterceptors();
            }
        }

        /// <summary>
        /// 应用生产模式配置
        /// </summary>
        [ContextMenu("应用生产模式配置")]
        public void ApplyProductionConfiguration()
        {
            _useAuthenticationInterceptor = true;
            _useParameterValidationInterceptor = true;
            _useRateLimitInterceptor = true;
            _useConditionalInterceptor = true;
            _useLoggingInterceptor = true;

            _logLevel = LoggingInterceptor.LogLevel.Warning; // 生产环境只记录警告和错误
            _enableFileLogging = false; // 生产环境不写文件日志
            _enableVerboseLogging = false;

            _defaultRateLimitMs = 50; // 生产环境更严格的频率限制

            Debug.Log("[InterceptorSetup] 已应用生产模式配置");

            if (Application.isPlaying)
            {
                CleanupInterceptors();
                SetupInterceptors();
            }
        }

        /// <summary>
        /// 应用调试模式配置
        /// </summary>
        [ContextMenu("应用调试模式配置")]
        public void ApplyDebugConfiguration()
        {
            _useAuthenticationInterceptor = true;
            _useParameterValidationInterceptor = true;
            _useRateLimitInterceptor = true;
            _useConditionalInterceptor = false; // 调试时不使用条件拦截
            _useLoggingInterceptor = true;

            _logLevel = LoggingInterceptor.LogLevel.Debug;
            _enableFileLogging = true;
            _enableVerboseLogging = true;
            _enableRateLimitLogging = true;

            Debug.Log("[InterceptorSetup] 已应用调试模式配置");

            if (Application.isPlaying)
            {
                CleanupInterceptors();
                SetupInterceptors();
            }
        }

        #endregion

        #region 运行时管理方法

        /// <summary>
        /// 启用/禁用条件拦截器
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetConditionalInterceptorEnabled(bool enabled)
        {
            if (_conditionalInterceptor != null)
            {
                _conditionalInterceptor.IsEnabled = enabled;
                Debug.Log($"[InterceptorSetup] 条件拦截器已{(enabled ? "启用" : "禁用")}");
            }
        }

        /// <summary>
        /// 更新频率限制
        /// </summary>
        /// <param name="intervalMs">新的间隔时间（毫秒）</param>
        public void UpdateRateLimit(int intervalMs)
        {
            _defaultRateLimitMs = intervalMs;
            if (_rateLimitInterceptor != null)
            {
                // 需要重新设置拦截器以应用新的配置
                var eventBus = Message.DefaultEvent;
                if (eventBus != null)
                {
                    eventBus.RemoveInterceptor(_rateLimitInterceptor);
                    SetupRateLimitInterceptor(eventBus);
                }
            }
        }

        #endregion

        #region 调试和信息

        /// <summary>
        /// 打印设置报告
        /// </summary>
        [ContextMenu("打印设置报告")]
        public void PrintSetupReport()
        {
            Debug.Log("=== InterceptorSetup 配置报告 ===");
            Debug.Log($"权限检查拦截器: {(_useAuthenticationInterceptor ? "启用" : "禁用")}");
            Debug.Log($"参数验证拦截器: {(_useParameterValidationInterceptor ? "启用" : "禁用")}");
            Debug.Log($"频率限制拦截器: {(_useRateLimitInterceptor ? "启用" : "禁用")} (间隔: {_defaultRateLimitMs}ms)");
            Debug.Log($"条件拦截器: {(_useConditionalInterceptor ? "启用" : "禁用")}");
            Debug.Log($"日志拦截器: {(_useLoggingInterceptor ? "启用" : "禁用")} (级别: {_logLevel})");
            Debug.Log($"详细日志: {(_enableVerboseLogging ? "启用" : "禁用")}");
            Debug.Log($"文件日志: {(_enableFileLogging ? "启用" : "禁用")}");

            // 打印当前拦截器状态
            var eventBus = Message.DefaultEvent;
            if (eventBus != null)
            {
                var interceptors = eventBus.GetInterceptorManager().GetInterceptors();
                Debug.Log($"当前已注册拦截器数量: {interceptors.Count}");
            }
        }

        /// <summary>
        /// 打印所有拦截器的详细统计
        /// </summary>
        [ContextMenu("打印详细统计")]
        public void PrintDetailedStatistics()
        {
            Debug.Log("=== 拦截器详细统计 ===");

            _authInterceptor?.PrintPermissionReport();
            _rateLimitInterceptor?.PrintStatisticsReport();
            _conditionalInterceptor?.PrintConfigurationReport();
            _loggingInterceptor?.PrintStatisticsReport();
        }

        /// <summary>
        /// 测试拦截器功能
        /// </summary>
        [ContextMenu("测试拦截器功能")]
        public void TestInterceptors()
        {
            var eventBus = Message.DefaultEvent;
            if (eventBus == null)
            {
                Debug.LogError("[InterceptorSetup] Message.Instance 为 null，无法测试");
                return;
            }

            Debug.Log("[InterceptorSetup] 开始测试拦截器...");

            // 测试各种消息
            eventBus.Post("TestMessage", "test data"); // 普通消息
            eventBus.Post("AdminCommand", "admin test"); // 管理员消息
            eventBus.Post("UserLogin"); // 缺少参数的消息
            eventBus.Post("UserLogin", "user", "pass"); // 正确的登录消息
            eventBus.Post("HighFrequencyUpdate", "data"); // 高频消息
            eventBus.Post("HighFrequencyUpdate", "data"); // 立即再次发送（测试频率限制）

            Debug.Log("[InterceptorSetup] 拦截器测试完成");
        }

        #endregion
    }
}