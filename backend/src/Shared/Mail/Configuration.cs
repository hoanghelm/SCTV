using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Mail
{
	public static class Configuration
	{
		public static IServiceCollection AddMailService(this IServiceCollection services)
		{
			var z = new MailServerSetting();
			services.AddSingleton(resolver => new Lazy<IMailService>(() =>
			{
				z  = new MailServerSetting()
				{
					DisplayName = Environment.GetEnvironmentVariable("MAIL_SERVER_DISPLAY_NAME"),
					FromAddress = Environment.GetEnvironmentVariable("MAIL_SERVER_FROM_ADDRESS"),
					Host = Environment.GetEnvironmentVariable("MAIL_SERVER_HOST"),
					SiteUrl = Environment.GetEnvironmentVariable("MAIL_SERVER_SITEURL"),
					UserName = Environment.GetEnvironmentVariable("MAIL_SERVER_USERNAME"),
					Password = Environment.GetEnvironmentVariable("MAIL_SERVER_PASSWORD"),
					Port = int.Parse(Environment.GetEnvironmentVariable("MAIL_SERVER_PORT")),
					UseSSL = bool.Parse(Environment.GetEnvironmentVariable("MAIL_SERVER_USE_SSL")),
				};

				return new MailService(z);
			}));

			return services;
		}
	}
}
