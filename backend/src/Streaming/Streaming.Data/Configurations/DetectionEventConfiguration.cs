using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class DetectionEventConfiguration : IEntityTypeConfiguration<DetectionEvent>
	{
		public void Configure(EntityTypeBuilder<DetectionEvent> builder)
		{
			builder.HasKey(d => d.Id);

			builder.Property(d => d.EventType)
				.IsRequired()
				.HasMaxLength(50);

			builder.Property(d => d.DetectionData)
				.HasMaxLength(2000);

			builder.Property(d => d.FrameImageUrl)
				.HasMaxLength(500);

			builder.Property(d => d.VideoClipUrl)
				.HasMaxLength(500);

			builder.Property(d => d.Metadata)
				.HasMaxLength(1000);

			// Indexes
			builder.HasIndex(d => d.CameraId);
			builder.HasIndex(d => d.EventType);
			builder.HasIndex(d => d.EventTimestamp);
			builder.HasIndex(d => d.AlertTriggered);

			// Relationships
			builder.HasOne(d => d.Camera)
				.WithMany()
				.HasForeignKey(d => d.CameraId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}