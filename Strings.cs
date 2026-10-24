using System;
using System.Globalization;

namespace IMEKeyBinder
{
    /// <summary>
    /// 多言語（日本語・英語）対応リソース文字列クラス
    /// Windowsの表示言語設定（CultureInfo.CurrentUICulture）に応じて自動切り替えを行います。
    /// </summary>
    public static class Strings
    {
        private static bool IsJapanese =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("ja", StringComparison.OrdinalIgnoreCase);

        // アプリケーション全般
        public static string AppTitle => "IMEKeyBinder";
        public static string AppSubtitle => IsJapanese
            ? "英語キーボード用 右Alt / 左Alt 日本語入力切り替え常駐ツール"
            : "US Keyboard Right Alt / Left Alt Japanese IME Switcher";

        public static string FormTitle => IsJapanese
            ? "IMEKeyBinder - 英語キーボード IME 切り替え"
            : "IMEKeyBinder - US Keyboard IME Switcher";

        // タスクトレイ
        public static string TrayToolTip => IsJapanese
            ? "IMEKeyBinder (右Alt: IME ON / 左Alt: IME OFF)"
            : "IMEKeyBinder (Right Alt: IME ON / Left Alt: IME OFF)";

        public static string MenuEnabled => IsJapanese
            ? "有効 (AltでIME切り替え)"
            : "Enabled (Switch IME with Alt)";

        public static string MenuMethod => IsJapanese
            ? "IME切り替え方式"
            : "IME Switching Method";

        public static string MenuMethodStandard => IsJapanese
            ? "標準 (VK_IME_ON/OFF)"
            : "Standard (VK_IME_ON/OFF)";

        public static string MenuMethodHenkan => IsJapanese
            ? "変換・無変換 (VK_CONVERT/NONCONVERT)"
            : "Convert / Non-Convert";

        public static string MenuStartup => IsJapanese
            ? "Windowsログオン時に自動起動"
            : "Start automatically with Windows";

        public static string MenuNotifications => IsJapanese
            ? "切り替え通知を表示"
            : "Show notifications on switch";

        public static string MenuOpenSettings => IsJapanese
            ? "設定 / 動作ログを開く..."
            : "Settings / Live Log...";

        public static string MenuExit => IsJapanese
            ? "終了"
            : "Exit";

        // ステータスカード
        public static string StatusActiveTitle => IsJapanese
            ? "✓ 常駐フック稼働中"
            : "✓ Resident Hook Active";

        public static string StatusActiveDetail => IsJapanese
            ? "「右 Alt」単体押し: IME ON (日本語) ｜ 「左 Alt」単体押し: IME OFF (英語)\n※ Alt + 他のキー（Alt+Tab, Alt+F4 など）は通常通り機能します"
            : "Single Right Alt: IME ON (Japanese) | Single Left Alt: IME OFF (English)\n* Alt + other keys (Alt+Tab, Alt+F4, etc.) function normally";

        public static string StatusInactiveTitle => IsJapanese
            ? "⚠ 停止中 (無効化されています)"
            : "⚠ Inactive (Disabled)";

        // 設定グループ
        public static string SettingsGroupTitle => IsJapanese
            ? "設定"
            : "Settings";

        public static string ChkEnableTitle => IsJapanese
            ? "キーバインドによる IME 切り替えを有効にする"
            : "Enable Alt-key IME switching";

        public static string LblMethodTitle => IsJapanese
            ? "IME切り替え方式:"
            : "Switch Method:";

        public static string MethodStandardOption => IsJapanese
            ? "標準 (VK_IME_ON / VK_IME_OFF + ImmSetOpenStatus) [推奨]"
            : "Standard (VK_IME_ON / VK_IME_OFF + ImmSetOpenStatus) [Recommended]";

        public static string MethodHenkanOption => IsJapanese
            ? "変換・無変換 (VK_CONVERT / VK_NONCONVERT)"
            : "Convert / Non-Convert (VK_CONVERT / VK_NONCONVERT)";

        public static string ChkStartupTitle => IsJapanese
            ? "Windows ログオン時に自動起動する (スタートアップ登録)"
            : "Start automatically when signing in to Windows";

        public static string ChkNotificationsTitle => IsJapanese
            ? "切り替え時にトレイバルーン通知を表示する"
            : "Show balloon notification when IME state changes";

        // テスト入力エリア
        public static string TestAreaLabel => IsJapanese
            ? "テスト入力エリア (ここで右Alt / 左Altを押して日本語・英語の切り替えを試せます):"
            : "Test Input Area (Press Right Alt / Left Alt here to test switching):";

        public static string TestAreaPlaceholder => IsJapanese
            ? "ここをクリックして、右Alt / 左Alt を押して文字を入力してみてください..."
            : "Click here, press Right Alt / Left Alt, and type text to test...";

        // リアルタイムログ
        public static string LogHeader => IsJapanese
            ? "動作リアルタイムログ:"
            : "Real-time Event Log:";

        public static string BtnClearLog => IsJapanese
            ? "ログクリア"
            : "Clear Log";

        public static string BtnHide => IsJapanese
            ? "トレイに最小化"
            : "Minimize to Tray";

        public static string CloseHint => IsJapanese
            ? "※ 画面を閉じてもタスクトレイで常駐動作を続けます。"
            : "* The app continues running in the system tray when closed.";

        // ログ・通知メッセージ
        public static string LogInitialized => IsJapanese
            ? "IMEKeyBinder が初期化されました。"
            : "IMEKeyBinder initialized.";

        public static string KeyRightAlt => IsJapanese ? "右 Alt" : "Right Alt";
        public static string KeyLeftAlt => IsJapanese ? "左 Alt" : "Left Alt";

        public static string ImeOnState => IsJapanese ? "IME ON (日本語入力)" : "IME ON (Japanese)";
        public static string ImeOffState => IsJapanese ? "IME OFF (直接入力)" : "IME OFF (Direct / English)";

        public static string LogSwitched(string key, string state) => IsJapanese
            ? $"{key} 単体押し検知 -> {state} に切り替え"
            : $"Single tap detected: {key} -> Switched to {state}";

        public static string LogCombo(string key) => IsJapanese
            ? $"コンボ検出: {key} -> 通常ショートカットとしてパススルー (IME切り替えスキップ)"
            : $"Combo detected: {key} -> Passed through normally (IME toggle skipped)";

        public static string LogSettingChanged(string name, object val) => IsJapanese
            ? $"設定変更: {name} = {val}"
            : $"Setting updated: {name} = {val}";

        public static string BalloonTitle => "IMEKeyBinder";
        public static string BalloonText(string key, string state) => $"{key} → {state}";

        // エラー・ダイアログ
        public static string HookError => IsJapanese
            ? "キーボードフックの登録に失敗しました。管理者権限が必要な場合があります。"
            : "Failed to install keyboard hook. Administrator privileges may be required.";

        public static string ErrorDialogTitle => IsJapanese
            ? "IMEKeyBinder エラー"
            : "IMEKeyBinder Error";

        public static string AlreadyRunning => IsJapanese
            ? "IMEKeyBinder は既に起動しています。\nタスクトレイ（画面右下の通知領域）のアイコンを確認してください。"
            : "IMEKeyBinder is already running.\nPlease check the icon in your system tray (notification area).";
    }
}
