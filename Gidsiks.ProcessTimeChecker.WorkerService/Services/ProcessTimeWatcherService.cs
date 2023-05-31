using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using static User32Helper;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Services
{
	public class ProcessTimeWatcherService : BackgroundService
	{
		private readonly ILogger<ProcessTimeWatcherService> _logger;

		private readonly IServiceProvider _serviceProvider;

		private IntPtr _hookHandle;
		private WinEventDelegate _winEvent;

		public ProcessTimeWatcherService(ILogger<ProcessTimeWatcherService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("{name} Starting", nameof(ProcessTimeWatcherService));

			using (var dbConxtext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				dbConxtext.Database.EnsureCreated();

				dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
				{
					EventType = IsUserNowIdle() ? Types.EventType.IdleStarted : Types.EventType.ActivityStarted,
					Timestamp = DateTime.UtcNow,
				});

				dbConxtext.WatchedApp.Where(p => p.IsWatched).ToList()
					.ForEach(p =>
					{

					});
				dbConxtext.SaveChanges();
			}

			_winEvent = new WinEventDelegate(WinEventProc);
			_hookHandle = SetWinEventHook(
				WinEventConstants.EVENT_SYSTEM_FOREGROUND, //WinEventConstants.EVENT_MIN , 
				WinEventConstants.EVENT_SYSTEM_FOREGROUND, //WinEventConstants.EVENT_MAX , 
				IntPtr.Zero,
				_winEvent,
				0, 0,
				WinEventHookFlags.WINEVENT_OUTOFCONTEXT);

			return base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("{name} Stopping", nameof(ProcessTimeWatcherService));

			UnhookWinEvent(_hookHandle);

			using (var dbConxtext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				var activityEvent = dbConxtext.ActivityEvents.OrderBy(x => x.Id).Last();
				dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
				{
					EventType = activityEvent.EventType == Types.EventType.IdleStarted ? Types.EventType.IdleStopped : Types.EventType.ActivityStopped,
					Timestamp = DateTime.UtcNow,
				});
				dbConxtext.SaveChanges();
			}

			return base.StopAsync(cancellationToken);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("{name} Has Started", nameof(ProcessTimeWatcherService));
			while (!stoppingToken.IsCancellationRequested)
			{
				CheckIfActivityStateHasChanged();

				await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
			}

			void CheckIfActivityStateHasChanged()
			{
				using (var dbConxtext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
				{
					var lastActivityEventType = dbConxtext.ActivityEvents.OrderBy(x => x.Id).Last().EventType;
					if (lastActivityEventType == Types.EventType.ActivityStarted && IsUserNowIdle())
					{
						_logger.LogInformation("Activity state changed to Idle");
						dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = Types.EventType.ActivityStopped,
							Timestamp = DateTime.UtcNow,
						});
						dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = Types.EventType.IdleStarted,
							Timestamp = DateTime.UtcNow,
						});
					}
					else if (lastActivityEventType == Types.EventType.IdleStarted && !IsUserNowIdle())
					{
						_logger.LogInformation("Activity state changed to Active");
						dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = Types.EventType.IdleStopped,
							Timestamp = DateTime.UtcNow,
						});
						dbConxtext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = Types.EventType.ActivityStarted,
							Timestamp = DateTime.UtcNow,
						});
					}
					else
					{
					}
					dbConxtext.SaveChanges();
				}
			}
		}

		private bool IsUserNowIdle() => GetCurrentIdleTime() >= TimeSpan.FromSeconds(5);

		private TimeSpan GetCurrentIdleTime()
		{
			LASTINPUTINFO lastInput = new LASTINPUTINFO();
			lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
			if (!GetLastInputInfo(ref lastInput))
			{
				_logger.LogError("Can't get users last input info to calculate idle time");
				return TimeSpan.Zero;
			}

			var timeIddling = TimeSpan.FromMilliseconds(Environment.TickCount - lastInput.dwTime);
			if (timeIddling.Duration() > TimeSpan.FromMinutes(2))
			{
				_logger.LogWarning("User is in idle state for {timeIddling}", timeIddling);
			}
			return timeIddling;
		}


		/// <summary>
		/// Handles Foreground window change
		/// </summary>
		/// <param name="hForegroundWindow">handle of new foreground window</param>
		/// <param name="dwmsEventTime"></param>
		public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hForegroundWindow, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			//var fwindowHandle = GetForegroundWindow(); //handle of new foreground window
			//Int32 processHande;
			GetWindowThreadProcessId(hForegroundWindow, out var processHande);

			_logger.LogInformation("Window {hForegroundWindow} owned by {processHandle} | {hWinEventHook}", processHande, hForegroundWindow, hWinEventHook);
		}
	}
}