using Common.ApiResponse;
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
	public class CreateStreamSessionHandler : IRequestHandler<CreateStreamSessionCommand, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<CreateStreamSessionHandler> _logger;

		public CreateStreamSessionHandler(IUnitOfWork unitOfWork, ILogger<CreateStreamSessionHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(CreateStreamSessionCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var session = new StreamSession
				{
					Id = Guid.NewGuid(),
					CameraId = request.CameraId,
					ViewerId = request.ViewerId,
					ConnectionId = request.ConnectionId,
					SessionDescription = request.SessionDescription,
					StartedAt = DateTime.UtcNow,
					Status = StreamSessionStatus.Active.ToString(),
					UserAgent = request.UserAgent,
					IpAddress = request.IpAddress
				};

				await _unitOfWork.GetRepository<StreamSession>().InsertAsync(session, cancellationToken);
				await _unitOfWork.CommitAsync();

				_logger.LogInformation($"Created stream session {session.Id} for camera {request.CameraId} and user {request.ViewerId}");

				return ApiResult.Succeeded(session.Id);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error creating stream session for camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError, ex.Message);
			}
		}
	}
}