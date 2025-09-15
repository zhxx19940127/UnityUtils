using System;
using UnityEngine;
using EventSystem.Core;

namespace EventSystem.Interceptors
{
    /// <summary>
    /// 参数验证拦截器 - 验证特定消息的参数完整性和有效性
    /// 可以为不同的消息类型配置不同的验证规则
    /// </summary>
    public class ParameterValidationInterceptor : IMessageInterceptor
    {
        /// <summary>
        /// 判断是否应该处理该消息
        /// </summary>
        /// <param name="tag">消息标签</param>
        /// <param name="parameters">消息参数</param>
        /// <returns>true表示继续处理，false表示拦截</returns>
        public bool ShouldProcess(string tag, object[] parameters)
        {
            try
            {
                // 验证特定消息的参数
                switch (tag)
                {
                    case "UserLogin":
                        return ValidateUserLoginParameters(parameters);
                        
                    case "SaveData":
                        return ValidateSaveDataParameters(parameters);
                        
                    case "LoadLevel":
                        return ValidateLoadLevelParameters(parameters);
                        
                    case "SendChatMessage":
                        return ValidateChatMessageParameters(parameters);
                        
                    case "PurchaseItem":
                        return ValidatePurchaseItemParameters(parameters);
                        
                    default:
                        // 对于未定义验证规则的消息，进行基本验证
                        return ValidateBasicParameters(tag, parameters);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ParameterValidationInterceptor] 验证过程中发生异常: {ex.Message}");
                return false; // 验证异常时拦截消息
            }
        }
        
        /// <summary>
        /// 验证用户登录参数
        /// </summary>
        private bool ValidateUserLoginParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length < 2)
            {
                Debug.LogError("[ParameterValidationInterceptor] UserLogin 消息参数不足，需要用户名和密码");
                return false;
            }
            
            var username = parameters[0] as string;
            var password = parameters[1] as string;
            
            if (string.IsNullOrEmpty(username))
            {
                Debug.LogError("[ParameterValidationInterceptor] UserLogin 用户名不能为空");
                return false;
            }
            
            if (string.IsNullOrEmpty(password))
            {
                Debug.LogError("[ParameterValidationInterceptor] UserLogin 密码不能为空");
                return false;
            }
            
            if (username.Length < 3)
            {
                Debug.LogError("[ParameterValidationInterceptor] UserLogin 用户名长度不能少于3个字符");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证保存数据参数
        /// </summary>
        private bool ValidateSaveDataParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                Debug.LogError("[ParameterValidationInterceptor] SaveData 消息缺少数据参数");
                return false;
            }
            
            if (parameters[0] == null)
            {
                Debug.LogError("[ParameterValidationInterceptor] SaveData 数据参数不能为null");
                return false;
            }
            
            // 可以添加更多数据格式验证
            return true;
        }
        
        /// <summary>
        /// 验证加载关卡参数
        /// </summary>
        private bool ValidateLoadLevelParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                Debug.LogError("[ParameterValidationInterceptor] LoadLevel 消息缺少关卡参数");
                return false;
            }
            
            // 检查关卡ID
            if (parameters[0] is string levelName)
            {
                if (string.IsNullOrEmpty(levelName))
                {
                    Debug.LogError("[ParameterValidationInterceptor] LoadLevel 关卡名称不能为空");
                    return false;
                }
            }
            else if (parameters[0] is int levelId)
            {
                if (levelId < 0)
                {
                    Debug.LogError("[ParameterValidationInterceptor] LoadLevel 关卡ID不能为负数");
                    return false;
                }
            }
            else
            {
                Debug.LogError("[ParameterValidationInterceptor] LoadLevel 关卡参数类型无效，应为string或int");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证聊天消息参数
        /// </summary>
        private bool ValidateChatMessageParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length == 0)
            {
                Debug.LogError("[ParameterValidationInterceptor] SendChatMessage 消息缺少内容参数");
                return false;
            }
            
            var message = parameters[0] as string;
            if (string.IsNullOrEmpty(message))
            {
                Debug.LogError("[ParameterValidationInterceptor] SendChatMessage 消息内容不能为空");
                return false;
            }
            
            if (message.Length > 500)
            {
                Debug.LogError("[ParameterValidationInterceptor] SendChatMessage 消息内容过长（超过500字符）");
                return false;
            }
            
            // 检查是否包含敏感词（示例）
            if (ContainsSensitiveWords(message))
            {
                Debug.LogWarning("[ParameterValidationInterceptor] SendChatMessage 消息包含敏感词汇");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 验证购买物品参数
        /// </summary>
        private bool ValidatePurchaseItemParameters(object[] parameters)
        {
            if (parameters == null || parameters.Length < 2)
            {
                Debug.LogError("[ParameterValidationInterceptor] PurchaseItem 消息参数不足，需要物品ID和数量");
                return false;
            }
            
            // 验证物品ID
            if (!(parameters[0] is string itemId) || string.IsNullOrEmpty(itemId))
            {
                Debug.LogError("[ParameterValidationInterceptor] PurchaseItem 物品ID无效");
                return false;
            }
            
            // 验证数量
            if (!(parameters[1] is int quantity) || quantity <= 0)
            {
                Debug.LogError("[ParameterValidationInterceptor] PurchaseItem 购买数量必须为正整数");
                return false;
            }
            
            if (quantity > 999)
            {
                Debug.LogError("[ParameterValidationInterceptor] PurchaseItem 单次购买数量不能超过999");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 基本参数验证
        /// </summary>
        private bool ValidateBasicParameters(string tag, object[] parameters)
        {
            // 对于未定义特殊验证规则的消息，进行基本检查
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogError("[ParameterValidationInterceptor] 消息标签不能为空");
                return false;
            }
            
            // 检查参数数组是否过大（防止内存问题）
            if (parameters != null && parameters.Length > 100)
            {
                Debug.LogWarning($"[ParameterValidationInterceptor] 消息 '{tag}' 参数数量过多（{parameters.Length}个），可能存在性能问题");
            }
            
            return true; // 基本验证通过
        }
        
        /// <summary>
        /// 检查是否包含敏感词汇（示例实现）
        /// </summary>
        private bool ContainsSensitiveWords(string message)
        {
            // 简单的敏感词检查示例
            string[] sensitiveWords = { "hack", "cheat", "exploit", "spam" };
            
            var lowerMessage = message.ToLower();
            foreach (var word in sensitiveWords)
            {
                if (lowerMessage.Contains(word))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}