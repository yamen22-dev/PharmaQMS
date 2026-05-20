namespace PharmaQMS.API.DTOs.RawMaterials;

public sealed record RawMaterialOverviewResponse(
    int Id,
    string Name,
    string PharmaceuticalApi,
    string Category,
    string Unit,
    int ActiveLots,
    int QuarantineLots);