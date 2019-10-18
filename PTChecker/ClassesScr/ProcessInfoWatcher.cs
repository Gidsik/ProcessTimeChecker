using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Principal;
using System.ComponentModel;
using System.Reflection;
using System.Data;
using System.Management;


namespace PTChecker
{
    internal class ProcessInfoWatcher
    {
        // определяем делегаты
        public delegate void StartedEventHandler(object sender, EventArrivedEventArgs e);
        public delegate void TerminatedEventHandler(object sender, EventArrivedEventArgs e);

        // события
        public event StartedEventHandler Started = null;
        public event TerminatedEventHandler Terminated = null;

        // отслеживание событий WMI
        private ManagementEventWatcher watcher;

        // конструктор принимает имя приложения (notepad.exe например)
        // запускает для него отслеживание WMI
        public ProcessInfoWatcher(string appName)
        {
            // запрос каждые [pol] секунд
            string pol = "1";

            string queryString =
                "SELECT *" +
                "  FROM __InstanceOperationEvent " +
                "WITHIN  " + pol +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + appName + "'";

            // область отслеживания события @"\\имяМашины\root\CIMV2" или точка для отслеживания всей области
            string scope = @"\\.\root\CIMV2";

            // создание слушателя и запуск отслеживания
            watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += new EventArrivedEventHandler(this.OnEventArrived);
            watcher.Start();
        }
        public void Dispose()
        {
            watcher.Stop();
            watcher.Dispose();
        }

        public static DataTable RunningProcesses()
        {
            /* One way of constructing a query
            string className = "Win32_Process";
            string condition = "";
            string[] selectedProperties = new string[] {"Name", "ProcessId", "Caption", "ExecutablePath"};
            SelectQuery query = new SelectQuery(className, condition, selectedProperties);
            */

            // The second way of constructing a query
            string queryString =
                $"SELECT Name, Caption, Description, ProcessId, Handle, ExecutablePath " +
                "  FROM Win32_Process" +
                "  WHERE Name = Caption AND Caption = Description AND Handle = ProcessId";

            SelectQuery query = new SelectQuery(queryString);
            ManagementScope scope = new System.Management.ManagementScope(@"\\.\root\CIMV2");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection processes = searcher.Get();

            foreach(ManagementObject mo in processes)
            {
                Debug.WriteLine(mo["Name"].ToString());
            }

            DataTable result = new DataTable();
            result.Columns.Add("Name", Type.GetType("System.String"));
            result.Columns.Add("Caption", Type.GetType("System.String"));
            result.Columns.Add("Description", Type.GetType("System.String"));
            result.Columns.Add("ProcessId", Type.GetType("System.Int32"));
            result.Columns.Add("Handle", Type.GetType("System.Int32"));
            result.Columns.Add("ExecutablePath", Type.GetType("System.String"));

            foreach (ManagementObject mo in processes)
            {
                DataRow row = result.NewRow();
                foreach (PropertyData m in mo.Properties)
                {
                    row[m.Name] = m.Value?.ToString();
                }
                result.Rows.Add(row);
            }

            return result;
        }

        private void OnEventArrived(object sender, System.Management.EventArrivedEventArgs e)
        {
            try
            {
                string eventName = e.NewEvent.ClassPath.ClassName;

                if (eventName.CompareTo("__InstanceCreationEvent") == 0)
                {
                    // Started
                    Started?.Invoke(this, e);
                }
                else if (eventName.CompareTo("__InstanceDeletionEvent") == 0)
                {
                    // Terminated
                    Terminated?.Invoke(this, e);

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }
}
