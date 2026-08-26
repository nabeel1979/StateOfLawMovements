using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class JoinRequest
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ReferenceNumber { get; set; } = string.Empty;

    // ─── البيانات الشخصية ──────────────────────────────────
    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Email { get; set; }

    public DateOnly? BirthDate { get; set; }

    public Gender? Gender { get; set; }

    // ─── بيانات السكن ──────────────────────────────────────
    [MaxLength(100)]
    public string? Province { get; set; }      // المحافظة

    [MaxLength(100)]
    public string? District { get; set; }      // القضاء

    [MaxLength(100)]
    public string? SubDistrict { get; set; }   // الناحية

    [MaxLength(500)]
    public string? Address { get; set; }       // العنوان التفصيلي

    // ─── البيانات العلمية ──────────────────────────────────
    [MaxLength(100)]
    public string? EducationLevel { get; set; }

    [MaxLength(200)]
    public string? Specialization { get; set; }

    // ─── البيانات المهنية ──────────────────────────────────
    [MaxLength(200)]
    public string? Occupation { get; set; }

    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [MaxLength(200)]
    public string? WorkPlace { get; set; }

    // ─── سنوات الخدمة الوظيفية ────────────────────────────
    public DateOnly? ServiceStartDate { get; set; }

    public int? ServiceYears { get; set; }

    // ─── الطاقات والخبرات ──────────────────────────────────
    [MaxLength(500)]
    public string? Skills { get; set; }

    [MaxLength(500)]
    public string? Experiences { get; set; }

    [MaxLength(500)]
    public string? TrainingCourses { get; set; }

    [MaxLength(200)]
    public string? Languages { get; set; }

    [MaxLength(500)]
    public string? BenefitField { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? PhotoPath { get; set; }         // صورة العضو

    // ─── حالة الطلب ────────────────────────────────────────
    public int MovementId { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Movement Movement { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
    public Member? ConvertedMember { get; set; }
}
