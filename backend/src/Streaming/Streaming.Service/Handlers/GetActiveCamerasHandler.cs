using AutoMapper;
using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
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
	public class GetActiveCamerasHandler : IRequestHandler<GetActiveCamerasQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<GetActiveCamerasHandler> _logger;

		public GetActiveCamerasHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetActiveCamerasHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetActiveCamerasQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var cameras = await _unitOfWork.GetRepository<Camera>()
					.GetListAsync(
						predicate: c => c.Status == CameraStatus.Active.ToString(),
						orderBy: q => q.OrderBy(c => c.Name),
						cancellationToken: cancellationToken);

				var viewModels = cameras.Select(c => _mapper.Map<ViewModels.CameraViewModel>(c)).ToList();

				return ApiResult.Succeeded(viewModels);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting active cameras");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error getting active cameras");
			}
		}
	}
}