using PharmaQMS.API.Infrastructure;

namespace PharmaQMS.API.DTOs.Lot;

public sealed record CreateLotRequest(
    [SanitizedString] string LotNumber,
    decimal Quantity,
    DateTime ReceivedDateUtc,
    DateTime ExpiryDateUtc,
    [SanitizedString] string PurchaseOrderNumber,
    [SanitizedString] string? AnalysisCertificate = null
);