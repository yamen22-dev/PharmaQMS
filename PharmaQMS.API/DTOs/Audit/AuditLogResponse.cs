namespace PharmaQMS.API.Models.DTOs.Audit;

public sealed record AuditLogResponse(
    long Id,
    DateTime Tijdstip,
    string GebruikerId,
    string Actie,
    string EntiteitType,
    string EntiteitId,
    string? OudWaarde,
    string? NieuweWaarde,
    string IPAdres
);