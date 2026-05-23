namespace PharmaQMS.API.Models.DTOs.Bmr;

public sealed record BmrStepResponse(
    Guid Id,
    int StepNumber,
    string StepName,
    bool IsCritical,
    BmrStepStatus Status,
    string? EnteredData,
    string? EnteredById,
    DateTime? EnteredAt,
    string? VerifiedById,
    DateTime? VerifiedAt,
    string? DeviationNote
);
