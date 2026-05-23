using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.Models.DTOs.Bmr;

public sealed record CreateBmrRequest
{
    [Required]
    public required Guid MasterRecipeId { get; init; }

    [Required]
    [Range(0.001, double.MaxValue, ErrorMessage = "BatchSize must be greater than 0.")]
    public required decimal BatchSize { get; init; }

    [Required]
    public required Guid ProductionLineId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "At least one lot must be linked.")]
    public required IReadOnlyList<int> LotIds { get; init; }
}