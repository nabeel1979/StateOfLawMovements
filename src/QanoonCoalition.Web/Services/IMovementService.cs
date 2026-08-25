using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public interface IMovementService
{
    Task<List<Movement>> GetAllAsync(bool includeInactive = false);
    Task<Movement?> GetByIdAsync(int id);
    Task<Movement?> GetByTokenAsync(string token);
    Task<Movement> CreateAsync(string name, string? logo, string? address, string? description,
        string? phone, string? email, string? website, int createdByUserId);
    Task UpdateAsync(int id, string name, string? logo, string? address, string? description,
        string? phone, string? email, string? website);
    Task SetActiveAsync(int id, bool isActive);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<(int Members, int Requests, int PendingRequests)> GetStatsAsync(int movementId);
    Task<List<Movement>> GetSummaryForAdminAsync();
}
