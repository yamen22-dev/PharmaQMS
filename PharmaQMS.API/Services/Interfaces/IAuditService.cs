using PharmaQMS.API.Models.DTOs.Audit;

namespace PharmaQMS.API.Services.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        string performedByUserId,
        CancellationToken ct = default);

    Task UpdateAsync(
        long auditId,
        AuditLogUpdateRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(
        long auditId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogResponse>> GetAllAsync(CancellationToken ct = default);
}