using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Models;
using QanoonCoalition.Web.Services;

namespace QanoonCoalition.Web.Controllers;

public class PublicController : Controller
{
    private readonly IMovementService _movements;
    private readonly IJoinRequestService _requests;
    private readonly IAuditLogService _audit;
    private readonly SystemConstantService _sysConst;

    public PublicController(IMovementService movements, IJoinRequestService requests,
        IAuditLogService audit, SystemConstantService sysConst)
    {
        _movements = movements;
        _requests = requests;
        _audit = audit;
        _sysConst = sysConst;
    }

    [HttpGet("join/{token}")]
    public async Task<IActionResult> Join(string token)
    {
        var movement = await _movements.GetByTokenAsync(token);
        if (movement == null) return NotFound();
        ViewBag.Movement = movement;
        ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
        ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
        return View(new JoinRequest { MovementId = movement.Id });
    }

    [HttpPost("join/{token}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(string token, JoinRequest model)
    {
        var movement = await _movements.GetByTokenAsync(token);
        if (movement == null) return NotFound();
        ViewBag.Movement = movement;

        ModelState.Remove("Movement");
        ModelState.Remove("ReferenceNumber");
        ModelState.Remove("ReviewedByUser");
        ModelState.Remove("ConvertedMember");

        // التحقق من الحقول الإلزامية
        if (string.IsNullOrWhiteSpace(model.FullName))
            ModelState.AddModelError("FullName", "الاسم الكامل مطلوب");
        if (string.IsNullOrWhiteSpace(model.Phone))
            ModelState.AddModelError("Phone", "رقم الهاتف مطلوب");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Phone.Trim(), @"^07\d{9}$"))
            ModelState.AddModelError("Phone", "رقم الهاتف غير صحيح — يجب أن يبدأ بـ 07 ويتكون من 11 رقم");

        if (!ModelState.IsValid)
        {
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
            return View(model);
        }

        try
        {
            model.MovementId = movement.Id;
            model.Phone = model.Phone!.Trim();
            var request = await _requests.SubmitAsync(model);

            await _audit.LogAsync(AuditAction.SubmitRequest, "JoinRequest", request.Id.ToString(),
                movementId: movement.Id,
                description: $"طلب انضمام جديد: {request.FullName} - المرجع: {request.ReferenceNumber}");

            return RedirectToAction(nameof(Confirmation),
                new { reference = request.ReferenceNumber, movementName = movement.Name });
        }
        catch (InvalidOperationException ex)
        {
            // الرسالة بصيغة "حقل|نص الخطأ"
            var parts = ex.Message.Split('|', 2);
            if (parts.Length == 2)
            {
                var fieldKey = parts[0] switch
                {
                    "الاسم"  => "FullName",
                    "الهاتف" => "Phone",
                    "البريد" => "Email",
                    _        => ""
                };
                if (!string.IsNullOrEmpty(fieldKey))
                    ModelState.AddModelError(fieldKey, parts[1]);
                else
                    ModelState.AddModelError("", parts[1]);
            }
            else
            {
                ModelState.AddModelError("", ex.Message);
            }
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
            return View(model);
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? dbEx.Message;

            if (inner.Contains("IX_JoinRequests_Phone") || inner.Contains("Phone_Movement"))
                ModelState.AddModelError("Phone", "يوجد طلب انضمام مسبق بهذا الرقم لهذه الحركة");
            else if (inner.Contains("IX_Members_FullName") || (inner.Contains("FullName") && inner.Contains("unique")))
                ModelState.AddModelError("FullName", "هذا الاسم مسجل مسبقاً في النظام");
            else if (inner.Contains("IX_Members_Phone") || (inner.Contains("Phone") && inner.Contains("unique")))
                ModelState.AddModelError("Phone", "رقم الهاتف مسجل مسبقاً في النظام");
            else if (inner.Contains("IX_Members_Email") || (inner.Contains("Email") && inner.Contains("unique")))
                ModelState.AddModelError("Email", "البريد الإلكتروني مسجل مسبقاً في النظام");
            else if (inner.Contains("IX_JoinRequests_ReferenceNumber"))
                ModelState.AddModelError("", "حدث خطأ تقني. يرجى إعادة المحاولة.");
            else
                ModelState.AddModelError("", $"خطأ في حفظ البيانات: {inner}");

            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"خطأ غير متوقع: {ex.Message}");
            ViewBag.EducationLevels = await _sysConst.GetValuesAsync(SysConst.EducationLevel);
            ViewBag.BenefitFields   = await _sysConst.GetValuesAsync(SysConst.BenefitField);
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Confirmation(string reference, string movementName)
    {
        ViewBag.Reference = reference;
        ViewBag.MovementName = movementName;
        return View();
    }
}
