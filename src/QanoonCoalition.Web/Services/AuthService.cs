using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) => _db = db;

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.Movement)
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        return user;
    }

    public async Task<User?> GetByIdAsync(int id) =>
        await _db.Users.Include(u => u.Movement).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User> CreateUserAsync(string fullName, string email, string password,
        UserRole role, int? movementId)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("البريد الإلكتروني مستخدم مسبقاً");

        var user = new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            MovementId = movementId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdatePasswordAsync(int userId, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("المستخدم غير موجود");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
    }

    public async Task SetLastLoginAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
