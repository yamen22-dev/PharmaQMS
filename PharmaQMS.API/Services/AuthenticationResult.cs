using Microsoft.AspNetCore.Http;
using PharmaQMS.API.DTOs.Auth;

namespace PharmaQMS.API.Services;

public sealed record AuthenticationResult(bool Succeeded, string? Error, AuthResponse? Response, int StatusCode)
{
    public static AuthenticationResult Success(AuthResponse response, int statusCode = StatusCodes.Status200OK)
        => new(true, null, response, statusCode);

    public static AuthenticationResult SuccessNoContent()
        => new(true, null, null, StatusCodes.Status204NoContent);

    public static AuthenticationResult Failure(string error, int statusCode)
        => new(false, error, null, statusCode);
}
