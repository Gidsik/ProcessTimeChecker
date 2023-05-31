using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.Types;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using static User32Helper;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Services
{
	public class UserActivityHandlerService : BackgroundService
	{
		private readonly ILogger<UserActivityHandlerService> _logger;

		private readonly IServiceProvider _serviceProvider;

		private IntPtr _hookHandle;
		private WinEventDelegate _winEvent;

		public UserActivityHandlerService(ILogger<UserActivityHandlerService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("{name} Starting", nameof(UserActivityHandlerService));

			using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				dbContext.Database.EnsureCreated();

				dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
				{
					EventType = IsUserNowIdle() ? EventType.IdleStarted : EventType.ActivityStarted,
					Timestamp = DateTime.UtcNow,
				});

				dbContext.WatchedApp.Where(p => p.IsWatched).ToList()
					.ForEach(p =>
					{

					});
				dbContext.SaveChanges();
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
			_logger.LogInformation("{name} Stopping", nameof(UserActivityHandlerService));

			UnhookWinEvent(_hookHandle);

			using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				var activityEvent = dbContext.ActivityEvents.OrderBy(x => x.Id).Last();
				dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
				{
					EventType = activityEvent.EventType == EventType.IdleStarted ? EventType.IdleStopped : EventType.ActivityStopped,
					Timestamp = DateTime.UtcNow,
				});
				dbContext.SaveChanges();
			}

			return base.StopAsync(cancellationToken);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("{name} Has Started", nameof(UserActivityHandlerService));
			while (!stoppingToken.IsCancellationRequested)
			{
				CheckIfActivityStateHasChanged();

				await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
			}

			void CheckIfActivityStateHasChanged()
			{
				using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
				{
					var lastActivityEventType = dbContext.ActivityEvents.OrderBy(x => x.Id).Last().EventType;
					if (lastActivityEventType == EventType.ActivityStarted && IsUserNowIdle())
					{
						_logger.LogInformation("Activity state changed to Idle");
						dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = EventType.ActivityStopped,
							Timestamp = DateTime.UtcNow,
						});
						dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = EventType.IdleStarted,
							Timestamp = DateTime.UtcNow,
						});
					}
					else if (lastActivityEventType == EventType.IdleStarted && !IsUserNowIdle())
					{
						_logger.LogInformation("Activity state changed to Active");
						dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = EventType.IdleStopped,
							Timestamp = DateTime.UtcNow,
						});
						dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvents()
						{
							EventType = EventType.ActivityStarted,
							Timestamp = DateTime.UtcNow,
						});
					}
					else
					{
					}
					dbContext.SaveChanges();
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
			GetWindowThreadProcessId(hForegroundWindow, out var processHandle);

			_logger.LogInformation("Window {hForegroundWindow} owned by {processHandle} | {hWinEventHook}", processHandle, hForegroundWindow, hWinEventHook);
		}
	}
}