using System.ComponentModel.DataAnnotations;
using PharmaQMS.API.Infrastructure;

namespace PharmaQMS.API.DTOs.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email format is invalid.")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters.")]
    [SanitizedString(maxLength: 256, allowHtml: false)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(256, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 256 characters.")]
    public string Password { get; set; } = string.Empty;
}
