using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Streaming.Domain.Entities;

namespace Streaming.Data.Configurations
{
	public class StreamSessionConfiguration : IEntityTypeConfiguration<StreamSession>
	{
		public void Configure(EntityTypeBuilder<StreamSession> builder)
		{
			builder.HasKey(s => s.Id);

			builder.Property(s => s.ViewerId)
				.IsRequired()
				.HasMaxLength(450);

			builder.Property(s => s.ConnectionId)
				.HasMaxLength(50);

			builder.Property(s => s.SessionDescription)
				.HasMaxLength(2000);

			builder.Property(s => s.Status)
				.HasMaxLength(50);

			builder.Property(s => s.UserAgent)
				.HasMaxLength(500);

			builder.Property(s => s.IpAddress)
				.HasMaxLength(45);

			// Indexes
			builder.HasIndex(s => new { s.CameraId, s.ViewerId });
			builder.HasIndex(s => s.Status);
			builder.HasIndex(s => s.StartedAt);
			builder.HasIndex(s => s.EndedAt);

			// Relationships
			builder.HasOne(s => s.Camera)
				.WithMany(c => c.StreamSessions)
				.HasForeignKey(s => s.CameraId)
				.OnDelete(DeleteBehavior.Cascade);
		}
	}
}