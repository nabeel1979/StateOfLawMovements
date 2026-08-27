namespace QanoonCoalition.Web.Models;

/// <summary>
/// كتالوج الحقول القابلة للفلترة على طلبات المواطنين.
/// Property يقبل مساراً مركّباً مثل "Status.Name" للوصول عبر خصائص التنقل.
/// </summary>
public static class CitizenRequestFilterFields
{
    /// <summary>مفتاح خاص يُفلتر عبر مجموعة المرفقات لا عبر خاصية مباشرة</summary>
    public const string DocTypeKey = "doctype";

    public static readonly List<MemberFilterField> All = new()
    {
        new("code",        "كود الطلب",           MemberFilterKind.Text,   nameof(CitizenRequest.RequestCode)),
        new("applicant",   "اسم مقدم الطلب",      MemberFilterKind.Text,   nameof(CitizenRequest.ApplicantName)),
        new("phone",       "رقم الهاتف",          MemberFilterKind.Text,   nameof(CitizenRequest.ApplicantPhone)),
        new("email",       "البريد الإلكتروني",   MemberFilterKind.Text,   nameof(CitizenRequest.ApplicantEmail)),
        new("contact",     "معلومات اتصال إضافية", MemberFilterKind.Text,  nameof(CitizenRequest.ContactInformation)),
        new("subject",     "موضوع الطلب",         MemberFilterKind.Text,   nameof(CitizenRequest.RequestSubject)),
        new("details",     "تفاصيل الطلب",        MemberFilterKind.Text,   nameof(CitizenRequest.RequestDetails)),
        new("status",      "الحالة",              MemberFilterKind.Choice, "Status.Name"),
        new("destination", "الجهة الموجَّه إليها", MemberFilterKind.Choice, "Destination.Name"),
        new("destsub",     "تفصيل الجهة",         MemberFilterKind.Text,   nameof(CitizenRequest.DestinationSubText)),
        new("receiver",    "مستلم الطلب",         MemberFilterKind.Choice, "ReceivedByMember.FullName"),
        new(DocTypeKey,    "نوع الوثيقة",         MemberFilterKind.Choice, "Attachments.DocumentType.Name"),
        new("requestdate", "تاريخ الطلب",         MemberFilterKind.Date,   nameof(CitizenRequest.RequestDate)),
        new("sentdate",    "تاريخ الإرسال",       MemberFilterKind.Date,   nameof(CitizenRequest.SentDate)),
        new("answerdate",  "تاريخ الإجابة",       MemberFilterKind.Date,   nameof(CitizenRequest.AnswerDate))
    };

    public static MemberFilterField? Find(string? key) =>
        string.IsNullOrWhiteSpace(key) ? null : All.FirstOrDefault(f => f.Key == key);

    /// <summary>الشرط الافتراضي المعروض عند عدم وجود فلاتر</summary>
    public static List<MemberFilter> Normalize(List<MemberFilter>? filters)
    {
        var kept = filters?.Where(f => f != null && !f.IsEmpty).ToList() ?? new List<MemberFilter>();
        if (kept.Count == 0)
            kept.Add(new MemberFilter { Field = "applicant", Op = "contains" });
        return kept;
    }
}
