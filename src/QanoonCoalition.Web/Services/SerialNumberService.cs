using Microsoft.EntityFrameworkCore;
using QanoonCoalition.Web.Data;

namespace QanoonCoalition.Web.Services;

public class SerialNumberService : ISerialNumberService
{
    private readonly AppDbContext _db;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public SerialNumberService(AppDbContext db) => _db = db;

    public async Task<string> GenerateAsync()
    {
        await _lock.WaitAsync();
        try
        {
            string serial;
            do
            {
                // رقم عشوائي من 10000000 إلى 99999999
                serial = Random.Shared.Next(10000000, 99999999).ToString();
            }
            while (await _db.Members.AnyAsync(m => m.SerialNumber == serial));

            return serial;
        }
        finally
        {
            _lock.Release();
        }
    }

    public string GenerateReferenceNumber()
    {
        // REQ-yyMMddHHmmss-RRR  →  max 19 chars (fits in 20)
        var timestamp = DateTime.UtcNow.ToString("yyMMddHHmmss");
        var random = Random.Shared.Next(100, 999);
        return $"REQ-{timestamp}-{random}";
    }
}
