using System.ComponentModel.DataAnnotations;

namespace QanoonCoalition.Web.Models;

public class SystemConstant
{
    public int Id { get; set; }

    /// <summary>الفئة: انظر SysConst</summary>
    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Value { get; set; } = string.Empty;

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>وصف فئة ثوابت: عنوانها وأيقونتها وأين تُستخدم في النظام</summary>
public record SysConstCategory(string Key, string Label, string Icon, string UsedIn);

/// <summary>أسماء الفئات الثابتة - كل قائمة منسدلة قابلة للتحرير في النظام</summary>
public static class SysConst
{
    public const string EducationLevel = "EducationLevel";
    public const string BenefitField   = "BenefitField";
    public const string ManagerTitle   = "ManagerTitle";
    public const string Province       = "Province";

    public static readonly List<SysConstCategory> All = new()
    {
        new(EducationLevel, "التحصيل الدراسي", "fa-graduation-cap",
            "استمارة الانضمام وبيانات العضو"),
        new(BenefitField, "مجال الاستفادة من العضو", "fa-briefcase",
            "استمارة الانضمام وبيانات العضو وقبول الطلبات"),
        new(Province, "المحافظة", "fa-map-marker-alt",
            "سكن العضو وبيانات الحركة"),
        new(ManagerTitle, "صفة المسؤول", "fa-user-tie",
            "إنشاء وتعديل مسؤولي الحركات")
    };

    public static readonly Dictionary<string, string> Labels =
        All.ToDictionary(c => c.Key, c => c.Label);

    public static SysConstCategory? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(c => c.Key == key);

    /// <summary>القيم الافتراضية التي تُبذر عند أول تشغيل لكل فئة</summary>
    public static readonly Dictionary<string, string[]> Defaults = new()
    {
        [Province] = IraqiGovernorates.All
    };
}
