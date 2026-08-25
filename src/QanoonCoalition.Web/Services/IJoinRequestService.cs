using QanoonCoalition.Web.Models;

namespace QanoonCoalition.Web.Services;

public interface IJoinRequestService
{
    Task<JoinRequest> SubmitAsync(JoinRequest request);
    Task<(List<JoinRequest> Items, int Total)> GetAsync(int? movementId, RequestStatus? status, int page, int pageSize);
    Task<JoinRequest?> GetByIdAsync(int id);
    Task<JoinRequest?> GetByReferenceAsync(string reference);
    Task<Member> ApproveAsync(int requestId, string? benefitField, int reviewedByUserId);
    Task RejectAsync(int requestId, string reason, int reviewedByUserId);
    Task<(int Pending, int Approved, int Rejected)> GetCountsAsync(int? movementId = null);
}
