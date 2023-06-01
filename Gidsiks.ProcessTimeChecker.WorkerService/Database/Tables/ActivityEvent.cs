using Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.Types;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables
{
    public class ActivityEvent
    {
        public int Id { get; set; }
		public EventType EventType { get; set; }
		public DateTime Timestamp { get; set; }
	}
}