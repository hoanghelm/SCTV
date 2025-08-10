using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Streaming.Domain.Contracts;

namespace Streaming.Data.Context
{
	public abstract class ApplicationContext : DbContext, IApplicationDbContext
	{
		public ApplicationContext(DbContextOptions options) : base(options) { }



		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.HasPostgresExtension("unaccent");
		}
	}
}
