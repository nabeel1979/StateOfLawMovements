using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public interface IMemberService
{
    Task<(List<Member> Items, int Total)> SearchAsync(int? movementId, string? query,
        string? searchBy, int page, int pageSize);
    Task<Member?> GetByIdAsync(int id);
    Task<Member> AddManuallyAsync(Member member);
    Task UpdateAsync(Member member);
    Task<bool> PhoneExistsAsync(string phone, int? excludeId = null);
    Task<bool> NameExistsAsync(string name, int? excludeId = null);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    Task<int> GetTotalCountAsync(int? movementId = null);
}
