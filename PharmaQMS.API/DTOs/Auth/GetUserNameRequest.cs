using System.ComponentModel.DataAnnotations;

namespace PharmaQMS.API.DTOs.Auth;
public sealed class GetUserNameRequest
{
    [Required(ErrorMessage = "User ID is required.")]
    [StringLength(450, ErrorMessage = "User ID must not exceed 450 characters.")]
    public string UserId { get; set; } = string.Empty;
}