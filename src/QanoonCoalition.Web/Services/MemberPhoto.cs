namespace QanoonCoalition.Web.Services;

/// <summary>
/// قاعدة واحدة لصور الأعضاء: الامتدادات المسموحة والحد الأقصى للحجم،
/// تستخدمها استمارة الانضمام وصفحات إضافة وتعديل العضو.
/// </summary>
public static class MemberPhoto
{
    public static readonly string[] Extensions = { ".jpg", ".jpeg", ".png" };

    /// <summary>قيمة سمة accept في حقل الملف</summary>
    public const string Accept = ".jpg,.jpeg,.png,image/jpeg,image/png";

    public const long MaxBytes = 3 * 1024 * 1024;

    public const string Hint = "JPG أو PNG فقط، بحجم أقصى 3 ميغابايت";

    /// <summary>
    /// يتحقق من الامتداد والحجم. الامتداد هو الأساس لأنه ما يُحفظ به الملف،
    /// ونوع المحتوى المرسل من المتصفح قابل للتلاعب فلا يُعتمد عليه وحده.
    /// </summary>
    public static bool IsValid(IFormFile file, out string error)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!Extensions.Contains(ext))
        {
            error = "صيغة الصورة غير مدعومة. الصيغ المقبولة: JPG أو PNG فقط";
            return false;
        }

        if (file.Length > MaxBytes)
        {
            error = "حجم الصورة يتجاوز 3 ميغابايت";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>الامتداد المُعتمد للحفظ، وتُوحَّد jpeg إلى jpg</summary>
    public static string SafeExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext == ".jpeg" ? ".jpg" : ext;
    }
}
