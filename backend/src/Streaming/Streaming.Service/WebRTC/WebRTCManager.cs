using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SIPSorcery.Net;

namespace Streaming.Service.WebRTC
{
	public interface IWebRTCManager
	{
		Task<WebRTCConnection> CreateConnectionAsync(string connectionId, string streamSource);
		RTCSessionDescriptionInit CreateOfferAsync(string connectionId);
		bool SetAnswerAsync(string connectionId, RTCSessionDescriptionInit answer);
		bool AddIceCandidateAsync(string connectionId, RTCIceCandidateInit candidate);
		Task<bool> CloseConnectionAsync(string connectionId);
		Dictionary<string, object> GetConnectionStats(string connectionId);
	}

	public class WebRTCManager : IWebRTCManager, IDisposable
	{
		private readonly ConcurrentDictionary<string, WebRTCConnection> _connections;
		private readonly ILogger<WebRTCManager> _logger;
		private readonly WebRTCConfiguration _config;

		public WebRTCManager(ILogger<WebRTCManager> logger, WebRTCConfiguration config)
		{
			_logger = logger;
			_config = config;
			_connections = new ConcurrentDictionary<string, WebRTCConnection>();
		}

		public async Task<WebRTCConnection> CreateConnectionAsync(string connectionId, string streamSource)
		{
			try
			{
				var connection = new WebRTCConnection(connectionId, streamSource, _config, _logger);
				await connection.InitializeAsync();

				if (_connections.TryAdd(connectionId, connection))
				{
					_logger.LogInformation($"Created WebRTC connection {connectionId} for source {streamSource}");
					return connection;
				}

				throw new InvalidOperationException($"Connection {connectionId} already exists");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Failed to create connection {connectionId}");
				throw;
			}
		}

		public RTCSessionDescriptionInit CreateOfferAsync(string connectionId)
		{
			if (_connections.TryGetValue(connectionId, out var connection))
			{
				return connection.CreateOfferAsync();
			}

			throw new KeyNotFoundException($"Connection {connectionId} not found");
		}

		public bool SetAnswerAsync(string connectionId, RTCSessionDescriptionInit answer)
		{
			if (_connections.TryGetValue(connectionId, out var connection))
			{
				return connection.SetAnswerAsync(answer);
			}

			return false;
		}

		public bool AddIceCandidateAsync(string connectionId, RTCIceCandidateInit candidate)
		{
			if (_connections.TryGetValue(connectionId, out var connection))
			{
				return connection.AddIceCandidateAsync(candidate);
			}

			return false;
		}

		public async Task<bool> CloseConnectionAsync(string connectionId)
		{
			if (_connections.TryRemove(connectionId, out var connection))
			{
				await connection.CloseAsync();
				connection.Dispose();
				_logger.LogInformation($"Closed WebRTC connection {connectionId}");
				return true;
			}

			return false;
		}

		public Dictionary<string, object> GetConnectionStats(string connectionId)
		{
			if (_connections.TryGetValue(connectionId, out var connection))
			{
				return connection.GetStats();
			}

			return new Dictionary<string, object>();
		}

		public void Dispose()
		{
			_logger.LogInformation("Disposing WebRTC Manager");

			foreach (var connection in _connections.Values)
			{
				try
				{
					connection.CloseAsync().Wait();
					connection.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error disposing WebRTC connection");
				}
			}
			_connections.Clear();
		}
	}
}