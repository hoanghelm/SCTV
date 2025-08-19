using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PersonDetections.Domain.Contracts;
using PersonDetections.Domain.Entities;
using System.Reflection;
using System.Reflection.Emit;

namespace PersonDetections.Data.Context
{
	public abstract class ApplicationContext : DbContext, IApplicationDbContext
	{
		public ApplicationContext(DbContextOptions options) : base(options) { }
		public DbSet<PersonDetection> PersonDetections { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.HasPostgresExtension("unaccent");

			builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}
