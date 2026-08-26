using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class SystemConstantService
{
    private readonly AppDbContext _db;

    public SystemConstantService(AppDbContext db) => _db = db;

    /// <summary>جلب قيم فئة معينة مرتبة حسب DisplayOrder</summary>
    public async Task<List<string>> GetValuesAsync(string category) =>
        await _db.SystemConstants
            .Where(c => c.Category == category && c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.Value)
            .ToListAsync();

    /// <summary>جلب جميع الثوابت مجمعة حسب الفئة</summary>
    public async Task<List<SystemConstant>> GetAllAsync() =>
        await _db.SystemConstants
            .OrderBy(c => c.Category).ThenBy(c => c.DisplayOrder)
            .ToListAsync();

    public async Task<SystemConstant?> GetByIdAsync(int id) =>
        await _db.SystemConstants.FindAsync(id);

    public async Task CreateAsync(SystemConstant item)
    {
        var maxOrder = await _db.SystemConstants
            .Where(c => c.Category == item.Category)
            .MaxAsync(c => (int?)c.DisplayOrder) ?? 0;
        item.DisplayOrder = maxOrder + 1;
        _db.SystemConstants.Add(item);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SystemConstant item)
    {
        _db.SystemConstants.Update(item);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _db.SystemConstants.FindAsync(id);
        if (item != null)
        {
            _db.SystemConstants.Remove(item);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>عدد القيم وعدد النشط منها لكل فئة</summary>
    public async Task<Dictionary<string, (int Total, int Active)>> GetCountsAsync()
    {
        var rows = await _db.SystemConstants
            .GroupBy(c => c.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Count(),
                Active = g.Count(c => c.IsActive)
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.Category, r => (r.Total, r.Active));
    }

    public async Task ToggleActiveAsync(int id)
    {
        var item = await _db.SystemConstants.FindAsync(id);
        if (item == null) return;
        item.IsActive = !item.IsActive;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// يبدّل القيمة مع جارتها في نفس الفئة. الترتيب يحدد تسلسل الظهور
    /// في القوائم المنسدلة، ولذلك يُعاد ترقيمه أولاً لتفادي التساوي في القيم القديمة.
    /// </summary>
    public async Task MoveAsync(int id, bool up)
    {
        var item = await _db.SystemConstants.FindAsync(id);
        if (item == null) return;

        var siblings = await _db.SystemConstants
            .Where(c => c.Category == item.Category)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id)
            .ToListAsync();

        for (var i = 0; i < siblings.Count; i++)
            siblings[i].DisplayOrder = i + 1;

        var index = siblings.FindIndex(c => c.Id == id);
        var target = up ? index - 1 : index + 1;
        if (index >= 0 && target >= 0 && target < siblings.Count)
            (siblings[index].DisplayOrder, siblings[target].DisplayOrder) =
                (siblings[target].DisplayOrder, siblings[index].DisplayOrder);

        await _db.SaveChangesAsync();
    }

    public async Task<bool> ValueExistsAsync(string category, string value, int? excludeId = null)
    {
        var q = _db.SystemConstants.Where(c => c.Category == category && c.Value == value);
        if (excludeId.HasValue) q = q.Where(c => c.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
