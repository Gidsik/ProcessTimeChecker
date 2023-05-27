using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PTChecker.ClassesScr
{
    class ProcessesManager
    {
        private List<TargetProcess> targetProcesses;
        public ProcessesManager(System.Data.DataTable processesInfo)
        {
            targetProcesses = new List<TargetProcess>();
            foreach (System.Data.DataRow row in processesInfo.Rows)
            {

            }
        }
    }
}
