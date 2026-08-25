using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class SystemConstant
{
    public int Id { get; set; }

    /// <summary>الفئة: EducationLevel | BenefitField | ManagerTitle</summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Value { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>أسماء الفئات الثابتة</summary>
public static class SysConst
{
    public const string EducationLevel = "EducationLevel";
    public const string BenefitField   = "BenefitField";
    public const string ManagerTitle   = "ManagerTitle";

    public static readonly Dictionary<string, string> Labels = new()
    {
        [EducationLevel] = "التحصيل الدراسي",
        [BenefitField]   = "مجال الاستفادة من العضو",
        [ManagerTitle]   = "صفة المسؤول",
    };
}
