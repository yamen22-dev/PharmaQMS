using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using System.Net;
using MySqlConnector;
using PharmaQMS.API.Data;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Services;
using PharmaQMS.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 64 * 1024;
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);
    options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
});

// Bootstrap Serilog so startup logs (and early failures) are captured
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web host");

    // Add services to the container.
    builder.Services.AddControllers();
    builder.Services.AddProblemDetails();
    builder.Services.AddMemoryCache();
    builder.Services.AddRequestTimeouts(options =>
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
    });
    var knownProxies = builder.Configuration.GetSection("Security:ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
    var knownNetworks = builder.Configuration.GetSection("Security:ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [];

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.RequireHeaderSymmetry = true;

        foreach (var proxy in knownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ip))
            {
                options.KnownProxies.Add(ip);
            }
        }

        foreach (var network in knownNetworks)
        {
            var parts = network.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2 && IPAddress.TryParse(parts[0], out var prefix) && int.TryParse(parts[1], out var prefixLength))
            {
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
            }
        }
    });
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.Headers.RetryAfter = "10";
            await context.HttpContext.Response.WriteAsync("Too many requests. Please retry later.", token);
        };

        options.AddPolicy("auth", httpContext =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });

        options.AddPolicy("auth-login", httpContext =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        {
            var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SpaDev", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    // Add DbContexts
    var authDbConnectionString = builder.Configuration.GetConnectionString("AuthDb");
    if (string.IsNullOrWhiteSpace(authDbConnectionString))
    {
        throw new InvalidOperationException("ConnectionStrings:AuthDb is missing.");
    }

    if (!builder.Environment.IsDevelopment())
    {
        var csBuilder = new MySqlConnectionStringBuilder(authDbConnectionString);
        if (csBuilder.SslMode is MySqlSslMode.None or MySqlSslMode.Preferred)
        {
            throw new InvalidOperationException("Production AuthDb connection must enforce TLS. Configure SslMode=Required, VerifyCA, or VerifyFull.");
        }
    }


    builder.Services.AddDbContext<AuthDbContext>(options =>
    {
        options.UseMySql(authDbConnectionString, ServerVersion.AutoDetect(authDbConnectionString), mySqlOptions =>
        {
            mySqlOptions.CommandTimeout(15);
            mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null);
        });
    });


    // Add ASP.NET Core Identity
    builder.Services.AddIdentity<AuthUser, IdentityRole>(options =>
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

    // Add Authentication
    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        throw new InvalidOperationException("JWT signing key is missing. Configure Jwt:Key in appsettings.");
    }
    var jwtKeyBytes = System.Text.Encoding.UTF8.GetBytes(jwtKey!);

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer(options =>
    {
        options.IncludeErrorDetails = false;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(jwtKeyBytes)
        };
    });
    builder.Services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

    // Add Services
    builder.Services.AddScoped<IAuthService, AuthService>();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Configure Serilog for the host, read full config from app configuration
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration)
                     .Enrich.FromLogContext();
    });

    var app = builder.Build();

    // Apply migrations and seed database
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Log.Information("Ensuring database exists...");
        await dbContext.Database.MigrateAsync();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();

        Log.Information("Seeding roles...");
        await IdentitySeeder.SeedRolesAsync(roleManager);

        if (app.Environment.IsDevelopment())
        {
            var seedDefaultUsers = builder.Configuration.GetValue<bool>("Seed:EnableDefaultUsers");
            var seedDefaultUserPassword = builder.Configuration["Seed:DefaultUserPassword"];

            if (!seedDefaultUsers)
            {
                Log.Information("Skipping default user seeding in development because Seed:EnableDefaultUsers is false.");
            }
            else if (string.IsNullOrWhiteSpace(seedDefaultUserPassword)
                     || seedDefaultUserPassword.StartsWith("USE_", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Seed:DefaultUserPassword must be set to a strong non-placeholder value when Seed:EnableDefaultUsers=true.");
            }
            else
            {
                Log.Information("Seeding default users for development...");
                await IdentitySeeder.SeedDefaultUsersAsync(userManager, seedDefaultUserPassword);
            }
        }
        else
        {
            Log.Information("Skipping default user seeding outside development.");
        }

        Log.Information("Database seeded successfully");
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }
    else
    {
        app.UseHsts();
    }

    app.UseExceptionHandler();
    app.UseForwardedHeaders();

    app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'; base-uri 'none';";
        await next();
    });

    // Apply sanitization middleware early in the pipeline
    app.UseSanitization();

    app.UseHttpsRedirection();

    app.UseCors("SpaDev");

    app.UseRateLimiter();
    app.UseRequestTimeouts();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
