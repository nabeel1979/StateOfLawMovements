using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;
using QanoonCoalition.Web.Services;

namespace QanoonCoalition.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly IMovementService _movements;
    private readonly IMemberService _members;
    private readonly IJoinRequestService _requests;
    private readonly IAuditLogService _audit;
    private readonly IAuthService _auth;
    private readonly SystemConstantService _sysConst;

    public AdminController(AppDbContext db, IMovementService movements, IMemberService members,
        IJoinRequestService requests, IAuditLogService audit, IAuthService auth,
        SystemConstantService sysConst)
    {
        _db = db;
        _movements = movements;
        _members = members;
        _requests = requests;
        _audit = audit;
        _auth = auth;
        _sysConst = sysConst;
    }

    // ─── Dashboard ───────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard()
    {
        var (pending, approved, rejected) = await _requests.GetCountsAsync();
        ViewBag.TotalMovements = await _db.Movements.CountAsync();
        ViewBag.TotalMembers = await _members.GetTotalCountAsync();
        ViewBag.TotalRequests = pending + approved + rejected;
        ViewBag.PendingRequests = pending;
        ViewBag.ApprovedMembers = await _db.Members.CountAsync();
        ViewBag.ActiveManagers = await _db.Users.CountAsync(u => u.Role == UserRole.MovementManager && u.IsActive);
        ViewBag.Movements = await _movements.GetSummaryForAdminAsync();
        ViewBag.RecentLogs = (await _audit.GetLogsAsync(1, 10)).Items;
        return View();
    }

    // ─── Movements ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Movements()
    {
        var list = await _movements.GetAllAsync(includeInactive: true);
        return View(list);
    }

    public IActionResult CreateMovement() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovement(string name, string? address,
        string? description, string? phone, string? email, string? website)
    {
        if (string.IsNullOrWhiteSpace(name))
        { ModelState.AddModelError("name", "اسم الحركة مطلوب"); return View(); }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var movement = await _movements.CreateAsync(name, null, address, description, phone, email, website, userId);
            await _audit.LogAsync(AuditAction.CreateMovement, "Movement", movement.Id.ToString(),
                newValues: new { movement.Name }, movementId: movement.Id,
                description: $"إنشاء حركة: {movement.Name}");
            TempData["Success"] = $"تم إنشاء الحركة \"{name}\" بنجاح";
            return RedirectToAction(nameof(MovementDetails), new { id = movement.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View();
        }
    }

    public async Task<IActionResult> MovementDetails(int id)
    {
        var movement = await _movements.GetByIdAsync(id);
        if (movement == null) return NotFound();
        var (members, requests, pending) = await _movements.GetStatsAsync(id);
        ViewBag.MemberCount = members;
        ViewBag.RequestCount = requests;
        ViewBag.PendingCount = pending;
        return View(movement);
    }

    public async Task<IActionResult> EditMovement(int id)
    {
        var movement = await _movements.GetByIdAsync(id);
        if (movement == null) return NotFound();
        return View(movement);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMovement(int id, string name, string? address,
        string? description, string? phone, string? email, string? website)
    {
        try
        {
            var old = await _movements.GetByIdAsync(id);
            await _movements.UpdateAsync(id, name, null, address, description, phone, email, website);
            await _audit.LogAsync(AuditAction.UpdateMovement, "Movement", id.ToString(),
                oldValues: new { old?.Name }, newValues: new { Name = name }, movementId: id,
                description: $"تعديل حركة: {name}");
            TempData["Success"] = "تم تعديل بيانات الحركة بنجاح";
            return RedirectToAction(nameof(MovementDetails), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var movement = await _movements.GetByIdAsync(id);
            return View(movement);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleMovement(int id)
    {
        var movement = await _db.Movements.FindAsync(id);
        if (movement == null) return NotFound();
        await _movements.SetActiveAsync(id, !movement.IsActive);
        TempData["Success"] = movement.IsActive ? "تم تعطيل الحركة" : "تم تفعيل الحركة";
        return RedirectToAction(nameof(Movements));
    }

    // ─── Movement Managers ────────────────────────────────────────────────────
    public async Task<IActionResult> MovementManagers(int movementId)
    {
        var movement = await _movements.GetByIdAsync(movementId);
        if (movement == null) return NotFound();
        ViewBag.Movement = movement;
        var managers = await _db.Users
            .Where(u => u.MovementId == movementId && u.Role == UserRole.MovementManager)
            .ToListAsync();
        return View(managers);
    }

    public async Task<IActionResult> CreateManager(int movementId)
    {
        var movement = await _db.Movements.FindAsync(movementId);
        if (movement == null) return NotFound();
        ViewBag.Movement = movement;
        ViewBag.ManagerTitles = await _sysConst.GetValuesAsync(SysConst.ManagerTitle);
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateManager(int movementId, string fullName, string email,
        string password, string? title)
    {
        try
        {
            var user = await _auth.CreateUserAsync(fullName, email, password, UserRole.MovementManager, movementId);
            user.Title = title;
            await _db.SaveChangesAsync();
            await _audit.LogAsync(AuditAction.CreateUser, "User", user.Id.ToString(),
                newValues: new { user.FullName, user.Email, user.Title }, movementId: movementId,
                description: $"إنشاء مسؤول حركة: {fullName}");
            TempData["Success"] = $"تم إنشاء المسؤول \"{fullName}\" بنجاح";
            return RedirectToAction(nameof(MovementManagers), new { movementId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            var movement = await _db.Movements.FindAsync(movementId);
            ViewBag.Movement = movement;
            return View();
        }
    }

    public async Task<IActionResult> EditManager(int id)
    {
        var user = await _db.Users.Include(u => u.Movement).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null || user.Role != UserRole.MovementManager) return NotFound();
        ViewBag.Movement = user.Movement;
        ViewBag.ManagerTitles = await _sysConst.GetValuesAsync(SysConst.ManagerTitle);
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditManager(int id, string fullName, string? title,
        string? newPassword, bool isActive)
    {
        var user = await _db.Users.Include(u => u.Movement).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null || user.Role != UserRole.MovementManager) return NotFound();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            ModelState.AddModelError("fullName", "الاسم مطلوب");
            ViewBag.Movement = user.Movement;
            return View(user);
        }

        var old = new { user.FullName, user.Title, user.IsActive };
        user.FullName = fullName.Trim();
        user.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        user.IsActive = isActive;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            if (newPassword.Length < 8)
            {
                ModelState.AddModelError("newPassword", "كلمة المرور يجب أن تكون 8 أحرف على الأقل");
                ViewBag.Movement = user.Movement;
                return View(user);
            }
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.UpdateUser, "User", user.Id.ToString(),
            oldValues: old, newValues: new { user.FullName, user.Title, user.IsActive },
            movementId: user.MovementId,
            description: $"تعديل مسؤول حركة: {user.FullName}");

        TempData["Success"] = $"تم تحديث بيانات المسؤول \"{user.FullName}\" بنجاح";
        return RedirectToAction(nameof(MovementManagers), new { movementId = user.MovementId });
    }

    // ─── Members ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Members(string? q, string? by, int? movementId, int page = 1)
    {
        var (items, total) = await _members.SearchAsync(movementId, q, by, page, 20);
        ViewBag.Query = q;
        ViewBag.SearchBy = by;
        ViewBag.MovementId = movementId;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Total = total;
        ViewBag.Movements = await _movements.GetAllAsync();
        return View(items);
    }

    public async Task<IActionResult> MemberDetails(int id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) return NotFound();
        return View(member);
    }

    public async Task<IActionResult> PrintMember(int id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) return NotFound();
        return View("~/Views/Manager/PrintMember.cshtml", member);
    }

    // ─── Join Requests ────────────────────────────────────────────────────────
    public async Task<IActionResult> JoinRequests(int? movementId, RequestStatus? status, int page = 1)
    {
        var (items, total) = await _requests.GetAsync(movementId, status, page, 20);
        var (pending, approved, rejected) = await _requests.GetCountsAsync(movementId);
        ViewBag.MovementId = movementId;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Total = total;
        ViewBag.Pending = pending;
        ViewBag.Approved = approved;
        ViewBag.Rejected = rejected;
        ViewBag.Movements = await _movements.GetAllAsync();
        return View(items);
    }

    // ─── Audit Logs ───────────────────────────────────────────────────────────
    public async Task<IActionResult> AuditLogs(int? movementId, AuditAction? action,
        DateTime? from, DateTime? to, int page = 1)
    {
        var (items, total) = await _audit.GetLogsAsync(page, 30, movementId, action, from, to);
        ViewBag.MovementId = movementId;
        ViewBag.Action = action;
        ViewBag.From = from;
        ViewBag.To = to;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 30.0);
        ViewBag.Total = total;
        ViewBag.Movements = await _movements.GetAllAsync();
        return View(items);
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    public async Task<IActionResult> Reports()
    {
        var movements = await _db.Movements.Include(m => m.Members).Include(m => m.JoinRequests).ToListAsync();
        return View(movements);
    }

    // ─── System Constants ─────────────────────────────────────────────────────
    public async Task<IActionResult> SystemConstants(string? category)
    {
        var all = await _sysConst.GetAllAsync();
        ViewBag.Category = category;
        ViewBag.CategoryLabels = SysConst.Labels;
        if (!string.IsNullOrEmpty(category))
            all = all.Where(c => c.Category == category).ToList();
        return View(all);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConstant(string category, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            TempData["Error"] = "القيمة مطلوبة";
            return RedirectToAction(nameof(SystemConstants), new { category });
        }
        if (await _sysConst.ValueExistsAsync(category, value.Trim()))
        {
            TempData["Error"] = "هذه القيمة موجودة مسبقاً";
            return RedirectToAction(nameof(SystemConstants), new { category });
        }
        await _sysConst.CreateAsync(new SystemConstant
        {
            Category = category,
            Value = value.Trim(),
            IsActive = true
        });
        TempData["Success"] = "تمت الإضافة بنجاح";
        return RedirectToAction(nameof(SystemConstants), new { category });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditConstant(int id, string value, bool isActive)
    {
        var item = await _sysConst.GetByIdAsync(id);
        if (item == null) return NotFound();

        if (await _sysConst.ValueExistsAsync(item.Category, value.Trim(), id))
        {
            TempData["Error"] = "هذه القيمة موجودة مسبقاً";
            return RedirectToAction(nameof(SystemConstants), new { category = item.Category });
        }
        item.Value = value.Trim();
        item.IsActive = isActive;
        await _sysConst.UpdateAsync(item);
        TempData["Success"] = "تم التعديل بنجاح";
        return RedirectToAction(nameof(SystemConstants), new { category = item.Category });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConstant(int id)
    {
        var item = await _sysConst.GetByIdAsync(id);
        if (item == null) return NotFound();
        var cat = item.Category;
        await _sysConst.DeleteAsync(id);
        TempData["Success"] = "تم الحذف";
        return RedirectToAction(nameof(SystemConstants), new { category = cat });
    }
}
