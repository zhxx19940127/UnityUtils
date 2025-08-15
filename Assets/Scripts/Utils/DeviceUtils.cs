namespace GameObjectToolkit
{
    using UnityEngine;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.iOS;
#endif

    /// <summary>
    /// Unity设备信息工具类
    /// 提供获取各种设备信息的方法
    /// </summary>
    public static class DeviceUtils
    {
        #region 基础设备信息

        /// <summary>
        /// 获取设备唯一标识符（注意：iOS14+和Android10+可能有隐私限制）
        /// </summary>
        public static string DeviceUniqueID
        {
            get
            {
                // 优先使用SystemInfo.deviceUniqueIdentifier
                // 注意：在部分平台上可能需要权限
                return SystemInfo.deviceUniqueIdentifier;
            }
        }

        /// <summary>
        /// 获取设备型号（如 iPhone12,3 或 SM-G975F）
        /// </summary>
        public static string DeviceModel
        {
            get { return SystemInfo.deviceModel; }
        }

        /// <summary>
        /// 获取设备名称（用户在系统中设置的设备名）
        /// </summary>
        public static string DeviceName
        {
            get { return SystemInfo.deviceName; }
        }

        /// <summary>
        /// 获取操作系统版本（如 Android 11 或 iOS 14.5）
        /// </summary>
        public static string OSVersion
        {
            get { return SystemInfo.operatingSystem; }
        }

        /// <summary>
        /// 获取设备类型（Desktop, Console, Handheld等）
        /// </summary>
        public static DeviceType DeviceType
        {
            get { return SystemInfo.deviceType; }
        }

        #endregion

        #region 硬件信息

        /// <summary>
        /// 获取处理器类型
        /// </summary>
        public static string ProcessorType
        {
            get { return SystemInfo.processorType; }
        }

        /// <summary>
        /// 获取处理器频率（MHz）
        /// </summary>
        public static int ProcessorFrequency
        {
            get { return SystemInfo.processorFrequency; }
        }

        /// <summary>
        /// 获取处理器核心数
        /// </summary>
        public static int ProcessorCount
        {
            get { return SystemInfo.processorCount; }
        }

        /// <summary>
        /// 获取系统内存大小（MB）
        /// </summary>
        public static int SystemMemorySize
        {
            get { return SystemInfo.systemMemorySize; }
        }

        /// <summary>
        /// 检查是否支持多点触控
        /// </summary>
        public static bool IsMultiTouchSupported
        {
            get { return Input.multiTouchEnabled; }
        }

        #endregion

        #region 图形设备信息

        /// <summary>
        /// 获取显卡名称
        /// </summary>
        public static string GraphicsDeviceName
        {
            get { return SystemInfo.graphicsDeviceName; }
        }

        /// <summary>
        /// 获取显卡厂商
        /// </summary>
        public static string GraphicsDeviceVendor
        {
            get { return SystemInfo.graphicsDeviceVendor; }
        }

        /// <summary>
        /// 获取显卡内存大小（MB）
        /// </summary>
        public static int GraphicsMemorySize
        {
            get { return SystemInfo.graphicsMemorySize; }
        }

        /// <summary>
        /// 获取当前屏幕宽度（像素）
        /// </summary>
        public static int ScreenWidth
        {
            get { return Screen.width; }
        }

        /// <summary>
        /// 获取当前屏幕高度（像素）
        /// </summary>
        public static int ScreenHeight
        {
            get { return Screen.height; }
        }

        /// <summary>
        /// 获取屏幕DPI
        /// </summary>
        public static float ScreenDPI
        {
            get { return Screen.dpi; }
        }

        /// <summary>
        /// 获取屏幕宽高比
        /// </summary>
        public static float ScreenAspectRatio
        {
            get { return (float)Screen.width / Screen.height; }
        }

        #endregion

        #region 网络信息

        /// <summary>
        /// 获取设备IP地址（可能返回多个）
        /// </summary>
        public static string[] IPAddresses
        {
            get
            {
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    var addresses = new System.Collections.Generic.List<string>();

                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            addresses.Add(ip.ToString());
                        }
                    }

                    return addresses.ToArray();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"DeviceUtils: 获取IP地址失败 - {e.Message}");
                    return new string[0];
                }
            }
        }

        /// <summary>
        /// 获取MAC地址（注意：Android 6.0+需要权限）
        /// </summary>
        public static string MACAddress
        {
            get
            {
                try
                {
                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in nics)
                    {
                        if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                            adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                        {
                            PhysicalAddress address = adapter.GetPhysicalAddress();
                            return address.ToString();
                        }
                    }

                    return string.Empty;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"DeviceUtils: 获取MAC地址失败 - {e.Message}");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// 检查网络连接状态
        /// </summary>
        public static NetworkReachability NetworkStatus
        {
            get { return Application.internetReachability; }
        }

        /// <summary>
        /// 检查是否连接到WiFi
        /// </summary>
        public static bool IsWifiConnected
        {
            get { return Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork; }
        }

        #endregion

        #region 平台特定功能

        /// <summary>
        /// 获取Android设备信息（仅Android平台有效）
        /// </summary>
        public static AndroidDeviceInfoClass AndroidDeviceInfo
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidDeviceInfo();
#else
                Debug.LogWarning("DeviceUtils: 当前平台不是Android，无法获取Android设备信息");
                return null;
#endif
            }
        }

        /// <summary>
        /// 获取iOS设备信息（仅iOS平台有效）
        /// </summary>
        public static iOSDeviceInfo IOSDeviceInfo
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
            return new iOSDeviceInfo();
