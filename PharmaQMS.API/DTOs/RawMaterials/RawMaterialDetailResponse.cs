namespace PharmaQMS.API.DTOs.RawMaterials;

public sealed record RawMaterialDetailResponse(
    int Id,
    string Name,
    string PharmaceuticalApi,
    string Category,
    string Unit,
    string Supplier,
    decimal MinSpecificationLimit,
    decimal MaxSpecificationLimit,
    string Notes,
    IReadOnlyList<RawMaterialLotResponse> Lots
);