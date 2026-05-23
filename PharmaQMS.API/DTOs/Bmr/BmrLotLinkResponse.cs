namespace PharmaQMS.API.Models.DTOs.Bmr;

public sealed record BmrLotLinkResponse(
    int LotId,
    string LotNumber,
    string RawMaterialName
);