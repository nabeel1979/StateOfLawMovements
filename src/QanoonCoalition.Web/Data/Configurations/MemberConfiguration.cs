using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("Members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SerialNumber)
            .IsRequired()
            .HasMaxLength(8)
            .IsFixedLength();

        builder.Property(m => m.FullName)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("Arabic_CI_AI");

        builder.Property(m => m.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(m => m.Email)
            .HasMaxLength(200);

        builder.Property(m => m.Address).HasMaxLength(500);
        builder.Property(m => m.Occupation).HasMaxLength(200);
        builder.Property(m => m.BenefitField).HasMaxLength(500);
        builder.Property(m => m.Notes).HasMaxLength(500);
        builder.Property(m => m.PhotoPath).HasMaxLength(500);

        builder.Property(m => m.Gender)
            .HasConversion<int>();

        builder.Property(m => m.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // منع التكرار على مستوى SQL Server - عبر جميع الحركات
        builder.HasIndex(m => m.SerialNumber).IsUnique().HasDatabaseName("IX_Members_SerialNumber");
        builder.HasIndex(m => m.FullName).IsUnique().HasDatabaseName("IX_Members_FullName");
        builder.HasIndex(m => m.Phone).IsUnique().HasDatabaseName("IX_Members_Phone");
        builder.HasIndex(m => m.Email).IsUnique().HasDatabaseName("IX_Members_Email")
            .HasFilter("[Email] IS NOT NULL");

        // Relationship with Movement
        builder.HasOne(m => m.Movement)
            .WithMany(mv => mv.Members)
            .HasForeignKey(m => m.MovementId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with User (approver)
        builder.HasOne(m => m.ApprovedByUser)
            .WithMany()
            .HasForeignKey(m => m.ApprovedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Relationship with JoinRequest
        builder.HasOne(m => m.JoinRequest)
            .WithOne(jr => jr.ConvertedMember)
            .HasForeignKey<Member>(m => m.JoinRequestId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
