using System.ComponentModel.DataAnnotations;
namespace PharmaQMS.API.Models.DTOs.Bmr;

public sealed record VerifyStepRequest
{
    [Required]
    public required string Password { get; init; }
}