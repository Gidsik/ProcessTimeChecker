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
using System.Runtime.InteropServices;

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

        public static int NumberOfExemplars(string appName)
        {

            string queryString =
                   $"SELECT Name " +
                   "  FROM Win32_Process" +
                   $"  WHERE Name = '{appName}'";

            SelectQuery query = new SelectQuery(queryString);
            ManagementScope scope = new System.Management.ManagementScope(@"\\.\root\CIMV2");

            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

            Debug.WriteLine(searcher.Get().Count);

            return searcher.Get().Count;
        }

        public static Process GetParentOf(int id)
        {
            try
            {
                string queryString =
                   $"SELECT * " +
                   "  FROM Win32_Process" +
                   $"  WHERE ProcessId = '{id}'";

                SelectQuery query = new SelectQuery(queryString);
                ManagementScope scope = new System.Management.ManagementScope(@"\\.\root\CIMV2");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);

                return searcher
                    .Get()
                    .OfType<ManagementObject>()
                    .Select(p => Process.GetProcessById((int)(uint)p["ParentProcessId"]))
                    .FirstOrDefault();
            }
            catch(ManagementException ex)
            {
                Debug.WriteLine($"exeption at ProcessInfoWatcher.ParentOf() |source:{ex.Source} |message:{ex.Message}");
                return null;
            }
        }

        // More perfomanced and more old way to get parent mainProcess
        //struct to hold NT info about parent mainProcess of some other mainProcess
        [StructLayout(LayoutKind.Sequential)]
        public struct ParentProcessUtilities
        {
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        // NT method to get info about mainProcess
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        // some magic...
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
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
