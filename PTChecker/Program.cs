using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PTChecker
{
    static class Program
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, Application.ProductName, out createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);


                    using(NotifyIcon icon = new NotifyIcon())
                    {
                        icon.Icon = Properties.Resources.efQ7jzLXw_4;
                        icon.ContextMenuStrip = new ContextMenuStrip();
                        icon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Show Form1", null, (s, e) => (new Forms.Form1()).Show()));
                        icon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Show ProcessesForm", null, (s, e) => (new Forms.ProcessesForm()).Show()));
                        icon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Settings", null, (s, e) => (new Forms.SettingsForm()).Show()));
                        icon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
                        icon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit()));

                        

                        icon.Text = Application.ProductName;
                        icon.Visible = true;

                        Application.Run();
                    }


                }
                else
                {
                    Process current = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(current.ProcessName))
                    {
                        if (process.Id != current.Id)
                        {
                            SetForegroundWindow(process.MainWindowHandle);
                        }
                    }
                }
            }
        }



    }
}
