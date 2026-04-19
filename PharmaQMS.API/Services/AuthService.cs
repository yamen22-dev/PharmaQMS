using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Auth;
using PharmaQMS.API.Models.Entities;

namespace PharmaQMS.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<AuthUser> _userManager;
    private readonly AuthDbContext _dbContext;
    private readonly byte[] _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;
    private readonly int _maxFailedAccessAttempts;
    private readonly TimeSpan _defaultLockoutTimeSpan;

    public AuthService(
        UserManager<AuthUser> userManager,
        AuthDbContext dbContext,
        IConfiguration configuration,
        IOptions<IdentityOptions> identityOptions)
    {
        _userManager = userManager;
        _dbContext = dbContext;

        _issuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Issuer' is missing.");
        _audience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Audience' is missing.");
        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configuration value 'Jwt:Key' is missing.");

        _accessTokenMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        _refreshTokenDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;
        _maxFailedAccessAttempts = identityOptions.Value.Lockout.MaxFailedAccessAttempts;
        _defaultLockoutTimeSpan = identityOptions.Value.Lockout.DefaultLockoutTimeSpan <= TimeSpan.Zero
            ? TimeSpan.FromMinutes(15)
            : identityOptions.Value.Lockout.DefaultLockoutTimeSpan;
        _signingKey = Encoding.UTF8.GetBytes(key);
    }

    public async Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return AuthenticationResult.Failure("Email and password are required.", StatusCodes.Status400BadRequest);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return AuthenticationResult.Failure("Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        var lockoutEnabled = await _userManager.GetLockoutEnabledAsync(user);
        if (!lockoutEnabled)
        {
            var setLockoutEnabledResult = await _userManager.SetLockoutEnabledAsync(user, true);
            if (!setLockoutEnabledResult.Succeeded)
            {
                return AuthenticationResult.Failure("Failed to update account lockout state.", StatusCodes.Status500InternalServerError);
            }

            user = await _userManager.FindByIdAsync(user.Id) ?? user;
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return AuthenticationResult.Failure("Account is temporarily locked. Try again later.", StatusCodes.Status423Locked);
        }

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            var accessFailedResult = await _userManager.AccessFailedAsync(user);
            if (!accessFailedResult.Succeeded)
            {
                return AuthenticationResult.Failure("Failed to update account lockout state.", StatusCodes.Status500InternalServerError);
            }

            user = await _userManager.FindByIdAsync(user.Id) ?? user;

            if (await _userManager.IsLockedOutAsync(user))
            {
                return AuthenticationResult.Failure("Account is temporarily locked. Try again later.", StatusCodes.Status423Locked);
            }

            var accessFailedCount = await _userManager.GetAccessFailedCountAsync(user);
            if (_maxFailedAccessAttempts > 0 && accessFailedCount >= _maxFailedAccessAttempts)
            {
                var lockoutEndUtc = DateTimeOffset.UtcNow.Add(_defaultLockoutTimeSpan);
                var forceLockoutResult = await _userManager.SetLockoutEndDateAsync(user, lockoutEndUtc);
                if (!forceLockoutResult.Succeeded)
                {
                    return AuthenticationResult.Failure("Failed to update account lockout state.", StatusCodes.Status500InternalServerError);
                }

                return AuthenticationResult.Failure("Account is temporarily locked. Try again later.", StatusCodes.Status423Locked);
            }

            return AuthenticationResult.Failure("Invalid email or password.", StatusCodes.Status401Unauthorized);
        }

        var resetFailedCountResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!resetFailedCountResult.Succeeded)
        {
            return AuthenticationResult.Failure("Failed to reset failed login attempts.", StatusCodes.Status500InternalServerError);
        }

        var response = await IssueTokensAsync(user, cancellationToken);
        return AuthenticationResult.Success(response);
    }

    public async Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthenticationResult.Failure("Refresh token is required.", StatusCodes.Status400BadRequest);
        }

        var refreshTokenHash = HashToken(request.RefreshToken);
        var tokenRecord = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (tokenRecord is null)
        {
            return AuthenticationResult.Failure("Invalid refresh token.", StatusCodes.Status400BadRequest);
        }

        var now = DateTime.UtcNow;
        if (tokenRecord.RevokedUtc is not null)
        {
            await RevokeAllActiveTokensAsync(tokenRecord.UserId, "Refresh token reuse detected.", cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return AuthenticationResult.Failure("Refresh token reuse detected.", StatusCodes.Status401Unauthorized);
        }

        if (tokenRecord.ExpiresUtc <= now)
        {
            return AuthenticationResult.Failure("Refresh token has expired.", StatusCodes.Status401Unauthorized);
        }

        var user = await _userManager.FindByIdAsync(tokenRecord.UserId);
        if (user is null)
        {
            return AuthenticationResult.Failure("User not found.", StatusCodes.Status404NotFound);
        }

        var response = await IssueRotatedTokensAsync(user, tokenRecord, cancellationToken);
        return AuthenticationResult.Success(response);
    }

    public async Task<AuthenticationResult> RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return AuthenticationResult.SuccessNoContent();
        }

        var refreshTokenHash = HashToken(request.RefreshToken);
        var tokenRecord = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (tokenRecord is null)
        {
            return AuthenticationResult.SuccessNoContent();
        }

        if (tokenRecord.RevokedUtc is null)
        {
            tokenRecord.RevokedUtc = DateTime.UtcNow;
            tokenRecord.RevokeReason = "Revoked by user.";
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return AuthenticationResult.SuccessNoContent();
    }

    private async Task<AuthResponse> IssueTokensAsync(AuthUser user, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, out var accessTokenExpiresUtc, out var jwtId);
        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, jwtId);

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            accessTokenExpiresUtc,
            refreshToken,
            refreshTokenEntity.ExpiresUtc,
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToArray());
    }

    private async Task<AuthResponse> IssueRotatedTokensAsync(AuthUser user, RefreshToken currentToken, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = CreateAccessToken(user, roles, out var accessTokenExpiresUtc, out var jwtId);
        var (refreshToken, refreshTokenEntity) = CreateRefreshToken(user.Id, jwtId);

        currentToken.RevokedUtc = DateTime.UtcNow;
        currentToken.RevokeReason = "Rotated.";
        currentToken.ReplacedByTokenHash = refreshTokenEntity.TokenHash;

        _dbContext.RefreshTokens.Add(refreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            accessTokenExpiresUtc,
            refreshToken,
            refreshTokenEntity.ExpiresUtc,
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToArray());
    }

    private async Task RevokeAllActiveTokensAsync(string userId, string reason, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var activeTokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.ExpiresUtc > now)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens.Where(x => x.RevokedUtc is null))
        {
            token.RevokedUtc = now;
            token.RevokeReason = reason;
        }
    }

    private string CreateAccessToken(AuthUser user, IEnumerable<string> roles, out DateTime expiresUtc, out string jwtId)
    {
        jwtId = Guid.NewGuid().ToString("N");
        expiresUtc = DateTime.UtcNow.AddMinutes(_accessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (string Token, RefreshToken Entity) CreateRefreshToken(string userId, string jwtId)
    {
        var token = GenerateRefreshToken();
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(token),
            JwtId = jwtId,
            CreatedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.AddDays(_refreshTokenDays)
        };

        return (token, entity);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
