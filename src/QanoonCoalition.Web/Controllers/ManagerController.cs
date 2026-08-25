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

    public ManagerController(AppDbContext db, IMemberService members, IJoinRequestService requests,
        IMovementService movements, IAuditLogService audit, SystemConstantService sysConst)
    {
        _db = db;
        _members = members;
        _requests = requests;
        _movements = movements;
        _audit = audit;
        _sysConst = sysConst;
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
    public async Task<IActionResult> Members(string? q, string? by, int page = 1)
    {
        var movementId = GetMovementId();
        var (items, total) = await _members.SearchAsync(movementId, q, by, page, 20);
        ViewBag.Query = q;
        ViewBag.SearchBy = by;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.Total = total;
        return View(items);
    }

    // helper لحفظ صورة العضو
    private async Task<string?> SavePhotoAsync(IFormFile? photo, string? oldPath = null)
    {
        if (photo == null || photo.Length == 0) return oldPath;
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(photo.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) return oldPath;
        if (photo.Length > 3 * 1024 * 1024) return oldPath; // max 3MB

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "members");
        Directory.CreateDirectory(dir);

        // حذف الصورة القديمة إن وجدت
        if (!string.IsNullOrEmpty(oldPath))
        {
            var oldFile = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldPath.TrimStart('/'));
            if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);
        await using var stream = new FileStream(fullPath, FileMode.Create);
        await photo.CopyToAsync(stream);
        return $"/uploads/members/{fileName}";
    }

    public async Task<IActionResult> AddMember()
    {
        ViewBag.MovementId = GetMovementId();
        ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
        ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
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
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
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
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
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
        ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
        ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
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
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
            return View(existing);
        }
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
