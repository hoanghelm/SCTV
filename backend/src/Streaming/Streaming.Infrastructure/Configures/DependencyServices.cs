using Microsoft.Extensions.DependencyInjection;
using Streaming.Infrastructure.Mediators;
using System;
using System.Collections.Generic;
using System.Text;

namespace Streaming.Infrastructure.Configures
{
	public static class DependencyServices
	{
		public static IServiceCollection AddServices(this IServiceCollection services)
		{

			services.AddStackExchangeRedisCache(options =>
			{
				options.Configuration = Environment.GetEnvironmentVariable("REDIS_HOST");
			});

            return services;
		}
	}
}