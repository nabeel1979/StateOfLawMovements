namespace QanoonCoalition.Web.Models;

/// <summary>شرط فلترة واحد على الأعضاء: حقل + معامل + قيمة</summary>
public class MemberFilter
{
    public string? Field { get; set; }
    public string? Op { get; set; }
    public string? Val { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Field) ||
        (string.IsNullOrWhiteSpace(Val) && Op != "empty" && Op != "notempty");
}

/// <summary>كيف تُدمج شروط الفلترة معاً</summary>
public enum FilterMatch
{
    /// <summary>كل الشروط</summary>
    All = 0,
    /// <summary>أي شرط</summary>
    Any = 1
}

/// <summary>تعريف حقل قابل للفلترة - يُستخدم لبناء القوائم في الواجهة</summary>
public record MemberFilterField(string Key, string Label, MemberFilterKind Kind, string Property);

public enum MemberFilterKind
{
    Text,
    /// <summary>حقل قيمه من قائمة منسدلة - يُفلتر باختيار لا بكتابة</summary>
    Choice,
    Gender,
    Date
}

public static class MemberFilterFields
{
    public static readonly List<MemberFilterField> All = new()
    {
        new("name",           "الاسم الكامل",       MemberFilterKind.Text,   nameof(Member.FullName)),
        new("serial",         "الرقم التسلسلي",     MemberFilterKind.Text,   nameof(Member.SerialNumber)),
        new("phone",          "رقم الهاتف",         MemberFilterKind.Text,   nameof(Member.Phone)),
        new("email",          "البريد الإلكتروني",  MemberFilterKind.Text,   nameof(Member.Email)),
        new("benefit",        "مجال الاستفادة",     MemberFilterKind.Choice, nameof(Member.BenefitField)),
        new("province",       "المحافظة",           MemberFilterKind.Choice, nameof(Member.Province)),
        new("district",       "القضاء",             MemberFilterKind.Text,   nameof(Member.District)),
        new("subdistrict",    "الناحية",            MemberFilterKind.Text,   nameof(Member.SubDistrict)),
        new("address",        "العنوان",            MemberFilterKind.Text,   nameof(Member.Address)),
        new("education",      "التحصيل الدراسي",    MemberFilterKind.Choice, nameof(Member.EducationLevel)),
        new("specialization", "الاختصاص",           MemberFilterKind.Text,   nameof(Member.Specialization)),
        new("occupation",     "المهنة",             MemberFilterKind.Text,   nameof(Member.Occupation)),
        new("jobtitle",       "العنوان الوظيفي",    MemberFilterKind.Text,   nameof(Member.JobTitle)),
        new("workplace",      "مكان العمل",         MemberFilterKind.Text,   nameof(Member.WorkPlace)),
        new("skills",         "المهارات",           MemberFilterKind.Text,   nameof(Member.Skills)),
        new("languages",      "اللغات",             MemberFilterKind.Text,   nameof(Member.Languages)),
        new("notes",          "الملاحظات",          MemberFilterKind.Text,   nameof(Member.Notes)),
        new("gender",         "الجنس",              MemberFilterKind.Gender, nameof(Member.Gender)),
        new("birthdate",      "تاريخ الميلاد",      MemberFilterKind.Date,   nameof(Member.BirthDate)),
        new("createdat",      "تاريخ الإضافة",      MemberFilterKind.Date,   nameof(Member.CreatedAt))
    };

    public static MemberFilterField? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(f => f.Key == key);

    public static readonly Dictionary<string, string> TextOperators = new()
    {
        ["contains"]    = "يحتوي",
        ["notcontains"] = "لا يحتوي",
        ["eq"]          = "يساوي",
        ["startswith"]  = "يبدأ بـ",
        ["endswith"]    = "ينتهي بـ",
        ["empty"]       = "فارغ",
        ["notempty"]    = "غير فارغ"
    };

    public static readonly Dictionary<string, string> DateOperators = new()
    {
        ["on"]       = "بتاريخ",
        ["before"]   = "قبل",
        ["after"]    = "بعد",
        ["empty"]    = "فارغ",
        ["notempty"] = "غير فارغ"
    };

    public static readonly Dictionary<string, string> GenderOperators = new()
    {
        ["eq"]       = "يساوي",
        ["empty"]    = "فارغ",
        ["notempty"] = "غير فارغ"
    };

    public static readonly Dictionary<string, string> ChoiceOperators = new()
    {
        ["eq"]       = "يساوي",
        ["neq"]      = "لا يساوي",
        ["contains"] = "يحتوي",
        ["empty"]    = "فارغ",
        ["notempty"] = "غير فارغ"
    };

    public static Dictionary<string, string> OperatorsFor(MemberFilterKind kind) => kind switch
    {
        MemberFilterKind.Date   => DateOperators,
        MemberFilterKind.Gender => GenderOperators,
        MemberFilterKind.Choice => ChoiceOperators,
        _                       => TextOperators
    };
}
