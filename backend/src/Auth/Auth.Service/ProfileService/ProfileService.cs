using Auth.Domain.Entities;
using Common.Extensions;
using Duende;
using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Auth.Service.ProfileService
{
	public class ProfileService : IProfileService
	{
		private readonly IUserClaimsPrincipalFactory<UserEntity> _claimsFactory;
		private readonly UserManager<UserEntity> _userManager;

		public ProfileService(IUserClaimsPrincipalFactory<UserEntity> claimsFactory, UserManager<UserEntity> userManager)
		{
			_claimsFactory = claimsFactory;
			_userManager = userManager;
		}

		public async Task GetProfileDataAsync(ProfileDataRequestContext context)
		{
			var sub = context.Subject.GetSubjectId();
			var user = await _userManager.FindByIdAsync(sub);
			var principal = await _claimsFactory.CreateAsync(user);
			var claims = principal.Claims.ToList();
			var roles = await _userManager.GetRolesAsync(user);

			claims = claims.Where(c => context.RequestedClaimTypes.Contains(c.Type)).ToList();

			if (user.Id.IsNotEmpty())
			{
				claims.Add(new Claim(JwtClaimTypes.Id, user.Id));
			}
			if (user.FullName.IsNotEmpty())
			{
				claims.Add(new Claim(JwtClaimTypes.Name, user.FullName));
			}
			if (user.Email.IsNotEmpty())
			{
				claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
			}

			foreach (var role in roles)
			{
				claims.Add(new Claim(JwtClaimTypes.Role, role));
			}

			context.IssuedClaims = claims;
		}

		public Task IsActiveAsync(IsActiveContext context)
		{
			context.IsActive = true;

			return Task.CompletedTask;
		}
	}
}
