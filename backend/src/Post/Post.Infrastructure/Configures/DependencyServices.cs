using Microsoft.Extensions.DependencyInjection;
using Post.Infrastructure.Mediators;
using Post.Service.Post.Commands;
using Post.Service.Post.Queries;
using System;
using System.Collections.Generic;
using System.Text;

namespace Post.Infrastructure.Configures
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