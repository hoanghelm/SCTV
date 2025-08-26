using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PersonDetections.Data.Context;
using PersonDetections.Data.EntityFramework;
using PersonDetections.Domain.Contracts;
using PersonDetections.Infrastructure.Mediators;
using PersonDetections.Service.Commands;
using PersonDetections.Service.Handlers;
using PersonDetections.Service.Queries;
using PersonDetections.Service.Services;
using SIPSorceryMedia.Abstractions;
using System.Reflection;

namespace PersonDetections.Infrastructure.Configures
{
	public static class DependencyServices
	{
		public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddService<ProcessPersonDetectionCommand, bool, ProcessPersonDetectionHandler>();
			services.AddHostedService<KafkaConsumerService>();
			services.AddScoped<HttpClient>();
			services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();

			return services;
		}

		public static IServiceCollection AddNotificationServices(this IServiceCollection services)
		{
			services.AddService<GetPersonDetectionNotificationsRequest, GetPersonDetectionNotificationsHandler>();
			services.AddService<GetPersonDetectionNotificationByIdQuery, GetPersonDetectionNotificationByIdHandler>();

			return services;
		}
	}
}