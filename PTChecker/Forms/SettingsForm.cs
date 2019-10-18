using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PTChecker.Forms
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();

            isAutorun.Checked = Properties.Settings.Default.AutoRun;
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AcceptBtn_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoRun = isAutorun.Checked;
            Properties.Settings.Default.Save();
        }

        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            AutoRunIt.SetAutoRun(Properties.Settings.Default.AutoRun);
        }
    }
}
