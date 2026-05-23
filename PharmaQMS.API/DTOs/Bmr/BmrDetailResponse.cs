namespace PharmaQMS.API.Models.DTOs.Bmr;

using PharmaQMS.API.Models.Enums;

public sealed record BmrDetailResponse(
    Guid Id,
    string BatchNumber,
    string RecipeName,
    string RecipeVersion,
    string ProductionLineName,
    decimal BatchSize,
    BmrStatus Status,
    DateTime CreatedAt,
    IReadOnlyList<BmrLotLinkResponse> Lots,
    IReadOnlyList<BmrStepResponse> Steps
);
