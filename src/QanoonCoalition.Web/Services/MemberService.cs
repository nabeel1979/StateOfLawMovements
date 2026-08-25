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

    public async Task<(List<Member> Items, int Total)> SearchAsync(int? movementId, string? query,
        string? searchBy, int page, int pageSize)
    {
        var q = _db.Members.Include(m => m.Movement).AsQueryable();

        if (movementId.HasValue)
            q = q.Where(m => m.MovementId == movementId);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            q = searchBy switch
            {
                "serial" => q.Where(m => m.SerialNumber.Contains(query)),
                "phone"  => q.Where(m => m.Phone.Contains(query)),
                "email"  => q.Where(m => m.Email != null && m.Email.Contains(query)),
                _        => q.Where(m => m.FullName.Contains(query))
            };
        }

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
