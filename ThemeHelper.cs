using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace IMEKeyBinder
{
    /// <summary>
    /// Windowsのシステムテーマ（ライトモード／ダークモード）判定およびUIテーマ適用ヘルパー
    /// </summary>
    public static class ThemeHelper
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightThemeValue = "AppsUseLightTheme";

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// 現在のWindows設定がダークモードであるかを取得します。
        /// </summary>
        public static bool IsDarkMode()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, false);
                if (key != null)
                {
                    object? val = key.GetValue(AppsUseLightThemeValue);
                    if (val is int intVal)
                    {
                        return intVal == 0;
                    }
                }
            }
            catch
            {
                // レジストリ読み込み失敗時はライトモードをデフォルトとする
            }

            return false;
        }

        /// <summary>
        /// ウィンドウのタイトルバーにダークモード設定を適用します。
        /// </summary>
        public static void ApplyTitleBarTheme(IntPtr hWnd, bool isDarkMode)
        {
            if (hWnd == IntPtr.Zero) return;

            try
            {
                int useDarkMode = isDarkMode ? 1 : 0;
                int hr = DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int));
                if (hr != 0)
                {
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
                }
            }
            catch
            {
                // 古いOS環境などでは無視
            }
        }
    }
}
