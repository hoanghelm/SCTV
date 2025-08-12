using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class GetStreamStatisticsHandler : IRequestHandler<GetStreamStatisticsRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<GetStreamStatisticsHandler> _logger;

		public GetStreamStatisticsHandler(IUnitOfWork unitOfWork, ILogger<GetStreamStatisticsHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetStreamStatisticsRequest request, CancellationToken cancellationToken)
		{
			try
			{
				var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
				var toDate = request.ToDate ?? DateTime.UtcNow;

				Expression<Func<StreamSession, bool>> predicate =
					s => s.StartedAt >= fromDate && s.StartedAt <= toDate;

				if (request.CameraId.HasValue)
				{
					var cameraId = request.CameraId.Value;
					predicate = predicate.And(s => s.CameraId == cameraId);
				}

				var sessions = await _unitOfWork.GetRepository<StreamSession>()
					.GetListAsync(
						predicate: predicate,
						include: q => q.Include(s => s.Camera),
						cancellationToken: cancellationToken);

				var totalSessions = sessions.Count;
				var activeSessions = sessions.Count(s => s.Status == StreamSessionStatus.Active.ToString());
				var endedSessions = sessions.Count(s => s.Status == StreamSessionStatus.Ended.ToString());
				var failedSessions = sessions.Count(s => s.Status == StreamSessionStatus.Failed.ToString());

				var averageDuration = sessions
					.Where(s => s.Duration.HasValue)
					.Select(s => s.Duration.Value.TotalMinutes)
					.DefaultIfEmpty(0)
					.Average();

				var totalBytesTransferred = sessions
					.Where(s => s.BytesTransferred.HasValue)
					.Sum(s => s.BytesTransferred.Value);

				var uniqueViewers = sessions.Select(s => s.ViewerId).Distinct().Count();

				var cameraStats = sessions
					.GroupBy(s => new { s.CameraId, s.Camera.Name })
					.Select(g => new
					{
						CameraId = g.Key.CameraId,
						CameraName = g.Key.Name,
						SessionCount = g.Count(),
						TotalDuration = g.Where(s => s.Duration.HasValue).Sum(s => s.Duration.Value.TotalMinutes),
						AverageDuration = g.Where(s => s.Duration.HasValue).Select(s => s.Duration.Value.TotalMinutes).DefaultIfEmpty(0).Average()
					})
					.OrderByDescending(c => c.SessionCount)
					.Take(10)
					.ToList();

				var dailyStats = sessions
					.GroupBy(s => s.StartedAt.Date)
					.Select(g => new
					{
						Date = g.Key,
						SessionCount = g.Count(),
						UniqueViewers = g.Select(s => s.ViewerId).Distinct().Count(),
						TotalDuration = g.Where(s => s.Duration.HasValue).Sum(s => s.Duration.Value.TotalMinutes)
					})
					.OrderBy(d => d.Date)
					.ToList();

				var result = new
				{
					Summary = new
					{
						TotalSessions = totalSessions,
						ActiveSessions = activeSessions,
						EndedSessions = endedSessions,
						FailedSessions = failedSessions,
						AverageDurationMinutes = Math.Round(averageDuration, 2),
						TotalBytesTransferred = totalBytesTransferred,
						UniqueViewers = uniqueViewers,
						DateRange = new { From = fromDate, To = toDate }
					},
					TopCameras = cameraStats,
					DailyStats = dailyStats
				};

				return ApiResult.Succeeded(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting stream statistics");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting stream statistics");
			}
		}
	}
}
