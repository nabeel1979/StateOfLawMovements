using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class Member
{
    public int Id { get; set; }

    /// <summary>رقم تسلسلي من 8 أرقام - فريد على مستوى كل الحركات</summary>
    [Required]
    [MaxLength(8)]
    [MinLength(8)]
    public string SerialNumber { get; set; } = string.Empty;

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
    public string? EducationLevel { get; set; }  // التحصيل الدراسي

    [MaxLength(200)]
    public string? Specialization { get; set; }  // الاختصاص

    // ─── البيانات المهنية ──────────────────────────────────
    [MaxLength(200)]
    public string? Occupation { get; set; }      // المهنة

    [MaxLength(200)]
    public string? JobTitle { get; set; }        // العنوان الوظيفي

    [MaxLength(200)]
    public string? WorkPlace { get; set; }       // مكان العمل

    // ─── سنوات الخدمة الوظيفية ────────────────────────────
    public DateOnly? ServiceStartDate { get; set; }  // تاريخ المباشرة بالوظيفة

    public int? ServiceYears { get; set; }           // سنوات الخدمة

    // ─── الطاقات والخبرات ──────────────────────────────────
    [MaxLength(500)]
    public string? Skills { get; set; }          // المهارات

    [MaxLength(500)]
    public string? Experiences { get; set; }     // الخبرات

    [MaxLength(500)]
    public string? TrainingCourses { get; set; } // الدورات التدريبية

    [MaxLength(200)]
    public string? Languages { get; set; }       // اللغات

    [MaxLength(500)]
    public string? BenefitField { get; set; }    // مجال الاستفادة من العضو

    [MaxLength(500)]
    public string? Notes { get; set; }           // ملاحظات

    [MaxLength(500)]
    public string? PhotoPath { get; set; }       // مسار صورة العضو

    // ─── علاقات ────────────────────────────────────────────
    public int MovementId { get; set; }

    public int? ApprovedByUserId { get; set; }

    public int? JoinRequestId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    // Navigation
    public Movement Movement { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public JoinRequest? JoinRequest { get; set; }
}
