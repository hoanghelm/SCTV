using Auth.Domain.Entities;
using Auth.Infrastructure.Configures;
using Common.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Auth.API
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
			services.AddHttpClient();
			services.AddDatabaseConfiguration();
			services.AddIdentityServerConfig(Configuration)
				.AddServices<UserEntity>().AddAuth();
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth.API", Version = "v1" });
			});

			services.AddHealthChecks();
			services.AddResponseCaching();

			services.AddDataProtection()
				.PersistKeysToFileSystem(new DirectoryInfo(Environment.GetEnvironmentVariable("PROTECT_KEY_PATH")))
				.SetDefaultKeyLifetime(TimeSpan.FromDays(9999));

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
				app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth.API v1"));
			}
			app.UseDefaultFiles();
			app.UseStaticFiles();
			app.UseHttpsRedirection();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseResponseCaching();
			app.UseIdentityServer();
			app.UseRequestLocalization(new RequestLocalizationOptions()
			{
				DefaultRequestCulture = new RequestCulture("vi"),
				SupportedCultures = new[] { new CultureInfo("en"), new CultureInfo("vi") },
				SupportedUICultures = new[] { new CultureInfo("en"), new CultureInfo("vi") },
				RequestCultureProviders = new IRequestCultureProvider[] {
					new QueryStringRequestCultureProvider(),
					new AcceptLanguageHeaderRequestCultureProvider()
				},
			});

			app.UseHealthChecks("/", new HealthCheckOptions
			{
				ResponseWriter = async (context, report) =>
				{
					context.Response.ContentType = "application/json";
					var response = new object { };
					await context.Response.WriteAsync(JsonSerializer.Serialize(response));
				}
			});

			app.UseEndpoints(endpoints => endpoints.MapControllers().RequireAuthorization(Policies.API_SCOPE));
		}
	}
}
