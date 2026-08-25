using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class MovementService : IMovementService
{
    private readonly AppDbContext _db;

    public MovementService(AppDbContext db) => _db = db;

    public async Task<List<Movement>> GetAllAsync(bool includeInactive = false)
    {
        var q = _db.Movements.AsQueryable();
        if (!includeInactive) q = q.Where(m => m.IsActive);
        return await q.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<Movement?> GetByIdAsync(int id) =>
        await _db.Movements
            .Include(m => m.Managers)
            .Include(m => m.Constants)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<Movement?> GetByTokenAsync(string token) =>
        await _db.Movements
            .Include(m => m.Constants)
            .FirstOrDefaultAsync(m => m.PublicToken == token && m.IsActive);

    public async Task<Movement> CreateAsync(string name, string? logo, string? address,
        string? description, string? phone, string? email, string? website, int createdByUserId)
    {
        if (await _db.Movements.AnyAsync(m => m.Name == name))
            throw new InvalidOperationException("اسم الحركة مستخدم مسبقاً");

        var movement = new Movement
        {
            Name = name,
            NameSlug = GenerateSlug(name),
            PublicToken = GenerateToken(),
            Logo = logo,
            Address = address,
            Description = description,
            Phone = phone,
            Email = email,
            Website = website,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        _db.Movements.Add(movement);
        await _db.SaveChangesAsync();
        return movement;
    }

    public async Task UpdateAsync(int id, string name, string? logo, string? address,
        string? description, string? phone, string? email, string? website)
    {
        var movement = await _db.Movements.FindAsync(id)
            ?? throw new InvalidOperationException("الحركة غير موجودة");

        if (await _db.Movements.AnyAsync(m => m.Name == name && m.Id != id))
            throw new InvalidOperationException("اسم الحركة مستخدم مسبقاً");

        movement.Name = name;
        movement.NameSlug = GenerateSlug(name);
        movement.Logo = logo;
        movement.Address = address;
        movement.Description = description;
        movement.Phone = phone;
        movement.Email = email;
        movement.Website = website;

        await _db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        var movement = await _db.Movements.FindAsync(id)
            ?? throw new InvalidOperationException("الحركة غير موجودة");
        movement.IsActive = isActive;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        var q = _db.Movements.Where(m => m.Name == name);
        if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId);
        return await q.AnyAsync();
    }

    public async Task<(int Members, int Requests, int PendingRequests)> GetStatsAsync(int movementId)
    {
        var members = await _db.Members.CountAsync(m => m.MovementId == movementId);
        var requests = await _db.JoinRequests.CountAsync(r => r.MovementId == movementId);
        var pending = await _db.JoinRequests.CountAsync(r => r.MovementId == movementId && r.Status == RequestStatus.Pending);
        return (members, requests, pending);
    }

    public async Task<List<Movement>> GetSummaryForAdminAsync() =>
        await _db.Movements
            .Include(m => m.Managers)
            .OrderBy(m => m.Name)
            .ToListAsync();

    private static string GenerateSlug(string name)
    {
        var slug = name.Trim().ToLower().Replace(" ", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^\u0600-\u06FF\w\-]", "");
        return slug;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("/", "_").Replace("+", "-").Replace("=", "")[..22];
}
