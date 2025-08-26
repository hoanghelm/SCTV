using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SCTV.AppHost.Extensions;
using PersonDetections.Infrastructure.Configures;
using PersonDetections.Infrastructure.Mediators;
using PersonDetections.Infrastructure.Migrations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Notifications.API
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		public void ConfigureServices(IServiceCollection services)
		{
			services.AddServiceDefaults(Configuration);
			services.AddControllers();
			services.AddSwaggerGen(c => 
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Notifications.API", Version = "v1" });
			});

			services.AddMediator();
			services.AddUnitOfWork();
			//services.AddServices(Configuration);
			services.AddNotificationServices();
			services.AddHttpClient();
			services.AddHealthChecks();
			services.AddResponseCaching();
			services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddApiVersioning(config =>
			{
				// Specify the default API Version as 1.0
				config.DefaultApiVersion = new ApiVersion(1, 0);
				// If the client hasn't specified the API version in the request, use the default API version number 
				config.AssumeDefaultVersionWhenUnspecified = true;
				// Advertise the API versions supported for the particular endpoint
				config.ReportApiVersions = true;
			});
			services.AddControllers().AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Notifications.API v1"));
			}
			app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
			
			var detectionsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Detections", "storage");
			if (Directory.Exists(detectionsPath))
			{
				app.UseStaticFiles(new StaticFileOptions
				{
					FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(detectionsPath),
					RequestPath = "/detections"
				});
			}
			
			app.UseRouting();
			app.UseResponseCaching();
			app.UseAuthentication().UseAuthorization();
			app.UseHealthChecks("/", new HealthCheckOptions
			{
				ResponseWriter = async (context, report) =>
				{
					var response = new object { };
					context.Response.ContentType = "application/json";
					await context.Response.WriteAsync(JsonSerializer.Serialize(response));
				}
			});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
