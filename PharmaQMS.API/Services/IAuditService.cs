namespace PharmaQMS.API.Services;

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
}
