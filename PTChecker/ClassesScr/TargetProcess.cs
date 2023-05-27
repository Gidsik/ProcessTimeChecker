using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;

namespace PTChecker
{
    class TargetProcess
    {
        //handles
        private Microsoft.Win32.SafeHandles.SafeProcessHandle safeHandle;
        private IntPtr handle;

        //mainProcess
        public Process mainProcess;

        //info
        public string appName;
        public string exePath;
        public bool watched;

        public bool running = false;

        //watcher
        private ProcessInfoWatcher watcher;

        TargetProcess()
        {
        }

        public TargetProcess(string appName, string exePath, bool watched)
        {
            this.appName = appName;
            this.exePath = exePath;
            this.watched = watched;
            if (watched)
            {
                initWatcher();
            }
            //this.mainProcess.Exited += (object sender, EventArgs e) => { Debug.WriteLine("Exited started"); this.Close(); MessageBox.Show("pause"); Debug.WriteLine("Exited complete"); };
            
        }

        private void initWatcher()
        {
            watcher = new ProcessInfoWatcher(appName);
            watcher.Started += StartedEvent;
            watcher.Terminated += TerminatedEvent;
        }

        private void StartedEvent(object sender, System.Management.EventArrivedEventArgs e)
        {
            ManagementBaseObject eventProcess = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            setMainProcess((string)eventProcess.Properties["Handle"].Value);

            Debug.WriteLine($"{appName}:{mainProcess.Id} started[{mainProcess.StartTime.ToLongTimeString()}] ended[{(mainProcess.HasExited?mainProcess.ExitTime.ToLongTimeString():"--:--:--")}] was running[{(mainProcess.HasExited?(mainProcess.ExitTime - mainProcess.StartTime).ToString():"--:--:--.-------")}]");

            if (ProcessInfoWatcher.NumberOfExemplars(appName) < 2)
            {
                running = true;
                Debug.WriteLine("First one started");
            }



            //TODO check if that was first mainProcess of target app
        }

        private void TerminatedEvent(object sender, System.Management.EventArrivedEventArgs e)
        {
            ManagementBaseObject eventProcess = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            //setMainProcess((string)eventProcess.Properties["Handle"].Value);

            Debug.WriteLine($"{appName}:{mainProcess.Id} started[{mainProcess.StartTime.ToLongTimeString()}] ended[{(mainProcess.HasExited ? mainProcess.ExitTime.ToLongTimeString() : "--:--:--")}] was running[{(mainProcess.HasExited ? (mainProcess.ExitTime - mainProcess.StartTime).ToString() : "--:--:--.-------")}]");


            if (ProcessInfoWatcher.NumberOfExemplars(appName) < 1)
            {
                running = false;
                Debug.WriteLine("Last one closed");
            }
            else if ((int)eventProcess.Properties["Handle"].Value == mainProcess.Id)
            {
                Debug.WriteLine("Fuck, mainProcess is closed, but there is more exemplars of this app. Hope this case will never happen");
                //TODO take care of this case.
            }
            //TODO session commit to SQLiteDataBase
        }

        public void setMainProcess(string handle)
        {
            try
            {
                Process tempProcess;
                if (mainProcess == null)
                {
                    tempProcess = Process.GetProcessById(Int32.Parse(handle));
                    mainProcess = tempProcess;
                    safeHandle = mainProcess.SafeHandle;
                    this.handle = mainProcess.Handle;
                    return;
                }
                else
                {
                    try
                    {
                        tempProcess = Process.GetProcessById(Int32.Parse(handle));
                        var parentOfTempProcess = ProcessInfoWatcher.GetParentOf(tempProcess.Id);

                        if (parentOfTempProcess.ProcessName == tempProcess.ProcessName)
                        {
                            Debug.WriteLine($"parent >{parentOfTempProcess.ProcessName} == {tempProcess.ProcessName}");
                            return;
                        }
                        else
                        {
                            mainProcess = tempProcess;
                            safeHandle = mainProcess.SafeHandle;
                            this.handle = mainProcess.Handle;
                            return;
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Debug.WriteLine($"method:{e.TargetSite}  param:{e.ParamName}  message:{e.Message}");
                    }
                }

            }
            catch (ArgumentException e)
            {
                Debug.WriteLine($"method:{e.TargetSite}  param:{e.ParamName}  message:{e.Message}");
            }
            catch (NullReferenceException e)
            {
                Debug.WriteLine($"method:{e.TargetSite}  message:{e.Message}");
            }


        }
        public void Close()
        {

        }
    }
}
