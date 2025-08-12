using AutoMapper;
using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class GetDetectionEventsHandler : IRequestHandler<GetDetectionEventsRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<GetDetectionEventsHandler> _logger;

		public GetDetectionEventsHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetDetectionEventsHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetDetectionEventsRequest request, CancellationToken cancellationToken)
		{
			try
			{
				System.Linq.Expressions.Expression<System.Func<DetectionEvent, bool>> predicate = d => true;

				// Filter by camera if specified
				if (request.CameraId.HasValue)
				{
					predicate = d => d.CameraId == request.CameraId.Value;
				}

				// Filter by event type
				if (!string.IsNullOrEmpty(request.EventType))
				{
					var currentPredicate = predicate;
					predicate = d => currentPredicate.Compile()(d) && d.EventType == request.EventType;
				}

				// Filter by date range
				if (request.FromDate.HasValue)
				{
					var currentPredicate = predicate;
					predicate = d => currentPredicate.Compile()(d) && d.EventTimestamp >= request.FromDate.Value;
				}

				if (request.ToDate.HasValue)
				{
					var currentPredicate = predicate;
					predicate = d => currentPredicate.Compile()(d) && d.EventTimestamp <= request.ToDate.Value;
				}

				// Filter by confidence
				if (request.MinConfidence.HasValue)
				{
					var currentPredicate = predicate;
					predicate = d => currentPredicate.Compile()(d) && d.Confidence >= request.MinConfidence.Value;
				}

				// TODO: Add user permission filtering based on request.UserId

				var events = await _unitOfWork.GetRepository<DetectionEvent>()
					.GetPagingListAsync(
						predicate: predicate,
						orderBy: q => q.OrderByDescending(d => d.EventTimestamp),
						include: q => q.Include(d => d.Camera),
						page: request.Page,
						size: request.PageSize,
						cancellationToken: cancellationToken);

				var viewModels = events.Items.Select(e => new
				{
					e.Id,
					e.CameraId,
					CameraName = e.Camera.Name,
					e.EventType,
					e.EventTimestamp,
					e.Confidence,
					e.DetectionData,
					e.FrameImageUrl,
					e.VideoClipUrl,
					e.AlertTriggered
				}).ToList();

				return ApiResult.Succeeded(new
				{
					items = viewModels,
					totalCount = events.Total,
					page = events.Page,
					pageSize = events.Size
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting detection events");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting detection events");
			}
		}
	}
}