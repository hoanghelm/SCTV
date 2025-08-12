using AutoMapper;
using Common.ApiResponse;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Queries;
using Streaming.Service.ViewModels;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class GetCameraByIdHandler : IRequestHandler<GetCameraByIdQuery, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<GetCameraByIdHandler> _logger;

		public GetCameraByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetCameraByIdHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(GetCameraByIdQuery request, CancellationToken cancellationToken)
		{
			try
			{
				var camera = await _unitOfWork.GetRepository<Camera>()
					.SingleOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken: cancellationToken);

				return ApiResult.Succeeded(camera != null ? _mapper.Map<CameraViewModel>(camera) : null);
			}
			catch (System.Exception ex)
			{
				_logger.LogError(ex, $"Error getting camera {request.CameraId}");
				return ApiResult.Succeeded();
			}
		}
	}
}