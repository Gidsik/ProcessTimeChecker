using Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.Types;
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
		public virtual WatchedApp? WatchedApp { get; set; }
		public EventType EventType { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
