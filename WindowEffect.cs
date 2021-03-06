using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WeldingProductionMaterialSystem.Framworks.Effect
{
    /// <summary>
    ///     窗口效果类
    /// </summary>
    internal static class WindowEffect
    {
        #region Win32 (common)

        [DllImport("Dwmapi.dll", SetLastError = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            uint dwAttribute,
            ref int attrValue, // IntPtr
            uint cbAttribute);

        private enum DWMWA : uint
        {
            DWMWA_NCRENDERING_ENABLED = 1,     // [get] Is non-client rendering enabled/disabled
            DWMWA_NCRENDERING_POLICY,          // [set] Non-client rendering policy
            DWMWA_TRANSITIONS_FORCEDISABLED,   // [set] Potentially enable/forcibly disable transitions
            DWMWA_ALLOW_NCPAINT,               // [set] Allow contents rendered in the non-client area to be visible on the DWM-drawn frame.
            DWMWA_CAPTION_BUTTON_BOUNDS,       // [get] Bounds of the caption button area in window-relative space.
            DWMWA_NONCLIENT_RTL_LAYOUT,        // [set] Is non-client content RTL mirrored
            DWMWA_FORCE_ICONIC_REPRESENTATION, // [set] Force this window to display iconic thumbnails.
            DWMWA_FLIP3D_POLICY,               // [set] Designates how Flip3D will treat the window.
            DWMWA_EXTENDED_FRAME_BOUNDS,       // [get] Gets the extended frame bounds rectangle in screen space
            DWMWA_HAS_ICONIC_BITMAP,           // [set] Indicates an available bitmap when there is no better thumbnail representation.
            DWMWA_DISALLOW_PEEK,               // [set] Don't invoke Peek on the window.
            DWMWA_EXCLUDED_FROM_PEEK,          // [set] LivePreview exclusion information
            DWMWA_CLOAK,                       // [set] Cloak or uncloak the window
            DWMWA_CLOAKED,                     // [get] Gets the cloaked state of the window
            DWMWA_FREEZE_REPRESENTATION,       // [set] Force this window to freeze the thumbnail without live update
            DWMWA_LAST
        }

        #endregion Win32 (common)

        #region Win32 (for Win7)

        [DllImport("Dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool pfEnabled);

        [DllImport("Dwmapi.dll")]
        private static extern int DwmEnableBlurBehindWindow(
            IntPtr hWnd,
            [In] ref DWM_BLURBEHIND pBlurBehind);

        [StructLayout(LayoutKind.Sequential)]
        private struct DWM_BLURBEHIND
        {
            public DWM_BB dwFlags;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fEnable;

            public IntPtr hRgnBlur;

            [MarshalAs(UnmanagedType.Bool)]
            public bool fTransitionOnMaximized;
        }

        [Flags]
        private enum DWM_BB : uint
        {
            DWM_BB_ENABLE = 0x00000001,
            DWM_BB_BLURREGION = 0x00000002,
            DWM_BB_TRANSITIONONMAXIMIZED = 0x00000004
        }

        private const int S_OK = 0x0;

        #endregion Win32 (for Win7)

        #region Win32 (for Win10)

        /// <summary>
        ///     Sets window composition attribute (Undocumented API).
        /// </summary>
        /// <param name="hwnd">Window handle</param>
        /// <param name="data">Attribute data</param>
        /// <returns>True if succeeded</returns>
        /// <remarks>
        /// This API and relevant parameters are derived from:
        /// https://github.com/riverar/sample-win10-aeroglass
        /// </remarks>
        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowCompositionAttribute(
            IntPtr hwnd,
            ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        private enum WindowCompositionAttribute
        {
            // ...
            WCA_ACCENT_POLICY = 19

            // ...
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_INVALID_STATE = 4
        }

        #endregion Win32 (for Win10)

        #region Method

        /// <summary>
        ///     禁用转换
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static bool DisableTransitions(Window window)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;
            int attrValue = 1;

            return (DwmSetWindowAttribute(
                windowHandle,
                (uint) DWMWA.DWMWA_TRANSITIONS_FORCEDISABLED,
                ref attrValue,
                (uint) sizeof(int)) == S_OK);
        }

        /// <summary>
        ///     开启背景模糊
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static bool EnableBackgroundBlur(Window window)
        {
            if (!OsVersion.IsVistaOrNewer)
            {
                // MessageBox.Show("IsVistaOrNewer");
                return false;
            }

            if (!OsVersion.Is8OrNewer)
            {
                // MessageBox.Show("EnableBackgroundBlurForWin7");
                return EnableBackgroundBlurForWin7(window);
            }

            //if (!OsVersion.Is10Threshold1OrNewer)
            //{
            //    MessageBox.Show("Is10Threshold1OrNewer");
            //    return false; // For Windows 8 and 8.1, no blur effect is available.
            //}

            // MessageBox.Show("EnableBackgroundBlurForWin10");
            return EnableBackgroundBlurForWin10(window);
        }

        /// <summary>
        ///     开启背景模糊（Win7），Areo效果
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        private static bool EnableBackgroundBlurForWin7(Window window)
        {
            bool isEnabled;
            if ((DwmIsCompositionEnabled(out isEnabled) != S_OK) || !isEnabled)
                return false;

            var windowHandle = new WindowInteropHelper(window).Handle;

            var bb = new DWM_BLURBEHIND
            {
                dwFlags = DWM_BB.DWM_BB_ENABLE,
                fEnable = true,
                hRgnBlur = IntPtr.Zero
            };

            return (DwmEnableBlurBehindWindow(
                windowHandle,
                ref bb) == S_OK);
        }

        /// <summary>
        ///     开启背景模糊效果（Win10）
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        private static bool EnableBackgroundBlurForWin10(Window window)
        {
            // 获取用于Native Methon的窗口句柄
            var windowHandle = new WindowInteropHelper(window).Handle;

            var accent = new AccentPolicy { AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND };
            var accentSize = Marshal.SizeOf(accent);

            var accentPointer = IntPtr.Zero;
            try
            {
                accentPointer = Marshal.AllocHGlobal(accentSize);
                Marshal.StructureToPtr(accent, accentPointer, false);

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = accentPointer,
                    SizeOfData = accentSize,
                };

                return SetWindowCompositionAttribute(
                    windowHandle,
                    ref data);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to set window composition attribute." + Environment.NewLine
                    + ex);
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(accentPointer);
            }
        }

        #endregion Method
    }

    #region OS version information

    /// <summary>
    ///     OS version information
    /// </summary>
    public static class OsVersion
    {
        /// <summary>
        ///     Whether OS is Windows Vista or newer
        /// </summary>
        /// <remarks>Windows Vista = version 6.0</remarks>
        public static bool IsVistaOrNewer
        {
            private set { IsVistaOrNewer = value; }
            get { return IsEqualToOrNewer(6); }
        }

        /// <summary>
        ///     Whether OS is Windows 8 or newer
        /// </summary>
        /// <remarks>Windows 8 = version 6.2</remarks>
        public static bool Is8OrNewer
        {
            private set { IsVistaOrNewer = value; }
            get { return IsEqualToOrNewer(6, 2); }
        }

        /// <summary>
        ///     Whether OS is Windows 8.1 or newer
        /// </summary>
        /// <remarks>Windows 8.1 = version 6.3</remarks>
        public static bool Is81OrNewer
        {
            private set { IsVistaOrNewer = value; }
            get { return IsEqualToOrNewer(6, 3); }
        }

        /// <summary>
        ///     Whether OS is Windows 10 (Threshold 1) or newer
        /// </summary>
        /// <remarks>Windows 10 (Threshold 1) = version 10.0.10240</remarks>
        public static bool Is10Threshold1OrNewer
        {
            private set { IsVistaOrNewer = value; }
            get { return IsEqualToOrNewer(10, 0, 10240); }
        }

        #region Method

        private static readonly object _lock = new object();

        public static bool IsEqualToOrNewer(int major, int minor = 0, int build = 0)
        {
            lock (_lock)
            {
                return new Version(major, minor, build) <= Environment.OSVersion.Version;
            }
        }

        #endregion Method
    }

    #endregion OS version information
}
