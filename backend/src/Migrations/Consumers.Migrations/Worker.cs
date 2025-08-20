using System.Diagnostics;
using PersonDetections.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;

namespace PersonDetections.Migrations
{
	public class Worker(
	IServiceProvider serviceProvider,
	IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
	{
		public const string ActivitySourceName = "Consumers.Migrations";
		private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

			try
			{
				await MigrationManager.Up(serviceProvider);
			}
			catch (Exception ex)
			{
				activity?.RecordException(ex);
				throw;
			}

			hostApplicationLifetime.StopApplication();
		}
	}
}