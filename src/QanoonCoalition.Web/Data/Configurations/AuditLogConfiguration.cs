using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(al => al.Id);

        builder.Property(al => al.Action)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(al => al.EntityType).HasMaxLength(100);
        builder.Property(al => al.EntityId).HasMaxLength(50);
        builder.Property(al => al.IpAddress).HasMaxLength(45);
        builder.Property(al => al.UserAgent).HasMaxLength(500);
        builder.Property(al => al.Description).HasMaxLength(500);

        // OldValues and NewValues stored as nvarchar(MAX) for JSON
        builder.Property(al => al.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(al => al.NewValues).HasColumnType("nvarchar(max)");

        builder.Property(al => al.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for common queries
        builder.HasIndex(al => al.CreatedAt).HasDatabaseName("IX_AuditLogs_CreatedAt");
        builder.HasIndex(al => al.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        builder.HasIndex(al => al.MovementId).HasDatabaseName("IX_AuditLogs_MovementId");
        builder.HasIndex(al => al.Action).HasDatabaseName("IX_AuditLogs_Action");

        builder.HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(al => al.Movement)
            .WithMany(m => m.AuditLogs)
            .HasForeignKey(al => al.MovementId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
