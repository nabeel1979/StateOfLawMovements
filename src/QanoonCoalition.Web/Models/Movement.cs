using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class Movement
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NameSlug { get; set; } = string.Empty;

    /// <summary>معرف آمن فريد للرابط العام لطلب الانضمام</summary>
    [Required]
    [MaxLength(64)]
    public string PublicToken { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Logo { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    /// <summary>المحافظة التي تتبعها الحركة</summary>
    [MaxLength(50)]
    public string? Governorate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? CreatedByUserId { get; set; }

    // Navigation
    public User? CreatedByUser { get; set; }
    public ICollection<User> Managers { get; set; } = new List<User>();
    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<JoinRequest> JoinRequests { get; set; } = new List<JoinRequest>();
    public ICollection<MovementConstant> Constants { get; set; } = new List<MovementConstant>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
