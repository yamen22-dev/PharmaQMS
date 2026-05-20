namespace PharmaQMS.API.DTOs.RawMaterials;

public sealed record RawMaterialLotResponse(
    int Id,
    string LotNumber,
    string Status);