using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.DTOs.QualityControl;

public sealed record SubmitQcTestResultsRequest
{
    [Required]
    public string Password { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public IReadOnlyList<SubmitQcTestResultParameterRequest> Parameters { get; init; }
        = Array.Empty<SubmitQcTestResultParameterRequest>();
}

public sealed record SubmitQcTestResultParameterRequest
{
    [Required]
    public int ParameterId { get; init; }

    [Required]
    public decimal MeasuredValue { get; init; }
}