using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
