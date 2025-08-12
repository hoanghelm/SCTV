using AutoMapper;
using Common.ApiResponse;
using Common.Extensions;
using Common.ErrorResult;
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
	public class GetCamerasHandler : IRequestHandler<GetCamerasRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<GetCamerasHandler> _logger;

		public GetCamerasHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetCamerasHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetCamerasRequest request, CancellationToken cancellationToken)
		{
			try
			{
				// Build predicate based on filters
				Expression<Func<Camera, bool>> predicate = c => true;

				if (!string.IsNullOrEmpty(request.Search))
				{
					predicate = c => c.Name.Contains(request.Search) || c.Location.Contains(request.Search);
				}

				if (!string.IsNullOrEmpty(request.Status))
				{
					var status = request.Status;
					predicate = predicate.And(c => c.Status == status);
				}

				if (!string.IsNullOrEmpty(request.CameraType))
				{
					var cameraType = request.CameraType;
					predicate = predicate.And(c => c.CameraType == cameraType);
				}

				if (!string.IsNullOrEmpty(request.Location))
				{
					var location = request.Location;
					predicate = predicate.And(c => c.Location.Contains(location));
				}

				// TODO: Add user permission filtering based on request.UserId

				Func<IQueryable<Camera>, IOrderedQueryable<Camera>> orderBy = null;

				switch (request.SortBy.ToLower())
				{
					case "name":
						orderBy = request.SortOrder.ToLower() == "desc"
							? q => q.OrderByDescending(c => c.Name)
							: q => q.OrderBy(c => c.Name);
						break;
					case "location":
						orderBy = request.SortOrder.ToLower() == "desc"
							? q => q.OrderByDescending(c => c.Location)
							: q => q.OrderBy(c => c.Location);
						break;
					case "createdat":
						orderBy = request.SortOrder.ToLower() == "desc"
							? q => q.OrderByDescending(c => c.CreatedAt)
							: q => q.OrderBy(c => c.CreatedAt);
						break;
					default:
						orderBy = q => q.OrderBy(c => c.Name);
						break;
				}

				var cameras = await _unitOfWork.GetRepository<Camera>()
					.GetPagingListAsync(
						predicate: predicate,
						orderBy: orderBy,
						page: request.Page,
						size: request.PageSize,
						cancellationToken: cancellationToken);

				var viewModels = cameras.Items.Select(c => _mapper.Map<ViewModels.CameraViewModel>(c)).ToList();

				return ApiResult.Succeeded(new
				{
					items = viewModels,
					totalCount = cameras.Total,
					page = cameras.Page,
					pageSize = cameras.Size
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting cameras");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting cameras");
			}
		}
	}
}