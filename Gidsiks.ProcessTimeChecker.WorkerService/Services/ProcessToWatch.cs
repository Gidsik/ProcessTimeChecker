using Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Services
{
	public class ProcessToWatch
	{
		//handles
		private Microsoft.Win32.SafeHandles.SafeProcessHandle? _safeHandle;
		private IntPtr? _handle;

		//mainProcess
		private Process? _mainProcess;

		//app info
		public string processName;
		public string? exePath;
		public bool isWatched;

		public bool isrunning = false;




	}
}
