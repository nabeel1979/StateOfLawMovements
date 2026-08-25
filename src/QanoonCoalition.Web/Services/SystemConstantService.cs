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

    public async Task<bool> ValueExistsAsync(string category, string value, int? excludeId = null)
    {
        var q = _db.SystemConstants.Where(c => c.Category == category && c.Value == value);
        if (excludeId.HasValue) q = q.Where(c => c.Id != excludeId.Value);
        return await q.AnyAsync();
    }
}
