using System;
using System.Drawing;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    public class SettingsForm : Form
    {
        private readonly KeyboardHookManager _hookManager;
        private readonly AppSettings _settings;
        private readonly Action _onSettingsChanged;

        private Label _lblStatusTitle = null!;
        private Label _lblStatusDetail = null!;
        private Panel _panelStatusCard = null!;

        private CheckBox _chkEnabled = null!;
        private ComboBox _cmbMethod = null!;
        private CheckBox _chkStartup = null!;
        private CheckBox _chkNotifications = null!;

        private TextBox _txtTestArea = null!;
        private ListBox _lstLog = null!;
        private Button _btnClearLog = null!;
        private Button _btnHide = null!;

        public SettingsForm(KeyboardHookManager hookManager, AppSettings settings, Action onSettingsChanged)
        {
            _hookManager = hookManager;
            _settings = settings;
            _onSettingsChanged = onSettingsChanged;

            InitializeComponent();
            ApplySettingsToUI();
            SubscribeEvents();
        }

        private void InitializeComponent()
        {
            Text = "IMEKeyBinder - 英語キーボード IME 切り替え";
            ClientSize = new Size(620, 680);
            MinimumSize = new Size(540, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 249, 250);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Icon = IconHelper.CreateAppIcon(true);

            // Header Banner
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.FromArgb(33, 37, 41),
                Padding = new Padding(20, 12, 20, 12)
            };

            var titleLabel = new Label
            {
                Text = "IMEKeyBinder",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(18, 10)
            };

            var subtitleLabel = new Label
            {
                Text = "英語キーボード用 右Alt / 左Alt 日本語入力切り替え常駐ツール",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(173, 181, 189),
                AutoSize = true,
                Location = new Point(20, 36)
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);

            // Main Layout Container
            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                AutoScroll = true
            };

            int curY = 15;

            // 1. Status Card
            _panelStatusCard = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(560, 75),
                BackColor = Color.FromArgb(232, 245, 233),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _panelStatusCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(165, 214, 167), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _panelStatusCard.Width - 1, _panelStatusCard.Height - 1);
            };

            _lblStatusTitle = new Label
            {
                Text = "✓ 常駐フック稼働中",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 125, 50),
                Location = new Point(16, 12),
                AutoSize = true
            };

            _lblStatusDetail = new Label
            {
                Text = "「右 Alt」単体押し: IME ON (日本語) ｜ 「左 Alt」単体押し: IME OFF (英語)\n※ Alt + 他のキー（Alt+Tab, Alt+F4 など）は通常通り機能します",
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(16, 36),
                AutoSize = true
            };

            _panelStatusCard.Controls.Add(_lblStatusTitle);
            _panelStatusCard.Controls.Add(_lblStatusDetail);
            mainContainer.Controls.Add(_panelStatusCard);
            curY += 88;

            // 2. Settings Group
            var grpSettings = new GroupBox
            {
                Text = "設定",
                Location = new Point(20, curY),
                Size = new Size(560, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(33, 37, 41)
            };

            _chkEnabled = new CheckBox
            {
                Text = "キーバインドによる IME 切り替えを有効にする",
                Location = new Point(20, 26),
                AutoSize = true,
                Checked = _settings.IsEnabled
            };
            _chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;

            var lblMethod = new Label
            {
                Text = "IME切り替え方式:",
                Location = new Point(20, 58),
                AutoSize = true
            };

            _cmbMethod = new ComboBox
            {
                Location = new Point(150, 55),
                Size = new Size(380, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cmbMethod.Items.Add("標準 (VK_IME_ON / VK_IME_OFF + ImmSetOpenStatus) [推奨]");
            _cmbMethod.Items.Add("変換・無変換 (VK_CONVERT / VK_NONCONVERT)");
            _cmbMethod.SelectedIndex = (int)_settings.Method;
            _cmbMethod.SelectedIndexChanged += CmbMethod_SelectedIndexChanged;

            _chkStartup = new CheckBox
            {
                Text = "Windows ログオン時に自動起動する (スタートアップ登録)",
                Location = new Point(20, 92),
                AutoSize = true,
                Checked = AppSettings.IsStartupEnabled()
            };
            _chkStartup.CheckedChanged += ChkStartup_CheckedChanged;

            _chkNotifications = new CheckBox
            {
                Text = "切り替え時にトレイバルーン通知を表示する",
                Location = new Point(20, 122),
                AutoSize = true,
                Checked = _settings.ShowNotifications
            };
            _chkNotifications.CheckedChanged += ChkNotifications_CheckedChanged;

            grpSettings.Controls.Add(_chkEnabled);
            grpSettings.Controls.Add(lblMethod);
            grpSettings.Controls.Add(_cmbMethod);
            grpSettings.Controls.Add(_chkStartup);
            grpSettings.Controls.Add(_chkNotifications);
            mainContainer.Controls.Add(grpSettings);
            curY += 175;

            // 3. Test Input Area
            var lblTest = new Label
            {
                Text = "テスト入力エリア (ここで右Alt / 左Altを押して日本語・英語の切り替えを試せます):",
                Location = new Point(20, curY),
                AutoSize = true,
                ForeColor = Color.FromArgb(73, 80, 87)
            };
            mainContainer.Controls.Add(lblTest);
            curY += 22;

            _txtTestArea = new TextBox
            {
                Location = new Point(20, curY),
                Size = new Size(560, 32),
                Font = new Font("Segoe UI", 10.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "ここをクリックして、右Alt / 左Alt を押して文字を入力してみてください..."
            };
            mainContainer.Controls.Add(_txtTestArea);
            curY += 44;

            // 4. Live Hook Log
            var pnlLogHeader = new Panel
            {
                Location = new Point(20, curY),
                Size = new Size(560, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblLog = new Label
            {
                Text = "動作リアルタイムログ:",
                Location = new Point(0, 4),
                AutoSize = true,
                ForeColor = Color.FromArgb(73, 80, 87),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };

            _btnClearLog = new Button
            {
                Text = "ログクリア",
                Size = new Size(80, 24),
                Location = new Point(480, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.White
            };
            _btnClearLog.Click += (s, e) => _lstLog.Items.Clear();

            pnlLogHeader.Controls.Add(lblLog);
            pnlLogHeader.Controls.Add(_btnClearLog);
            mainContainer.Controls.Add(pnlLogHeader);
            curY += 30;

            _lstLog = new ListBox
            {
                Location = new Point(20, curY),
                Size = new Size(560, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Consolas", 9f),
                BackColor = Color.FromArgb(250, 252, 255),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            mainContainer.Controls.Add(_lstLog);

            // Bottom Actions Panel
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(238, 242, 246),
                Padding = new Padding(20, 8, 20, 8)
            };

            var lblCloseHint = new Label
            {
                Text = "※ 画面を閉じてもタスクトレイで常駐動作を続けます。",
                Location = new Point(20, 16),
                AutoSize = true,
                ForeColor = Color.FromArgb(108, 117, 125),
                Font = new Font("Segoe UI", 8.5f)
            };

            _btnHide = new Button
            {
                Text = "トレイに最小化",
                Size = new Size(110, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(490, 9),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(33, 37, 41),
                ForeColor = Color.White
            };
            _btnHide.Click += (s, e) => Hide();

            bottomPanel.Controls.Add(lblCloseHint);
            bottomPanel.Controls.Add(_btnHide);

            Controls.Add(mainContainer);
            Controls.Add(bottomPanel);
            Controls.Add(headerPanel);

            AddLog("IMEKeyBinder が初期化されました。");
        }

        private void SubscribeEvents()
        {
            _hookManager.ImeActionTriggered += HookManager_ImeActionTriggered;
            _hookManager.KeyComboDetected += HookManager_KeyComboDetected;
        }

        private void HookManager_ImeActionTriggered(object? sender, ImeActionEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HookManager_ImeActionTriggered(sender, e)));
                return;
            }

            string state = e.IsImeOn ? "IME ON (日本語入力)" : "IME OFF (直接入力)";
            AddLog($"[{e.Timestamp:HH:mm:ss.fff}] {e.TriggerKey} 単体押し検知 -> {state} に切り替え");
        }

        private void HookManager_KeyComboDetected(object? sender, KeyComboEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HookManager_KeyComboDetected(sender, e)));
                return;
            }

            AddLog($"[{e.Timestamp:HH:mm:ss.fff}] コンボ検出: {e.KeyName} -> 通常ショートカットとしてパススルー (IME切り替えスキップ)");
        }

        private void AddLog(string message)
        {
            _lstLog.Items.Add(message);
            _lstLog.TopIndex = _lstLog.Items.Count - 1;

            if (_lstLog.Items.Count > 300)
            {
                _lstLog.Items.RemoveAt(0);
            }
        }

        private void ApplySettingsToUI()
        {
            UpdateStatusCard();
        }

        private void UpdateStatusCard()
        {
            if (_settings.IsEnabled)
            {
                _panelStatusCard.BackColor = Color.FromArgb(232, 245, 233);
                _lblStatusTitle.Text = "✓ 常駐フック稼働中";
                _lblStatusTitle.ForeColor = Color.FromArgb(46, 125, 50);
            }
            else
            {
                _panelStatusCard.BackColor = Color.FromArgb(254, 237, 237);
                _lblStatusTitle.Text = "⚠ 停止中 (無効化されています)";
                _lblStatusTitle.ForeColor = Color.FromArgb(198, 40, 40);
            }
        }

        private void ChkEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            _settings.IsEnabled = _chkEnabled.Checked;
            _settings.Save();
            UpdateStatusCard();
            _onSettingsChanged();
            AddLog($"設定変更: フック有効 = {_settings.IsEnabled}");
        }

        private void CmbMethod_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _settings.Method = (ImeSwitchMethod)_cmbMethod.SelectedIndex;
            _settings.Save();
            _onSettingsChanged();
            AddLog($"設定変更: 切替方式 = {_settings.Method}");
        }

        private void ChkStartup_CheckedChanged(object? sender, EventArgs e)
        {
            AppSettings.SetStartupEnabled(_chkStartup.Checked);
            AddLog($"設定変更: スタートアップ登録 = {_chkStartup.Checked}");
        }

        private void ChkNotifications_CheckedChanged(object? sender, EventArgs e)
        {
            _settings.ShowNotifications = _chkNotifications.Checked;
            _settings.Save();
            _onSettingsChanged();
            AddLog($"設定変更: 通知表示 = {_settings.ShowNotifications}");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Minimize to tray instead of quitting
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
