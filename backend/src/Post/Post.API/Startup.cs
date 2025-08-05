using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Post.Infrastructure.Configures;
using Post.Infrastructure.Mapper;
using Post.Infrastructure.Mediators;
using Post.Infrastructure.Middlewares;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Post.API
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{

			services.AddControllers();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Post.API", Version = "v1" });
			});

			services.AddMediator();
			services.AddServices();
			services.AddAutoMapper();
			services.AddUnitOfWork();
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

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
				app.UseSwagger();
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Post.API v1"));
			}
			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseCors(options => options.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
			app.UseRouting();
			app.UseMiddlewares();
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

			app.UseEndpoints(endpoints => endpoints.MapControllers());
		}
	}
}
