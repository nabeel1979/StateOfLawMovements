using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;
using QanoonCoalition.Web.Services;

namespace QanoonCoalition.Web.Controllers;

[Authorize(Roles = "MovementManager")]
public class ManagerController : Controller
{
    private readonly AppDbContext _db;
    private readonly IMemberService _members;
    private readonly IJoinRequestService _requests;
    private readonly IMovementService _movements;
    private readonly IAuditLogService _audit;
    private readonly SystemConstantService _sysConst;
    private readonly MemberFilterOptionsService _filterOptions;

    public ManagerController(AppDbContext db, IMemberService members, IJoinRequestService requests,
        IMovementService movements, IAuditLogService audit, SystemConstantService sysConst,
        MemberFilterOptionsService filterOptions)
    {
        _db = db;
        _members = members;
        _requests = requests;
        _movements = movements;
        _audit = audit;
        _sysConst = sysConst;
        _filterOptions = filterOptions;
    }

    private int GetMovementId() =>
        int.Parse(User.FindFirstValue("MovementId")!);

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ─── Dashboard ────────────────────────────────────────────────────────────
    public async Task<IActionResult> Dashboard()
    {
        var movementId = GetMovementId();
        var movement = await _movements.GetByIdAsync(movementId);
        var (pending, approved, rejected) = await _requests.GetCountsAsync(movementId);
        ViewBag.Movement = movement;
        ViewBag.TotalMembers = await _members.GetTotalCountAsync(movementId);
        ViewBag.PendingRequests = pending;
        ViewBag.ApprovedRequests = approved;
        ViewBag.RejectedRequests = rejected;
        var recentRequests = (await _requests.GetAsync(movementId, RequestStatus.Pending, 1, 5)).Items;
        ViewBag.RecentRequests = recentRequests;
        return View(movement);
    }

    // ─── Members ─────────────────────────────────────────────────────────────
    public async Task<IActionResult> Members(List<MemberFilter>? filters, FilterMatch match = FilterMatch.All,
        int page = 1)
    {
        var movementId = GetMovementId();
        var (items, total) = await _members.SearchAsync(movementId, filters, match, page, 20);
        ViewBag.Filters = MemberFilterHelper.Normalize(filters);
        ViewBag.FilterOptions = await _filterOptions.GetAsync(movementId);
        ViewBag.Match = match;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Total = total;
        return View(items);
    }

