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
				// Check if camera with same name already exists
				var existingCamera = await _unitOfWork.GetRepository<Camera>()
					.SingleOrDefaultAsync(c => c.Name == request.Name, cancellationToken: cancellationToken);

				if (existingCamera != null)
				{
					return ApiResult.Failed(HttpCode.BadRequest, "Camera with this name already exists");
				}

				var camera = new Camera
				{
					Id = Guid.NewGuid(),
					Name = request.Name,
					Description = request.Description,
					StreamUrl = request.StreamUrl,
					Location = request.Location,
					Latitude = request.Latitude,
					Longitude = request.Longitude,
					CameraType = request.CameraType,
					Brand = request.Brand,
					Model = request.Model,
					Resolution = request.Resolution,
					HasAudio = request.HasAudio,
					HasPTZ = request.HasPTZ,
					HasNightVision = request.HasNightVision,
					HasMotionDetection = request.HasMotionDetection,
					Status = CameraStatus.Active.ToString(),
					IsOnline = false,
					CreatedBy = request.CreatedBy
				};

				await _unitOfWork.GetRepository<Camera>().InsertAsync(camera, cancellationToken);
				await _unitOfWork.CommitAsync();

				_logger.LogInformation($"Camera {camera.Name} registered successfully with ID {camera.Id}");

				var cameraViewModel = _mapper.Map<ViewModels.CameraViewModel>(camera);
				return ApiResult.Succeeded(cameraViewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error registering camera");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error registering camera");
			}
		}
	}
}