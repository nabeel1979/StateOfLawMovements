using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;
using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

// ─── ViewModels ───────────────────────────────────────────────────────────────

public class CitizenRequestListItem
{
    public int Id { get; set; }
    public string RequestCode { get; set; } = "";
    public DateTime RequestDate { get; set; }
    public string ApplicantName { get; set; } = "";
    public string ApplicantPhone { get; set; } = "";
    public string RequestSubject { get; set; } = "";
    public string? ReceivedByMemberName { get; set; }
    public string? DestinationName { get; set; }
    public string StatusName { get; set; } = "";
    public string StatusColor { get; set; } = "";
    public int AttachmentCount { get; set; }
}

public class CitizenRequestFilter
{
    public string? Search { get; set; }        // بحث حر
    public int? StatusId { get; set; }
    public int? DestinationId { get; set; }
    public int? ReceivedByMemberId { get; set; }
    public int? DocumentTypeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// ─── Interface ────────────────────────────────────────────────────────────────

public interface ICitizenRequestService
{
    Task<(IList<CitizenRequestListItem> Items, int Total)> GetListAsync(
        int movementId, CitizenRequestFilter filter, int page, int pageSize);

    /// <summary>بحث بشروط متعددة مبنية في الواجهة (فلتر متقدم)</summary>
    Task<(IList<CitizenRequestListItem> Items, int Total)> SearchAsync(
        int movementId, List<MemberFilter>? filters, FilterMatch match, int page, int pageSize);

    /// <summary>القيم الجاهزة لقوائم الفلتر المنسدلة، مفتاحها هو مفتاح الحقل</summary>
    Task<Dictionary<string, List<string>>> GetFilterOptionsAsync(int movementId);

    Task<CitizenRequest?> GetByIdAsync(int id, int movementId);

    Task<string> GenerateCodeAsync();

    Task<CitizenRequest> CreateAsync(CitizenRequest request, int userId);

    Task UpdateAsync(CitizenRequest request, int userId);

    Task ChangeStatusAsync(int requestId, int movementId, int newStatusId,
        int userId, string? notes, DateTime? overrideDate);

    Task DeleteAsync(int id, int movementId);

    // Lookups
    Task<IList<RequestDestination>> GetDestinationsAsync(bool activeOnly = true);
    Task<IList<CitizenRequestStatus>> GetStatusesAsync(bool activeOnly = true);
    Task<IList<DocumentType>> GetDocumentTypesAsync(bool activeOnly = true);
    Task<int> GetDefaultStatusIdAsync();

