namespace PharmaQMS.API.DTOs.QualityControl;

public sealed record QcTestParameterResponse(
    int Id,
    string Name,
    string Unit,
    decimal Min,
    decimal Max,
    decimal? MeasuredValue,
    bool IsWithinSpecification);