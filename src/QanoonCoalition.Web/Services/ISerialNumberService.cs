namespace QanoonCoalition.Web.Services;

public interface ISerialNumberService
{
    /// <summary>توليد رقم تسلسلي فريد من 8 أرقام</summary>
    Task<string> GenerateAsync();

    /// <summary>توليد رقم مرجعي لطلب الانضمام</summary>
    string GenerateReferenceNumber();
}
