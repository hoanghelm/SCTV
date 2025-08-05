using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Post.Data.Context;
using Post.Data.EntityFramework;
using Post.Domain.Contracts;
using System;

namespace Post.Infrastructure.Configures
{
	public static class Database
	{
		public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
		{
			var readConnectionString = Environment.GetEnvironmentVariable("READ_DATABASE_CONNECTION_STRING");
			var writeConnectionString = Environment.GetEnvironmentVariable("WRITE_DATABASE_CONNECTION_STRING");


			services.AddDbContext<ReadDbContext>(opt => opt.UseNpgsql(readConnectionString));
			services.AddScoped<IUnitOfWork<ReadDbContext>, UnitOfWork<ReadDbContext>>();

			services.AddDbContext<WriteDbContext>(opt => opt.UseNpgsql(writeConnectionString));
			services.AddScoped<IUnitOfWork<WriteDbContext>, UnitOfWork<WriteDbContext>>();

			return services;
		}
	}
}
