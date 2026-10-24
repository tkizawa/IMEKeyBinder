using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Win32;

namespace IMEKeyBinder
{
    /// <summary>
    /// IME切り替え方式の定義
    /// </summary>
    public enum ImeSwitchMethod
    {
        Standard = 0,        // 標準 (VK_IME_ON / VK_IME_OFF + ImmSetOpenStatus)
        HenkanMuhenkan = 1   // 変換・無変換 (VK_CONVERT / VK_NONCONVERT + ImmSetOpenStatus)
    }

    /// <summary>
    /// アプリケーション設定クラス
    /// AppData\Local\IMEKeyBinder\settings.json に設定を保存・管理します。
    /// </summary>
    public class AppSettings
    {
        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "IMEKeyBinder";

        /// <summary>
        /// キー監視・IME切り替え機能が有効かどうか
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// IME切り替え方式
        /// </summary>
        public ImeSwitchMethod Method { get; set; } = ImeSwitchMethod.Standard;

        /// <summary>
        /// 切り替え時のトレイバルーン通知表示フラグ
        /// </summary>
        public bool ShowNotifications { get; set; } = false;

        /// <summary>
        /// 設定画面の終了時ウィンドウ位置 X座標 (-1の場合は画面中央)
        /// </summary>
        public int WindowLeft { get; set; } = -1;

        /// <summary>
        /// 設定画面の終了時ウィンドウ位置 Y座標 (-1の場合は画面中央)
        /// </summary>
        public int WindowTop { get; set; } = -1;

        /// <summary>
        /// 設定画面の終了時ウィンドウ幅
        /// </summary>
        public int WindowWidth { get; set; } = 640;

        /// <summary>
        /// 設定画面の終了時ウィンドウ高さ
        /// </summary>
        public int WindowHeight { get; set; } = 700;

        /// <summary>
        /// 設定ファイルの保存先ディレクトリ (AppData\Local\IMEKeyBinder)
        /// </summary>
        private static string SettingsDirectory =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IMEKeyBinder");

        /// <summary>
        /// 設定ファイルのフルパス
        /// </summary>
        private static string SettingsFilePath =>
            Path.Combine(SettingsDirectory, "settings.json");

        /// <summary>
        /// 設定を JSON ファイルから読み込みます。ファイルが存在しないか破損している場合はデフォルト設定を返します。
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // 読み込みに失敗した場合は初期値を採用
            }

            return new AppSettings();
        }

        /// <summary>
        /// 現在の設定を JSON ファイルとして保存します。
        /// 日本語文字が \uXXXX にエスケープされないよう UnsafeRelaxedJsonEscaping を使用します。
        /// </summary>
        public void Save()
        {
            try
            {
                Directory.CreateDirectory(SettingsDirectory);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // 設定保存失敗時は例外を握りつぶし、アプリの継続動作を優先
            }
        }

        /// <summary>
        /// Windows ログオン時の自動起動が有効になっているかを確認します。
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppRegistryName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Windows ログオン時の自動起動（スタートアップレジストリ）を登録または解除します。
        /// </summary>
        public static void SetStartupEnabled(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string exePath = Environment.ProcessPath ?? "";
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(AppRegistryName, $"\"{exePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppRegistryName, false);
                }
            }
            catch
            {
                // レジストリ操作失敗時は無視
            }
        }
    }
}