#else
                Debug.LogWarning("DeviceUtils: 当前平台不是iOS，无法获取iOS设备信息");
                return null;
#endif
            }
        }

        #endregion

        #region 辅助类

        /// <summary>
        /// Android设备信息辅助类
        /// </summary>
        public class AndroidDeviceInfoClass
        {
            /// <summary>
            /// 获取Android API级别
            /// </summary>
            public int APILevel
            {
                get
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    return version.GetStatic<int>("SDK_INT");
                }
#else
                    return -1;
#endif
                }
            }

            /// <summary>
            /// 获取设备制造商（如 Samsung, Huawei等）
            /// </summary>
            public string Manufacturer
            {
                get
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                return new AndroidJavaClass("android.os.Build").GetStatic<string>("MANUFACTURER");
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 获取设备品牌（如 huawei, xiaomi等）
            /// </summary>
            public string Brand
            {
                get
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                return new AndroidJavaClass("android.os.Build").GetStatic<string>("BRAND");
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 获取设备产品名称（如 Pixel 3）
            /// </summary>
            public string Product
            {
                get
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                return new AndroidJavaClass("android.os.Build").GetStatic<string>("PRODUCT");
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 检查是否有摄像头
            /// </summary>
            public bool HasCamera
            {
                get
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
                    return WebCamTexture.devices.Length > 0;
#endif
                }
            }
        }

        /// <summary>
        /// iOS设备信息辅助类
        /// </summary>
        public class iOSDeviceInfo
        {
            /// <summary>
            /// 获取设备世代（如 iPhone 12）
            /// </summary>
            public string Generation
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                return Device.generation.ToString();
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 获取设备系统版本
            /// </summary>
            public string SystemVersion
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                return Device.systemVersion;
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 获取设备供应商标识符
            /// </summary>
            public string VendorIdentifier
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                return Device.vendorIdentifier;
#else
                    return string.Empty;
#endif
                }
            }

            /// <summary>
            /// 检查是否越狱
            /// </summary>
            public bool IsJailbroken
            {
                get
                {
#if UNITY_IOS && !UNITY_EDITOR
                return Device.isJailbroken;
#else
                    return false;
#endif
                }
            }
        }

        #endregion

        #region 实用方法

        /// <summary>
        /// 获取设备信息摘要（用于日志或调试）
        /// </summary>
        public static string GetDeviceSummary()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("===== 设备信息摘要 =====");
            sb.AppendLine($"设备型号: {DeviceModel}");
            sb.AppendLine($"设备名称: {DeviceName}");
            sb.AppendLine($"操作系统: {OSVersion}");
            sb.AppendLine($"设备类型: {DeviceType}");
            sb.AppendLine($"处理器: {ProcessorType} ({ProcessorCount}核@{ProcessorFrequency}MHz)");
            sb.AppendLine($"内存: {SystemMemorySize}MB");
            sb.AppendLine($"显卡: {GraphicsDeviceName} ({GraphicsMemorySize}MB)");
            sb.AppendLine($"屏幕: {ScreenWidth}x{ScreenHeight} @{ScreenDPI:0.##}DPI (比例:{ScreenAspectRatio:0.##})");

            var ips = IPAddresses;
            sb.AppendLine($"IP地址: {(ips.Length > 0 ? string.Join(", ", ips) : "未知")}");
            sb.AppendLine($"MAC地址: {MACAddress}");
            sb.AppendLine($"网络状态: {NetworkStatus}");

#if UNITY_ANDROID && !UNITY_EDITOR
        sb.AppendLine("--- Android特定信息 ---");
        sb.AppendLine($"制造商: {AndroidDeviceInfo.Manufacturer}");
        sb.AppendLine($"品牌: {AndroidDeviceInfo.Brand}");
        sb.AppendLine($"产品: {AndroidDeviceInfo.Product}");
        sb.AppendLine($"API级别: {AndroidDeviceInfo.APILevel}");
        sb.AppendLine($"摄像头: {(AndroidDeviceInfo.HasCamera ? "有" : "无")}");
#elif UNITY_IOS && !UNITY_EDITOR
        sb.AppendLine("--- iOS特定信息 ---");
        sb.AppendLine($"设备世代: {IOSDeviceInfo.Generation}");
        sb.AppendLine($"系统版本: {IOSDeviceInfo.SystemVersion}");
        sb.AppendLine($"是否越狱: {IOSDeviceInfo.IsJailbroken}");
#endif

            sb.AppendLine("======================");
            return sb.ToString();
        }

        /// <summary>
        /// 检查当前设备是否是低端设备（根据硬件配置判断）
        /// </summary>
        public static bool IsLowEndDevice()
        {
            // 判断标准：处理器核心数少于4或内存小于2GB
            return ProcessorCount < 4 || SystemMemorySize < 2000;
        }

        /// <summary>
        /// 检查当前设备是否是平板电脑（根据屏幕尺寸和DPI判断）
        /// </summary>
        public static bool IsTablet()
        {
            // 简单判断：屏幕尺寸大于6英寸且DPI小于400
            float screenInches = Mathf.Sqrt(
                Mathf.Pow(Screen.width / ScreenDPI, 2) +
                Mathf.Pow(Screen.height / ScreenDPI, 2)
            );

            return screenInches > 6.0f && ScreenDPI < 400;
        }

        #endregion
    }
}