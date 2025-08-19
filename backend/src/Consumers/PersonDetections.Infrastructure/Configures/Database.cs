using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonDetections.Data.Context;
using PersonDetections.Data.EntityFramework;
using PersonDetections.Domain.Contracts;
using System;

namespace PersonDetections.Infrastructure.Configures
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

			services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<IUnitOfWork<WriteDbContext>>());

			return services;
		}
	}
}
