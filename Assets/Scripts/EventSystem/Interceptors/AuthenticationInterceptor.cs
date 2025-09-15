using System;
using System.Collections.Generic;
using UnityEngine;
using EventSystem.Core;
using EventSystem.Components;

namespace EventSystem.Interceptors
{
    /// <summary>
    /// 权限检查拦截器 - 实现基于用户权限的消息拦截
    /// 支持优先级控制，在拦截器链中优先执行权限检查
    /// </summary>
    public class AuthenticationInterceptor : IMessageInterceptor, InterceptorManager.IPriorityInterceptor
    {
        #region 配置和状态
        
        /// <summary>
        /// 拦截器优先级（数值越高优先级越高）
        /// </summary>
        public int Priority => 100; // 高优先级，优先执行权限检查
        
        /// <summary>
        /// 需要管理员权限的消息标签
        /// </summary>
        private readonly HashSet<string> _adminRequiredMessages;
        
        /// <summary>
        /// 需要登录的消息标签
        /// </summary>
        private readonly HashSet<string> _loginRequiredMessages;
        
        /// <summary>
        /// 需要特殊权限的消息标签和对应权限
        /// </summary>
        private readonly Dictionary<string, string> _specialPermissionMessages;
        
        /// <summary>
        /// 用户权限提供者
        /// </summary>
        private IUserPermissionProvider _permissionProvider;
        
        /// <summary>
        /// 是否启用详细权限日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = true;
        
        #endregion
        
        #region 构造函数
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="permissionProvider">用户权限提供者</param>
        public AuthenticationInterceptor(IUserPermissionProvider permissionProvider = null)
        {
            _adminRequiredMessages = new HashSet<string>();
            _loginRequiredMessages = new HashSet<string>();
            _specialPermissionMessages = new Dictionary<string, string>();
            
            _permissionProvider = permissionProvider ?? new DefaultUserPermissionProvider();
            
            // 设置默认权限规则
            SetupDefaultPermissionRules();
        }
        
        #endregion
        
        #region 权限配置方法
        
        /// <summary>
        /// 设置用户权限提供者
        /// </summary>
        /// <param name="provider">权限提供者</param>
        public void SetPermissionProvider(IUserPermissionProvider provider)
        {
            _permissionProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            Debug.Log("[AuthenticationInterceptor] 用户权限提供者已更新");
        }
        
