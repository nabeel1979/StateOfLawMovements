using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using QanoonCoalition.Web.Models;
using QanoonCoalition.Web.Services;

namespace QanoonCoalition.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly IAuditLogService _audit;

    public AccountController(IAuthService auth, IAuditLogService audit)
    {
        _auth = auth;
        _audit = audit;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Dashboard", GetDashboardController());
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
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

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true });

        await _audit.LogAsync(AuditAction.Login, "User", user.Id.ToString(),
            description: $"تسجيل دخول: {user.FullName}",
            movementId: user.MovementId);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return user.Role == UserRole.Admin
            ? RedirectToAction("Dashboard", "Admin")
            : RedirectToAction("Dashboard", "Manager");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync(AuditAction.Logout, description: "تسجيل خروج");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    public IActionResult AccessDenied() => View();

    private string GetDashboardController() =>
        User.IsInRole("Admin") ? "Admin" : "Manager";
}
