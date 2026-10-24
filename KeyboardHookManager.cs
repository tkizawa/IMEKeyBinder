using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IMEKeyBinder
{
    /// <summary>
    /// IME状態変更イベントの引数
    /// </summary>
    public class ImeActionEventArgs : EventArgs
    {
        /// <summary>IMEがONになったかどうか</summary>
        public bool IsImeOn { get; }
        /// <summary>トリガーとなったキー名（右 Alt / 左 Alt）</summary>
        public string TriggerKey { get; }
        /// <summary>イベント発生日時</summary>
        public DateTime Timestamp { get; }

        public ImeActionEventArgs(bool isImeOn, string triggerKey)
        {
            IsImeOn = isImeOn;
            TriggerKey = triggerKey;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Altキーを含むコンボショートカット検出イベントの引数
    /// </summary>
    public class KeyComboEventArgs : EventArgs
    {
        /// <summary>検出されたキーコンボ名（例: LAlt + Key (0x09)）</summary>
        public string KeyName { get; }
        /// <summary>イベント発生日時</summary>
        public DateTime Timestamp { get; }

        public KeyComboEventArgs(string keyName)
        {
            KeyName = keyName;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// グローバル低レベルキーボードフック管理クラス (WH_KEYBOARD_LL)
    /// 右Alt単体押しでIME ON、左Alt単体押しでIME OFF、コンボキー時はパススルーする処理を司ります。
    /// </summary>
    public class KeyboardHookManager : IDisposable
    {
        /// <summary>IME状態の切り替えがトリガーされたときのイベント</summary>
        public event EventHandler<ImeActionEventArgs>? ImeActionTriggered;
        /// <summary>Altキーと他のキーの同時押し（コンボキー）が検出されたときのイベント</summary>
        public event EventHandler<KeyComboEventArgs>? KeyComboDetected;

        private IntPtr _hookId = IntPtr.Zero;
        private readonly NativeMethods.LowLevelKeyboardProc _hookProc;

        private bool _leftAltPressed = false;
        private bool _rightAltPressed = false;
        private bool _isCombo = false;

        public AppSettings Settings { get; set; }

        public KeyboardHookManager(AppSettings settings)
        {
            Settings = settings;
            _hookProc = HookCallback;
        }

        /// <summary>
        /// 低レベルキーボードフックを開始します。
        /// </summary>
        public bool Start()
        {
            if (_hookId != IntPtr.Zero) return true;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(curModule?.ModuleName);

            _hookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _hookProc,
                moduleHandle,
                0);

            return _hookId != IntPtr.Zero;
        }

        /// <summary>
        /// キーボードフックを停止します。
        /// </summary>
        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            _leftAltPressed = false;
            _rightAltPressed = false;
            _isCombo = false;
        }

        /// <summary>
        /// 低レベルキーボードフックのコールバックプロシージャ
        /// すべてのキー入力イベントを捕捉し、Altキーの単体押し離しやコンボ入力を判定します。
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                // 自プロセスが SendInput で合成したキーイベント（EXTRA_INFO_MAGIC 付き）は無限ループ防止のためスキップ
                if (hookStruct.dwExtraInfo == NativeMethods.EXTRA_INFO_MAGIC)
                {
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = wParam.ToInt32();
                bool isKeyDown = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN);
                bool isKeyUp = (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP);

                uint vk = hookStruct.vkCode;
                bool isExtended = (hookStruct.flags & NativeMethods.LLKHF_EXTENDED) != 0;

                // 英語配列キーボードにおける左右Altの判別
                bool isLeftAlt = (vk == NativeMethods.VK_LMENU) || (vk == NativeMethods.VK_MENU && !isExtended);
                bool isRightAlt = (vk == NativeMethods.VK_RMENU) || (vk == NativeMethods.VK_MENU && isExtended);

                if (isKeyDown)
                {
                    if (isLeftAlt)
                    {
                        if (!_leftAltPressed)
                        {
                            _leftAltPressed = true;
                            // 既に他のキーや修飾キーが押されていればコンボ扱い
                            _isCombo = _rightAltPressed || CheckIfModifiersAlreadyPressed(isLeftAlt: true);
                            // Windowsのメニューバー誤アクティブ化防止ダミーキー送信
                            SendDummyMaskKey();
                        }
                    }
                    else if (isRightAlt)
                    {
                        if (!_rightAltPressed)
                        {
                            _rightAltPressed = true;
                            // 既に他のキーや修飾キーが押されていればコンボ扱い
                            _isCombo = _leftAltPressed || CheckIfModifiersAlreadyPressed(isLeftAlt: false);
                            // Windowsのメニューバー誤アクティブ化防止ダミーキー送信
                            SendDummyMaskKey();
                        }
                    }
                    else
                    {
                        // Altキーが押下されている最中に他のキーが押された場合はコンボと判定
                        if (_leftAltPressed || _rightAltPressed)
                        {
                            if (!_isCombo)
                            {
                                _isCombo = true;
                                string comboModifier = _leftAltPressed ? "LAlt" : "RAlt";
                                KeyComboDetected?.Invoke(this, new KeyComboEventArgs($"{comboModifier} + Key (0x{vk:X2})"));
                            }
                        }
                    }
                }
                else if (isKeyUp)
                {
                    if (isLeftAlt)
                    {
                        // 左Altが単体で押されて離された場合 -> IME OFF
                        if (_leftAltPressed && !_isCombo && Settings.IsEnabled)
                        {
                            ApplyImeState(false);
                            ImeActionTriggered?.Invoke(this, new ImeActionEventArgs(false, Strings.KeyLeftAlt));
                        }
                        _leftAltPressed = false;
                        if (!_rightAltPressed)
                        {
                            _isCombo = false;
                        }
                    }
                    else if (isRightAlt)
                    {
                        // 右Altが単体で押されて離された場合 -> IME ON
                        if (_rightAltPressed && !_isCombo && Settings.IsEnabled)
                        {
                            ApplyImeState(true);
                            ImeActionTriggered?.Invoke(this, new ImeActionEventArgs(true, Strings.KeyRightAlt));
                        }
                        _rightAltPressed = false;
                        if (!_leftAltPressed)
                        {
                            _isCombo = false;
                        }
                    }
                }
            }

            // 通常のキーイベントはそのまま次へ通過（OSや他アプリのキーバインドを阻害しない）
            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        /// <summary>
        /// 修飾キー（Ctrl, Shift, Win, または反対側のAlt）が既に押されているかを確認します。
        /// </summary>
        private bool CheckIfModifiersAlreadyPressed(bool isLeftAlt)
        {
            bool otherAlt = isLeftAlt
                ? (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RMENU) & 0x8000) != 0
                : (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LMENU) & 0x8000) != 0;

            return otherAlt ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
        }

        /// <summary>
        /// Windows標準の「Altキー単体押しでウィンドウメニューバーがアクティブになる挙動」を防ぐため、
        /// 安全なダミーキー（VK_DUMMY_MASK: 0xE8）の合成入力を送信します。
        /// </summary>
        private void SendDummyMaskKey()
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2];
            int inputSize = Marshal.SizeOf(typeof(NativeMethods.INPUT));

            inputs[0].type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].u.ki.wVk = NativeMethods.VK_DUMMY_MASK;
            inputs[0].u.ki.dwFlags = 0;
            inputs[0].u.ki.dwExtraInfo = NativeMethods.EXTRA_INFO_MAGIC;

            inputs[1].type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].u.ki.wVk = NativeMethods.VK_DUMMY_MASK;
            inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
            inputs[1].u.ki.dwExtraInfo = NativeMethods.EXTRA_INFO_MAGIC;

            NativeMethods.SendInput((uint)inputs.Length, inputs, inputSize);
        }

        /// <summary>
        /// 指定された状態（ONまたはOFF）にIMEを切り替えます。
        /// Win32メッセージとSendInput仮想キーのハイブリッド方式で、クラシックアプリからモダンアプリまで幅広く対応します。
        /// </summary>
        public void ApplyImeState(bool enable)
        {
            // 1. アクティブウィンドウのデフォルトIMEウィンドウへ直接 WM_IME_CONTROL メッセージを送信
            try
            {
                IntPtr targetHwnd = IntPtr.Zero;
                var gti = new NativeMethods.GUITHREADINFO();
                gti.cbSize = Marshal.SizeOf(gti);

                if (NativeMethods.GetGUIThreadInfo(0, ref gti) && gti.hwndFocus != IntPtr.Zero)
                {
                    targetHwnd = gti.hwndFocus;
                }
                else
                {
                    targetHwnd = NativeMethods.GetForegroundWindow();
                }

                if (targetHwnd != IntPtr.Zero)
                {
                    IntPtr imeHwnd = NativeMethods.ImmGetDefaultIMEWnd(targetHwnd);
                    if (imeHwnd != IntPtr.Zero)
                    {
                        NativeMethods.SendMessage(
                            imeHwnd,
                            NativeMethods.WM_IME_CONTROL,
                            (IntPtr)NativeMethods.IMC_SETOPENSTATUS,
                            (IntPtr)(enable ? 1 : 0));
                    }
                }
            }
            catch
            {
                // ウィンドウごとのアクセス権限によるエラーは無視
            }

            // 2. 仮想キー入力を合成（SendInput）
            // Chromium系 (Chrome, VSCode, Slack)、Windows Terminal、UWPなどのモダンアプリへ確実に切り替えを反映
            try
            {
                ushort keyToSend;
                if (Settings.Method == ImeSwitchMethod.HenkanMuhenkan)
                {
                    keyToSend = enable ? NativeMethods.VK_CONVERT : NativeMethods.VK_NONCONVERT;
                }
                else
                {
                    keyToSend = enable ? NativeMethods.VK_IME_ON : NativeMethods.VK_IME_OFF;
                }

                NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[2];
                int inputSize = Marshal.SizeOf(typeof(NativeMethods.INPUT));

                inputs[0].type = NativeMethods.INPUT_KEYBOARD;
                inputs[0].u.ki.wVk = keyToSend;
                inputs[0].u.ki.dwFlags = 0;
                inputs[0].u.ki.dwExtraInfo = NativeMethods.EXTRA_INFO_MAGIC;

                inputs[1].type = NativeMethods.INPUT_KEYBOARD;
                inputs[1].u.ki.wVk = keyToSend;
                inputs[1].u.ki.dwFlags = NativeMethods.KEYEVENTF_KEYUP;
                inputs[1].u.ki.dwExtraInfo = NativeMethods.EXTRA_INFO_MAGIC;

                NativeMethods.SendInput((uint)inputs.Length, inputs, inputSize);
            }
            catch
            {
                // 入力合成失敗時は無視
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
