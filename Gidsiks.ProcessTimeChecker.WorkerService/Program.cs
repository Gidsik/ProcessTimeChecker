using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Gidsiks.ProcessTimeChecker.WorkerService.Services;


namespace Gidsiks.ProcessTimeChecker.WorkerService;

public class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddSqlite<PTCheckerDbContext>("FileName=PTChecker.db");
		builder.Services.AddHostedService<UserActivityHandlerService>();
		builder.Services.AddGrpc();
		builder.Services.AddWindowsService();

		builder.WebHost.ConfigureKestrel(options =>
		{
			options.ListenLocalhost(28550, listenOptions =>
			{
				listenOptions.Protocols = HttpProtocols.Http2;
			});
		});
		WebApplication app = builder.Build();

		app.MapGrpcService<ProcessTimeCheckerService>();

		app.Run();
	}
}
