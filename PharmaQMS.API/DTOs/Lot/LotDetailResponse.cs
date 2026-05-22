namespace PharmaQMS.API.DTOs.Lot;
public sealed record LotDetailResponse(
    int Id,
    string LotNumber,
    int RawMaterialId,
    string RawMaterialName,
    string Supplier,
    string PharmaceuticalApi,
    decimal Quantity,
    string Unit,
    DateTime ReceivedDateUtc,
    decimal MinSpecificationLimit,
    decimal MaxSpecificationLimit,
    string Status,
    DateTime ExpireDateUtc
);