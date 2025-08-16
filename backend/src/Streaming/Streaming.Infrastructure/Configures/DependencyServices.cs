using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIPSorceryMedia.Abstractions;
using Streaming.Data.Context;
using Streaming.Data.EntityFramework;
using Streaming.Domain.Contracts;
using Streaming.Infrastructure.Mediators;
using Streaming.Service.Commands;
using Streaming.Service.Handlers;
using Streaming.Service.Queries;
using Streaming.Service.WebRTC;
using System.Reflection;

namespace Streaming.Infrastructure.Configures
{
	public static class DependencyServices
	{
		public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add WebRTC Configuration
			services.AddSingleton<WebRTCConfiguration>(provider =>
			{
				var config = new WebRTCConfiguration();

				// Read from environment variables with fallbacks
				if (int.TryParse(Environment.GetEnvironmentVariable("WEBRTC_VIDEO_WIDTH"), out int videoWidth))
					config.VideoWidth = videoWidth;
				else
					config.VideoWidth = 1280;

				if (int.TryParse(Environment.GetEnvironmentVariable("WEBRTC_VIDEO_HEIGHT"), out int videoHeight))
					config.VideoHeight = videoHeight;
				else
					config.VideoHeight = 720;

				if (int.TryParse(Environment.GetEnvironmentVariable("WEBRTC_VIDEO_FRAMERATE"), out int videoFrameRate))
					config.VideoFrameRate = videoFrameRate;
				else
					config.VideoFrameRate = 30;

				// Handle VideoCodec enum
				var videoCodecString = Environment.GetEnvironmentVariable("WEBRTC_VIDEO_CODEC");
				if (!string.IsNullOrEmpty(videoCodecString) &&
					Enum.TryParse<VideoCodecsEnum>(videoCodecString, true, out var videoCodec))
				{
					config.VideoCodec = videoCodec;
				}
				else
				{
					config.VideoCodec = VideoCodecsEnum.VP8;
				}

				// Handle ICE Servers from environment variable
				var iceServersString = Environment.GetEnvironmentVariable("WEBRTC_ICE_SERVERS");
				if (!string.IsNullOrEmpty(iceServersString))
				{
					config.IceServers = new List<IceServerConfig>();
					// Split by semicolon for multiple ICE servers
					var iceServerUrls = iceServersString.Split(';', StringSplitOptions.RemoveEmptyEntries);

					foreach (var url in iceServerUrls)
					{
						config.IceServers.Add(new IceServerConfig { Url = url.Trim() });
					}
				}
				else
				{
					// Default ICE server
					config.IceServers = new List<IceServerConfig>
					{
						new IceServerConfig { Url = "stun:stun.l.google.com:19302" }
					};
				}

				return config;
			});

			services.AddSingleton<IWebRTCManager, WebRTCManager>();

			// Add MediatR
			services.AddService<CreateStreamSessionCommand, CreateStreamSessionHandler>();
			services.AddService<DeleteCameraCommand, DeleteCameraHandler>();
			services.AddService<EndStreamSessionCommand, EndStreamSessionHandler>();

			services.AddService<ExecuteCameraControlCommand, ExecuteCameraControlHandler>();
			services.AddService<RegisterCameraRequest, RegisterCameraHandler>();
			services.AddService<UpdateCameraRequest, UpdateCameraHandler>();

			services.AddService<CheckCameraControlPermissionQuery, CheckCameraControlPermissionHandler>();
			services.AddService<CheckCameraPermissionQuery, CheckCameraPermissionHandler>();
			services.AddService<GetActiveCamerasQuery, GetActiveCamerasHandler>();

			services.AddService<GetAlertUsersForCameraQuery, GetAlertUsersForCameraHandler>();
			services.AddService<GetCameraByIdQuery, GetCameraByIdHandler>();
			services.AddService<GetCamerasRequest, GetCamerasHandler>();

			services.AddService<GetDetectionEventByIdQuery, GetDetectionEventByIdHandler>();
			services.AddService<GetDetectionEventsRequest, GetDetectionEventsHandler>();
			services.AddService<GetStreamSessionsRequest, GetStreamSessionsHandler>();
			services.AddService<GetStreamStatisticsRequest, GetStreamStatisticsHandler>();

			// Add SignalR
			services.AddSignalR(options =>
			{
				options.EnableDetailedErrors = true;
			});

			return services;
		}
	}
}