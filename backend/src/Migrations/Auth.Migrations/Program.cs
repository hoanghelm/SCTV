using Auth.Domain.Entities;
using Auth.Infrastructure.Configures;
using Auth.Migrations;
using SCTV.AppHost.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRouting();

builder.Services.AddServiceDefaults(builder.Configuration);
builder.Services.AddDatabaseConfiguration();
builder.Services.AddIdentityServerConfig(builder.Configuration)
	.AddServices<UserEntity>().AddAuth(); ;

builder.Services.AddHostedService<Worker>();

builder.Services.AddOpenTelemetry()
	.WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
host.Run();