using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace IMEKeyBinder
{
    public class ImeActionEventArgs : EventArgs
    {
        public bool IsImeOn { get; }
        public string TriggerKey { get; }
        public DateTime Timestamp { get; }

        public ImeActionEventArgs(bool isImeOn, string triggerKey)
        {
            IsImeOn = isImeOn;
            TriggerKey = triggerKey;
            Timestamp = DateTime.Now;
        }
    }

    public class KeyComboEventArgs : EventArgs
    {
        public string KeyName { get; }
        public DateTime Timestamp { get; }

        public KeyComboEventArgs(string keyName)
        {
            KeyName = keyName;
            Timestamp = DateTime.Now;
        }
    }

    public class KeyboardHookManager : IDisposable
    {
        public event EventHandler<ImeActionEventArgs>? ImeActionTriggered;
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

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

                // Ignore keys injected by our own process
                if (hookStruct.dwExtraInfo == NativeMethods.EXTRA_INFO_MAGIC)
                {
                    return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
                }

                int msg = wParam.ToInt32();
                bool isKeyDown = (msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN);
                bool isKeyUp = (msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP);

                uint vk = hookStruct.vkCode;
                bool isExtended = (hookStruct.flags & NativeMethods.LLKHF_EXTENDED) != 0;

                bool isLeftAlt = (vk == NativeMethods.VK_LMENU) || (vk == NativeMethods.VK_MENU && !isExtended);
                bool isRightAlt = (vk == NativeMethods.VK_RMENU) || (vk == NativeMethods.VK_MENU && isExtended);

                if (isKeyDown)
                {
                    if (isLeftAlt)
                    {
                        if (!_leftAltPressed)
                        {
                            _leftAltPressed = true;
                            _isCombo = _rightAltPressed || CheckIfModifiersAlreadyPressed(isLeftAlt: true);
                            SendDummyMaskKey();
                        }
                    }
                    else if (isRightAlt)
                    {
                        if (!_rightAltPressed)
                        {
                            _rightAltPressed = true;
                            _isCombo = _leftAltPressed || CheckIfModifiersAlreadyPressed(isLeftAlt: false);
                            SendDummyMaskKey();
                        }
                    }
                    else
                    {
                        // Any other key pressed while an Alt key is down marks this as a combo!
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
                        if (_leftAltPressed && !_isCombo && Settings.IsEnabled)
                        {
                            // Left Alt tapped alone -> IME OFF
                            ApplyImeState(false);
                            ImeActionTriggered?.Invoke(this, new ImeActionEventArgs(false, "左 Alt"));
                        }
                        _leftAltPressed = false;
                        if (!_rightAltPressed)
                        {
                            _isCombo = false;
                        }
                    }
                    else if (isRightAlt)
                    {
                        if (_rightAltPressed && !_isCombo && Settings.IsEnabled)
                        {
                            // Right Alt tapped alone -> IME ON
                            ApplyImeState(true);
                            ImeActionTriggered?.Invoke(this, new ImeActionEventArgs(true, "右 Alt"));
                        }
                        _rightAltPressed = false;
                        if (!_leftAltPressed)
                        {
                            _isCombo = false;
                        }
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool CheckIfModifiersAlreadyPressed(bool isLeftAlt)
        {
            // Check other modifiers: Ctrl, Shift, Win, and the opposite Alt key
            bool otherAlt = isLeftAlt
                ? (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RMENU) & 0x8000) != 0
                : (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LMENU) & 0x8000) != 0;

            return otherAlt ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LWIN) & 0x8000) != 0 ||
                   (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RWIN) & 0x8000) != 0;
        }

        private void SendDummyMaskKey()
        {
            // Sends a benign mask key (VK_DUMMY_MASK: 0xE8) with EXTRA_INFO_MAGIC
            // This prevents Windows from opening the menu bar on single Alt release.
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

        public void ApplyImeState(bool enable)
        {
            // 1. Send direct message to the active window's default IME window
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
                // Ignore errors from specific windows
            }

            // 2. Synthesize virtual key inputs (SendInput)
            // Ensures modern apps (UWP, Windows Terminal, Edge, Chromium) also switch state reliably
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
                // Ignore input synthesis errors
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
