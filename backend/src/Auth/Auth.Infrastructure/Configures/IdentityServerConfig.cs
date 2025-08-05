using Auth.Data.Context;
using Auth.Domain.Entities;
using Auth.Infrastructure.Identity;
using Auth.Service.ProfileService;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Auth.Infrastructure.Configures
{
	public static class IdentityServerConfig
	{
		public static IServiceCollection AddIdentityServerConfig(this IServiceCollection services, IConfiguration configuration)
		{
			var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			var connectionString = Environment.GetEnvironmentVariable("IDENTITY_SERVER4_CONNECTION_STRING");
			var signingCredential = Environment.GetEnvironmentVariable("SIGNING_CREDENTIAL");
			var protectKeyPath = Environment.GetEnvironmentVariable("PROTECT_KEY_PATH");
			var tokenLifespanInHours = Environment.GetEnvironmentVariable("TOKEN_EXPIRE_TIME_IN_HOURS");
			services.AddIdentity<UserEntity, RoleEntity>(options =>
			{
				options.User.RequireUniqueEmail = false;
				options.Password.RequiredLength = 0;
				options.Password.RequiredUniqueChars = 0;
				options.Password.RequireLowercase = false;
				options.Password.RequireUppercase = false;
				options.Password.RequireDigit = false;
				options.Password.RequireNonAlphanumeric = false;
				options.User.AllowedUserNameCharacters = "abcdefghiıjklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+'#!/^%{}*";
			}).AddEntityFrameworkStores<ApplicationDbContext>()
			.AddSignInManager<SignInValidator<UserEntity>>()
			.AddDefaultTokenProviders();

			var builder = services.AddIdentityServer(
				options =>
				{
					options.Events.RaiseErrorEvents = true;
					options.Events.RaiseInformationEvents = true;
					options.Events.RaiseFailureEvents = true;
					options.Events.RaiseSuccessEvents = true;
				})
				//.AddSigningCredential(new X509Certificate2(signingCredential, "password"))
				.AddDeveloperSigningCredential()
					.AddOperationalStore(options =>
					{
						options.ConfigureDbContext = builder =>
						builder.UseNpgsql(connectionString);
						options.EnableTokenCleanup = true;
					})
					.AddConfigurationStore(options =>
					{
						options.ConfigureDbContext = builder =>
						builder.UseNpgsql(connectionString);
					})
					.AddAspNetIdentity<UserEntity>();

			return services;
		}

		public static IServiceCollection AddServices<TUser>(this IServiceCollection services) where TUser : IdentityUser<string>, new()
		{
			services.AddTransient<IProfileService, ProfileService>();
			return services;
		}

		public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services)
		{
			var connectionString = Environment.GetEnvironmentVariable("IDENTITY_CONNECTION_STRING");
			services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
			services.AddDbContext<AppPersistedGrantContext>(options => options.UseNpgsql(connectionString));
			services.AddDbContext<AppConfigurationContext>(options => options.UseNpgsql(connectionString));

			return services;
		}
	}
}
