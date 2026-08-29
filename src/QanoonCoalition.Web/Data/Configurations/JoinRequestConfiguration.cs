using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class JoinRequestConfiguration : IEntityTypeConfiguration<JoinRequest>
{
    public void Configure(EntityTypeBuilder<JoinRequest> builder)
    {
        builder.ToTable("JoinRequests");
        builder.HasKey(jr => jr.Id);

        builder.Property(jr => jr.ReferenceNumber)
            .IsRequired()
            .HasMaxLength(25);

        builder.Property(jr => jr.FullName)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("Arabic_CI_AI");

        builder.Property(jr => jr.Phone)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(jr => jr.Email).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.Address).HasMaxLength(500).IsRequired(false);
        builder.Property(jr => jr.Province).HasMaxLength(100).IsRequired(false);
        builder.Property(jr => jr.District).HasMaxLength(100).IsRequired(false);
        builder.Property(jr => jr.SubDistrict).HasMaxLength(100).IsRequired(false);
        builder.Property(jr => jr.EducationLevel).HasMaxLength(100).IsRequired(false);
        builder.Property(jr => jr.Specialization).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.Occupation).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.JobTitle).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.WorkPlace).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.Skills).HasMaxLength(500).IsRequired(false);
        builder.Property(jr => jr.Experiences).HasMaxLength(500).IsRequired(false);
        builder.Property(jr => jr.TrainingCourses).HasMaxLength(500).IsRequired(false);
        builder.Property(jr => jr.Languages).HasMaxLength(200).IsRequired(false);
        builder.Property(jr => jr.BenefitField).HasMaxLength(500).IsRequired(false);
        builder.Property(jr => jr.Notes).HasMaxLength(1000).IsRequired(false);
        builder.Property(jr => jr.RejectionReason).HasMaxLength(500).IsRequired(false);

        builder.Property(jr => jr.Status)
            .HasConversion<int>()
            .HasDefaultValue(RequestStatus.Pending)
            .HasSentinel(RequestStatus.Pending);

        builder.Property(jr => jr.Gender)
            .HasConversion<int>();

        builder.Property(jr => jr.SubmittedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Index on ReferenceNumber
        builder.HasIndex(jr => jr.ReferenceNumber).IsUnique().HasDatabaseName("IX_JoinRequests_ReferenceNumber");

        // Composite index for duplicate check in pending requests
        builder.HasIndex(jr => new { jr.Phone, jr.MovementId }).HasDatabaseName("IX_JoinRequests_Phone_Movement");
        builder.HasIndex(jr => jr.Status).HasDatabaseName("IX_JoinRequests_Status");

        // Relationship with Movement
        builder.HasOne(jr => jr.Movement)
            .WithMany(m => m.JoinRequests)
            .HasForeignKey(jr => jr.MovementId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with User (reviewer)
        builder.HasOne(jr => jr.ReviewedByUser)
            .WithMany()
            .HasForeignKey(jr => jr.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