        /// <summary>
        /// 添加需要管理员权限的消息
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void AddAdminRequiredMessage(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _adminRequiredMessages.Add(messageTag);
                
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[AuthenticationInterceptor] 添加管理员权限消息: {messageTag}");
                }
            }
        }
        
        /// <summary>
        /// 添加需要登录的消息
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void AddLoginRequiredMessage(string messageTag)
        {
            if (!string.IsNullOrEmpty(messageTag))
            {
                _loginRequiredMessages.Add(messageTag);
                
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[AuthenticationInterceptor] 添加登录权限消息: {messageTag}");
                }
            }
        }
        
        /// <summary>
        /// 添加需要特殊权限的消息
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        /// <param name="requiredPermission">所需权限</param>
        public void AddSpecialPermissionMessage(string messageTag, string requiredPermission)
        {
            if (!string.IsNullOrEmpty(messageTag) && !string.IsNullOrEmpty(requiredPermission))
            {
                _specialPermissionMessages[messageTag] = requiredPermission;
                
                if (EnableVerboseLogging)
                {
                    Debug.Log($"[AuthenticationInterceptor] 添加特殊权限消息: {messageTag} -> {requiredPermission}");
                }
            }
        }
        
        /// <summary>
        /// 移除权限要求
        /// </summary>
        /// <param name="messageTag">消息标签</param>
        public void RemovePermissionRequirement(string messageTag)
        {
            if (string.IsNullOrEmpty(messageTag))
                return;
                
            _adminRequiredMessages.Remove(messageTag);
            _loginRequiredMessages.Remove(messageTag);
            _specialPermissionMessages.Remove(messageTag);
            
            if (EnableVerboseLogging)
            {
                Debug.Log($"[AuthenticationInterceptor] 移除权限要求: {messageTag}");
            }
        }
        
        /// <summary>
        /// 清除所有权限配置
        /// </summary>
        public void ClearAllPermissionRules()
        {
            _adminRequiredMessages.Clear();
            _loginRequiredMessages.Clear();
            _specialPermissionMessages.Clear();
            
            Debug.Log("[AuthenticationInterceptor] 已清除所有权限规则");
        }
        
        #endregion
        
        #region IMessageInterceptor 实现
        
        /// <summary>
        /// 判断是否应该处理该消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>true表示继续处理，false表示拦截</returns>
        public bool ShouldProcess(string tag, object[] parameters)
        {
            if (string.IsNullOrEmpty(tag))
                return true;
                
            try
            {
                // 检查是否需要管理员权限
                if (_adminRequiredMessages.Contains(tag))
                {
                    if (!_permissionProvider.IsUserAdmin())
                    {
                        if (EnableVerboseLogging)
                        {
                            Debug.LogWarning($"[AuthenticationInterceptor] 权限不足，无法处理管理员消息: {tag}");
                        }
                        return false;
                    }
                }
                
                // 检查是否需要登录
                if (_loginRequiredMessages.Contains(tag))
                {
                    if (!_permissionProvider.IsUserLoggedIn())
                    {
                        if (EnableVerboseLogging)
                        {
                            Debug.LogWarning($"[AuthenticationInterceptor] 用户未登录，无法处理消息: {tag}");
                        }
                        return false;
                    }
                }
                
                // 检查是否需要特殊权限
                if (_specialPermissionMessages.TryGetValue(tag, out var requiredPermission))
                {
                    if (!_permissionProvider.HasPermission(requiredPermission))
                    {
                        if (EnableVerboseLogging)
                        {
                            Debug.LogWarning($"[AuthenticationInterceptor] 缺少权限 '{requiredPermission}'，无法处理消息: {tag}");
                        }
                        return false;
                    }
                }
                
                // 检查消息标签模式匹配
                if (IsRestrictedByPattern(tag))
                {
                    if (EnableVerboseLogging)
                    {
                        Debug.LogWarning($"[AuthenticationInterceptor] 消息匹配受限模式，权限检查失败: {tag}");
                    }
                    return false;
                }
                
                // 权限检查通过
                if (EnableVerboseLogging && IsMonitoredMessage(tag))
                {
                    var userInfo = _permissionProvider.GetCurrentUserInfo();
                    Debug.Log($"[AuthenticationInterceptor] 权限检查通过: {tag} (用户: {userInfo})");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AuthenticationInterceptor] 权限检查过程中发生异常: {ex.Message}");
                return false; // 权限检查异常时拒绝访问
            }
        }
        
        #endregion
        
        #region 权限检查逻辑
        
        /// <summary>
        /// 检查消息是否匹配受限模式
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否受限</returns>
        private bool IsRestrictedByPattern(string tag)
        {
            // 管理员相关操作
            if (tag.StartsWith("Admin") && !_permissionProvider.IsUserAdmin())
            {
                return true;
            }
            
            // 系统级操作
            if (tag.StartsWith("System") && !_permissionProvider.HasPermission("SystemAccess"))
            {
                return true;
            }
            
            // 数据库操作
            if (tag.StartsWith("DB") && !_permissionProvider.HasPermission("DatabaseAccess"))
            {
                return true;
            }
            
            // 网络配置操作
            if (tag.StartsWith("Network") && !_permissionProvider.HasPermission("NetworkConfig"))
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查是否为需要监控的消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <returns>是否需要监控</returns>
        private bool IsMonitoredMessage(string tag)
        {
            return _adminRequiredMessages.Contains(tag) ||
                   _loginRequiredMessages.Contains(tag) ||
                   _specialPermissionMessages.ContainsKey(tag) ||
                   tag.StartsWith("Admin") ||
                   tag.StartsWith("System");
        }
        
        #endregion
        
        #region 默认配置
        
        /// <summary>
        /// 设置默认权限规则
        /// </summary>
        private void SetupDefaultPermissionRules()
        {
            // 管理员权限消息
            AddAdminRequiredMessage("AdminPanel");
            AddAdminRequiredMessage("AdminCommand");
            AddAdminRequiredMessage("UserManagement");
            AddAdminRequiredMessage("SystemConfig");
            AddAdminRequiredMessage("DebugCommand");
            
            // 登录权限消息
            AddLoginRequiredMessage("SaveProfile");
            AddLoginRequiredMessage("LoadProfile");
            AddLoginRequiredMessage("SendChatMessage");
            AddLoginRequiredMessage("JoinRoom");
            AddLoginRequiredMessage("PurchaseItem");
            AddLoginRequiredMessage("UnlockAchievement");
            
            // 特殊权限消息
            AddSpecialPermissionMessage("ModerateChat", "ChatModeration");
            AddSpecialPermissionMessage("BanUser", "UserModeration");
            AddSpecialPermissionMessage("AccessAnalytics", "AnalyticsAccess");
            AddSpecialPermissionMessage("ManagePayments", "PaymentAccess");
            AddSpecialPermissionMessage("BackupData", "DataBackup");
        }
        
        #endregion
        
        #region 调试和统计
        
        /// <summary>
        /// 打印权限配置报告
        /// </summary>
        public void PrintPermissionReport()
        {
            Debug.Log("=== AuthenticationInterceptor 权限配置报告 ===");
            Debug.Log($"管理员权限消息数量: {_adminRequiredMessages.Count}");
            Debug.Log($"登录权限消息数量: {_loginRequiredMessages.Count}");
            Debug.Log($"特殊权限消息数量: {_specialPermissionMessages.Count}");
            
            var currentUser = _permissionProvider.GetCurrentUserInfo();
            var isAdmin = _permissionProvider.IsUserAdmin();
            var isLoggedIn = _permissionProvider.IsUserLoggedIn();
            
            Debug.Log($"当前用户: {currentUser}");
            Debug.Log($"是否为管理员: {isAdmin}");
            Debug.Log($"是否已登录: {isLoggedIn}");
        }
        
        #endregion
    }
    
    #region 权限提供者接口和默认实现
    
    /// <summary>
    /// 用户权限提供者接口
    /// </summary>
    public interface IUserPermissionProvider
    {
        /// <summary>
        /// 检查用户是否为管理员
        /// </summary>
        /// <returns>是否为管理员</returns>
        bool IsUserAdmin();
        
        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        /// <returns>是否已登录</returns>
        bool IsUserLoggedIn();
        
        /// <summary>
        /// 检查用户是否具有特定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否具有权限</returns>
        bool HasPermission(string permission);
        
        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <returns>用户信息字符串</returns>
        string GetCurrentUserInfo();
    }
    
    /// <summary>
    /// 默认用户权限提供者实现
    /// </summary>
    public class DefaultUserPermissionProvider : IUserPermissionProvider
    {
        /// <summary>
        /// 当前用户是否为管理员
        /// </summary>
        public bool IsAdmin { get; set; } = false;
        
        /// <summary>
        /// 当前用户是否已登录
        /// </summary>
        public bool IsLoggedIn { get; set; } = false;
        
        /// <summary>
        /// 当前用户名
        /// </summary>
        public string Username { get; set; } = "Guest";
        
        /// <summary>
        /// 用户权限集合
        /// </summary>
        public HashSet<string> UserPermissions { get; set; } = new HashSet<string>();
        
        /// <summary>
        /// 检查用户是否为管理员
        /// </summary>
        public bool IsUserAdmin() => IsAdmin;
        
        /// <summary>
        /// 检查用户是否已登录
        /// </summary>
        public bool IsUserLoggedIn() => IsLoggedIn;
        
        /// <summary>
        /// 检查用户是否具有特定权限
        /// </summary>
        public bool HasPermission(string permission)
        {
            if (string.IsNullOrEmpty(permission))
                return false;
                
            return IsAdmin || UserPermissions.Contains(permission);
        }
        
        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        public string GetCurrentUserInfo()
        {
            var status = IsLoggedIn ? (IsAdmin ? "管理员" : "普通用户") : "未登录";
            return $"{Username} ({status})";
        }
    }
    
    #endregion
}