    /// <summary>تصدير أعضاء الحركة إلى Excel بقالب "استمارة البيانات الهيكلية"</summary>
    public async Task<IActionResult> ExportMembers(List<MemberFilter>? filters,
        FilterMatch match = FilterMatch.All)
    {
        var movementId = GetMovementId();
        var (items, _) = await _members.SearchAsync(movementId, filters, match, 1, int.MaxValue);

        var movement = await _db.Movements
            .Include(m => m.Managers)
            .FirstOrDefaultAsync(m => m.Id == movementId);

        var bytes = MembersExcelExporter.Build(
            items,
            movement?.Name ?? "الحركة",
            movement?.Managers.FirstOrDefault(u => u.IsActive)?.FullName,
            movement?.CreatedAt);

        await _audit.LogAsync(AuditAction.Export, "Member",
            description: $"تصدير {items.Count} عضو إلى Excel", movementId: movementId);

        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"أعضاء_{movement?.Name}_{DateTime.Now:yyyy-MM-dd}.xlsx");
    }

    // helper لحفظ صورة العضو
    private async Task<string?> SavePhotoAsync(IFormFile? photo, string? oldPath = null)
    {
        if (photo == null || photo.Length == 0) return oldPath;
        if (!MemberPhoto.IsValid(photo, out var error))
        {
            TempData["Error"] = error;
            return oldPath;
        }

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "members");
        Directory.CreateDirectory(dir);

        // حذف الصورة القديمة إن وجدت
        if (!string.IsNullOrEmpty(oldPath))
        {
            var oldFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
        }

        var fileName = $"{Guid.NewGuid()}{MemberPhoto.SafeExtension(photo.FileName)}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await photo.CopyToAsync(stream);
        return $"/uploads/members/{fileName}";
    }

    /// <summary>قوائم استمارة العضو المنسدلة، كلها من ثوابت النظام</summary>
    private async Task LoadMemberFormLists()
    {
        ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
        ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
        ViewBag.Provinces       = await _sysConst.GetValuesAsync(SysConst.Province);
    }

    public async Task<IActionResult> AddMember()
    {
        ViewBag.MovementId = GetMovementId();
        await LoadMemberFormLists();
        return View(new Member());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(Member member, IFormFile? photo)
    {
        member.MovementId = GetMovementId();
        member.ApprovedByUserId = GetUserId();
        member.ApprovedAt = DateTime.UtcNow;
        ModelState.Remove("Movement");
        ModelState.Remove("SerialNumber");
        ModelState.Remove("PhotoPath");

        if (!ModelState.IsValid)
        {
            ViewBag.MovementId = member.MovementId;
            await LoadMemberFormLists();
            return View(member);
        }

        try
        {
            member.PhotoPath = await SavePhotoAsync(photo);
            await _members.AddManuallyAsync(member);
            await _audit.LogAsync(AuditAction.AddMember, "Member", member.Id.ToString(),
                newValues: new { member.FullName, member.Phone, member.SerialNumber },
                movementId: member.MovementId,
                description: $"إضافة عضو يدوي: {member.FullName}");
            TempData["Success"] = $"تم إضافة العضو \"{member.FullName}\" بنجاح - الرقم التسلسلي: {member.SerialNumber}";
            return RedirectToAction(nameof(Members));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.MovementId = member.MovementId;
            await LoadMemberFormLists();
            return View(member);
        }
    }

    public async Task<IActionResult> PrintMember(int id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null || member.MovementId != GetMovementId()) return NotFound();
        return View(member);
    }

    public async Task<IActionResult> EditMember(int id)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null || member.MovementId != GetMovementId()) return NotFound();
        await LoadMemberFormLists();
        return View(member);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditMember(int id, Member model, IFormFile? photo, bool removePhoto = false)
    {
        var existing = await _members.GetByIdAsync(id);
        if (existing == null || existing.MovementId != GetMovementId()) return NotFound();

        existing.FullName = model.FullName;
        existing.Phone = model.Phone;
        existing.Email = model.Email;
        existing.BirthDate = model.BirthDate;
        existing.Gender = model.Gender;
        existing.Province = model.Province;
        existing.District = model.District;
        existing.SubDistrict = model.SubDistrict;
        existing.Address = model.Address;
        existing.EducationLevel = model.EducationLevel;
        existing.Specialization = model.Specialization;
        existing.Occupation = model.Occupation;
        existing.JobTitle = model.JobTitle;
        existing.WorkPlace = model.WorkPlace;
        existing.ServiceStartDate = model.ServiceStartDate;
        existing.ServiceYears = model.ServiceYears;
        existing.Skills = model.Skills;
        existing.Experiences = model.Experiences;
        existing.TrainingCourses = model.TrainingCourses;
        existing.Languages = model.Languages;
        existing.BenefitField = model.BenefitField;
        existing.Notes = model.Notes;

        // معالجة الصورة
        if (removePhoto)
        {
            if (!string.IsNullOrEmpty(existing.PhotoPath))
            {
                var oldFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existing.PhotoPath.TrimStart('/'));
                if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
            }
            existing.PhotoPath = null;
        }
        else if (photo != null && photo.Length > 0)
        {
            existing.PhotoPath = await SavePhotoAsync(photo, existing.PhotoPath);
        }

        try
        {
            await _members.UpdateAsync(existing);
            await _audit.LogAsync(AuditAction.UpdateMember, "Member", id.ToString(),
                newValues: new { existing.FullName, existing.Phone },
                movementId: existing.MovementId,
                description: $"تعديل عضو: {existing.FullName}");
            TempData["Success"] = "تم تعديل بيانات العضو بنجاح";
            return RedirectToAction(nameof(Members));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await LoadMemberFormLists();
            return View(existing);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMember(int id, string? returnUrl)
    {
        var member = await _members.GetByIdAsync(id);
        if (member == null || member.MovementId != GetMovementId()) return NotFound();

        var name = member.FullName;
        var serial = member.SerialNumber;
        var photoPath = member.PhotoPath;

        try
        {
            await _members.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"تعذّر حذف العضو: {ex.Message}";
            return BackToMembers(returnUrl);
        }

        DeletePhotoFile(photoPath);

        await _audit.LogAsync(AuditAction.DeleteMember, "Member", id.ToString(),
            oldValues: new { FullName = name, SerialNumber = serial, member.Phone },
            movementId: member.MovementId,
            description: $"حذف عضو: {name} ({serial})");

        TempData["Success"] = $"تم حذف العضو \"{name}\" نهائياً";
        return BackToMembers(returnUrl);
    }

    /// <summary>يرجع إلى القائمة مع الحفاظ على الفلاتر وترقيم الصفحات</summary>
    private IActionResult BackToMembers(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(nameof(Members));

    private static void DeletePhotoFile(string? photoPath)
    {
        if (string.IsNullOrEmpty(photoPath)) return;
        var file = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", photoPath.TrimStart('/'));
        if (System.IO.File.Exists(file)) System.IO.File.Delete(file);
    }

    // ─── Join Requests ────────────────────────────────────────────────────────
    public async Task<IActionResult> JoinRequests(RequestStatus? status, int page = 1)
    {
        var movementId = GetMovementId();
        var (items, total) = await _requests.GetAsync(movementId, status, page, 20);
        var (pending, approved, rejected) = await _requests.GetCountsAsync(movementId);
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Pending = pending;
        ViewBag.Approved = approved;
        ViewBag.Rejected = rejected;
        return View(items);
    }

    public async Task<IActionResult> RequestDetails(int id)
    {
        var request = await _requests.GetByIdAsync(id);
        if (request == null || request.MovementId != GetMovementId()) return NotFound();
        ViewBag.BenefitFields = await _sysConst.GetValuesAsync(SysConst.BenefitField);
        return View(request);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int id, string? benefitField)
    {
        try
        {
            var request = await _requests.GetByIdAsync(id);
            if (request == null || request.MovementId != GetMovementId()) return NotFound();

            var member = await _requests.ApproveAsync(id, benefitField, GetUserId());
            await _audit.LogAsync(AuditAction.ApproveRequest, "JoinRequest", id.ToString(),
                newValues: new { MemberId = member.Id, member.SerialNumber },
                movementId: request.MovementId,
                description: $"قبول طلب: {request.FullName} - الرقم التسلسلي: {member.SerialNumber}");
            TempData["Success"] = $"تمت الموافقة على الطلب. الرقم التسلسلي للعضو: {member.SerialNumber}";
            return RedirectToAction(nameof(JoinRequests));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(RequestDetails), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int id, string reason)
    {
        try
        {
            var request = await _requests.GetByIdAsync(id);
            if (request == null || request.MovementId != GetMovementId()) return NotFound();

            await _requests.RejectAsync(id, reason, GetUserId());
            await _audit.LogAsync(AuditAction.RejectRequest, "JoinRequest", id.ToString(),
                newValues: new { Reason = reason }, movementId: request.MovementId,
                description: $"رفض طلب: {request.FullName}");
            TempData["Success"] = "تم رفض الطلب";
            return RedirectToAction(nameof(JoinRequests));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(RequestDetails), new { id });
        }
    }

    // ─── Movement Constants ───────────────────────────────────────────────────
    public async Task<IActionResult> Constants()
    {
        var movementId = GetMovementId();
        var constants = await _db.MovementConstants
            .Where(c => c.MovementId == movementId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();
        ViewBag.MovementId = movementId;
        return View(constants);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveConstant(int movementId, string key, string value,
        string? description, string dataType = "text", int displayOrder = 0)
    {
        if (movementId != GetMovementId()) return Forbid();

        var existing = await _db.MovementConstants
            .FirstOrDefaultAsync(c => c.MovementId == movementId && c.Key == key);

        if (existing == null)
        {
            _db.MovementConstants.Add(new MovementConstant
            {
                MovementId = movementId, Key = key, Value = value,
                Description = description, DataType = dataType,
                DisplayOrder = displayOrder, CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Value = value;
            existing.Description = description;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(AuditAction.UpdateConstants, "MovementConstant", key,
            movementId: movementId, description: $"تعديل ثابت: {key}");
        TempData["Success"] = "تم حفظ الثابت بنجاح";
        return RedirectToAction(nameof(Constants));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConstant(int id)
    {
        var c = await _db.MovementConstants.FindAsync(id);
        if (c == null || c.MovementId != GetMovementId()) return NotFound();
        _db.MovementConstants.Remove(c);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم حذف الثابت";
        return RedirectToAction(nameof(Constants));
    }

    // ─── Movement Info ────────────────────────────────────────────────────────
    public async Task<IActionResult> MovementInfo()
    {
        var movement = await _movements.GetByIdAsync(GetMovementId());
        return View(movement);
    }
}
