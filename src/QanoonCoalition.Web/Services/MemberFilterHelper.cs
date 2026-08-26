using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public static class MemberFilterHelper
{
    /// <summary>
    /// يُسقط الصفوف الفارغة ويضمن وجود صف واحد على الأقل لعرضه في منشئ الفلاتر.
    /// </summary>
    public static List<MemberFilter> Normalize(List<MemberFilter>? filters)
    {
        var kept = filters?.Where(f => f != null && !f.IsEmpty).ToList() ?? new List<MemberFilter>();
        if (kept.Count == 0)
            kept.Add(new MemberFilter { Field = "name", Op = "contains" });
        return kept;
    }

    /// <summary>عدد الشروط الفعّالة - يُستخدم للشارة على زر الفلاتر</summary>
    public static int ActiveCount(List<MemberFilter>? filters) =>
        filters?.Count(f => f != null && !f.IsEmpty) ?? 0;
}
