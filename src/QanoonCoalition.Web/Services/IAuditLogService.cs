using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public interface IAuditLogService
{
    Task LogAsync(AuditAction action, string? entityType = null, string? entityId = null,
        object? oldValues = null, object? newValues = null, int? movementId = null,
        string? description = null);

    Task<(List<AuditLog> Items, int Total)> GetLogsAsync(
        int page, int pageSize, int? movementId = null,
        AuditAction? action = null, DateTime? from = null, DateTime? to = null);
}
