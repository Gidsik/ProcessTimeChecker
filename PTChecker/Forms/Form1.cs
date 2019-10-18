using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;
using System.Diagnostics;

namespace PTChecker.Forms
{
    public partial class Form1 : Form
    {
        Thread thread;
        System.Diagnostics.Process PS;
        Boolean isMouseDown = false;
        Point MouseOffset;

        public Form1()
        {
            InitializeComponent();

            thread = new Thread(
                    () => {
                        while (true)
                        {
                            if (ProcessesCheck())
                            {
                                DateTime start = PS.StartTime;
                                DateTime end;
                                Action action = () => {
                                    label1.Text = PS.ProcessName;
                                    label2.Text = PS.StartTime.ToString();
                                    if (PS.HasExited) {
                                        end = System.DateTime.Now;
                                        label3.Text = end.ToString();
                                        TimeSpan dt2 = end - start;
                                        label4.Text = dt2.ToString();
                                        MessageBox.Show(pc.closed.ToString());
                                    }
                                };
                                label1.Invoke(action);
                                
                            }
                            Thread.Sleep(100);
                        }
                    }
                );
            //thread.Start();
            ProcessStartWatching();
            ProcessInfoWatcher.RunningProcesses();
        }

        void ProcessStartWatching()
        {
            
            ProcessInfoWatcher watcher = new ProcessInfoWatcher("notepad++.exe");
            watcher.Started += (object sender, EventArrivedEventArgs e) =>
            {
                ManagementBaseObject targetInstanse = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
                int pid = Int32.Parse((string)targetInstanse.Properties["Handle"].Value);
                Process process = Process.GetProcessById(pid);
                Debug.WriteLine(DateTime.Now.ToLocalTime() + " " + process.ProcessName);
            };
            watcher.Terminated += (object sender, EventArrivedEventArgs e) =>
            {
                ManagementBaseObject targetInstanse = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
                int pid = Int32.Parse((string)targetInstanse.Properties["Handle"].Value);
                Debug.WriteLine(DateTime.Now.ToLocalTime() + " " + pid.ToString());


            };
        }

        MyProcess pc;
        Boolean ProcessesCheck()
        {
            System.Diagnostics.Process[] psProcess = System.Diagnostics.Process.GetProcessesByName("Photoshop");

            List<String> targetProcesses = new List<String>();
            targetProcesses.Add("Photoshop");
            targetProcesses.Add("chrome");
            targetProcesses.Add("explorer");

            var processes = System.Diagnostics.Process.GetProcesses()
                .Where( x => targetProcesses.Any( y => x.ProcessName == y ) );

            

            if (psProcess.Length > 0)
            {
                PS = psProcess[0];
                if (!PS.HasExited)
                {
                    pc = new MyProcess(PS);
                }
                else { pc.Close(); }
                
                return true;
            }
            return false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            thread.Abort();
            Application.Exit();
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int x = -e.X;
                int y = -e.Y;
                isMouseDown = true;
                MouseOffset = new Point(x, y);
            }
        }

        private void Form1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = false;
            }
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                Point mousePos = Control.MousePosition;
                mousePos.Offset(MouseOffset);
                Location = mousePos;
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                if (MessageBox.Show("Выйти?", "Выход", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    this.Close();
                }
            }
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int x = -e.X - ((Control)sender).Location.X;
                int y = -e.Y - ((Control)sender).Location.Y;
                isMouseDown = true;
                MouseOffset = new Point(x, y);
            }
        }
    }
}
