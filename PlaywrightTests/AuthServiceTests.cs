using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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