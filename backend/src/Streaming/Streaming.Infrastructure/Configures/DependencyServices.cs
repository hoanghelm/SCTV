using Microsoft.Extensions.DependencyInjection;
using Streaming.Infrastructure.Mediators;
using Streaming.Service.Streaming.Commands;
using Streaming.Service.Streaming.Queries;
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

			#region Posts
			services.AddService<CreatePostRequest, CreatePostHandler>();
			services.AddService<GetPostsRequest, GetPostsHandler>();
			services.AddService<CreateCommentRequest, CreateCommentHandler>();
            #endregion

            return services;
		}
	}
}