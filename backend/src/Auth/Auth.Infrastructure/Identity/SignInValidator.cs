using Auth.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Auth.Infrastructure.Identity
{
	public class SignInValidator<TUser> : SignInManager<UserEntity>
	{
		private readonly IHttpContextAccessor _contextAccessor;

		public SignInValidator(
			UserManager<UserEntity> userManager,
			IHttpContextAccessor contextAccessor,
			IUserClaimsPrincipalFactory<UserEntity> claimsFactory,
			IOptions<IdentityOptions> optionsAccessor,
			ILogger<SignInManager<UserEntity>> logger,
			IAuthenticationSchemeProvider schemes,
			IUserConfirmation<UserEntity> confirmation) :
			base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
		{
			_contextAccessor = contextAccessor;
		}


		/// <summary>
		/// Custom validate when sign in
		/// </summary>
		public override Task<bool> CanSignInAsync(UserEntity user)
		{
			var canSignIn = user.EmailConfirmed;
			return Task.FromResult(canSignIn);
		}
	}
}