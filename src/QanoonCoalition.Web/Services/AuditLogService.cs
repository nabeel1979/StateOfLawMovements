using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(AuditAction action, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, int? movementId = null,
        string? description = null)
    {
        var http = _httpContextAccessor.HttpContext;
        var userIdStr = http?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = userIdStr != null ? int.Parse(userIdStr) : null;

        var log = new AuditLog
        {
            UserId = userId,
            MovementId = movementId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = http?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request?.Headers["User-Agent"].ToString(),
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<(List<AuditLog> Items, int Total)> GetLogsAsync(
        int page, int pageSize, int? movementId = null,
        AuditAction? action = null, DateTime? from = null, DateTime? to = null)
    {
        var q = _db.AuditLogs
            .Include(a => a.User)
            .Include(a => a.Movement)
            .AsQueryable();

        if (movementId.HasValue)
            q = q.Where(a => a.MovementId == movementId);
        if (action.HasValue)
            q = q.Where(a => a.Action == action);
        if (from.HasValue)
            q = q.Where(a => a.CreatedAt >= from.Value);
        if (to.HasValue)
            q = q.Where(a => a.CreatedAt <= to.Value.AddDays(1));

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
