using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Streaming.Domain.Contracts;
using Streaming.Domain.Entities;
using System.Reflection;
using System.Reflection.Emit;

namespace Streaming.Data.Context
{
	public abstract class ApplicationContext : DbContext, IApplicationDbContext
	{
		public ApplicationContext(DbContextOptions options) : base(options) { }

		public DbSet<Camera> Cameras { get; set; }
		public DbSet<StreamSession> StreamSessions { get; set; }
		public DbSet<CameraPermission> CameraPermissions { get; set; }
		public DbSet<AlertRule> AlertRules { get; set; }
		public DbSet<AlertNotificationRule> AlertNotificationRules { get; set; }
		public DbSet<DetectionEvent> DetectionEvents { get; set; }

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			builder.HasPostgresExtension("unaccent");

			builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
		}
	}
}
