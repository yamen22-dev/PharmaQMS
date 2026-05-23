namespace PharmaQMS.API.DTOs.QualityControl;
using System.ComponentModel.DataAnnotations;
public sealed record CreateQcTestRequest
{
    [Required]
    public string TestObjectType { get; init; } = string.Empty;

    [Required]
    public int TestObjectId { get; init; }

    [Required, MinLength(1)]
    public IReadOnlyList<QcTestParameterDto> Parameters { get; init; }
        = Array.Empty<QcTestParameterDto>();

    /// <summary>
    /// Elektronische handtekening — vereist door FDA 21 CFR Part 11.
    /// Wachtwoord van de aangemelde gebruiker ter bevestiging van de identiteit.
    /// </summary>
    [Required]
    public string Password { get; init; } = string.Empty;
}