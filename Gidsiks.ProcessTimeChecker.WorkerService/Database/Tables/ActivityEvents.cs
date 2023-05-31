using Gidsiks.ProcessTimeChecker.WorkerService.Types;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables
{
    public class ActivityEvents
    {
        public int Id { get; set; }
		public EventType EventType { get; set; }
		public DateTime Timestamp { get; set; }
	}
}