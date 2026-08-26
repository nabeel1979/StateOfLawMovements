using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

/// <summary>
/// يبني قوائم خيارات الفلترة للحقول المنسدلة.
/// كل قائمة = القيم المعرّفة في النظام + أي قيمة موجودة فعلاً في بيانات الأعضاء،
/// حتى لا تبقى قيمة قديمة في القاعدة غير قابلة للفلترة بعد تعديل ثوابت النظام.
/// </summary>
public class MemberFilterOptionsService
{
    private readonly AppDbContext _db;
    private readonly SystemConstantService _sysConst;

    public MemberFilterOptionsService(AppDbContext db, SystemConstantService sysConst)
    {
        _db = db;
        _sysConst = sysConst;
    }

    /// <summary>القوائم مفتاحها هو مفتاح حقل الفلترة (province / education / benefit)</summary>
    public async Task<Dictionary<string, List<string>>> GetAsync(int? movementId)
    {
        var members = _db.Members.AsQueryable();
        if (movementId.HasValue)
            members = members.Where(m => m.MovementId == movementId.Value);

        var provincesInUse = await DistinctAsync(members, m => m.Province);
        var educationInUse = await DistinctAsync(members, m => m.EducationLevel);
        var benefitsInUse  = await DistinctAsync(members, m => m.BenefitField);

        return new Dictionary<string, List<string>>
        {
            ["province"]  = Merge(await _sysConst.GetValuesAsync(SysConst.Province), provincesInUse),
            ["education"] = Merge(await _sysConst.GetValuesAsync(SysConst.EducationLevel), educationInUse),
            ["benefit"]   = Merge(await _sysConst.GetValuesAsync(SysConst.BenefitField), benefitsInUse)
        };
    }

    private static async Task<List<string>> DistinctAsync(
        IQueryable<Member> members, Expression<Func<Member, string?>> selector)
    {
        var values = await members.Select(selector).Distinct().ToListAsync();
        return values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!).ToList();
    }

    private static List<string> Merge(IEnumerable<string> defined, IEnumerable<string> inUse)
    {
        var seen = new HashSet<string>();
        var result = new List<string>();

        foreach (var value in defined.Select(v => v.Trim()).Where(v => v.Length > 0))
            if (seen.Add(value)) result.Add(value);

        foreach (var value in inUse.Select(v => v.Trim()).Where(v => v.Length > 0).OrderBy(v => v, StringComparer.CurrentCulture))
            if (seen.Add(value)) result.Add(value);

        return result;
    }
}
