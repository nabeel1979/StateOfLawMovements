using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class MovementConstantConfiguration : IEntityTypeConfiguration<MovementConstant>
{
    public void Configure(EntityTypeBuilder<MovementConstant> builder)
    {
        builder.ToTable("MovementConstants");
        builder.HasKey(mc => mc.Id);

        builder.Property(mc => mc.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(mc => mc.Value)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(mc => mc.DataType)
            .HasMaxLength(50)
            .HasDefaultValue("text");

        builder.Property(mc => mc.Description).HasMaxLength(500);

        builder.Property(mc => mc.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique key per movement
        builder.HasIndex(mc => new { mc.MovementId, mc.Key })
            .IsUnique()
            .HasDatabaseName("IX_MovementConstants_MovementId_Key");

        builder.HasOne(mc => mc.Movement)
            .WithMany(m => m.Constants)
            .HasForeignKey(mc => mc.MovementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
