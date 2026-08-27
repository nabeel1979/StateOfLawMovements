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
    private readonly MemberFilterOptionsService _filterOptions;

    public AdminController(AppDbContext db, IMovementService movements, IMemberService members,
        IJoinRequestService requests, IAuditLogService audit, IAuthService auth,
        SystemConstantService sysConst, MemberFilterOptionsService filterOptions)
    {
        _db = db;
        _movements = movements;
        _members = members;
        _requests = requests;
        _audit = audit;
        _auth = auth;
        _sysConst = sysConst;
        _filterOptions = filterOptions;
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

    /// <summary>قائمة المحافظات من ثوابت النظام</summary>
    private async Task LoadProvinces() =>
        ViewBag.Provinces = await _sysConst.GetValuesAsync(SysConst.Province);

    public async Task<IActionResult> CreateMovement()
    {
        await LoadProvinces();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMovement(string name, string? address,
        string? description, string? phone, string? email, string? website, string? governorate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("name", "اسم الحركة مطلوب");
            await LoadProvinces();
            return View();
        }

        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var movement = await _movements.CreateAsync(name, null, address, description, phone, email, website, userId);
            if (!string.IsNullOrWhiteSpace(governorate)) { movement.Governorate = governorate; await _db.SaveChangesAsync(); }
            await _audit.LogAsync(AuditAction.CreateMovement, "Movement", movement.Id.ToString(),
                newValues: new { movement.Name }, movementId: movement.Id,
                description: $"إنشاء حركة: {movement.Name}");
            TempData["Success"] = $"تم إنشاء الحركة \"{name}\" بنجاح";
            return RedirectToAction(nameof(MovementDetails), new { id = movement.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadProvinces();
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
        await LoadProvinces();
        return View(movement);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMovement(int id, string name, string? address,
        string? description, string? phone, string? email, string? website, string? governorate)
    {
        try
        {
            var old = await _movements.GetByIdAsync(id);
            await _movements.UpdateAsync(id, name, null, address, description, phone, email, website);
            // حفظ المحافظة
            var mov = await _db.Movements.FindAsync(id);
            if (mov != null) { mov.Governorate = string.IsNullOrWhiteSpace(governorate) ? null : governorate; await _db.SaveChangesAsync(); }
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
            await LoadProvinces();
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
            user.MustChangePassword = true; // إجبار تغيير كلمة المرور عند أول دخول
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
    public async Task<IActionResult> EditManager(int id, string fullName, string? email, string? title,
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

        // التحقق من تكرار الإيميل
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailTaken = await _db.Users.AnyAsync(u => u.Email == email.Trim() && u.Id != id);
            if (emailTaken)
            {
                ModelState.AddModelError("email", "هذا البريد الإلكتروني مستخدم مسبقاً");
                ViewBag.Movement = user.Movement;
                ViewBag.ManagerTitles = await _sysConst.GetValuesAsync(SysConst.ManagerTitle);
                return View(user);
            }
        }

        var old = new { user.FullName, user.Email, user.Title, user.IsActive };
        user.FullName = fullName.Trim();
        if (!string.IsNullOrWhiteSpace(email)) user.Email = email.Trim();
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
            user.PasswordHash       = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.MustChangePassword = true; // إجبار تغيير كلمة المرور عند الدخول التالي
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.UpdateUser, "User", user.Id.ToString(),
            oldValues: old, newValues: new { user.FullName, user.Email, user.Title, user.IsActive },
            movementId: user.MovementId,
            description: $"تعديل مسؤول حركة: {user.FullName}");

        TempData["Success"] = $"تم تحديث بيانات المسؤول \"{user.FullName}\" بنجاح";
        return RedirectToAction(nameof(MovementManagers), new { movementId = user.MovementId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleManagerStatus(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null || user.Role != UserRole.MovementManager) return NotFound();

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();

        var statusText = user.IsActive ? "تم تفعيل" : "تم تعطيل";
        TempData["Success"] = $"{statusText} حساب المسؤول \"{user.FullName}\"";
        return RedirectToAction(nameof(MovementManagers), new { movementId = user.MovementId });
    }

    // ─── Members ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Members(List<MemberFilter>? filters, FilterMatch match = FilterMatch.All,
        int? movementId = null, int page = 1)
    {
        var (items, total) = await _members.SearchAsync(movementId, filters, match, page, 20);
        ViewBag.Filters = MemberFilterHelper.Normalize(filters);
        ViewBag.FilterOptions = await _filterOptions.GetAsync(movementId);
        ViewBag.Match = match;
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

    /// <summary>تصدير الأعضاء إلى Excel بقالب "استمارة البيانات الهيكلية"</summary>
    public async Task<IActionResult> ExportMembers(List<MemberFilter>? filters,
        FilterMatch match = FilterMatch.All, int? movementId = null)
    {
        // نجلب كل النتائج المطابقة للفلترة (بدون ترقيم صفحات)
        var (items, _) = await _members.SearchAsync(movementId, filters, match, 1, int.MaxValue);

        var movementName = "جميع الحركات";
        string? managerName = null;
        DateTime? createdAt = null;

        if (movementId.HasValue)
        {
            var movement = await _db.Movements
                .Include(m => m.Managers)
                .FirstOrDefaultAsync(m => m.Id == movementId.Value);
            if (movement != null)
            {
                movementName = movement.Name;
                managerName  = movement.Managers.FirstOrDefault(u => u.IsActive)?.FullName;
                createdAt    = movement.CreatedAt;
            }
        }

        var bytes = MembersExcelExporter.Build(items, movementName, managerName, createdAt);
        var fileName = $"أعضاء_{movementName}_{DateTime.Now:yyyy-MM-dd}.xlsx";

        await _audit.LogAsync(AuditAction.Export, "Member",
            description: $"تصدير {items.Count} عضو إلى Excel", movementId: movementId);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    public async Task<IActionResult> PrintMember(int id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null) return NotFound();
        return View("~/Views/Manager/PrintMember.cshtml", member);
    }

    // حذف الأعضاء صلاحية مسؤول الحركة وحده - لا يوجد إجراء حذف هنا.

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
        ViewBag.Category = SysConst.Find(category) == null ? null : category;
        // الأعداد تُحسب قبل التصفية ليبقى شريط الفئات معبّراً عن الحالة كلها
        ViewBag.Counts = await _sysConst.GetCountsAsync();
        if (!string.IsNullOrEmpty(ViewBag.Category as string))
            all = all.Where(c => c.Category == category).ToList();
        return View(all);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateConstant(string category, string value)
    {
        if (SysConst.Find(category) == null)
        {
            TempData["Error"] = "فئة غير معروفة";
            return RedirectToAction(nameof(SystemConstants));
        }
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
    public async Task<IActionResult> ToggleConstant(int id)
    {
        var item = await _sysConst.GetByIdAsync(id);
        if (item == null) return NotFound();

        await _sysConst.ToggleActiveAsync(id);
        TempData["Success"] = item.IsActive
            ? $"تم تعطيل \"{item.Value}\" ولن تظهر في القوائم"
            : $"تم تفعيل \"{item.Value}\"";
        return RedirectToAction(nameof(SystemConstants), new { category = item.Category });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveConstant(int id, bool up)
    {
        var item = await _sysConst.GetByIdAsync(id);
        if (item == null) return NotFound();

        await _sysConst.MoveAsync(id, up);
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

    // ─── إدارة مستخدمي النظام (Viewer) ──────────────────────────────────────

    public async Task<IActionResult> Users(string? search)
    {
        var query = _db.Users
            .Include(u => u.Movement)
            .Where(u => u.Role == UserRole.Viewer || u.Role == UserRole.Admin);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        ViewBag.Search = search;
        return View(await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
    }

    public async Task<IActionResult> CreateUser()
    {
        ViewBag.Movements = await _db.Movements.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string fullName, string email, string password, int? movementId)
    {
        ViewBag.Movements = await _db.Movements.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "جميع الحقول المطلوبة يجب ملؤها");
            return View();
        }
        if (await _db.Users.AnyAsync(u => u.Email == email.Trim()))
        {
            ModelState.AddModelError("email", "هذا البريد الإلكتروني مستخدم مسبقاً");
            return View();
        }

        var user = new User
        {
            FullName         = fullName.Trim(),
            Email            = email.Trim(),
            PasswordHash     = BCrypt.Net.BCrypt.HashPassword(password),
            Role             = UserRole.Viewer,
            MovementId       = movementId == 0 ? null : movementId,
            MustChangePassword = true,
            IsActive         = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(AuditAction.CreateUser, "User", user.Id.ToString(),
            newValues: new { user.FullName, user.Email },
            description: $"إنشاء مستخدم نظام: {fullName}");

        TempData["Success"] = $"تم إنشاء المستخدم \"{fullName}\" بنجاح";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> EditUser(int id)
    {
        var user = await _db.Users.Include(u => u.Movement)
            .FirstOrDefaultAsync(u => u.Id == id && (u.Role == UserRole.Viewer || u.Role == UserRole.Admin));
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(int id, string fullName, string email, string? newPassword, bool isActive)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == id && (u.Role == UserRole.Viewer || u.Role == UserRole.Admin));
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "الاسم والبريد الإلكتروني مطلوبان");
            return View(user);
        }
        if (await _db.Users.AnyAsync(u => u.Email == email.Trim() && u.Id != id))
        {
            ModelState.AddModelError("email", "هذا البريد الإلكتروني مستخدم مسبقاً");
            return View(user);
        }

        user.FullName = fullName.Trim();
        user.Email    = email.Trim();

        // لا نغيّر حالة تفعيل مدير النظام
        if (user.Role != UserRole.Admin)
            user.IsActive = isActive;

        if (!string.IsNullOrWhiteSpace(newPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.UpdateUser, "User", user.Id.ToString(),
            description: $"تعديل بيانات: {user.FullName}");

        TempData["Success"] = $"تم تحديث بيانات \"{user.FullName}\"";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == UserRole.Viewer); // Admin لا يُحذف
        if (user == null) return NotFound();
        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.DeleteUser, "User", id.ToString(),
            description: $"حذف مستخدم نظام: {user.FullName}");
        TempData["Success"] = $"تم حذف المستخدم \"{user.FullName}\"";
        return RedirectToAction(nameof(Users));
    }

    // ─── تطبيق المهاجرات المعلّقة (مؤقت) ────────────────────────────────────
    [HttpGet]
    public IActionResult RunMigrations()
    {
        var result = new System.Text.StringBuilder();
        try
        {
            var pending = _db.Database.GetPendingMigrations().ToList();
            result.AppendLine($"المهاجرات المعلّقة: {pending.Count}");
            foreach (var m in pending) result.AppendLine("  - " + m);

            _db.Database.Migrate();
            result.AppendLine("✓ تم تطبيق جميع المهاجرات بنجاح");

            var applied = _db.Database.GetAppliedMigrations().ToList();
            result.AppendLine($"إجمالي المهاجرات المطبّقة: {applied.Count}");
            foreach (var m in applied) result.AppendLine("  ✓ " + m);
        }
        catch (Exception ex)
        {
            result.AppendLine("✗ خطأ: " + ex.Message);
            result.AppendLine(ex.ToString());
        }
        return Content(result.ToString(), "text/plain; charset=utf-8");
    }
}
