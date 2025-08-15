using System.Text;

namespace GameObjectToolkit
{
    using UnityEngine;
    using System;
    using System.Runtime.InteropServices;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if UNITY_IOS && !UNITY_EDITOR
using UnityEngine.iOS;
#endif

    /// <summary>
    /// Unity剪贴板工具类
    /// 提供跨平台的剪贴板读写功能
    /// </summary>
    public static class ClipboardUtils
    {
        #region 平台原生方法声明

        // Windows平台原生方法
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll")]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll")]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        private const uint CF_TEXT = 1;
#endif

        // macOS平台原生方法
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    [DllImport("Objective-C")]
    private static extern IntPtr GetMacOSClipboard();

    [DllImport("Objective-C")]
    private static extern void SetMacOSClipboard(IntPtr str);
#endif

        #endregion

        #region 公共方法

        /// <summary>
        /// 将文本复制到剪贴板（跨平台）
        /// </summary>
        /// <param name="text">要复制的文本</param>
        /// <returns>是否成功</returns>
        public static bool CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Debug.LogWarning("ClipboardUtils: 尝试复制空文本到剪贴板");
                return false;
            }

            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidCopyToClipboard(text);
#elif UNITY_IOS && !UNITY_EDITOR
            return iOSCopyToClipboard(text);
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return WindowsCopyToClipboard(text);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return MacOSCopyToClipboard(text);
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        return LinuxCopyToClipboard(text);
#elif UNITY_WEBGL && !UNITY_EDITOR
            return WebGLCopyToClipboard(text);
