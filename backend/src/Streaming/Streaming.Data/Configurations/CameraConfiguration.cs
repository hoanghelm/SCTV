using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class CameraConfiguration : IEntityTypeConfiguration<Camera>
	{
		public void Configure(EntityTypeBuilder<Camera> builder)
		{
			builder.HasKey(c => c.Id);

			builder.Property(c => c.Name)
				.IsRequired()
				.HasMaxLength(100);

			builder.Property(c => c.Description)
				.HasMaxLength(500);

			builder.Property(c => c.StreamUrl)
				.IsRequired()
				.HasMaxLength(500);

			builder.Property(c => c.Status)
				.IsRequired()
				.HasMaxLength(50);

			builder.Property(c => c.Location)
				.HasMaxLength(100);

			builder.Property(c => c.CameraType)
				.HasMaxLength(50);

			builder.Property(c => c.Brand)
				.HasMaxLength(100);

			builder.Property(c => c.Model)
				.HasMaxLength(100);

			builder.Property(c => c.Resolution)
				.HasMaxLength(20);

			builder.Property(c => c.ConfigurationJson)
				.HasMaxLength(1000);

			// Indexes
			builder.HasIndex(c => c.Status);
			builder.HasIndex(c => c.IsOnline);
			builder.HasIndex(c => c.CreatedAt);

			// Relationships
			builder.HasMany(c => c.StreamSessions)
				.WithOne(s => s.Camera)
				.HasForeignKey(s => s.CameraId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(c => c.CameraPermissions)
				.WithOne(p => p.Camera)
				.HasForeignKey(p => p.CameraId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(c => c.AlertRules)
				.WithOne(a => a.Camera)
				.HasForeignKey(a => a.CameraId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}
