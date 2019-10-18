using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace PTChecker
{
    class MyProcess
    {
        public DateTime endTime;
        public Process process;
        private Microsoft.Win32.SafeHandles.SafeProcessHandle safeHandle;
        private IntPtr handle;
        public Boolean closed = false;

        MyProcess()
        {
            process = new Process();
            endTime = new DateTime();
        }

        public MyProcess(Process process)
        {
            this.process = process;
            endTime = new DateTime();
            this.process.Exited += (object sender, EventArgs e) => { this.Close(); MessageBox.Show("Exited complete"); };
            safeHandle = process.SafeHandle;
            handle = process.Handle;
        }

        public void Close()
        {
        //    var x = new FILETIME();
        //    var y = new FILETIME();
        //    GetProcessTimes(handle, out x, out y, out x, out x);
            MessageBox.Show(process.ExitTime.ToString());
            endTime = System.DateTime.Now;
            closed = true;
        }
    }
}
