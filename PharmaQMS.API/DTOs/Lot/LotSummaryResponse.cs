namespace PharmaQMS.API.DTOs.Lot;

public sealed record LotSummaryResponse(
    int Id,
    string LotNumber,
    int RawMaterialId,
    string RawMaterialName,
    string Supplier,
    decimal Quantity,
    string Unit,
    DateTime ReceivedDateUtc,
    string Status
);