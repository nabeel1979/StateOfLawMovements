using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

// ─── طلب مواطن ────────────────────────────────────────────────────────────────
public class CitizenRequest
{
    public int Id { get; set; }

    public int MovementId { get; set; }

    /// <summary>كود فريد يُولَّد تلقائياً. مثال: CRQ-20260827-00001</summary>
    [MaxLength(30)]
    public string RequestCode { get; set; } = string.Empty;

    public DateTime RequestDate { get; set; } = DateTime.UtcNow;

    [Required, MaxLength(200)]
    public string ApplicantName { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ApplicantPhone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? ApplicantEmail { get; set; }

    [MaxLength(500)]
    public string? ContactInformation { get; set; }

    [Required, MaxLength(500)]
    public string RequestSubject { get; set; } = string.Empty;

    [Required]
    public string RequestDetails { get; set; } = string.Empty;

    /// <summary>عضو مستلم الطلب — مرتبط بأعضاء الحركة نفسها</summary>
    public int? ReceivedByMemberId { get; set; }

    /// <summary>الجهة الموجَّه إليها الطلب</summary>
    public int? DestinationId { get; set; }

    [MaxLength(500)]
    public string? DestinationSubText { get; set; }

    /// <summary>يُسجَّل تلقائياً عند تحويل الحالة إلى "مرسل"</summary>
    public DateTime? SentDate { get; set; }

    /// <summary>يُسجَّل تلقائياً عند تحويل الحالة إلى "إجابة عنه" مع إمكانية التعديل</summary>
    public DateTime? AnswerDate { get; set; }

    public int StatusId { get; set; }

    public int CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Movement Movement { get; set; } = null!;
    public Member? ReceivedByMember { get; set; }
    public RequestDestination? Destination { get; set; }
    public CitizenRequestStatus Status { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;

    public ICollection<CitizenRequestAttachment> Attachments { get; set; } = new List<CitizenRequestAttachment>();
    public ICollection<CitizenRequestStatusHistory> StatusHistory { get; set; } = new List<CitizenRequestStatusHistory>();
}

// ─── جهة الطلب ────────────────────────────────────────────────────────────────
public class RequestDestination
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>نوع الجهة: وزارة، هيئة، دائرة… إلخ</summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<CitizenRequest> CitizenRequests { get; set; } = new List<CitizenRequest>();
}

// ─── حالة الطلب ───────────────────────────────────────────────────────────────
public class CitizenRequestStatus
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string ColorClass { get; set; } = "secondary";   // bootstrap badge color

    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;

    public ICollection<CitizenRequest> CitizenRequests { get; set; } = new List<CitizenRequest>();
}

// ─── نوع الوثيقة ──────────────────────────────────────────────────────────────
public class DocumentType
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    public ICollection<CitizenRequestAttachment> Attachments { get; set; } = new List<CitizenRequestAttachment>();
}

// ─── مرفق الطلب ───────────────────────────────────────────────────────────────
public class CitizenRequestAttachment
{
    public int Id { get; set; }

    public int CitizenRequestId { get; set; }

    [Required, MaxLength(300)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public int? DocumentTypeId { get; set; }

    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CitizenRequest CitizenRequest { get; set; } = null!;
    public DocumentType? DocumentType { get; set; }
    public User UploadedByUser { get; set; } = null!;
}

// ─── سجل تغييرات الحالة ───────────────────────────────────────────────────────
public class CitizenRequestStatusHistory
{
    public int Id { get; set; }

    public int CitizenRequestId { get; set; }

    public int? FromStatusId { get; set; }
    public int ToStatusId { get; set; }

    public int ChangedByUserId { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navigation
    public CitizenRequest CitizenRequest { get; set; } = null!;
    public CitizenRequestStatus? FromStatus { get; set; }
    public CitizenRequestStatus ToStatus { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
