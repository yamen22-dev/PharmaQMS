namespace PharmaQMS.API.DTOs.QualityControl;
using System.ComponentModel.DataAnnotations;
public sealed record QcTestParameterDto
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    [Required]
    public decimal Min { get; init; }

    [Required]
    public decimal Max { get; init; }

    [Required, MaxLength(20)]
    public string Unit { get; init; } = string.Empty;
}