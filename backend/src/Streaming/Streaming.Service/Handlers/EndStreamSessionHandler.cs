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
	public class EndStreamSessionHandler : IRequestHandler<EndStreamSessionCommand, ApiResult>
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<EndStreamSessionHandler> _logger;

		public EndStreamSessionHandler(IUnitOfWork unitOfWork, ILogger<EndStreamSessionHandler> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task<ApiResult> Handle(EndStreamSessionCommand request, CancellationToken cancellationToken)
		{
			try
			{
				var session = await _unitOfWork.GetRepository<StreamSession>()
					.SingleOrDefaultAsync(
						s => s.CameraId == request.CameraId &&
							 s.ViewerId == request.ViewerId &&
							 s.Status == StreamSessionStatus.Active.ToString(),
						cancellationToken: cancellationToken);

				if (session == null)
				{
					var msg = $"Active stream session not found for camera {request.CameraId} and user {request.ViewerId}";
					_logger.LogWarning(msg);
					return ApiResult.Failed(HttpCode.BadRequest, msg);
				}

				session.EndedAt = DateTime.UtcNow;
				session.Status = StreamSessionStatus.Ended.ToString();
				session.Duration = session.EndedAt - session.StartedAt;
				session.BytesTransferred = request.BytesTransferred;
				session.FramesSent = request.FramesSent;

				_unitOfWork.GetRepository<StreamSession>().Update(session);
				await _unitOfWork.CommitAsync();

				_logger.LogInformation($"Ended stream session {session.Id}");

				return ApiResult.Succeeded(true);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error ending stream session for camera {request.CameraId}");
				return ApiResult.Failed(HttpCode.InternalServerError);
			}
		}
	}
}