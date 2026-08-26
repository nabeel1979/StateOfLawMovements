using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class MemberService : IMemberService
{
    private readonly AppDbContext _db;
    private readonly ISerialNumberService _serial;

    public MemberService(AppDbContext db, ISerialNumberService serial)
    {
        _db = db;
        _serial = serial;
    }

    public async Task<(List<Member> Items, int Total)> SearchAsync(int? movementId,
        List<MemberFilter>? filters, FilterMatch match, int page, int pageSize)
    {
        var q = _db.Members.Include(m => m.Movement).AsQueryable();

        if (movementId.HasValue)
            q = q.Where(m => m.MovementId == movementId);

        var predicate = MemberFilterBuilder.Build(filters, match);
        if (predicate != null)
            q = q.Where(predicate);

        var total = await q.CountAsync();
        var items = await q.OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Member?> GetByIdAsync(int id) =>
        await _db.Members.Include(m => m.Movement).Include(m => m.ApprovedByUser)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Member> AddManuallyAsync(Member member)
    {
        await ValidateUniquenessAsync(member.FullName, member.Phone, member.Email);
        member.SerialNumber = await _serial.GenerateAsync();
        member.CreatedAt = DateTime.UtcNow;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();
        return member;
    }

    public async Task UpdateAsync(Member member)
    {
        await ValidateUniquenessAsync(member.FullName, member.Phone, member.Email, member.Id);
        _db.Members.Update(member);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
        if (member == null) return;

        // طلب الانضمام يبقى كما هو: المفتاح الأجنبي على العضو لا على الطلب
        _db.Members.Remove(member);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> PhoneExistsAsync(string phone, int? excludeId = null)
    {
        var q = _db.Members.Where(m => m.Phone == phone);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId);
        return await q.AnyAsync();
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        var q = _db.Members.Where(m => m.FullName == name);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId);
        return await q.AnyAsync();
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var q = _db.Members.Where(m => m.Email != null && m.Email == email);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId);
        return await q.AnyAsync();
    }

    public async Task<int> GetTotalCountAsync(int? movementId = null)
    {
        var q = _db.Members.AsQueryable();
        if (movementId.HasValue) q = q.Where(m => m.MovementId == movementId);
        return await q.CountAsync();
    }

    private async Task ValidateUniquenessAsync(string fullName, string phone, string? email, int? excludeId = null)
    {
        if (await NameExistsAsync(fullName, excludeId))
            throw new InvalidOperationException("الاسم مستخدم مسبقاً في إحدى الحركات");
        if (await PhoneExistsAsync(phone, excludeId))
            throw new InvalidOperationException("رقم الهاتف مستخدم مسبقاً في إحدى الحركات");
        if (!string.IsNullOrEmpty(email) && await EmailExistsAsync(email, excludeId))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً في إحدى الحركات");
    }
}
