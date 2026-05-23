namespace PharmaQMS.API.Models.DTOs.Bmr;

using PharmaQMS.API.Models.Enums;

public sealed record BmrSummaryResponse(
    Guid Id,
    string BatchNumber,
    string RecipeName,
    string ProductionLineName,
    DateTime CreatedAt,
    BmrStatus Status,
    int TotalSteps,
    int CompletedSteps
);