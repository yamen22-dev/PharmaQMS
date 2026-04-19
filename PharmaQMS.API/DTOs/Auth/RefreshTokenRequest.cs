using System.ComponentModel.DataAnnotations;
using PharmaQMS.API.Infrastructure;

namespace PharmaQMS.API.DTOs.Auth;

public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    [StringLength(512, ErrorMessage = "Refresh token must not exceed 512 characters.")]
    [SanitizedString(maxLength: 512, allowHtml: false)]
    public string RefreshToken { get; set; } = string.Empty;
}
