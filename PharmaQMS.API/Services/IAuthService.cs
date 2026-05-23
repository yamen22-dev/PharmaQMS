using PharmaQMS.API.DTOs.Auth;

namespace PharmaQMS.API.Services;

public interface IAuthService
{
    Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<AuthenticationResult> RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<bool> VerifyPasswordAsync(string userId, string password, CancellationToken cancellationToken = default);
    Task<string> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> GetUsernameByIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<string> GetUserFullNameAsync(string userId, CancellationToken cancellationToken = default);
}
