using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.Types;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;
using static User32Helper;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using Gidsiks.ProcessTimeChecker.WorkerService.Database.Tables;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Services
{
	public class UserActivityHandlerService : BackgroundService
	{
		private readonly ILogger<UserActivityHandlerService> _logger;

		private readonly IServiceProvider _serviceProvider;

		private readonly WinEventDelegate _winEvent;
		private IntPtr _hookHandle;

		public UserActivityHandlerService(ILogger<UserActivityHandlerService> logger, IServiceProvider serviceProvider)
		{
			_logger = logger;
			_serviceProvider = serviceProvider;
			_winEvent = new WinEventDelegate(WinEventProc);
		}

		public override Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("{name} Starting", nameof(UserActivityHandlerService));

			using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				if (dbContext is null) throw new ArgumentNullException(nameof(dbContext),"DI returned null for dbContext");
				dbContext.Database.EnsureCreated();

				dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
				{
					EventType = IsUserNowIdle() ? EventType.IdleStarted : EventType.ActivityStarted,
					Timestamp = DateTime.UtcNow,
				});
				_logger.LogInformation("New Activity Started");

				var foregroundWindow = GetForegroundWindow();
				_ = GetWindowThreadProcessId(foregroundWindow, out var processId);
				var process = Process.GetProcessById(processId);
				_logger.LogInformation("Window entered for {processName}", process.ProcessName);

				// track new window entered
				var watchedApp = GetOrCreateWatchedAppForProcess(process, dbContext);
				dbContext.WatchedAppEvents.Add(new()
				{
					EventType = EventType.WindowEntered,
					Timestamp = DateTime.UtcNow,
					WatchedAppId = watchedApp.Id,
					WatchedApp = watchedApp,
				});

				dbContext.SaveChanges();
			}

			_hookHandle = SetWinEventHook(
				WinEventConstants.EVENT_SYSTEM_FOREGROUND, //WinEventConstants.EVENT_MIN , 
				WinEventConstants.EVENT_SYSTEM_FOREGROUND, //WinEventConstants.EVENT_MAX , 
				IntPtr.Zero,
				_winEvent,
				0, 0,
				WinEventHookFlags.WINEVENT_OUTOFCONTEXT);

			_logger.LogInformation("{name} Started", nameof(UserActivityHandlerService));
			return base.StartAsync(cancellationToken);
		}

		public override Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("{name} Stopping", nameof(UserActivityHandlerService));

			UnhookWinEvent(_hookHandle);

			using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>())
			{
				if (dbContext is null) throw new ArgumentNullException(nameof(dbContext), "DI returned null for dbContext");

				var activityEvent = dbContext.ActivityEvents.OrderBy(x => x.Id).Last();
				dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
				{
					EventType = activityEvent.EventType == EventType.IdleStarted ? EventType.IdleStopped : EventType.ActivityStopped,
					Timestamp = DateTime.UtcNow,
				});
				_logger.LogInformation("Activity state changed to stopped");

				var watchedAppEvent = dbContext.WatchedAppEvents.OrderBy(x => x.Id).Last();
				if (watchedAppEvent.EventType == EventType.WindowEntered)
				{
					dbContext.WatchedAppEvents.Add(new WatchedAppEvent()
					{
						EventType = EventType.WindowLeaved,
						Timestamp = DateTime.UtcNow,
						WatchedAppId = watchedAppEvent.WatchedAppId,
						WatchedApp = watchedAppEvent.WatchedApp,
					});
				}
				_logger.LogInformation("Finalized last foreground window entrance");

				dbContext.SaveChanges();
			}

			_logger.LogInformation("{name} Stopped", nameof(UserActivityHandlerService));
			return base.StopAsync(cancellationToken);
		}

		/// <summary>
		/// Checks and tracks user activity
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				CheckIfActivityStateHasChanged();

				await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
			}

			void CheckIfActivityStateHasChanged()
			{
				using var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>();
				if (dbContext is null) throw new ArgumentNullException(nameof(dbContext), "DI returned null for dbContext");
				var lastActivityEventType = dbContext.ActivityEvents.OrderBy(x => x.Id).Last().EventType;
				if (lastActivityEventType == EventType.ActivityStarted && IsUserNowIdle())
				{
					_logger.LogInformation("Activity state changed to Idle");
					dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
					{
						EventType = EventType.ActivityStopped,
						Timestamp = DateTime.UtcNow,
					});
					dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
					{
						EventType = EventType.IdleStarted,
						Timestamp = DateTime.UtcNow,
					});
				}
				else if (lastActivityEventType == EventType.IdleStarted && !IsUserNowIdle())
				{
					_logger.LogInformation("Activity state changed to Active");
					dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
					{
						EventType = EventType.IdleStopped,
						Timestamp = DateTime.UtcNow,
					});
					dbContext.ActivityEvents.Add(new Database.Tables.ActivityEvent()
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


		private bool IsUserNowIdle() => GetCurrentIdleTime() >= TimeSpan.FromSeconds(120);

		private TimeSpan GetCurrentIdleTime()
		{
			var lastInput = new LASTINPUTINFO();
			lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
			if (!GetLastInputInfo(ref lastInput))
			{
				_logger.LogWarning("Can't get users last input info to calculate idle time");
				return TimeSpan.Zero;
			}

			var timeIddling = TimeSpan.FromMilliseconds(Environment.TickCount - lastInput.dwTime);
			if (timeIddling.Duration() > TimeSpan.FromMinutes(2))
			{
				_logger.LogTrace("User is in idle state for {timeIddling}", timeIddling);
			}
			return timeIddling;
		}


		/// <summary>
		/// Handles foreground window change and tracks it to db
		/// </summary>
		public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hForegroundWindow, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
		{
			_ = GetWindowThreadProcessId(hForegroundWindow, out var processId);
			var process = Process.GetProcessById(processId);
			_logger.LogDebug("Window {hForegroundWindow} owned by {processId} | {processName}", hForegroundWindow, processId, process.ProcessName);

			using var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<PTCheckerDbContext>();
			if (dbContext is null) throw new ArgumentNullException(nameof(dbContext), "DI returned null for dbContext");

			WatchedApp watchedApp = GetOrCreateWatchedAppForProcess(process, dbContext);
			var lastEvent = dbContext.WatchedAppEvents.OrderBy(x => x.Id).LastOrDefault();

			if (lastEvent is not null && lastEvent.EventType == EventType.WindowEntered && lastEvent.WatchedAppId != watchedApp.Id)
			{
				// track last window leaved
				dbContext.WatchedAppEvents.Add(new()
				{
					EventType = EventType.WindowLeaved,
					Timestamp = DateTime.UtcNow,
					WatchedAppId = lastEvent.WatchedAppId,
					WatchedApp = lastEvent.WatchedApp,
				});
				_logger.LogInformation("Window leaved for {processName}", lastEvent.WatchedApp?.ProcessName);

				dbContext.SaveChanges();

				// track new window entered
				dbContext.WatchedAppEvents.Add(new()
				{
					EventType = EventType.WindowEntered,
					Timestamp = DateTime.UtcNow,
					WatchedAppId = watchedApp.Id,
					WatchedApp = watchedApp,
				});
				_logger.LogInformation("Window entered for {processName}", process.ProcessName);
			}

			dbContext.SaveChanges();
		}

		private WatchedApp GetOrCreateWatchedAppForProcess(Process process, PTCheckerDbContext dbContext)
		{
			var watchedApp = dbContext.WatchedApps.Where(x => x.ProcessName == process.ProcessName).FirstOrDefault();

			if (watchedApp is null)
			{
				watchedApp = new WatchedApp()
				{
					AppName = process.ProcessName,
					ProcessName = process.ProcessName,
					IsWatched = true,
				};
				string exePath = "";
				try
				{
					exePath = process.MainModule!.FileName!;
				}catch(Exception ex)
				{
					_logger.LogWarning("Error retrieving exePath of {processName}. {ex}", process.ProcessName, ex.Message);
				}
				finally
				{
					watchedApp.ExePath = exePath;
				}
				dbContext.WatchedApps.Add(watchedApp);
				dbContext.SaveChanges();
			}

			return watchedApp;
		}
	}
}
