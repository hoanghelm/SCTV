using SCTV.AppHost.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonDetections.Migrations;
using PersonDetections.Infrastructure.Mediators;
using PersonDetections.Infrastructure.Configures;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRouting();

builder.Services.AddServiceDefaults(builder.Configuration);
builder.Services.AddMediator();
builder.Services.AddServices(builder.Configuration);
builder.Services.AddUnitOfWork();


builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
	.WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
host.Run();