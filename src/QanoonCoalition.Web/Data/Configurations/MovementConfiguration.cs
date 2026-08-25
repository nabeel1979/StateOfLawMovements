using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class MovementConfiguration : IEntityTypeConfiguration<Movement>
{
    public void Configure(EntityTypeBuilder<Movement> builder)
    {
        builder.ToTable("Movements");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("Arabic_CI_AI");

        builder.Property(m => m.NameSlug)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.PublicToken)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(m => m.Logo).HasMaxLength(500);
        builder.Property(m => m.Address).HasMaxLength(500);
        builder.Property(m => m.Description).HasMaxLength(2000);
        builder.Property(m => m.Phone).HasMaxLength(20);
        builder.Property(m => m.Email).HasMaxLength(200);
        builder.Property(m => m.Website).HasMaxLength(200);

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraints
        builder.HasIndex(m => m.Name).IsUnique().HasDatabaseName("IX_Movements_Name");
        builder.HasIndex(m => m.NameSlug).IsUnique().HasDatabaseName("IX_Movements_NameSlug");
        builder.HasIndex(m => m.PublicToken).IsUnique().HasDatabaseName("IX_Movements_PublicToken");

        // Relationship: created by user
        builder.HasOne(m => m.CreatedByUser)
            .WithMany()
            .HasForeignKey(m => m.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