#else
            Debug.LogWarning("ClipboardUtils: 当前平台不支持剪贴板操作");
            return false;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"ClipboardUtils: 复制到剪贴板失败 - {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从剪贴板获取文本（跨平台）
        /// </summary>
        /// <returns>剪贴板中的文本，失败返回空字符串</returns>
        public static string PasteFromClipboard()
        {
            try
            {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidPasteFromClipboard();
#elif UNITY_IOS && !UNITY_EDITOR
            return iOSPasteFromClipboard();
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return WindowsPasteFromClipboard();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return MacOSPasteFromClipboard();
#elif UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
        return LinuxPasteFromClipboard();
#elif UNITY_WEBGL && !UNITY_EDITOR
            return WebGLPasteFromClipboard();
#else
            Debug.LogWarning("ClipboardUtils: 当前平台不支持剪贴板操作");
            return string.Empty;
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"ClipboardUtils: 从剪贴板获取失败 - {e.Message}");
                return string.Empty;
            }
        }

        #endregion

        #region 各平台实现

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>
    /// Android平台复制到剪贴板实现
    /// </summary>
    private static bool AndroidCopyToClipboard(string text)
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject clipboard = activity.Call<AndroidJavaObject>("getSystemService", "clipboard"))
            using (AndroidJavaClass clipData = new AndroidJavaClass("android.content.ClipData"))
            using (AndroidJavaObject clip = clipData.CallStatic<AndroidJavaObject>("newPlainText", "label", text))
            {
                clipboard.Call("setPrimaryClip", clip);
                return true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: Android复制失败 - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Android平台从剪贴板获取实现
    /// </summary>
    private static string AndroidPasteFromClipboard()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject clipboard = activity.Call<AndroidJavaObject>("getSystemService", "clipboard"))
            using (AndroidJavaObject clip = clipboard.Call<AndroidJavaObject>("getPrimaryClip"))
            {
                if (clip == null) return string.Empty;

                int itemCount = clip.Call<int>("getItemCount");
                if (itemCount <= 0) return string.Empty;

                using (AndroidJavaObject item = clip.Call<AndroidJavaObject>("getItemAt", 0))
                {
                    return item.Call<string>("getText");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: Android获取失败 - {e.Message}");
            return string.Empty;
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    /// <summary>
    /// iOS平台复制到剪贴板实现
    /// </summary>
    private static bool iOSCopyToClipboard(string text)
    {
        try
        {
            // iOS使用Unity的GUIUtility.systemCopyBuffer
            GUIUtility.systemCopyBuffer = text;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: iOS复制失败 - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// iOS平台从剪贴板获取实现
    /// </summary>
    private static string iOSPasteFromClipboard()
    {
        try
        {
            // iOS使用Unity的GUIUtility.systemCopyBuffer
            return GUIUtility.systemCopyBuffer;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: iOS获取失败 - {e.Message}");
            return string.Empty;
        }
    }
#endif


        #region Linux 平台实现

#if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    /// <summary>
    /// Linux 平台复制到剪贴板实现（使用 xclip）
    /// </summary>
    private static bool LinuxCopyToClipboard(string text)
    {
        try
        {
            // 尝试使用 xclip 命令
            string tempFile = Path.Combine(Application.temporaryCachePath, "clipboard_temp");
            File.WriteAllText(tempFile, text);
            
            // 优先尝试 xclip，如果失败则尝试 xsel
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "bash";
            process.StartInfo.Arguments =
 $"-c \"cat {tempFile} | xclip -selection clipboard 2>/dev/null || cat {tempFile} | xsel --clipboard 2>/dev/null\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.Start();
            process.WaitForExit();
            
            File.Delete(tempFile);
            
            if (process.ExitCode != 0)
            {
                Debug.LogError("ClipboardUtils: Linux 需要安装 xclip 或 xsel 工具");
                Debug.LogError($"错误信息: {process.StandardError.ReadToEnd()}");
                return false;
            }
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: Linux 复制失败 - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Linux 平台从剪贴板获取实现
    /// </summary>
    private static string LinuxPasteFromClipboard()
    {
        try
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "bash";
            process.StartInfo.Arguments =
 "-c \"xclip -selection clipboard -o 2>/dev/null || xsel --clipboard --output 2>/dev/null\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            
            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                Debug.LogError("ClipboardUtils: Linux 需要安装 xclip 或 xsel 工具");
                Debug.LogError($"错误信息: {process.StandardError.ReadToEnd()}");
                return string.Empty;
            }
            
            return result.Trim();
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: Linux 获取失败 - {e.Message}");
            return string.Empty;
        }
    }

#endif

        #endregion

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        /// <summary>
        /// Windows平台复制到剪贴板实现
        /// </summary>
        private static bool WindowsCopyToClipboard(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogError("ClipboardUtils: 无法打开剪贴板");
                return false;
            }

            try
            {
                EmptyClipboard();
                IntPtr hGlobal = IntPtr.Zero;

                try
                {
                    // 分配全局内存
                    hGlobal = GlobalAlloc(0x0002 /*GMEM_MOVEABLE*/, (UIntPtr)((text.Length + 1) * 2));
                    if (hGlobal == IntPtr.Zero)
                    {
                        Debug.LogError("ClipboardUtils: 内存分配失败");
                        return false;
                    }

                    // 锁定内存并复制数据
                    IntPtr pGlobal = GlobalLock(hGlobal);
                    if (pGlobal == IntPtr.Zero)
                    {
                        Debug.LogError("ClipboardUtils: 内存锁定失败");
                        return false;
                    }

                    try
                    {
                        byte[] bytes = Encoding.Default.GetBytes(text);
                        Marshal.Copy(bytes, 0, pGlobal, text.Length);
                    }
                    finally
                    {
                        GlobalUnlock(hGlobal);
                    }

                    // 设置剪贴板数据
                    if (SetClipboardData(CF_TEXT, hGlobal) == IntPtr.Zero)
                    {
                        Debug.LogError("ClipboardUtils: 设置剪贴板数据失败");
                        return false;
                    }

                    hGlobal = IntPtr.Zero; // 剪贴板现在拥有内存的所有权
                    return true;
                }
                finally
                {
                    if (hGlobal != IntPtr.Zero)
                    {
                        GlobalFree(hGlobal);
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        /// <summary>
        /// Windows平台从剪贴板获取实现
        /// </summary>
        private static string WindowsPasteFromClipboard()
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                Debug.LogError("ClipboardUtils: 无法打开剪贴板");
                return string.Empty;
            }

            try
            {
                IntPtr hClipboardData = GetClipboardData(CF_TEXT);
                if (hClipboardData == IntPtr.Zero)
                {
                    return string.Empty;
                }

                IntPtr pClipboardData = GlobalLock(hClipboardData);
                if (pClipboardData == IntPtr.Zero)
                {
                    return string.Empty;
                }

                try
                {
                    return Marshal.PtrToStringAnsi(pClipboardData);
                }
                finally
                {
                    GlobalUnlock(hClipboardData);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    /// <summary>
    /// macOS平台复制到剪贴板实现
    /// </summary>
    private static bool MacOSCopyToClipboard(string text)
    {
        try
        {
            IntPtr nsString = Marshal.StringToHGlobalAuto(text);
            SetMacOSClipboard(nsString);
            Marshal.FreeHGlobal(nsString);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: macOS复制失败 - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// macOS平台从剪贴板获取实现
    /// </summary>
    private static string MacOSPasteFromClipboard()
    {
        try
        {
            IntPtr clipboardData = GetMacOSClipboard();
            string result = Marshal.PtrToStringAuto(clipboardData);
            Marshal.FreeHGlobal(clipboardData);
            return result ?? string.Empty;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: macOS获取失败 - {e.Message}");
            return string.Empty;
        }
    }
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    /// <summary>
    /// WebGL平台复制到剪贴板实现
    /// </summary>
    [DllImport("__Internal")]
    private static extern void WebGLCopyToClipboard(string text);

    /// <summary>
    /// WebGL平台从剪贴板获取实现
    /// </summary>
    [DllImport("__Internal")]
    private static extern string WebGLPasteFromClipboard();

    private static bool WebGLCopyToClipboard(string text)
    {
        try
        {
            WebGLCopyToClipboard(text);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: WebGL复制失败 - {e.Message}");
            return false;
        }
    }

    private static string WebGLPasteFromClipboard()
    {
        try
        {
            return WebGLPasteFromClipboard() ?? string.Empty;
        }
        catch (Exception e)
        {
            Debug.LogError($"ClipboardUtils: WebGL获取失败 - {e.Message}");
            return string.Empty;
        }
    }
#endif

        #endregion

        #region 扩展功能

        /// <summary>
        /// 检查剪贴板中是否有文本内容
        /// </summary>
        public static bool HasText()
        {
            string text = PasteFromClipboard();
            return !string.IsNullOrEmpty(text);
        }

        /// <summary>
        /// 清空剪贴板内容
        /// </summary>
        public static bool ClearClipboard()
        {
            return CopyToClipboard("");
        }

        #endregion
    }
}