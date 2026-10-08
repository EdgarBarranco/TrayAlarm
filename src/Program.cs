using System;
using System.Threading;
using System.Windows.Forms;

namespace TrayAlarm
{
    static class Program
    {
        private const string AppMutexName = "Global\\TrayAlarm_SingleInstance_App_2026";

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            using (var mutex = new Mutex(true, AppMutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Tray Alarm is already running.\nPlease check the system tray near your clock.",
                        "Tray Alarm",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Initialize System Theme integration
                ThemeManager.Initialize();

                // Global exception handler for stability
                Application.ThreadException += (s, e) =>
                {
                    MessageBox.Show("An unexpected error occurred: " + e.Exception.Message, "Tray Alarm Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                };

                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    string msg = ex != null ? ex.Message : "Unknown error";
                    MessageBox.Show("Fatal error: " + msg, "Tray Alarm Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                Application.Run(new MainForm());
            }
        }
    }
}
