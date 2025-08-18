using Common.ApiResponse;
using Common.ErrorResult;
using MediatR;
using Microsoft.Extensions.Logging;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using Streaming.Service.Commands;
using System;
using System.Linq;
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
				StreamSession session = null;
				
				if (string.IsNullOrEmpty(request.ViewerId))
				{
					var sessions = await _unitOfWork.GetRepository<StreamSession>()
						.GetListAsync(
							predicate: s => s.CameraId == request.CameraId &&
								 s.Status == StreamSessionStatus.Active.ToString(),
							asNoTracking: false,
							cancellationToken: cancellationToken);

					if (sessions.Any())
					{
						_logger.LogInformation($"Ending {sessions.Count()} active session(s) for camera {request.CameraId} (no user ID provided)");
						
						foreach (var sess in sessions)
						{
							sess.EndedAt = DateTime.UtcNow;
							sess.Status = StreamSessionStatus.Ended.ToString();
							sess.Duration = sess.EndedAt - sess.StartedAt;
							sess.BytesTransferred = request.BytesTransferred;
							sess.FramesSent = request.FramesSent;
						}

						_unitOfWork.GetRepository<StreamSession>().Update(sessions);
						await _unitOfWork.CommitAsync();

						_logger.LogInformation($"Ended {sessions.Count()} stream sessions for camera {request.CameraId}");
						return ApiResult.Succeeded(true);
					}
					else
					{
						_logger.LogInformation($"No active stream sessions found for camera {request.CameraId}");
						return ApiResult.Succeeded(true);
					}
				}
				else
				{
					session = await _unitOfWork.GetRepository<StreamSession>()
						.SingleOrDefaultAsync(
							s => s.CameraId == request.CameraId &&
								 s.ViewerId == request.ViewerId &&
								 s.Status == StreamSessionStatus.Active.ToString(),
							asNoTracking: false,
							cancellationToken: cancellationToken);
				}

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