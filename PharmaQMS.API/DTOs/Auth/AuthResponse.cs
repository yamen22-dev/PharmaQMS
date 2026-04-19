namespace PharmaQMS.API.DTOs.Auth;

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc,
    string UserId,
    string Email,
    string FirstName,
    string LastName,
    string[] Roles);
