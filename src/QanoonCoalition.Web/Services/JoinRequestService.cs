using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public class JoinRequestService : IJoinRequestService
{
    private readonly AppDbContext _db;
    private readonly ISerialNumberService _serial;
    private readonly IMemberService _memberService;

    public JoinRequestService(AppDbContext db, ISerialNumberService serial, IMemberService memberService)
    {
        _db = db;
        _serial = serial;
        _memberService = memberService;
    }

    public async Task<JoinRequest> SubmitAsync(JoinRequest request)
    {
        // التحقق من عدم التكرار في الأعضاء الحاليين
        if (await _memberService.NameExistsAsync(request.FullName))
            throw new InvalidOperationException("الاسم|هذا الاسم مسجل مسبقاً كعضو في النظام");
        if (await _memberService.PhoneExistsAsync(request.Phone))
            throw new InvalidOperationException("الهاتف|رقم الهاتف مسجل مسبقاً كعضو في النظام");
        if (!string.IsNullOrEmpty(request.Email) && await _memberService.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("البريد|البريد الإلكتروني مسجل مسبقاً كعضو في النظام");

        // التحقق من عدم وجود طلب انضمام سابق بنفس الاسم أو الهاتف
        bool nameInRequests = await _db.JoinRequests.AnyAsync(r =>
            r.FullName == request.FullName && r.Status == RequestStatus.Pending);
        if (nameInRequests)
            throw new InvalidOperationException("الاسم|يوجد طلب انضمام قيد المراجعة بهذا الاسم مسبقاً");

        bool phoneInRequests = await _db.JoinRequests.AnyAsync(r =>
            r.Phone == request.Phone && r.MovementId == request.MovementId);
        if (phoneInRequests)
            throw new InvalidOperationException("الهاتف|يوجد طلب انضمام مسبق بهذا الرقم لهذه الحركة");

        request.ReferenceNumber = _serial.GenerateReferenceNumber();
        request.Status = RequestStatus.Pending;
        request.SubmittedAt = DateTime.UtcNow;

        _db.JoinRequests.Add(request);
        await _db.SaveChangesAsync();
        return request;
    }

    public async Task<(List<JoinRequest> Items, int Total)> GetAsync(int? movementId,
        RequestStatus? status, int page, int pageSize)
    {
        var q = _db.JoinRequests.Include(r => r.Movement).Include(r => r.ReviewedByUser).AsQueryable();

        if (movementId.HasValue) q = q.Where(r => r.MovementId == movementId);
        if (status.HasValue) q = q.Where(r => r.Status == status);

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(r => r.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return (items, total);
    }

    public async Task<JoinRequest?> GetByIdAsync(int id) =>
        await _db.JoinRequests
            .Include(r => r.Movement)
            .Include(r => r.ReviewedByUser)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<JoinRequest?> GetByReferenceAsync(string reference) =>
        await _db.JoinRequests
            .Include(r => r.Movement)
            .FirstOrDefaultAsync(r => r.ReferenceNumber == reference);

    /// <summary>
    /// ينسخ صورة الطلب نسخة خاصة بالعضو. النسخ مقصود: الطلب سجل تاريخي،
    /// ولو تشاركا الملف نفسه لأدّى تغيير صورة العضو أو حذفه إلى إتلاف صورة الطلب.
    /// عند تعذّر النسخ نشترك في المسار بدلاً من فقدان الصورة.
    /// </summary>
    public static string? CopyPhoto(string? sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath)) return null;

        try
        {
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var source = Path.Combine(root, sourcePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(source)) return null;

            var dir = Path.Combine(root, "uploads", "members");
            Directory.CreateDirectory(dir);
            var name = $"{Guid.NewGuid()}{Path.GetExtension(source)}";
            File.Copy(source, Path.Combine(dir, name));
            return $"/uploads/members/{name}";
        }
        catch
        {
            return sourcePath;
        }
    }

    public async Task<Member> ApproveAsync(int requestId, string? benefitField, int reviewedByUserId)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var request = await _db.JoinRequests.FindAsync(requestId)
                    ?? throw new InvalidOperationException("الطلب غير موجود");

                if (request.Status != RequestStatus.Pending)
                    throw new InvalidOperationException("الطلب تمت مراجعته مسبقاً");

                // التحقق من التكرار مجدداً عند الموافقة (Concurrency)
                if (await _memberService.NameExistsAsync(request.FullName))
                    throw new InvalidOperationException("الاسم مسجل مسبقاً كعضو");
                if (await _memberService.PhoneExistsAsync(request.Phone))
                    throw new InvalidOperationException("رقم الهاتف مسجل مسبقاً كعضو");

                request.Status = RequestStatus.Approved;
                request.ReviewedByUserId = reviewedByUserId;
                request.ReviewedAt = DateTime.UtcNow;

                var member = new Member
                {
                    SerialNumber = await _serial.GenerateAsync(),
                    FullName = request.FullName,
                    Phone = request.Phone,
                    Email = request.Email,
                    BirthDate = request.BirthDate,
                    Gender = request.Gender,
                    Province = request.Province,
                    District = request.District,
                    SubDistrict = request.SubDistrict,
                    Address = request.Address,
                    EducationLevel = request.EducationLevel,
                    Specialization = request.Specialization,
                    Occupation = request.Occupation,
                    JobTitle = request.JobTitle,
                    WorkPlace = request.WorkPlace,
                    ServiceStartDate = request.ServiceStartDate,
                    ServiceYears = request.ServiceYears,
                    Skills = request.Skills,
                    Experiences = request.Experiences,
                    TrainingCourses = request.TrainingCourses,
                    Languages = request.Languages,
                    BenefitField = benefitField ?? request.BenefitField,
                    PhotoPath = CopyPhoto(request.PhotoPath),
                    Notes = request.Notes,
                    MovementId = request.MovementId,
                    ApprovedByUserId = reviewedByUserId,
                    JoinRequestId = request.Id,
                    CreatedAt = DateTime.UtcNow,
                    ApprovedAt = DateTime.UtcNow
                };

                _db.Members.Add(member);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return member;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task RejectAsync(int requestId, string reason, int reviewedByUserId)
    {
        var request = await _db.JoinRequests.FindAsync(requestId)
            ?? throw new InvalidOperationException("الطلب غير موجود");

        if (request.Status != RequestStatus.Pending)
            throw new InvalidOperationException("الطلب تمت مراجعته مسبقاً");

        request.Status = RequestStatus.Rejected;
        request.ReviewedByUserId = reviewedByUserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.RejectionReason = reason;

        await _db.SaveChangesAsync();
    }

    public async Task<(int Pending, int Approved, int Rejected)> GetCountsAsync(int? movementId = null)
    {
        var q = _db.JoinRequests.AsQueryable();
        if (movementId.HasValue) q = q.Where(r => r.MovementId == movementId);

        var pending  = await q.CountAsync(r => r.Status == RequestStatus.Pending);
        var approved = await q.CountAsync(r => r.Status == RequestStatus.Approved);
        var rejected = await q.CountAsync(r => r.Status == RequestStatus.Rejected);
        return (pending, approved, rejected);
    }
}
