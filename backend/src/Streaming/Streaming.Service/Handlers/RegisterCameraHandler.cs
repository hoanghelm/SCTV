using AutoMapper;
using Common.ApiResponse;
using Common.Constants;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Streaming.Service.Handlers
{
	public class RegisterCameraHandler : IRequestHandler<RegisterCameraRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<RegisterCameraHandler> _logger;

		public RegisterCameraHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<RegisterCameraHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(RegisterCameraRequest request, CancellationToken cancellationToken)
		{
			try
			{
				var camera = new Camera
				{
					Id = Guid.NewGuid(),
					Name = request.Name,
					Description = request.Description ?? string.Empty,
					StreamUrl = request.StreamUrl,
					Location = request.Location ?? string.Empty,
					Latitude = request.Latitude,
					Longitude = request.Longitude,
					CameraType = request.CameraType ?? "Fixed",
					Brand = request.Brand ?? string.Empty,
					Model = request.Model ?? string.Empty,
					Resolution = request.Resolution ?? "1280x720",
					HasAudio = request.HasAudio,
					HasPTZ = request.HasPTZ,
					HasNightVision = request.HasNightVision,
					HasMotionDetection = request.HasMotionDetection,
					Status = CameraStatus.Active.ToString(),
					IsOnline = false,
					CreatedAt = DateTime.UtcNow,
					CreatedBy = request.CreatedBy ?? "System"
				};

				await _unitOfWork.GetRepository<Camera>().InsertAsync(camera, cancellationToken);
				await _unitOfWork.CommitAsync();

				return ApiResult.Succeeded(new { Id = camera.Id, Name = camera.Name });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error registering camera");
				return ApiResult.Failed(HttpCode.InternalServerError, ex.Message);
			}
		}
	}
}