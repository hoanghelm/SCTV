using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System.Linq;

namespace SCTV.AppHost.Extensions;

public static class ServiceExtensions
{
	// Add to ConfigureServices in Startup classes
	public static IServiceCollection AddServiceDefaults(this IServiceCollection services, IConfiguration configuration)
	{
		services.ConfigureOpenTelemetry(configuration);
		services.AddDefaultHealthChecks();
		services.AddServiceDiscovery();

		services.ConfigureHttpClientDefaults(http =>
		{
			// Turn on resilience by default
			http.AddStandardResilienceHandler();

			// Turn on service discovery by default
			http.AddServiceDiscovery();
		});

		return services;
	}

	// Add to ConfigureServices in Startup classes
	public static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services, IConfiguration configuration)
	{
		// Configure OpenTelemetry logging
		services.AddLogging(logging =>
		{
			logging.AddOpenTelemetry(options =>
			{
				options.IncludeFormattedMessage = true;
				options.IncludeScopes = true;
			});
		});

		// Configure OpenTelemetry metrics and tracing
		services.AddOpenTelemetry()
			.WithMetrics(metrics =>
			{
				metrics.AddAspNetCoreInstrumentation()
					.AddHttpClientInstrumentation()
					.AddRuntimeInstrumentation();
			})
			.WithTracing(tracing =>
			{
				tracing.AddAspNetCoreInstrumentation()
					// Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
					//.AddGrpcClientInstrumentation()
					.AddHttpClientInstrumentation();
			});

		services.AddOpenTelemetryExporters(configuration);

		return services;
	}

	// Used by ConfigureOpenTelemetry
	public static IServiceCollection AddOpenTelemetryExporters(this IServiceCollection services, IConfiguration configuration)
	{
		var useOtlpExporter = !string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

		if (useOtlpExporter)
		{
			services.AddOpenTelemetry().UseOtlpExporter();
		}

		// Uncomment the following lines to enable the Prometheus exporter (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
		// services.AddOpenTelemetry()
		//    .WithMetrics(metrics => metrics.AddPrometheusExporter());

		// Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
		//if (!string.IsNullOrEmpty(configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
		//{
		//    services.AddOpenTelemetry()
		//       .UseAzureMonitor();
		//}

		return services;
	}

	// Add to ConfigureServices in Startup classes
	public static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
	{
		services.AddHealthChecks()
			// Add a default liveness check to ensure app is responsive
			.AddCheck("self", () => HealthCheckResult.Healthy(), new[] { "live" });

		return services;
	}

	// Add to Configure in Startup classes
	public static IApplicationBuilder UseDefaultEndpoints(this IApplicationBuilder app, IWebHostEnvironment env)
	{
		// Adding health checks endpoints to applications in non-development environments has security implications.
		// See https://aka.ms/dotnet/aspire/healthchecks for details before enabling these endpoints in non-development environments.
		if (env.IsDevelopment())
		{
			// All health checks must pass for app to be considered ready to accept traffic after starting
			app.UseHealthChecks("/health");

			// Only health checks tagged with the "live" tag must pass for app to be considered alive
			app.UseHealthChecks("/alive", new HealthCheckOptions
			{
				Predicate = r => r.Tags.Contains("live")
			});

			// Uncomment the following line to enable the Prometheus endpoint (requires the OpenTelemetry.Exporter.Prometheus.AspNetCore package)
			// app.UseEndpoints(endpoints => endpoints.MapPrometheusScrapingEndpoint());
		}

		return app;
	}
}