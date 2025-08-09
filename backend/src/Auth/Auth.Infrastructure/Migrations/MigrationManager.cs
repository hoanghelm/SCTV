using Auth.Data.Context;
using Auth.Domain.Entities;
using Common.Identity;
using Duende.IdentityServer;
using Duende.IdentityServer.EntityFramework.Mappers;
using Duende.IdentityServer.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Auth.Infrastructure.Migrations
{
	public static class MigrationManager
	{
		public static async Task<IServiceProvider> Up(this IServiceProvider serviceProvider)
		{
			var provider = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
			provider.GetRequiredService<AppConfigurationContext>().Database.Migrate();
			provider.GetRequiredService<AppPersistedGrantContext>().Database.Migrate();
			provider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
			var context = provider.GetRequiredService<AppConfigurationContext>();

			if (!context.ApiScopes.Any())
			{
				var apiScopes = new List<ApiScope>()
				{
					new ApiScope()
					{
						Name = ApiScopes.API,
						Description="API Scope",
						DisplayName = "API Scope",
					},
				};
				foreach (var scope in apiScopes)
				{
					context.ApiScopes.Add(scope.ToEntity());
				}
				await context.SaveChangesAsync();
			}

			if (!context.ApiResources.Any())
			{
				var apiResources = new List<ApiResource>()
				{
					new ApiResource("api")
					{
						Name = "api",
						Description="API Resource",
						DisplayName = "API Resource",
						Scopes = { ApiScopes.API } // Add scopes explicitly
                    },
				};
				foreach (var apiResource in apiResources)
				{
					context.ApiResources.Add(apiResource.ToEntity());
				}
				context.SaveChanges();
			}

			if (!context.Clients.Any())
			{
				var clients = new List<Client>()
				{
					new Client
					{
						ClientName = "Website",
						ClientId = ClientId.WEBSITE,
						RequireClientSecret = true,
						ClientSecrets = new Secret[]
						{
							new Secret("I/hHiZKtYVT5U0C303fr4fwC41aj0vuteFXh1RvS6T8=")//a21zQDIwMjA=
                        },
						Description = "Website",
						AllowedGrantTypes = new string[] { GrantType.ResourceOwnerPassword, "external" },
						AccessTokenType = AccessTokenType.Jwt,
						AllowAccessTokensViaBrowser = true,
						AccessTokenLifetime = 86400,
						AllowOfflineAccess = true,
						AlwaysIncludeUserClaimsInIdToken = true,
						AllowedScopes = new List<string>
						{
							IdentityServerConstants.StandardScopes.OpenId,
							IdentityServerConstants.StandardScopes.Profile,
							IdentityServerConstants.StandardScopes.OfflineAccess,
							ApiScopes.API,
						},
						IdentityTokenLifetime = 60 * 60 * 24, // 1 day
                        AlwaysSendClientClaims = true,
					}
				};
				foreach (var client in clients)
				{
					context.Clients.Add(client.ToEntity());
				}
				await context.SaveChangesAsync();
			}

			if (!context.IdentityResources.Any())
			{
				var identityResources = new List<IdentityResource>()
				{
					new IdentityResources.OpenId(),
					new IdentityResources.Profile(),
				};
				foreach (var identityResource in identityResources)
				{
					context.IdentityResources.Add(identityResource.ToEntity());
				}
				await context.SaveChangesAsync();
			}

			await AddSeedUsers(provider);
			return serviceProvider;
		}

		private static async Task AddSeedUsers(IServiceProvider services)
		{
			var userManager = services.GetRequiredService<UserManager<UserEntity>>();
			var admin = await userManager.FindByNameAsync("admin");
			if (admin == null)
			{
				admin = new UserEntity
				{
					Id = Guid.NewGuid().ToString(),
					UserName = "admin",
					FullName = "Admin",
					FirstName = "admin",
					LastName = "admin",
					AvatarUrl = string.Empty,
					RefreshToken = string.Empty,
					LockedCount = 0,
					IsReceiveNotification = true,
					IsRoot = true,
					UserStatusId = UserStatus.Active,
					UpdatedBy = string.Empty,
					CreatedBy = string.Empty,

					Email = "admin@gmail.com",
					EmailConfirmed = true,
					PhoneNumberConfirmed = true,
					TwoFactorEnabled = false,
					LockoutEnabled = false,
					AccessFailedCount = 0
				};
				await userManager.CreateAsync(admin, "admin");
			}
		}
	}
}