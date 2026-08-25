using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class AuditLog
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public int? MovementId { get; set; }

    public AuditAction Action { get; set; }

    [MaxLength(100)]
    public string? EntityType { get; set; }

    [MaxLength(50)]
    public string? EntityId { get; set; }

    /// <summary>القيم القديمة بصيغة JSON</summary>
    public string? OldValues { get; set; }

    /// <summary>القيم الجديدة بصيغة JSON</summary>
    public string? NewValues { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? UserAgent { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Movement? Movement { get; set; }
}
