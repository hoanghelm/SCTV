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
	public class GetStreamSessionsHandler : IRequestHandler<GetStreamSessionsRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<GetStreamSessionsHandler> _logger;

		public GetStreamSessionsHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetStreamSessionsHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetStreamSessionsRequest request, CancellationToken cancellationToken)
		{
			try
			{
				System.Linq.Expressions.Expression<System.Func<StreamSession, bool>> predicate = s => s.CameraId == request.CameraId;

				if (request.FromDate.HasValue)
				{
					var currentPredicate = predicate;
					predicate = s => currentPredicate.Compile()(s) && s.StartedAt >= request.FromDate.Value;
				}

				if (request.ToDate.HasValue)
				{
					var currentPredicate = predicate;
					predicate = s => currentPredicate.Compile()(s) && s.StartedAt <= request.ToDate.Value;
				}

				if (!string.IsNullOrEmpty(request.Status))
				{
					var currentPredicate = predicate;
					predicate = s => currentPredicate.Compile()(s) && s.Status == request.Status;
				}

				var sessions = await _unitOfWork.GetRepository<StreamSession>()
					.GetPagingListAsync(
						predicate: predicate,
						orderBy: q => q.OrderByDescending(s => s.StartedAt),
						include: q => q.Include(s => s.Camera),
						page: request.Page,
						size: request.PageSize,
						cancellationToken: cancellationToken);

				var viewModels = sessions.Items.Select(s => _mapper.Map<ViewModels.StreamSessionViewModel>(s)).ToList();

				return ApiResult.Succeeded(new
				{
					items = viewModels,
					totalCount = sessions.Total,
					page = sessions.Page,
					pageSize = sessions.Size
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting stream sessions for camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting stream sessions");
			}
		}
	}
}