using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ApiGateway
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
			.ConfigureAppConfiguration((host, config) =>
			{
				config.SetBasePath(host.HostingEnvironment.ContentRootPath)
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true, true)
				.AddJsonFile(Path.Combine("OcelotConfiguration", "ocelot.json"), optional: false, reloadOnChange: true)
				.AddJsonFile(Path.Combine("OcelotConfiguration", $"ocelot.{host.HostingEnvironment.EnvironmentName}.json"), optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();
			}).ConfigureKestrel(options =>
			{
				options.Limits.MaxRequestBodySize = null; // or a given limit
				options.AddServerHeader = false;
			})
			.UseStartup<Startup>();
	}
}
