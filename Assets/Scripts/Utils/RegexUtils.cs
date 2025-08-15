namespace GameObjectToolkit
{
    using System.Text.RegularExpressions;
    using UnityEngine;

    /// <summary>
    /// Unity正则表达式工具类
    /// 提供常用的正则验证方法
    /// </summary>
    public static class RegexUtils
    {
        #region 基础验证方法

        /// <summary>
        /// 验证输入字符串是否匹配指定正则表达式
        /// </summary>
        /// <param name="input">要验证的字符串</param>
        /// <param name="pattern">正则表达式模式</param>
        /// <param name="options">正则选项（可选）</param>
        /// <returns>是否匹配</returns>
        public static bool IsMatch(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(input))
            {
                Debug.LogWarning("RegexUtils: 输入字符串为空");
                return false;
            }

            try
            {
                return Regex.IsMatch(input, pattern, options);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RegexUtils: 正则表达式验证出错 - {e.Message}");
                return false;
            }
        }

        #endregion

        #region 常用验证方法

        /// <summary>
        /// 验证是否为有效的电子邮件地址
        /// </summary>
        public static bool IsEmail(string input)
        {
            // 标准电子邮件正则表达式
            const string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为有效的手机号码（国际通用格式）
        /// </summary>
        public static bool IsPhoneNumber(string input)
        {
            // 国际手机号格式（以+开头，后面跟着数字）
            const string pattern = @"^\+?[0-9]{7,15}$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为有效的URL
        /// </summary>
        public static bool IsUrl(string input)
        {
            // 基本URL验证（http/https/ftp等协议）
            const string pattern = @"^(https?|ftp)://[^\s/$.?#].[^\s]*$";
            return IsMatch(input, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 验证是否为有效的IPv4地址
        /// </summary>
        public static bool IsIPv4(string input)
        {
            const string pattern =
                @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证字符串是否只包含字母和数字
        /// </summary>
        public static bool IsAlphanumeric(string input)
        {
            const string pattern = @"^[a-zA-Z0-9]+$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为有效的用户名（4-20位字母数字下划线）
        /// </summary>
        public static bool IsUsername(string input)
        {
            const string pattern = @"^[a-zA-Z0-9_]{4,20}$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为强密码（至少8位，包含大小写字母和数字）
        /// </summary>
        public static bool IsStrongPassword(string input)
        {
            const string pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d]{8,}$";
            return IsMatch(input, pattern);
        }

        #endregion

        #region Unity特定验证

        /// <summary>
        /// 验证是否为有效的Unity资源路径（Assets/...）
        /// </summary>
        public static bool IsUnityAssetPath(string input)
        {
            const string pattern = @"^Assets/([a-zA-Z0-9_\-]+/)*[a-zA-Z0-9_\-]+\.[a-zA-Z0-9]+$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为有效的Unity标签（Tag）
        /// </summary>
        public static bool IsUnityTag(string input)
        {
            // Unity标签只能包含字母、数字、下划线，不能以数字开头
            const string pattern = @"^[a-zA-Z_][a-zA-Z0-9_]*$";
            return IsMatch(input, pattern);
        }

        /// <summary>
        /// 验证是否为有效的十六进制颜色代码（#RRGGBB或#RRGGBBAA）
        /// </summary>
        public static bool IsHexColor(string input)
        {
            const string pattern = @"^#?([0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$";
            return IsMatch(input, pattern);
        }

        #endregion

        #region 提取和替换方法

        /// <summary>
        /// 从字符串中提取匹配正则的第一个结果
        /// </summary>
        public static string ExtractFirst(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            try
            {
                Match match = Regex.Match(input, pattern, options);
                return match.Success ? match.Value : string.Empty;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RegexUtils: 正则提取出错 - {e.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 从字符串中提取所有匹配正则的结果
        /// </summary>
        public static string[] ExtractAll(string input, string pattern, RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(input)) return new string[0];

            try
            {
                MatchCollection matches = Regex.Matches(input, pattern, options);
                string[] results = new string[matches.Count];
                for (int i = 0; i < matches.Count; i++)
                {
                    results[i] = matches[i].Value;
                }

                return results;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RegexUtils: 正则提取出错 - {e.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// 替换字符串中匹配正则的部分
        /// </summary>
        public static string Replace(string input, string pattern, string replacement,
            RegexOptions options = RegexOptions.None)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            try
            {
                return Regex.Replace(input, pattern, replacement, options);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RegexUtils: 正则替换出错 - {e.Message}");
                return input;
            }
        }

        #endregion
    }
}