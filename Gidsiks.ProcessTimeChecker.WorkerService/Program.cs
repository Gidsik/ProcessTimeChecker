using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Gidsiks.ProcessTimeChecker.WorkerService.Services;
using SQLitePCL;


namespace Gidsiks.ProcessTimeChecker.WorkerService;

public class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		IHost host = Host.CreateDefaultBuilder(args)
			.ConfigureServices(services =>
			{
				services.AddSqlite<PTCheckerDbContext>("FileName=PTChecker.db");
				//services.AddDbContext<PTCheckerDbContext>();

				services.AddHostedService<ProcessTimeWatcherService>();
			})
			.UseWindowsService()
			.Build();

		host.Run();
	}
}
