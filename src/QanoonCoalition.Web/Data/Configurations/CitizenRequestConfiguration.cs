using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Data.Configurations;

public class CitizenRequestConfiguration : IEntityTypeConfiguration<CitizenRequest>
{
    public void Configure(EntityTypeBuilder<CitizenRequest> b)
    {
        b.ToTable("CitizenRequests");
        b.HasKey(x => x.Id);

        b.Property(x => x.RequestCode).IsRequired().HasMaxLength(30);
        b.HasIndex(x => x.RequestCode).IsUnique();
        b.HasIndex(x => x.MovementId);
        b.HasIndex(x => x.StatusId);
        b.HasIndex(x => x.RequestDate);

        b.HasOne(x => x.Movement)
            .WithMany()
            .HasForeignKey(x => x.MovementId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ReceivedByMember)
            .WithMany()
            .HasForeignKey(x => x.ReceivedByMemberId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Destination)
            .WithMany(d => d.CitizenRequests)
            .HasForeignKey(x => x.DestinationId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.Status)
            .WithMany(s => s.CitizenRequests)
            .HasForeignKey(x => x.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RequestDestinationConfiguration : IEntityTypeConfiguration<RequestDestination>
{
    public void Configure(EntityTypeBuilder<RequestDestination> b)
    {
        b.ToTable("RequestDestinations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.HasIndex(x => x.DisplayOrder);

        b.HasData(
            new RequestDestination { Id = 1, Name = "وزارة الداخلية",       Type = "وزارة",    DisplayOrder = 1  },
            new RequestDestination { Id = 2, Name = "وزارة الخارجية",       Type = "وزارة",    DisplayOrder = 2  },
            new RequestDestination { Id = 3, Name = "وزارة المالية",        Type = "وزارة",    DisplayOrder = 3  },
            new RequestDestination { Id = 4, Name = "وزارة التعليم",        Type = "وزارة",    DisplayOrder = 4  },
            new RequestDestination { Id = 5, Name = "وزارة الصحة",          Type = "وزارة",    DisplayOrder = 5  },
            new RequestDestination { Id = 6, Name = "وزارة العدل",          Type = "وزارة",    DisplayOrder = 6  },
            new RequestDestination { Id = 7, Name = "وزارة الكهرباء",       Type = "وزارة",    DisplayOrder = 7  },
            new RequestDestination { Id = 8, Name = "وزارة الإعمار",        Type = "وزارة",    DisplayOrder = 8  },
            new RequestDestination { Id = 9, Name = "وزارة الاتصالات",      Type = "وزارة",    DisplayOrder = 9  },
            new RequestDestination { Id = 10, Name = "وزارة الموارد المائية", Type = "وزارة", DisplayOrder = 10 },
            new RequestDestination { Id = 11, Name = "هيئة النزاهة",        Type = "هيئة",     DisplayOrder = 11 },
            new RequestDestination { Id = 12, Name = "هيئة الاستثمار",      Type = "هيئة",     DisplayOrder = 12 },
            new RequestDestination { Id = 13, Name = "مجلس القضاء الأعلى",  Type = "هيئة",     DisplayOrder = 13 },
            new RequestDestination { Id = 14, Name = "الأمانة العامة لمجلس الوزراء", Type = "دائرة", DisplayOrder = 14 },
            new RequestDestination { Id = 15, Name = "محافظة بغداد",        Type = "محافظة",   DisplayOrder = 15 },
            new RequestDestination { Id = 16, Name = "محافظة البصرة",       Type = "محافظة",   DisplayOrder = 16 },
            new RequestDestination { Id = 17, Name = "محافظة النجف",        Type = "محافظة",   DisplayOrder = 17 },
            new RequestDestination { Id = 18, Name = "محافظة كربلاء",       Type = "محافظة",   DisplayOrder = 18 },
            new RequestDestination { Id = 19, Name = "مؤسسة الشهداء",       Type = "مؤسسة",    DisplayOrder = 19 },
            new RequestDestination { Id = 20, Name = "جهة أخرى",            Type = "أخرى",     DisplayOrder = 20 }
        );
    }
}

public class CitizenRequestStatusConfiguration : IEntityTypeConfiguration<CitizenRequestStatus>
{
    public void Configure(EntityTypeBuilder<CitizenRequestStatus> b)
    {
        b.ToTable("CitizenRequestStatuses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);
        b.Property(x => x.ColorClass).HasMaxLength(20);
        b.HasIndex(x => x.DisplayOrder);

        b.HasData(
            new CitizenRequestStatus { Id = 1, Name = "مستلم",       ColorClass = "warning", DisplayOrder = 1, IsDefault = true },
            new CitizenRequestStatus { Id = 2, Name = "مرسل",        ColorClass = "primary", DisplayOrder = 2 },
            new CitizenRequestStatus { Id = 3, Name = "إجابة عنه",   ColorClass = "info",    DisplayOrder = 3 },
            new CitizenRequestStatus { Id = 4, Name = "منجز",        ColorClass = "success", DisplayOrder = 4 }
        );
    }
}

public class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> b)
    {
        b.ToTable("DocumentTypes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(100);

        b.HasData(
            new DocumentType { Id = 1, Name = "كتاب",         DisplayOrder = 1 },
            new DocumentType { Id = 2, Name = "طلب",          DisplayOrder = 2 },
            new DocumentType { Id = 3, Name = "كتاب إرسال",   DisplayOrder = 3 },
            new DocumentType { Id = 4, Name = "إجابة",        DisplayOrder = 4 }
        );
    }
}

public class CitizenRequestAttachmentConfiguration : IEntityTypeConfiguration<CitizenRequestAttachment>
{
    public void Configure(EntityTypeBuilder<CitizenRequestAttachment> b)
    {
        b.ToTable("CitizenRequestAttachments");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CitizenRequestId);

        b.HasOne(x => x.CitizenRequest)
            .WithMany(r => r.Attachments)
            .HasForeignKey(x => x.CitizenRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.DocumentType)
            .WithMany(d => d.Attachments)
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(x => x.UploadedByUser)
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CitizenRequestStatusHistoryConfiguration : IEntityTypeConfiguration<CitizenRequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<CitizenRequestStatusHistory> b)
    {
        b.ToTable("CitizenRequestStatusHistory");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.CitizenRequestId);

        b.HasOne(x => x.CitizenRequest)
            .WithMany(r => r.StatusHistory)
            .HasForeignKey(x => x.CitizenRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.FromStatus)
            .WithMany()
            .HasForeignKey(x => x.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ToStatus)
            .WithMany()
            .HasForeignKey(x => x.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
