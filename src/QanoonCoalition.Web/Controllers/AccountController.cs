using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;
using QanoonCoalition.Web.Services;

namespace QanoonCoalition.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly IAuditLogService _audit;
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly IConfiguration _config;

    public AccountController(IAuthService auth, IAuditLogService audit,
        AppDbContext db, EmailService email, IConfiguration config)
    {
        _auth   = auth;
        _audit  = audit;
        _db     = db;
        _email  = email;
        _config = config;
    }

    // ─── Login ────────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", GetDashboardController());
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "يرجى إدخال البريد الإلكتروني وكلمة المرور");
            return View();
        }

        var user = await _auth.ValidateCredentialsAsync(email, password);
        if (user == null)
        {
            ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
            return View();
        }

        await _auth.SetLastLoginAsync(user.Id);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };
        if (user.MovementId.HasValue)
            claims.Add(new Claim("MovementId", user.MovementId.Value.ToString()));
        if (user.Movement != null)
            claims.Add(new Claim("MovementName", user.Movement.Name));

        var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        await _audit.LogAsync(AuditAction.Login, "User", user.Id.ToString(),
            description: $"تسجيل دخول: {user.FullName}", movementId: user.MovementId);

        // إذا وجب تغيير كلمة المرور → توجيه لصفحة التغيير
        if (user.MustChangePassword)
            return RedirectToAction(nameof(ChangePassword));

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        if (user.Role == UserRole.Admin)
            return RedirectToAction("Dashboard", "Admin");
        if (user.Role == UserRole.MovementManager)
            return RedirectToAction("Dashboard", "Manager");
        // Viewer: لا توجد لوحة تحكم
        return RedirectToAction("AccessDenied");
    }

    // ─── Logout ───────────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync(AuditAction.Logout, description: "تسجيل خروج");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ─── Change Password (إجباري عند أول دخول أو بعد الإعادة) ───────────────
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string newPassword, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ModelState.AddModelError("", "كلمة المرور يجب أن تكون 8 أحرف على الأقل");
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "كلمتا المرور غير متطابقتين");
            return View();
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.PasswordHash      = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تغيير كلمة المرور بنجاح";

        return user.Role == UserRole.Admin
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Dashboard", "Manager");
    }

    // ─── Forgot Password ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "يرجى إدخال البريد الإلكتروني");
            return View();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim() && u.IsActive);

        // دائماً نُظهر نفس الرسالة لأسباب أمنية
        if (user != null)
        {
            var token  = Guid.NewGuid().ToString("N");
            var expiry = DateTime.UtcNow.AddHours(1);

            user.PasswordResetToken  = token;
            user.PasswordResetExpiry = expiry;
            await _db.SaveChangesAsync();

            var baseUrl  = _config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
            var resetUrl = $"{baseUrl}/Account/ResetPassword?token={token}";

            var body = $@"
<div dir='rtl' style='font-family:Arial;max-width:500px;margin:auto'>
  <h2 style='color:#1a6b3a'>ائتلاف دولة القانون</h2>
  <p>مرحباً {user.FullName}،</p>
  <p>تلقّينا طلباً لإعادة تعيين كلمة مرورك. اضغط على الزر أدناه:</p>
  <a href='{resetUrl}' style='display:inline-block;background:#1a6b3a;color:#fff;
     padding:12px 28px;border-radius:6px;text-decoration:none;font-size:16px;margin:16px 0'>
    إعادة تعيين كلمة المرور
  </a>
  <p style='color:#888;font-size:.85rem'>الرابط صالح لمدة ساعة واحدة فقط.</p>
  <p style='color:#888;font-size:.85rem'>إذا لم تطلب هذا، تجاهل هذا البريد.</p>
</div>";

            try { await _email.SendAsync(user.Email, "إعادة تعيين كلمة المرور", body); }
            catch { /* سجّل الخطأ لكن لا تكشفه للمستخدم */ }
        }

        TempData["Success"] = "إذا كان البريد مسجلاً في النظام، ستصلك رسالة خلال دقائق.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    // ─── Reset Password ───────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string? token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest();
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token && u.PasswordResetExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "رابط إعادة التعيين غير صالح أو منتهي الصلاحية.";
            return RedirectToAction(nameof(ForgotPassword));
        }
        ViewBag.Token = token;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token && u.PasswordResetExpiry > DateTime.UtcNow);
        if (user == null)
        {
            TempData["Error"] = "رابط إعادة التعيين غير صالح أو منتهي الصلاحية.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
        {
            ModelState.AddModelError("", "كلمة المرور يجب أن تكون 8 أحرف على الأقل");
            ViewBag.Token = token;
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ModelState.AddModelError("", "كلمتا المرور غير متطابقتين");
            ViewBag.Token = token;
            return View();
        }

        user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetToken  = null;
        user.PasswordResetExpiry = null;
        user.MustChangePassword  = false;
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم تعيين كلمة المرور الجديدة. يمكنك تسجيل الدخول الآن.";
        return RedirectToAction(nameof(Login));
    }

    public async Task<IActionResult> AccessDenied(string? returnUrl)
    {
        // تسجيل الخروج تلقائياً وإعادة التوجيه لصفحة الدخول
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    private string GetDashboardController() =>
        User.IsInRole("Admin") ? "Admin" : "Manager";
}
