using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class AlertRuleConfiguration : IEntityTypeConfiguration<AlertRule>
	{
		public void Configure(EntityTypeBuilder<AlertRule> builder)
		{
			builder.HasKey(a => a.Id);

			builder.Property(a => a.RuleName)
				.IsRequired()
				.HasMaxLength(100);

			builder.Property(a => a.AlertType)
				.IsRequired()
				.HasMaxLength(50);

			builder.Property(a => a.Conditions)
				.HasMaxLength(1000);

			builder.Property(a => a.Message)
				.HasMaxLength(500);

			// Indexes
			builder.HasIndex(a => a.CameraId);
			builder.HasIndex(a => a.IsActive);
			builder.HasIndex(a => a.AlertType);

			// Relationships
			builder.HasOne(a => a.Camera)
				.WithMany(c => c.AlertRules)
				.HasForeignKey(a => a.CameraId)
				.OnDelete(DeleteBehavior.Cascade);

			builder.HasMany(a => a.NotificationRules)
				.WithOne(n => n.AlertRule)
				.HasForeignKey(n => n.AlertRuleId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}