using System;
using System.Drawing;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    public class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly KeyboardHookManager _hookManager;
        private readonly AppSettings _settings;

        private SettingsForm? _settingsForm;
        private readonly ToolStripMenuItem _menuEnabled;
        private readonly ToolStripMenuItem _menuStartup;
        private readonly ToolStripMenuItem _menuNotifications;
        private readonly ToolStripMenuItem _menuMethodStandard;
        private readonly ToolStripMenuItem _menuMethodHenkan;

        public TrayApplicationContext()
        {
            _settings = AppSettings.Load();
            _hookManager = new KeyboardHookManager(_settings);

            // Hook events
            _hookManager.ImeActionTriggered += HookManager_ImeActionTriggered;

            // Context Menu Setup
            var contextMenu = new ContextMenuStrip();

            _menuEnabled = new ToolStripMenuItem("有効 (AltでIME切り替え)", null, OnToggleEnabled)
            {
                Checked = _settings.IsEnabled
            };

            var methodSubMenu = new ToolStripMenuItem("IME切り替え方式");
            _menuMethodStandard = new ToolStripMenuItem("標準 (VK_IME_ON/OFF)", null, (s, e) => SetMethod(ImeSwitchMethod.Standard))
            {
                Checked = _settings.Method == ImeSwitchMethod.Standard
            };
            _menuMethodHenkan = new ToolStripMenuItem("変換・無変換 (VK_CONVERT/NONCONVERT)", null, (s, e) => SetMethod(ImeSwitchMethod.HenkanMuhenkan))
            {
                Checked = _settings.Method == ImeSwitchMethod.HenkanMuhenkan
            };
            methodSubMenu.DropDownItems.Add(_menuMethodStandard);
            methodSubMenu.DropDownItems.Add(_menuMethodHenkan);

            _menuStartup = new ToolStripMenuItem("Windowsログオン時に自動起動", null, OnToggleStartup)
            {
                Checked = AppSettings.IsStartupEnabled()
            };

            _menuNotifications = new ToolStripMenuItem("切り替え通知を表示", null, OnToggleNotifications)
            {
                Checked = _settings.ShowNotifications
            };

            var menuOpenSettings = new ToolStripMenuItem("設定 / 動作ログを開く...", null, (s, e) => ShowSettingsForm());
            menuOpenSettings.Font = new Font(menuOpenSettings.Font, FontStyle.Bold);

            var menuExit = new ToolStripMenuItem("終了", null, OnExit);

            contextMenu.Items.Add(_menuEnabled);
            contextMenu.Items.Add(methodSubMenu);
            contextMenu.Items.Add(_menuStartup);
            contextMenu.Items.Add(_menuNotifications);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuOpenSettings);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuExit);

            // Setup NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateAppIcon(_settings.IsEnabled),
                ContextMenuStrip = contextMenu,
                Text = "IMEKeyBinder (右Alt: IME ON / 左Alt: IME OFF)",
                Visible = true
            };

            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
            _notifyIcon.DoubleClick += (s, e) => ShowSettingsForm();

            // Start hook
            bool hookStarted = _hookManager.Start();
            if (!hookStarted)
            {
                MessageBox.Show(
                    "キーボードフックの登録に失敗しました。管理者権限が必要な場合があります。",
                    "IMEKeyBinder エラー",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowSettingsForm();
            }
        }

        private void ShowSettingsForm()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_hookManager, _settings, OnSettingsUpdated);
            }

            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }

        private void OnSettingsUpdated()
        {
            _menuEnabled.Checked = _settings.IsEnabled;
            _menuMethodStandard.Checked = _settings.Method == ImeSwitchMethod.Standard;
            _menuMethodHenkan.Checked = _settings.Method == ImeSwitchMethod.HenkanMuhenkan;
            _menuNotifications.Checked = _settings.ShowNotifications;
            _notifyIcon.Icon = IconHelper.CreateAppIcon(_settings.IsEnabled);
        }

        private void OnToggleEnabled(object? sender, EventArgs e)
        {
            _settings.IsEnabled = !_settings.IsEnabled;
            _settings.Save();
            OnSettingsUpdated();
        }

        private void SetMethod(ImeSwitchMethod method)
        {
            _settings.Method = method;
            _settings.Save();
            OnSettingsUpdated();
        }

        private void OnToggleStartup(object? sender, EventArgs e)
        {
            bool newState = !AppSettings.IsStartupEnabled();
            AppSettings.SetStartupEnabled(newState);
            _menuStartup.Checked = newState;
        }

        private void OnToggleNotifications(object? sender, EventArgs e)
        {
            _settings.ShowNotifications = !_settings.ShowNotifications;
            _settings.Save();
            OnSettingsUpdated();
        }

        private void HookManager_ImeActionTriggered(object? sender, ImeActionEventArgs e)
        {
            if (_settings.ShowNotifications)
            {
                string stateText = e.IsImeOn ? "日本語入力 (IME ON)" : "直接入力 (IME OFF)";
                _notifyIcon.ShowBalloonTip(
                    1200,
                    "IMEKeyBinder",
                    $"{e.TriggerKey} → {stateText}",
                    ToolTipIcon.Info);
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            _hookManager.Dispose();
            _settingsForm?.Dispose();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notifyIcon.Dispose();
                _hookManager.Dispose();
                _settingsForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
