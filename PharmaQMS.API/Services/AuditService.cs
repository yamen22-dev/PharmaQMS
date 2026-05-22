using PharmaQMS.API.Data;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Services;

public sealed class AuditService : IAuditService
{
    private readonly DomainDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(DomainDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        string performedByUserId,
        CancellationToken ct = default)
    {
        var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty;


        var utcNow = DateTime.UtcNow;
        var amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, amsterdamTimeZone);
        
        var entry = new AuditLog
        {
            Tijdstip = localTime,
            GebruikerId = performedByUserId.ToString(),
            Actie = action ?? string.Empty,
            EntiteitType = entityName ?? string.Empty,
            EntiteitId = entityId ?? string.Empty,
            OudWaarde = oldValue,
            NieuweWaarde = newValue,
            IPAdres = ip
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
