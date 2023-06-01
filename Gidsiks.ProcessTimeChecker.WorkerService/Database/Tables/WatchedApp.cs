
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables
{
	public class WatchedApp
	{
		public int Id { get; set; }
		public string? AppName { get; set; }
		public string? ProcessName { get; set; }
		public string? ExePath { get; set; }
		public bool IsWatched { get; set; }

		public virtual List<WatchedAppEvent>? WatchedAppEvents { get; set; }
	}
}
