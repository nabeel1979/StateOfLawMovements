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

    [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
    public DateOnly? BirthDate { get; set; }

    [Required(ErrorMessage = "الجنس مطلوب")]
    public Gender? Gender { get; set; }

    // ─── بيانات السكن ──────────────────────────────────────
    [Required(ErrorMessage = "المحافظة مطلوبة")]
    [MaxLength(100)]
    public string? Province { get; set; }

    [Required(ErrorMessage = "القضاء مطلوب")]
    [MaxLength(100)]
    public string? District { get; set; }

    [Required(ErrorMessage = "الناحية مطلوبة")]
    [MaxLength(100)]
    public string? SubDistrict { get; set; }

    [Required(ErrorMessage = "العنوان التفصيلي مطلوب")]
    [MaxLength(500)]
    public string? Address { get; set; }

    // ─── البيانات العلمية ──────────────────────────────────
    [Required(ErrorMessage = "التحصيل الدراسي مطلوب")]
    [MaxLength(100)]
    public string? EducationLevel { get; set; }

    [Required(ErrorMessage = "الاختصاص / التخصص مطلوب")]
    [MaxLength(200)]
    public string? Specialization { get; set; }

    // ─── البيانات المهنية ──────────────────────────────────
    [Required(ErrorMessage = "المهنة مطلوبة")]
    [MaxLength(200)]
    public string? Occupation { get; set; }

    [Required(ErrorMessage = "العنوان الوظيفي مطلوب")]
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    [Required(ErrorMessage = "مكان العمل مطلوب")]
    [MaxLength(200)]
    public string? WorkPlace { get; set; }

    // ─── سنوات الخدمة الوظيفية (اختيارية) ────────────────
    public DateOnly? ServiceStartDate { get; set; }

    public int? ServiceYears { get; set; }

    // ─── الطاقات والخبرات ──────────────────────────────────
    [Required(ErrorMessage = "المهارات مطلوبة")]
    [MaxLength(500)]
    public string? Skills { get; set; }

    [Required(ErrorMessage = "الخبرات مطلوبة")]
    [MaxLength(500)]
    public string? Experiences { get; set; }

    [Required(ErrorMessage = "الدورات التدريبية مطلوبة")]
    [MaxLength(500)]
    public string? TrainingCourses { get; set; }

    [Required(ErrorMessage = "اللغات مطلوبة")]
    [MaxLength(200)]
    public string? Languages { get; set; }

    [Required(ErrorMessage = "مجال الاستفادة مطلوب")]
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
