using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.MovementManager;

    /// <summary>null للـ Admin، وله قيمة للمسؤول المرتبط بحركة</summary>
    public int? MovementId { get; set; }

    /// <summary>صفة المسؤول: رئيس / نائب / موظف / إلخ</summary>
    [MaxLength(50)]
    public string? Title { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>إجبار تغيير كلمة المرور عند أول دخول أو بعد الإعادة</summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>رمز إعادة تعيين كلمة المرور</summary>
    [MaxLength(200)]
    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetExpiry { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public Movement? Movement { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
