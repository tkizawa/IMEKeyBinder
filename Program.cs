using System;
using System.Threading;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    static class Program
    {
        private const string AppMutexName = "Global\\IMEKeyBinder_SingleInstance_Mutex_6B7E1B1A";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using var mutex = new Mutex(true, AppMutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "IMEKeyBinder は既に起動しています。\nタスクトレイ（画面右下の通知領域）のアイコンを確認してください。",
                    "IMEKeyBinder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
        }
    }
}