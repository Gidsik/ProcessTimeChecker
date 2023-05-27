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
    public partial class TempFormForTesting : Form
    {
        Boolean isMouseDown = false;
        Point MouseOffset;

        TargetProcess tp1;
        TargetProcess tp2;
        TargetProcess tp3;


        public TempFormForTesting()
        {
            InitializeComponent();

            tp1 = new TargetProcess("discord.exe", "", true);
            tp2 = new TargetProcess("notepad++.exe", "", true);
            tp3 = new TargetProcess("chrome.exe", "", true);

            ProcessInfoWatcher.RunningProcesses();

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
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
