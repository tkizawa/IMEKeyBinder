using System;
using System.Drawing;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    /// <summary>
    /// タスクトレイ常駐アプリケーションコンテキスト
    /// NotifyIcon の管理、コンテキストメニューの生成、キー監視フックの開始・停止を統括します。
    /// </summary>
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

            // IME切り替えイベント購読
            _hookManager.ImeActionTriggered += HookManager_ImeActionTriggered;

            // コンテキストメニューの構築
            var contextMenu = new ContextMenuStrip();

            _menuEnabled = new ToolStripMenuItem(Strings.MenuEnabled, null, OnToggleEnabled)
            {
                Checked = _settings.IsEnabled
            };

            var methodSubMenu = new ToolStripMenuItem(Strings.MenuMethod);
            _menuMethodStandard = new ToolStripMenuItem(Strings.MenuMethodStandard, null, (s, e) => SetMethod(ImeSwitchMethod.Standard))
            {
                Checked = _settings.Method == ImeSwitchMethod.Standard
            };
            _menuMethodHenkan = new ToolStripMenuItem(Strings.MenuMethodHenkan, null, (s, e) => SetMethod(ImeSwitchMethod.HenkanMuhenkan))
            {
                Checked = _settings.Method == ImeSwitchMethod.HenkanMuhenkan
            };
            methodSubMenu.DropDownItems.Add(_menuMethodStandard);
            methodSubMenu.DropDownItems.Add(_menuMethodHenkan);

            _menuStartup = new ToolStripMenuItem(Strings.MenuStartup, null, OnToggleStartup)
            {
                Checked = AppSettings.IsStartupEnabled()
            };

            _menuNotifications = new ToolStripMenuItem(Strings.MenuNotifications, null, OnToggleNotifications)
            {
                Checked = _settings.ShowNotifications
            };

            var menuOpenSettings = new ToolStripMenuItem(Strings.MenuOpenSettings, null, (s, e) => ShowSettingsForm());
            menuOpenSettings.Font = new Font(menuOpenSettings.Font, FontStyle.Bold);

            var menuExit = new ToolStripMenuItem(Strings.MenuExit, null, OnExit);

            contextMenu.Items.Add(_menuEnabled);
            contextMenu.Items.Add(methodSubMenu);
            contextMenu.Items.Add(_menuStartup);
            contextMenu.Items.Add(_menuNotifications);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuOpenSettings);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(menuExit);

            // NotifyIcon の設定
            _notifyIcon = new NotifyIcon
            {
                Icon = IconHelper.CreateAppIcon(_settings.IsEnabled),
                ContextMenuStrip = contextMenu,
                Text = Strings.TrayToolTip,
                Visible = true
            };

            _notifyIcon.MouseClick += NotifyIcon_MouseClick;
            _notifyIcon.DoubleClick += (s, e) => ShowSettingsForm();

            // キーボードフックの開始
            bool hookStarted = _hookManager.Start();
            if (!hookStarted)
            {
                MessageBox.Show(
                    Strings.HookError,
                    Strings.ErrorDialogTitle,
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

        /// <summary>
        /// 設定・ログ画面を表示します。
        /// </summary>
        private void ShowSettingsForm()
        {
            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_hookManager, _settings, OnSettingsUpdated);
            }

            _settingsForm.ApplyTheme();
            _settingsForm.Show();
            _settingsForm.WindowState = FormWindowState.Normal;
            _settingsForm.BringToFront();
            _settingsForm.Activate();
        }

        /// <summary>
        /// 設定変更時のUI同期
        /// </summary>
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
                string stateText = e.IsImeOn ? Strings.ImeOnState : Strings.ImeOffState;
                _notifyIcon.ShowBalloonTip(
                    1200,
                    Strings.BalloonTitle,
                    Strings.BalloonText(e.TriggerKey, stateText),
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
