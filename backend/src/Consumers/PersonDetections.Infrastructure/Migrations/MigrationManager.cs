using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonDetections.Data.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonDetections.Infrastructure.Migrations
{
	public static class MigrationManager
	{
		public static async Task<IServiceProvider> Up(this IServiceProvider serviceProvider)
		{
			var provider = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope().ServiceProvider;
			provider.GetRequiredService<WriteDbContext>().Database.Migrate();

			return serviceProvider;
		}
	}
}
