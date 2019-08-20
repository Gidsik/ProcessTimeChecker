using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PTChecker
{
    class MyProcess
    {
        public DateTime endTime;
        public Process process;
        public Boolean closed = false;

        MyProcess()
        {
            process = new Process();
            endTime = new DateTime();
        }

        MyProcess(Process process)
        {
            this.process = process;
            endTime = new DateTime();
            this.process.Exited += (object sender, EventArgs e) => { this.Close(); };
        }

        public void Close()
        {
            endTime = System.DateTime.Now;
            closed = true;
        }

    }
}
