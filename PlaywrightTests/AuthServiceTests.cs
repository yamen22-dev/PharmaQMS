using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Auth;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Services;

namespace PlaywrightTests;

public sealed class AuthServiceTests
{
    private const string TestEmail = "jane.doe@example.com";
    private const string TestPassword = "ValidP@ssword123!";
    private readonly ITestOutputHelper _output;

    public AuthServiceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("TestId", "UT-UC01-01")]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessfulResultAndToken()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<AuthUser, IdentityRole>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 6;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        services.PostConfigure<IdentityOptions>(options =>
        {
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 6;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        });

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var user = new AuthUser
        {
            UserName = TestEmail,
            Email = TestEmail,
            FirstName = "Jane",
            LastName = "Doe",
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, TestPassword);
        Assert.True(createResult.Succeeded);

        var authService = new AuthService(
            userManager,
            dbContext,
            scope.ServiceProvider.GetRequiredService<ILogger<AuthService>>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>());

        var result = await authService.LoginAsync(new LoginRequest
        {
            Email = TestEmail,
            Password = TestPassword
        }, cancellationToken);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.NotNull(result.Response);
        Assert.NotNull(result.Response!.AccessToken);
        Assert.NotEmpty(result.Response.AccessToken);
        Assert.Equal(TestEmail, result.Response.Email);
        Assert.Equal(user.Id, result.Response.UserId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Response.AccessToken);
        Assert.Equal(user.Id, jwt.Subject);
        Assert.Equal(TestEmail, jwt.Claims.First(claim => claim.Type == ClaimTypes.Email).Value);
        Assert.Equal(1, await dbContext.RefreshTokens.CountAsync(cancellationToken));
    }

    [Fact]
    [Trait("TestId", "UT-UC01-02")]
    public void ExpiredAccessToken_IsRejectedByValidation()
    {
        var config = CreateConfiguration();
        var key = config["Jwt:Key"]!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "user-1"),
            new Claim(ClaimTypes.Email, TestEmail)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var handler = new JwtSecurityTokenHandler();

        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(tokenString, validationParameters, out _));
    }

    [Fact]
    [Trait("TestId", "UT-NFR02-01")]
    public async Task RefreshTokenRotation_GeneratesNewTokenAndDeactivatesOld()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<AuthUser, IdentityRole>(options => { })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var user = new AuthUser { UserName = TestEmail, Email = TestEmail };
        var createResult = await userManager.CreateAsync(user, TestPassword);
        Assert.True(createResult.Succeeded);

        var authService = new AuthService(
            userManager,
            dbContext,
            scope.ServiceProvider.GetRequiredService<ILogger<AuthService>>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>());

        var loginResult = await authService.LoginAsync(new LoginRequest { Email = TestEmail, Password = TestPassword }, cancellationToken);
        Assert.True(loginResult.Succeeded);
        var oldRefresh = loginResult.Response!.RefreshToken;

        var tokenRecordBefore = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(oldRefresh))), cancellationToken);
        Assert.NotNull(tokenRecordBefore);

        var refreshResult = await authService.RefreshAsync(new RefreshTokenRequest { RefreshToken = oldRefresh }, cancellationToken);
        Assert.True(refreshResult.Succeeded);
        var newRefresh = refreshResult.Response!.RefreshToken;

        var oldRecord = await dbContext.RefreshTokens.SingleAsync(x => x.TokenHash == tokenRecordBefore!.TokenHash, cancellationToken);
        var newRecord = await dbContext.RefreshTokens.SingleAsync(x => x.TokenHash == Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(newRefresh))), cancellationToken);

        Assert.NotNull(oldRecord.RevokedUtc);
        Assert.Equal(newRecord.TokenHash, oldRecord.ReplacedByTokenHash);
    }

    [Fact]
    [Trait("TestId", "UT-NFR04-01")]
    public async Task StoredPassword_IsHashedNotPlaintext()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<AuthUser, IdentityRole>(options => { })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var user = new AuthUser { UserName = TestEmail, Email = TestEmail };
        var createResult = await userManager.CreateAsync(user, TestPassword);
        Assert.True(createResult.Succeeded);

        var stored = await dbContext.Users.SingleAsync(u => u.Email == TestEmail, cancellationToken);
        Assert.False(string.Equals(stored.PasswordHash, TestPassword, StringComparison.Ordinal));
        Assert.DoesNotContain(TestPassword, stored.PasswordHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoginAsync_WithFiveFailedAttempts_ThenSixthAttempt_BlocksAccount()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddSingleton<IConfiguration>(CreateConfiguration());
        services.AddDbContext<AuthDbContext>(options => options.UseSqlite(connection));
        services
            .AddIdentity<AuthUser, IdentityRole>(options =>
            {
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();
        var user = new AuthUser
        {
            UserName = TestEmail,
            Email = TestEmail,
            FirstName = "Jane",
            LastName = "Doe",
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, TestPassword);
        Assert.True(createResult.Succeeded);

        var authService = new AuthService(
            userManager,
            dbContext,
            scope.ServiceProvider.GetRequiredService<ILogger<AuthService>>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>());

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var result = await authService.LoginAsync(new LoginRequest
            {
                Email = TestEmail,
                Password = $"WrongPassword-{attempt}!"
            }, cancellationToken);

            user = await userManager.FindByIdAsync(user.Id) ?? user;
            var failedLoginCount = await userManager.GetAccessFailedCountAsync(user);
            var isBlocked = await userManager.IsLockedOutAsync(user);

            _output.WriteLine($"Attempt {attempt}: Succeeded={result.Succeeded}, Status={result.StatusCode}, Error={result.Error}, FailedLoginCount={failedLoginCount}, IsBlocked={isBlocked}");

            if (attempt < 5)
            {
                Assert.False(result.Succeeded);
                Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
            }
        }

        user = await userManager.FindByIdAsync(user.Id) ?? user;
        Assert.Equal(5, await userManager.GetAccessFailedCountAsync(user));
        Assert.False(await userManager.IsLockedOutAsync(user));

        var sixthResult = await authService.LoginAsync(new LoginRequest
        {
            Email = TestEmail,
            Password = "WrongPassword-6!"
        }, cancellationToken);

        user = await userManager.FindByIdAsync(user.Id) ?? user;
        var sixthFailedLoginCount = await userManager.GetAccessFailedCountAsync(user);
        var sixthIsBlocked = await userManager.IsLockedOutAsync(user);

        _output.WriteLine($"Attempt 6: Succeeded={sixthResult.Succeeded}, Status={sixthResult.StatusCode}, Error={sixthResult.Error}, FailedLoginCount={sixthFailedLoginCount}, IsBlocked={sixthIsBlocked}");

        Assert.False(sixthResult.Succeeded);
        Assert.Equal(StatusCodes.Status423Locked, sixthResult.StatusCode);
        Assert.NotNull(sixthResult.Error);
        Assert.True(sixthIsBlocked);
    }

    private static IConfiguration CreateConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "https://tests.local",
            ["Jwt:Audience"] = "https://tests.local",
            ["Jwt:Key"] = "0123456789ABCDEF0123456789ABCDEF",
            ["Jwt:AccessTokenMinutes"] = "15",
            ["Jwt:RefreshTokenDays"] = "7"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}