using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Auth;
using PharmaQMS.API.DTOs.Lot;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.DTOs.Audit;
using PharmaQMS.API.Services;
using PharmaQMS.API.Services.Interfaces;

namespace PlaywrightTests;

public sealed class LotServiceTests
{
    [Fact]
    public async Task CreateLotAsync_WithValidRawMaterial_CreatesLotInQuarantineAndWritesAuditEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddDbContext<DomainDbContext>(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var rawMaterial = new RawMaterial
        {
            Id = 1,
            Name = "Acetylsalicylic Acid",
            PharmaceuticalApi = "Acetylsalicylic Acid",
            Unit = "kg",
            Supplier = "ACME Pharma",
            MinSpecificationLimit = 0.95m,
            MaxSpecificationLimit = 1.05m
        };

        dbContext.RawMaterials.Add(rawMaterial);
        await dbContext.SaveChangesAsync(cancellationToken);

        var auditService = new TestAuditService(dbContext);
        var lotService = new LotService(dbContext, new TestAuthService(), auditService);

        var request = new CreateLotRequest(
            LotNumber: "LOT-2026-0001",
            Quantity: 125.5000m,
            ReceivedDateUtc: new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc),
            ExpiryDateUtc: new DateTime(2027, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            PurchaseOrderNumber: "PO-778899");

        var result = await lotService.CreateLotAsync(rawMaterial.Id, request, "user-123", cancellationToken);

        Assert.NotNull(result);
        Assert.Equal("LOT-2026-0001", result.LotNumber);
        Assert.Equal(rawMaterial.Id, result.RawMaterialId);
        Assert.Equal("Quarantine", result.Status);
        Assert.Equal(request.Quantity, result.Quantity);

        var lot = await dbContext.Lots.SingleAsync(cancellationToken);
        Assert.Equal(rawMaterial.Id, lot.RawMaterialId);
        Assert.Equal(LotStatus.Quarantine, lot.Status);
        Assert.Equal(request.LotNumber, lot.LotNumber);

        var auditLog = await dbContext.AuditLogs.SingleAsync(cancellationToken);
        Assert.Equal("Lot", auditLog.EntiteitType);
        Assert.Equal(lot.Id.ToString(), auditLog.EntiteitId);
        Assert.Equal("CREATE", auditLog.Actie);
        Assert.Equal("user-123", auditLog.GebruikerId);
        Assert.Contains("LotNumber=LOT-2026-0001", auditLog.NieuweWaarde);
        Assert.Contains("RawMaterialId=1", auditLog.NieuweWaarde);
        Assert.Contains("Status=Quarantine", auditLog.NieuweWaarde);
    }

    [Fact]
    public async Task CreateLotAsync_WithDuplicateLotNumberForSameRawMaterial_RejectsSecondLot()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        var services = new ServiceCollection();
        services.AddDbContext<DomainDbContext>(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<DomainDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var rawMaterial = new RawMaterial
        {
            Id = 1,
            Name = "Acetylsalicylic Acid",
            PharmaceuticalApi = "Acetylsalicylic Acid",
            Unit = "kg",
            Supplier = "ACME Pharma",
            MinSpecificationLimit = 0.95m,
            MaxSpecificationLimit = 1.05m
        };

        dbContext.RawMaterials.Add(rawMaterial);
        await dbContext.SaveChangesAsync(cancellationToken);

        var auditService = new TestAuditService(dbContext);
        var lotService = new LotService(dbContext, new TestAuthService(), auditService);

        var request = new CreateLotRequest(
            LotNumber: "LT100",
            Quantity: 125.5000m,
            ReceivedDateUtc: new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc),
            ExpiryDateUtc: new DateTime(2027, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            PurchaseOrderNumber: "PO-778899");

        await lotService.CreateLotAsync(rawMaterial.Id, request, "user-123", cancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => lotService.CreateLotAsync(rawMaterial.Id, request, "user-123", cancellationToken));

        Assert.Equal("This lot number already exists for the selected raw material.", exception.Message);
        Assert.Equal(1, await dbContext.Lots.CountAsync(cancellationToken));
        Assert.Equal(1, await dbContext.AuditLogs.CountAsync(cancellationToken));
    }

    private sealed class TestAuthService : IAuthService
    {
        public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> VerifyPasswordAsync(string userId, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<string> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> GetUsernameByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> GetUserFullNameAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestAuditService : IAuditService
    {
        private readonly DomainDbContext _dbContext;

        public TestAuditService(DomainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task LogAsync(
            string entityName,
            string entityId,
            string action,
            string? oldValue,
            string? newValue,
            string performedByUserId,
            CancellationToken ct = default)
        {
            _dbContext.AuditLogs.Add(new AuditLog
            {
                Tijdstip = DateTime.UtcNow,
                GebruikerId = performedByUserId,
                Actie = action,
                EntiteitType = entityName,
                EntiteitId = entityId,
                OudWaarde = oldValue,
                NieuweWaarde = newValue,
                IPAdres = string.Empty
            });

            await _dbContext.SaveChangesAsync(ct);
        }

        public Task UpdateAsync(
            long auditId,
            AuditLogUpdateRequest request,
            CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(long auditId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuditLogResponse>> GetAllAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}