    // Attachment helpers
    Task<CitizenRequestAttachment> AddAttachmentAsync(CitizenRequestAttachment attachment);
    Task DeleteAttachmentAsync(int attachmentId, int movementId);
}

// ─── Implementation ───────────────────────────────────────────────────────────

public class CitizenRequestService : ICitizenRequestService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CitizenRequestService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<(IList<CitizenRequestListItem> Items, int Total)> GetListAsync(
        int movementId, CitizenRequestFilter f, int page, int pageSize)
    {
        var q = _db.CitizenRequests
            .Where(r => r.MovementId == movementId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim();
            q = q.Where(r =>
                r.RequestCode.Contains(s) ||
                r.ApplicantName.Contains(s) ||
                r.ApplicantPhone.Contains(s) ||
                r.RequestSubject.Contains(s) ||
                (r.ReceivedByMember != null && r.ReceivedByMember.FullName.Contains(s)) ||
                (r.Destination != null && r.Destination.Name.Contains(s)));
        }

        if (f.StatusId.HasValue) q = q.Where(r => r.StatusId == f.StatusId);
        if (f.DestinationId.HasValue) q = q.Where(r => r.DestinationId == f.DestinationId);
        if (f.ReceivedByMemberId.HasValue) q = q.Where(r => r.ReceivedByMemberId == f.ReceivedByMemberId);
        if (f.FromDate.HasValue) q = q.Where(r => r.RequestDate >= f.FromDate.Value);
        if (f.ToDate.HasValue) q = q.Where(r => r.RequestDate <= f.ToDate.Value.AddDays(1));

        if (f.DocumentTypeId.HasValue)
            q = q.Where(r => r.Attachments.Any(a => a.DocumentTypeId == f.DocumentTypeId));

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(r => r.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CitizenRequestListItem
            {
                Id = r.Id,
                RequestCode = r.RequestCode,
                RequestDate = r.RequestDate,
                ApplicantName = r.ApplicantName,
                ApplicantPhone = r.ApplicantPhone,
                RequestSubject = r.RequestSubject,
                ReceivedByMemberName = r.ReceivedByMember != null ? r.ReceivedByMember.FullName : null,
                DestinationName = r.Destination != null ? r.Destination.Name : null,
                StatusName = r.Status.Name,
                StatusColor = r.Status.ColorClass,
                AttachmentCount = r.Attachments.Count()
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<(IList<CitizenRequestListItem> Items, int Total)> SearchAsync(
        int movementId, List<MemberFilter>? filters, FilterMatch match, int page, int pageSize)
    {
        var q = _db.CitizenRequests
            .Where(r => r.MovementId == movementId)
            .AsQueryable();

        var predicate = CitizenRequestFilterBuilder.Build(filters, match);
        if (predicate != null) q = q.Where(predicate);

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(r => r.RequestDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new CitizenRequestListItem
            {
                Id = r.Id,
                RequestCode = r.RequestCode,
                RequestDate = r.RequestDate,
                ApplicantName = r.ApplicantName,
                ApplicantPhone = r.ApplicantPhone,
                RequestSubject = r.RequestSubject,
                ReceivedByMemberName = r.ReceivedByMember != null ? r.ReceivedByMember.FullName : null,
                DestinationName = r.Destination != null ? r.Destination.Name : null,
                StatusName = r.Status.Name,
                StatusColor = r.Status.ColorClass,
                AttachmentCount = r.Attachments.Count()
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task<Dictionary<string, List<string>>> GetFilterOptionsAsync(int movementId)
    {
        var statuses = await _db.CitizenRequestStatuses
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .Select(s => s.Name)
            .ToListAsync();

        var destinations = await _db.RequestDestinations
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name)
            .Select(d => d.Name)
            .ToListAsync();

        var docTypes = await _db.DocumentTypes
            .Where(d => d.IsActive)
            .OrderBy(d => d.DisplayOrder)
            .Select(d => d.Name)
            .ToListAsync();

        // مستلمو الطلب: أعضاء الحركة الحالية فقط
        var receivers = await _db.Members
            .Where(m => m.MovementId == movementId)
            .OrderBy(m => m.FullName)
            .Select(m => m.FullName)
            .ToListAsync();

        return new Dictionary<string, List<string>>
        {
            ["status"]      = statuses,
            ["destination"] = destinations,
            ["receiver"]    = receivers,
            [CitizenRequestFilterFields.DocTypeKey] = docTypes
        };
    }

    public async Task<CitizenRequest?> GetByIdAsync(int id, int movementId)
    {
        return await _db.CitizenRequests
            .Where(r => r.Id == id && r.MovementId == movementId)
            .Include(r => r.Status)
            .Include(r => r.Destination)
            .Include(r => r.ReceivedByMember)
            .Include(r => r.CreatedByUser)
            .Include(r => r.Attachments)
                .ThenInclude(a => a.DocumentType)
            .Include(r => r.Attachments)
                .ThenInclude(a => a.UploadedByUser)
            .Include(r => r.StatusHistory.OrderBy(h => h.ChangedAt))
                .ThenInclude(h => h.FromStatus)
            .Include(r => r.StatusHistory)
                .ThenInclude(h => h.ToStatus)
            .Include(r => r.StatusHistory)
                .ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync();
    }

    public async Task<string> GenerateCodeAsync()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"CRQ-{today}-";
        var count = await _db.CitizenRequests
            .CountAsync(r => r.RequestCode.StartsWith(prefix));
        return $"{prefix}{(count + 1):D5}";
    }

    public async Task<CitizenRequest> CreateAsync(CitizenRequest request, int userId)
    {
        request.RequestCode = await GenerateCodeAsync();
        request.CreatedByUserId = userId;
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;
        request.RequestDate = DateTime.UtcNow;

        // حالة افتراضية: مستلم
        var defaultStatus = await _db.CitizenRequestStatuses
            .Where(s => s.IsDefault && s.IsActive)
            .FirstOrDefaultAsync()
            ?? await _db.CitizenRequestStatuses.OrderBy(s => s.DisplayOrder).FirstAsync();
        request.StatusId = defaultStatus.Id;

        _db.CitizenRequests.Add(request);

        // سجل الحالة الأولى
        _db.CitizenRequestStatusHistory.Add(new CitizenRequestStatusHistory
        {
            CitizenRequest = request,
            FromStatusId = null,
            ToStatusId = defaultStatus.Id,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = "إنشاء الطلب"
        });

        await _db.SaveChangesAsync();
        return request;
    }

    public async Task UpdateAsync(CitizenRequest request, int userId)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _db.CitizenRequests.Update(request);
        await _db.SaveChangesAsync();
    }

    public async Task ChangeStatusAsync(int requestId, int movementId, int newStatusId,
        int userId, string? notes, DateTime? overrideDate)
    {
        var request = await _db.CitizenRequests
            .Where(r => r.Id == requestId && r.MovementId == movementId)
            .Include(r => r.Status)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("الطلب غير موجود");

        var newStatus = await _db.CitizenRequestStatuses.FindAsync(newStatusId)
            ?? throw new InvalidOperationException("الحالة غير موجودة");

        var oldStatusId = request.StatusId;

        // تسجيل التواريخ التلقائية
        if (newStatus.Name == "مرسل" && request.SentDate == null)
            request.SentDate = DateTime.UtcNow;

        if (newStatus.Name == "إجابة عنه")
            request.AnswerDate = overrideDate?.ToUniversalTime() ?? DateTime.UtcNow;

        request.StatusId = newStatusId;
        request.UpdatedAt = DateTime.UtcNow;

        _db.CitizenRequestStatusHistory.Add(new CitizenRequestStatusHistory
        {
            CitizenRequestId = requestId,
            FromStatusId = oldStatusId,
            ToStatusId = newStatusId,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow,
            Notes = notes
        });

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id, int movementId)
    {
        var request = await _db.CitizenRequests
            .Where(r => r.Id == id && r.MovementId == movementId)
            .Include(r => r.Attachments)
            .FirstOrDefaultAsync();

        if (request == null) return;

        // حذف ملفات المرفقات
        foreach (var att in request.Attachments)
            DeleteFile(att.FilePath);

        _db.CitizenRequests.Remove(request);
        await _db.SaveChangesAsync();
    }

    public async Task<IList<RequestDestination>> GetDestinationsAsync(bool activeOnly = true)
    {
        var q = _db.RequestDestinations.AsQueryable();
        if (activeOnly) q = q.Where(d => d.IsActive);
        return await q.OrderBy(d => d.DisplayOrder).ThenBy(d => d.Name).ToListAsync();
    }

    public async Task<IList<CitizenRequestStatus>> GetStatusesAsync(bool activeOnly = true)
    {
        var q = _db.CitizenRequestStatuses.AsQueryable();
        if (activeOnly) q = q.Where(s => s.IsActive);
        return await q.OrderBy(s => s.DisplayOrder).ToListAsync();
    }

    public async Task<IList<DocumentType>> GetDocumentTypesAsync(bool activeOnly = true)
    {
        var q = _db.DocumentTypes.AsQueryable();
        if (activeOnly) q = q.Where(d => d.IsActive);
        return await q.OrderBy(d => d.DisplayOrder).ToListAsync();
    }

    public async Task<int> GetDefaultStatusIdAsync()
    {
        var s = await _db.CitizenRequestStatuses
            .Where(x => x.IsDefault && x.IsActive)
            .FirstOrDefaultAsync()
            ?? await _db.CitizenRequestStatuses.OrderBy(x => x.DisplayOrder).FirstAsync();
        return s.Id;
    }

    public async Task<CitizenRequestAttachment> AddAttachmentAsync(CitizenRequestAttachment attachment)
    {
        attachment.UploadedAt = DateTime.UtcNow;
        _db.CitizenRequestAttachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task DeleteAttachmentAsync(int attachmentId, int movementId)
    {
        var att = await _db.CitizenRequestAttachments
            .Include(a => a.CitizenRequest)
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.CitizenRequest.MovementId == movementId);

        if (att == null) return;
        DeleteFile(att.FilePath);
        _db.CitizenRequestAttachments.Remove(att);
        await _db.SaveChangesAsync();
    }

    private void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return;
        try
        {
            var full = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(full)) File.Delete(full);
        }
        catch { /* ignore */ }
    }
}
