using System;
using System.Threading;
using System.Windows.Forms;

namespace IMEKeyBinder
{
    /// <summary>
    /// アプリケーションのエントリポイント
    /// グローバルミューテックスによる二重起動防止と常駐コンテキストの開始を行います。
    /// </summary>
    static class Program
    {
        private const string AppMutexName = "Global\\IMEKeyBinder_SingleInstance_Mutex_6B7E1B1A";

        [STAThread]
        static void Main()
        {
            // 二重起動を検知・防止
            using var mutex = new Mutex(true, AppMutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    Strings.AlreadyRunning,
                    Strings.AppTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Windows Forms の高DPI対応およびフォント初期化
            ApplicationConfiguration.Initialize();

            // タスクトレイ常駐コンテキストを実行
            Application.Run(new TrayApplicationContext());
        }
    }
}