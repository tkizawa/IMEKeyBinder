using System;
using System.Drawing;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    /// <summary>
    /// 設定およびキー監視リアルタイムログ表示用フォーム
    /// - 高DPI（125%, 150%, 175%, 200%等）環境での文字切れ・コントロール重なりを完全に防止する動的自動レイアウト
    /// - ウィンドウ位置とサイズの保存・復元に対応
    /// - Windowsテーマ設定（ライトモード/ダークモード）に対応
    /// - 多言語（日本語/英語）表示に対応
    /// </summary>
    public class SettingsForm : Form
    {
        private const int WM_SETTINGCHANGE = 0x001A;

        private readonly KeyboardHookManager _hookManager;
        private readonly AppSettings _settings;
        private readonly Action _onSettingsChanged;

        private Panel _headerPanel = null!;
        private Label _lblAppTitle = null!;
        private Label _lblAppSubtitle = null!;

        private Panel _mainContainer = null!;
        private Panel _panelStatusCard = null!;
        private Label _lblStatusTitle = null!;
        private Label _lblStatusDetail = null!;

        private GroupBox _grpSettings = null!;
        private CheckBox _chkEnabled = null!;
        private Label _lblMethod = null!;
        private ComboBox _cmbMethod = null!;
        private CheckBox _chkStartup = null!;
        private CheckBox _chkNotifications = null!;

        private Label _lblTest = null!;
        private TextBox _txtTestArea = null!;

        private Panel _pnlLogHeader = null!;
        private Label _lblLog = null!;
        private Button _btnClearLog = null!;
        private ListBox _lstLog = null!;

        private Panel _bottomPanel = null!;
        private Label _lblCloseHint = null!;
        private Button _btnHide = null!;

        public SettingsForm(KeyboardHookManager hookManager, AppSettings settings, Action onSettingsChanged)
        {
            _hookManager = hookManager;
            _settings = settings;
            _onSettingsChanged = onSettingsChanged;

            InitializeComponent();
            RestoreWindowBounds();
            ApplyTheme();
            SubscribeEvents();
        }

        /// <summary>
        /// コントロールの初期化
        /// </summary>
        private void InitializeComponent()
        {
            Text = Strings.FormTitle;
            MinimumSize = new Size(620, 740);
            StartPosition = FormStartPosition.Manual;
            // 日本語環境・高DPI環境でも最も美しい Yu Gothic UI を使用
            Font = new Font("Yu Gothic UI", 9.5f, FontStyle.Regular);
            Icon = IconHelper.CreateAppIcon(true);

            // ==========================================
            // ヘッダーパネル（文字切れ防止のため高さを十分確保）
            // ==========================================
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                Padding = new Padding(24, 14, 24, 14)
            };

            _lblAppTitle = new Label
            {
                Text = Strings.AppTitle,
                Font = new Font("Yu Gothic UI", 15f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(22, 12)
            };

            _lblAppSubtitle = new Label
            {
                Text = Strings.AppSubtitle,
                Font = new Font("Yu Gothic UI", 9.5f, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(24, 48)
            };

            _headerPanel.Controls.Add(_lblAppTitle);
            _headerPanel.Controls.Add(_lblAppSubtitle);

            // ==========================================
            // ボトムパネル
            // ==========================================
            _bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                Padding = new Padding(24, 12, 24, 12)
            };

            _lblCloseHint = new Label
            {
                Text = Strings.CloseHint,
                Location = new Point(24, 20),
                AutoSize = true,
                Font = new Font("Yu Gothic UI", 9f)
            };

            _btnHide = new Button
            {
                Text = Strings.BtnHide,
                Size = new Size(130, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 9.5f, FontStyle.Bold)
            };
            _btnHide.Click += (s, e) =>
            {
                SaveWindowBounds();
                Hide();
            };

            _bottomPanel.Controls.Add(_lblCloseHint);
            _bottomPanel.Controls.Add(_btnHide);

            // ==========================================
            // メインスクロールコンテナ
            // ==========================================
            _mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                AutoScroll = true
            };

            // 1. ステータスカード
            _panelStatusCard = new Panel
            {
                Padding = new Padding(18, 14, 18, 16)
            };
            _panelStatusCard.Paint += (s, e) =>
            {
                bool isDark = ThemeHelper.IsDarkMode();
                Color border = isDark ? Color.FromArgb(46, 125, 50) : Color.FromArgb(165, 214, 167);
                using var pen = new Pen(border, 1.5f);
                e.Graphics.DrawRectangle(pen, 0, 0, _panelStatusCard.Width - 1, _panelStatusCard.Height - 1);
            };

            _lblStatusTitle = new Label
            {
                Text = Strings.StatusActiveTitle,
                Font = new Font("Yu Gothic UI", 11.5f, FontStyle.Bold),
                Location = new Point(18, 12),
                AutoSize = true
            };

            _lblStatusDetail = new Label
            {
                Text = Strings.StatusActiveDetail,
                Font = new Font("Yu Gothic UI", 9.2f, FontStyle.Regular),
                Location = new Point(18, 44),
                AutoSize = true
            };

            _panelStatusCard.Controls.Add(_lblStatusTitle);
            _panelStatusCard.Controls.Add(_lblStatusDetail);
            _mainContainer.Controls.Add(_panelStatusCard);

            // 2. 設定グループ
            _grpSettings = new GroupBox
            {
                Text = Strings.SettingsGroupTitle,
                Padding = new Padding(16, 12, 16, 16)
            };

            _chkEnabled = new CheckBox
            {
                Text = Strings.ChkEnableTitle,
                Location = new Point(20, 30),
                AutoSize = true,
                Checked = _settings.IsEnabled
            };
            _chkEnabled.CheckedChanged += ChkEnabled_CheckedChanged;

            _lblMethod = new Label
            {
                Text = Strings.LblMethodTitle,
                Location = new Point(20, 72),
                AutoSize = true
            };

            _cmbMethod = new ComboBox
            {
                Location = new Point(190, 68),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DropDownWidth = 460
            };
            _cmbMethod.Items.Add(Strings.MethodStandardOption);
            _cmbMethod.Items.Add(Strings.MethodHenkanOption);
            _cmbMethod.SelectedIndex = (int)_settings.Method;
            _cmbMethod.SelectedIndexChanged += CmbMethod_SelectedIndexChanged;

            _chkStartup = new CheckBox
            {
                Text = Strings.ChkStartupTitle,
                Location = new Point(20, 116),
                AutoSize = true,
                Checked = AppSettings.IsStartupEnabled()
            };
            _chkStartup.CheckedChanged += ChkStartup_CheckedChanged;

            _chkNotifications = new CheckBox
            {
                Text = Strings.ChkNotificationsTitle,
                Location = new Point(20, 156),
                AutoSize = true,
                Checked = _settings.ShowNotifications
            };
            _chkNotifications.CheckedChanged += ChkNotifications_CheckedChanged;

            _grpSettings.Controls.Add(_chkEnabled);
            _grpSettings.Controls.Add(_lblMethod);
            _grpSettings.Controls.Add(_cmbMethod);
            _grpSettings.Controls.Add(_chkStartup);
            _grpSettings.Controls.Add(_chkNotifications);
            _mainContainer.Controls.Add(_grpSettings);

            // 3. テスト入力エリア
            _lblTest = new Label
            {
                Text = Strings.TestAreaLabel,
                AutoSize = true
            };
            _mainContainer.Controls.Add(_lblTest);

            _txtTestArea = new TextBox
            {
                Font = new Font("Yu Gothic UI", 10.5f),
                PlaceholderText = Strings.TestAreaPlaceholder
            };
            _mainContainer.Controls.Add(_txtTestArea);

            // 4. リアルタイムログ
            _pnlLogHeader = new Panel();

            _lblLog = new Label
            {
                Text = Strings.LogHeader,
                Location = new Point(0, 6),
                AutoSize = true,
                Font = new Font("Yu Gothic UI", 10f, FontStyle.Bold)
            };

            _btnClearLog = new Button
            {
                Text = Strings.BtnClearLog,
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 9f)
            };
            _btnClearLog.Click += (s, e) => _lstLog.Items.Clear();

            _pnlLogHeader.Controls.Add(_lblLog);
            _pnlLogHeader.Controls.Add(_btnClearLog);
            _mainContainer.Controls.Add(_pnlLogHeader);

            _lstLog = new ListBox
            {
                Font = new Font("Consolas", 9.5f),
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false
            };
            _mainContainer.Controls.Add(_lstLog);

            Controls.Add(_mainContainer);
            Controls.Add(_bottomPanel);
            Controls.Add(_headerPanel);

            AddLog(Strings.LogInitialized);
        }

        /// <summary>
        /// ウィンドウのサイズやDPI、コンテンツの行数に応じてすべてのコントロールの配置を動的に再計算します。
        /// これにより文字切れやコントロールの重なりを物理的に防止します。
        /// </summary>
        private void UpdateContentLayout()
        {
            if (_mainContainer == null || _panelStatusCard == null || _grpSettings == null) return;

            int paddingX = 24;
            int availableWidth = Math.Max(460, _mainContainer.ClientSize.Width - (paddingX * 2));

            // ボタン配置
            _btnHide.Location = new Point(_bottomPanel.ClientSize.Width - _btnHide.Width - 24, 12);

            // 1. ステータスカードの動的高さ計算
            _lblStatusDetail.MaximumSize = new Size(availableWidth - 36, 0);
            int requiredCardHeight = Math.Max(124, _lblStatusDetail.Bottom + 16);
            _panelStatusCard.Location = new Point(paddingX, 16);
            _panelStatusCard.Size = new Size(availableWidth, requiredCardHeight);

            int curY = _panelStatusCard.Bottom + 16;

            // 2. 設定グループの配置
            _grpSettings.Location = new Point(paddingX, curY);
            _grpSettings.Size = new Size(availableWidth, 205);
            _cmbMethod.Size = new Size(Math.Max(200, availableWidth - 210), 28);

            curY = _grpSettings.Bottom + 16;

            // 3. テスト入力エリアの配置
            _lblTest.Location = new Point(paddingX, curY);
            curY = _lblTest.Bottom + 8;
            _txtTestArea.Location = new Point(paddingX, curY);
            _txtTestArea.Size = new Size(availableWidth, 34);

            curY = _txtTestArea.Bottom + 16;

            // 4. リアルタイムログヘッダー
            _pnlLogHeader.Location = new Point(paddingX, curY);
            _pnlLogHeader.Size = new Size(availableWidth, 32);
            _btnClearLog.Location = new Point(availableWidth - _btnClearLog.Width, 0);

            curY = _pnlLogHeader.Bottom + 8;

            // 5. リアルタイムログリスト（下部の余白に合わせて伸縮）
            int remainingHeight = _mainContainer.ClientSize.Height - curY - 16;
            _lstLog.Location = new Point(paddingX, curY);
            _lstLog.Size = new Size(availableWidth, Math.Max(180, remainingHeight));
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            UpdateContentLayout();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateContentLayout();
        }

        /// <summary>
        /// ウィンドウの位置およびサイズを設定から復元します。
        /// 画面外にはみ出ている場合は中央に再配置します。
        /// </summary>
        private void RestoreWindowBounds()
        {
            int w = _settings.WindowWidth >= 620 ? _settings.WindowWidth : 680;
            int h = _settings.WindowHeight >= 740 ? _settings.WindowHeight : 780;
            Size = new Size(w, h);

            if (_settings.WindowLeft >= 0 && _settings.WindowTop >= 0)
            {
                var targetRect = new Rectangle(_settings.WindowLeft, _settings.WindowTop, w, h);
                bool isVisibleOnAnyScreen = false;

                foreach (var screen in Screen.AllScreens)
                {
                    if (screen.WorkingArea.IntersectsWith(targetRect))
                    {
                        isVisibleOnAnyScreen = true;
                        break;
                    }
                }

                if (isVisibleOnAnyScreen)
                {
                    Location = new Point(_settings.WindowLeft, _settings.WindowTop);
                    UpdateContentLayout();
                    return;
                }
            }

            CenterToScreen();
            UpdateContentLayout();
        }

        /// <summary>
        /// 現在のウィンドウ位置およびサイズを設定に保存します。
        /// </summary>
        private void SaveWindowBounds()
        {
            if (WindowState == FormWindowState.Normal)
            {
                _settings.WindowLeft = Location.X;
                _settings.WindowTop = Location.Y;
                _settings.WindowWidth = Width;
                _settings.WindowHeight = Height;
                _settings.Save();
            }
        }

        /// <summary>
        /// Windowsのテーマ設定（ダーク/ライト）に応じたカラースタイルを適用します。
        /// </summary>
        public void ApplyTheme()
        {
            bool isDark = ThemeHelper.IsDarkMode();
            ThemeHelper.ApplyTitleBarTheme(Handle, isDark);

            if (isDark)
            {
                // ダークテーマ
                BackColor = Color.FromArgb(32, 33, 36);
                ForeColor = Color.FromArgb(241, 243, 244);

                _headerPanel.BackColor = Color.FromArgb(24, 25, 27);
                _lblAppTitle.ForeColor = Color.White;
                _lblAppSubtitle.ForeColor = Color.FromArgb(170, 175, 180);

                _grpSettings.ForeColor = Color.FromArgb(241, 243, 244);
                _lblMethod.ForeColor = Color.FromArgb(220, 224, 230);
                _lblTest.ForeColor = Color.FromArgb(189, 193, 198);

                _txtTestArea.BackColor = Color.FromArgb(48, 49, 52);
                _txtTestArea.ForeColor = Color.White;

                _lblLog.ForeColor = Color.FromArgb(220, 224, 230);
                _btnClearLog.BackColor = Color.FromArgb(48, 49, 52);
                _btnClearLog.ForeColor = Color.White;
                _btnClearLog.FlatAppearance.BorderColor = Color.FromArgb(70, 72, 77);

                _lstLog.BackColor = Color.FromArgb(24, 25, 27);
                _lstLog.ForeColor = Color.FromArgb(220, 224, 230);

                _bottomPanel.BackColor = Color.FromArgb(24, 25, 27);
                _lblCloseHint.ForeColor = Color.FromArgb(154, 160, 166);

                _btnHide.BackColor = Color.FromArgb(59, 130, 246);
                _btnHide.ForeColor = Color.White;
                _btnHide.FlatAppearance.BorderColor = Color.FromArgb(37, 99, 235);
            }
            else
            {
                // ライトテーマ
                BackColor = Color.FromArgb(248, 249, 250);
                ForeColor = Color.FromArgb(33, 37, 41);

                _headerPanel.BackColor = Color.FromArgb(33, 37, 41);
                _lblAppTitle.ForeColor = Color.White;
                _lblAppSubtitle.ForeColor = Color.FromArgb(173, 181, 189);

                _grpSettings.ForeColor = Color.FromArgb(33, 37, 41);
                _lblMethod.ForeColor = Color.FromArgb(55, 65, 81);
                _lblTest.ForeColor = Color.FromArgb(73, 80, 87);

                _txtTestArea.BackColor = Color.White;
                _txtTestArea.ForeColor = Color.FromArgb(33, 37, 41);

                _lblLog.ForeColor = Color.FromArgb(73, 80, 87);
                _btnClearLog.BackColor = Color.White;
                _btnClearLog.ForeColor = Color.FromArgb(33, 37, 41);
                _btnClearLog.FlatAppearance.BorderColor = Color.FromArgb(206, 212, 218);

                _lstLog.BackColor = Color.FromArgb(250, 252, 255);
                _lstLog.ForeColor = Color.FromArgb(33, 37, 41);

                _bottomPanel.BackColor = Color.FromArgb(238, 242, 246);
                _lblCloseHint.ForeColor = Color.FromArgb(108, 117, 125);

                _btnHide.BackColor = Color.FromArgb(33, 37, 41);
                _btnHide.ForeColor = Color.White;
                _btnHide.FlatAppearance.BorderColor = Color.FromArgb(33, 37, 41);
            }

            UpdateStatusCard();
            UpdateContentLayout();
        }

        /// <summary>
        /// 有効/無効状態に応じたステータスカードの配色と文言を更新します。
        /// </summary>
        private void UpdateStatusCard()
        {
            bool isDark = ThemeHelper.IsDarkMode();

            if (_settings.IsEnabled)
            {
                _panelStatusCard.BackColor = isDark ? Color.FromArgb(27, 47, 36) : Color.FromArgb(232, 245, 233);
                _lblStatusTitle.Text = Strings.StatusActiveTitle;
                _lblStatusTitle.ForeColor = isDark ? Color.FromArgb(129, 199, 132) : Color.FromArgb(46, 125, 50);
                _lblStatusDetail.Text = Strings.StatusActiveDetail;
                _lblStatusDetail.ForeColor = isDark ? Color.FromArgb(200, 230, 201) : Color.FromArgb(55, 65, 81);
            }
            else
            {
                _panelStatusCard.BackColor = isDark ? Color.FromArgb(56, 30, 30) : Color.FromArgb(254, 237, 237);
                _lblStatusTitle.Text = Strings.StatusInactiveTitle;
                _lblStatusTitle.ForeColor = isDark ? Color.FromArgb(239, 154, 154) : Color.FromArgb(198, 40, 40);
                _lblStatusDetail.Text = Strings.StatusActiveDetail;
                _lblStatusDetail.ForeColor = isDark ? Color.FromArgb(224, 224, 224) : Color.FromArgb(108, 117, 125);
            }

            _panelStatusCard.Invalidate();
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

            string state = e.IsImeOn ? Strings.ImeOnState : Strings.ImeOffState;
            AddLog($"[{e.Timestamp:HH:mm:ss.fff}] {Strings.LogSwitched(e.TriggerKey, state)}");
        }

        private void HookManager_KeyComboDetected(object? sender, KeyComboEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => HookManager_KeyComboDetected(sender, e)));
                return;
            }

            AddLog($"[{e.Timestamp:HH:mm:ss.fff}] {Strings.LogCombo(e.KeyName)}");
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

        private void ChkEnabled_CheckedChanged(object? sender, EventArgs e)
        {
            _settings.IsEnabled = _chkEnabled.Checked;
            _settings.Save();
            UpdateStatusCard();
            UpdateContentLayout();
            _onSettingsChanged();
            AddLog(Strings.LogSettingChanged("IsEnabled", _settings.IsEnabled));
        }

        private void CmbMethod_SelectedIndexChanged(object? sender, EventArgs e)
        {
            _settings.Method = (ImeSwitchMethod)_cmbMethod.SelectedIndex;
            _settings.Save();
            _onSettingsChanged();
            AddLog(Strings.LogSettingChanged("Method", _settings.Method));
        }

        private void ChkStartup_CheckedChanged(object? sender, EventArgs e)
        {
            AppSettings.SetStartupEnabled(_chkStartup.Checked);
            AddLog(Strings.LogSettingChanged("Startup", _chkStartup.Checked));
        }

        private void ChkNotifications_CheckedChanged(object? sender, EventArgs e)
        {
            _settings.ShowNotifications = _chkNotifications.Checked;
            _settings.Save();
            _onSettingsChanged();
            AddLog(Strings.LogSettingChanged("Notifications", _settings.ShowNotifications));
        }

        /// <summary>
        /// ウィンドウ終了（閉じるボタン押下）時はアプリを終了せず、位置・サイズを保存してトレイに最小化
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowBounds();

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// OSのテーマ設定変更メッセージ (WM_SETTINGCHANGE) を捕捉してテーマを動的反映
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SETTINGCHANGE)
            {
                ApplyTheme();
            }
            base.WndProc(ref m);
        }
    }
}
