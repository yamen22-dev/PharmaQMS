namespace PharmaQMS.API.Models.DTOs.Audit;

public sealed record AuditLogUpdateRequest(
    string EntiteitType,
    string Actie,
    string? OudWaarde,
    string? NieuweWaarde,
    string GebruikerId,
    DateTime Tijdstip
);