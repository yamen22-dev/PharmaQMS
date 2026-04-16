using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PharmaQMS.API.Data;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Services;
using PharmaQMS.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
    builder.Services.AddMemoryCache();
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


    builder.Services.AddDbContext<AuthDbContext>(options =>
    {
        options.UseMySql(authDbConnectionString, ServerVersion.AutoDetect(authDbConnectionString));
    });


    // Add ASP.NET Core Identity
    builder.Services.AddIdentity<AuthUser, IdentityRole>()
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
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(jwtKeyBytes)
        };
    });

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

        Log.Information("Seeding roles and users...");
        await IdentitySeeder.SeedRolesAsync(roleManager);
        await IdentitySeeder.SeedDefaultUsersAsync(userManager, "PharmaQMS!1234");
        Log.Information("Database seeded successfully");
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Apply sanitization middleware early in the pipeline
    app.UseSanitization();

    app.UseHttpsRedirection();

    app.UseCors("SpaDev");

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
