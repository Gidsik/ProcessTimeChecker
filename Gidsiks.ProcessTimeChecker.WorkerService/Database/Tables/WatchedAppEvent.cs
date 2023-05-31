using Gidsiks.ProcessTimeChecker.WorkerService.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables
{
	public class WatchedAppEvent
	{
		public int Id { get; set; }
		public int WatchedAppId { get; set; }
		public WatchedApp WatchedApp { get; set; } = new WatchedApp();
		public EventType EventType { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
