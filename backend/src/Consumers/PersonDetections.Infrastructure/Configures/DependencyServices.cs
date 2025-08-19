using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIPSorceryMedia.Abstractions;
using PersonDetections.Data.Context;
using PersonDetections.Data.EntityFramework;
using PersonDetections.Domain.Contracts;
using PersonDetections.Infrastructure.Mediators;
using System.Reflection;

namespace PersonDetections.Infrastructure.Configures
{
	public static class DependencyServices
	{
		public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
		{
			// Add MediatR

			return services;
		}
	}
}