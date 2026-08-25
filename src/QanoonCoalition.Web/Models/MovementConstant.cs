using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class MovementConstant
{
    public int Id { get; set; }

    public int MovementId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DataType { get; set; } = "text";

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public Movement Movement { get; set; } = null!;
}
