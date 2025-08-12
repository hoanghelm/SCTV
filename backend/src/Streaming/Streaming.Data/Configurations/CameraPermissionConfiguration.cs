using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class CameraPermissionConfiguration : IEntityTypeConfiguration<CameraPermission>
	{
		public void Configure(EntityTypeBuilder<CameraPermission> builder)
		{
			builder.HasKey(p => p.Id);

			builder.Property(p => p.UserId)
				.IsRequired()
				.HasMaxLength(450);

			builder.Property(p => p.PermissionType)
				.IsRequired()
				.HasMaxLength(50);

			builder.Property(p => p.Notes)
				.HasMaxLength(500);

			// Indexes
			builder.HasIndex(p => new { p.CameraId, p.UserId, p.PermissionType });
			builder.HasIndex(p => p.IsActive);
			builder.HasIndex(p => p.ExpiresAt);

			// Relationships
			builder.HasOne(p => p.Camera)
				.WithMany(c => c.CameraPermissions)
				.HasForeignKey(p => p.CameraId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}