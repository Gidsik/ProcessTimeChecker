using Gidsiks.ProcessTimeChecker.InterfaceContractLibrary;
using Gidsiks.ProcessTimeChecker.WorkerService.Database;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gidsiks.ProcessTimeChecker.WorkerService.Services
{
	internal class ProcessTimeCheckerService : Gidsiks.ProcessTimeChecker.InterfaceContractLibrary.ProcessTimeCheckerService.ProcessTimeCheckerServiceBase
	{
		PTCheckerDbContext _dbContext;

		public ProcessTimeCheckerService(PTCheckerDbContext dbContext)
		{
			_dbContext = dbContext;
		}

		public override Task<Empty> AddWatchedApp(Empty request, ServerCallContext context)
		{
			return base.AddWatchedApp(request, context);
		}

		public override Task<Empty> DeleteWatchedApp(Empty request, ServerCallContext context)
		{
			return base.DeleteWatchedApp(request, context);
		}

		public override Task<Empty> UpdateWatchedApp(Empty request, ServerCallContext context)
		{
			return base.UpdateWatchedApp(request, context);
		}
		public override Task<Empty> GetWatchedAppList(Empty request, ServerCallContext context)
		{
			return base.GetWatchedAppList(request, context);
		}


		public override Task<Empty> GetWatchedAppEvents(Empty request, ServerCallContext context)
		{
			return base.GetWatchedAppEvents(request, context);
		}



		public override Task<GetActivityEventsResponse> GetActivityEventsFromDate(GetActivityEventsFromDateRequest request, ServerCallContext context)
		{
			var fromTime = request.FromTime;
			var toTime = request.ToTime;

			if (toTime < fromTime)
			{
				throw new ArgumentException();
			}

			var set = _dbContext.ActivityEvents
				.Where(x => Timestamp.FromDateTime(x.Timestamp) > fromTime && Timestamp.FromDateTime(x.Timestamp) < toTime)
				.Select(x => new ActivityEvent()
				{
					Id = x.Id,
					EventType = (int)x.EventType,
					Timestamp = Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())
				});

			var response = new GetActivityEventsResponse();
			response.Events.AddRange(set);

			return Task.FromResult(response);
		}

		public override Task<GetActivityEventsResponse> GetActivityEventsLast(GetActivityEventsLastRequest request, ServerCallContext context)
		{
			var count = (request.Count >= 1) ? request.Count : 100;
			var lastId = _dbContext.ActivityEvents.OrderByDescending(x => x.Id).First().Id;
			var fromId = request.FromId >=1 && request.FromId <= lastId ? request.FromId : lastId;

			var set = _dbContext.ActivityEvents
				.OrderByDescending(x => x.Id)
				.Where(x => x.Id <= fromId).Take(count)
				.Select(x => new ActivityEvent()
				{
					Id = x.Id,
					EventType = (int)x.EventType,
					Timestamp = Timestamp.FromDateTime(x.Timestamp.ToUniversalTime())
				});

			var response = new GetActivityEventsResponse();
			response.Events.AddRange(set);

			return Task.FromResult(response);
		}

	}
}
