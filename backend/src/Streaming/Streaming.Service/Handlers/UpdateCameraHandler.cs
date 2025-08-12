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
	public class UpdateCameraHandler : IRequestHandler<UpdateCameraRequest, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<UpdateCameraHandler> _logger;

		public UpdateCameraHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateCameraHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(UpdateCameraRequest request, CancellationToken cancellationToken)
		{
			try
			{
				var camera = await _unitOfWork.GetRepository<Camera>()
					.SingleOrDefaultAsync(c => c.Id == request.CameraId, cancellationToken: cancellationToken);

				if (camera == null)
				{
					return ApiResult.Failed(HttpCode.Notfound, "Camera not found");
				}

				// Update only provided fields
				if (!string.IsNullOrEmpty(request.Name)) camera.Name = request.Name;
				if (!string.IsNullOrEmpty(request.Description)) camera.Description = request.Description;
				if (!string.IsNullOrEmpty(request.StreamUrl)) camera.StreamUrl = request.StreamUrl;
				if (!string.IsNullOrEmpty(request.Location)) camera.Location = request.Location;
				if (request.Latitude.HasValue) camera.Latitude = request.Latitude;
				if (request.Longitude.HasValue) camera.Longitude = request.Longitude;
				if (!string.IsNullOrEmpty(request.CameraType)) camera.CameraType = request.CameraType;
				if (!string.IsNullOrEmpty(request.Brand)) camera.Brand = request.Brand;
				if (!string.IsNullOrEmpty(request.Model)) camera.Model = request.Model;
				if (!string.IsNullOrEmpty(request.Resolution)) camera.Resolution = request.Resolution;
				if (request.HasAudio.HasValue) camera.HasAudio = request.HasAudio.Value;
				if (request.HasPTZ.HasValue) camera.HasPTZ = request.HasPTZ.Value;
				if (request.HasNightVision.HasValue) camera.HasNightVision = request.HasNightVision.Value;
				if (request.HasMotionDetection.HasValue) camera.HasMotionDetection = request.HasMotionDetection.Value;
				if (!string.IsNullOrEmpty(request.Status)) camera.Status = request.Status;

				camera.UpdatedBy = request.UpdatedBy;

				_unitOfWork.GetRepository<Camera>().Update(camera);
				await _unitOfWork.CommitAsync();

				_logger.LogInformation($"Camera {camera.Id} updated successfully");

				var cameraViewModel = _mapper.Map<ViewModels.CameraViewModel>(camera);
				return ApiResult.Succeeded(cameraViewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, "Error updating camera");
			}
		}
	}
}