using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Infrastructure;

public sealed class AuditTrailInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly List<PendingAuditEntry> _pendingEntries = [];
    private bool _isWritingAudit;

    public AuditTrailInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_isWritingAudit || eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        _pendingEntries.Clear();

        var context = eventData.Context;
        var now = DateTime.UtcNow;
        var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException("AuditLog records zijn onwijzigbaar en kunnen niet worden aangepast of verwijderd.");
                }

                continue;
            }

            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var action = entry.State switch
            {
                EntityState.Added => "CREATE",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => "UNKNOWN"
            };

            var oldValues = entry.State == EntityState.Added
                ? null
                : JsonSerializer.Serialize(entry.OriginalValues.Properties.ToDictionary(
                    p => p.Name,
                    p => entry.OriginalValues[p]));

            var newValues = entry.State == EntityState.Deleted
                ? null
                : JsonSerializer.Serialize(entry.CurrentValues.Properties.ToDictionary(
                    p => p.Name,
                    p => entry.CurrentValues[p]));

            _pendingEntries.Add(new PendingAuditEntry(entry, now, userId, ipAddress, action, oldValues, newValues));
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (_isWritingAudit || eventData.Context is null || _pendingEntries.Count == 0)
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        var context = eventData.Context;
        var auditLogs = _pendingEntries.Select(x => x.ToAuditLog()).ToList();

        if (auditLogs.Count > 0)
        {
            _isWritingAudit = true;
            try
            {
                context.Set<AuditLog>().AddRange(auditLogs);
                await context.SaveChangesAsync(cancellationToken);
            }
            finally
            {
                _isWritingAudit = false;
            }
        }

        _pendingEntries.Clear();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private sealed class PendingAuditEntry
    {
        private readonly EntityEntry _entry;
        private readonly DateTime _tijdstip;
        private readonly string _gebruikerId;
        private readonly string _ipAdres;
        private readonly string _actie;
        private readonly string? _oudeWaarde;
        private readonly string? _nieuweWaarde;

        public PendingAuditEntry(
            EntityEntry entry,
            DateTime tijdstip,
            string gebruikerId,
            string ipAdres,
            string actie,
            string? oudeWaarde,
            string? nieuweWaarde)
        {
            _entry = entry;
            _tijdstip = tijdstip;
            _gebruikerId = gebruikerId;
            _ipAdres = ipAdres;
            _actie = actie;
            _oudeWaarde = oudeWaarde;
            _nieuweWaarde = nieuweWaarde;
        }

        public AuditLog ToAuditLog()
        {
            var key = _entry.Metadata.FindPrimaryKey();
            var keyValue = key is null
                ? string.Empty
                : string.Join(",", key.Properties.Select(k => _entry.Property(k.Name).CurrentValue?.ToString() ?? string.Empty));

            return new AuditLog
            {
                Tijdstip = _tijdstip,
                GebruikerId = _gebruikerId,
                Actie = _actie,
                EntiteitType = _entry.Metadata.ClrType.Name,
                EntiteitId = keyValue,
                OudWaarde = _oudeWaarde,
                NieuweWaarde = _nieuweWaarde,
                IPAdres = _ipAdres
            };
        }
    }
}