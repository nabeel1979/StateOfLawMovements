using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public interface IAuthService
{
    Task<User?> ValidateCredentialsAsync(string email, string password);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateUserAsync(string fullName, string email, string password, UserRole role, int? movementId);
    Task UpdatePasswordAsync(int userId, string newPassword);
    Task SetLastLoginAsync(int userId);
}
