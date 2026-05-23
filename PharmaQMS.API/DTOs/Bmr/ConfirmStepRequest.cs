using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Models.DTOs.Bmr;

public sealed record ConfirmStepRequest
{
    [Required]
    [MaxLength(2000)]
    public required string EnteredData { get; init; }

    [MaxLength(1000)]
    public string? DeviationNote { get; init; }
}