using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.Models.DTOs.Audit;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Services.Interfaces;

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

    public async Task UpdateAsync(
        long auditId,
        AuditLogUpdateRequest request,
        CancellationToken ct = default)
    {
        var auditLog = await _db.AuditLogs
            .FirstOrDefaultAsync(x => x.Id == auditId, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"AuditLog met id {auditId} niet gevonden.");

        auditLog.Tijdstip = request.Tijdstip;
        auditLog.GebruikerId = request.GebruikerId;
        auditLog.Actie = request.Actie;
        auditLog.EntiteitType = request.EntiteitType;
        auditLog.OudWaarde = request.OudWaarde;
        auditLog.NieuweWaarde = request.NieuweWaarde;

        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAsync(long auditId, CancellationToken ct = default)
    {
        var auditLog = await _db.AuditLogs
            .FirstOrDefaultAsync(x => x.Id == auditId, ct)
            .ConfigureAwait(false)
            ?? throw new KeyNotFoundException($"AuditLog met id {auditId} niet gevonden.");

        _db.AuditLogs.Remove(auditLog);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<AuditLogResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.AuditLogs
            .OrderByDescending(x => x.Tijdstip)
            .Select(x => new AuditLogResponse(
                x.Id,
                x.Tijdstip,
                x.GebruikerId,
                x.Actie,
                x.EntiteitType,
                x.EntiteitId,
                x.OudWaarde,
                x.NieuweWaarde,
                x.IPAdres))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
