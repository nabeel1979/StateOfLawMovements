using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Title)
            .HasMaxLength(50);

        builder.Property(u => u.Role)
            .HasConversion<int>();

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Unique index on Email
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

        // Relationship with Movement
        builder.HasOne(u => u.Movement)
            .WithMany(m => m.Managers)
            .HasForeignKey(u => u.MovementId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
