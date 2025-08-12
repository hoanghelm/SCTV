using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class AlertNotificationRuleConfiguration : IEntityTypeConfiguration<AlertNotificationRule>
	{
		public void Configure(EntityTypeBuilder<AlertNotificationRule> builder)
		{
			builder.HasKey(n => n.Id);

			builder.Property(n => n.UserId)
				.IsRequired()
				.HasMaxLength(450);

			builder.Property(n => n.NotificationType)
				.HasMaxLength(50);

			// Indexes
			builder.HasIndex(n => new { n.AlertRuleId, n.UserId });
			builder.HasIndex(n => n.IsActive);

			// Relationships
			builder.HasOne(n => n.AlertRule)
				.WithMany(a => a.NotificationRules)
				.HasForeignKey(n => n.AlertRuleId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}