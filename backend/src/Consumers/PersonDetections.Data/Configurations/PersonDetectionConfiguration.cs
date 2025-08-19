using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonDetections.Domain.Entities;

namespace PersonDetections.Data.Configurations;

public class PersonDetectionConfiguration : IEntityTypeConfiguration<PersonDetection>
{
    public void Configure(EntityTypeBuilder<PersonDetection> builder)
    {
        builder.ToTable("PersonDetections");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CameraId)
            .IsRequired();
            
        builder.Property(x => x.CameraName)
            .IsRequired()
            .HasMaxLength(200);
            
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(x => x.EventTimestamp)
            .IsRequired();
            
        builder.Property(x => x.DetectionCount)
            .IsRequired();
            
        builder.Property(x => x.DetectionsData)
            .IsRequired()
            .HasColumnType("jsonb");
            
        builder.Property(x => x.FrameData)
            .HasColumnType("text");
            
        builder.Property(x => x.FrameStoragePath)
            .HasMaxLength(500);
            
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("NOW()");
    }
}