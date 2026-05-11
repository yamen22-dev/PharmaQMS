namespace PharmaQMS.API.DTOs.RawMaterials;

public sealed record RawMaterialResponse(
    int Id,
    string Name,
    string PharmaceuticalApi,
    string Category,
    string Unit,
    decimal MinSpecificationLimit,
    decimal MaxSpecificationLimit,
    string Supplier,
    string CepNumber,
    string Notes,
    DateTime CreatedUtc);