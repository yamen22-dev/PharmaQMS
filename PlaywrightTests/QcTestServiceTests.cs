using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Auth;
using PharmaQMS.API.DTOs.QualityControl;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.DTOs.Audit;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services;
using PharmaQMS.API.Services.Interfaces;
using PharmaQMS.Infrastructure.QualityControl;

namespace PlaywrightTests;

public sealed class QcTestServiceTests
{
    [Fact]
    public async Task SubmitResultsAsync_WithWithinAndOutOfSpecificationValues_ApprovesOrRejectsTestAndLotAndWritesAuditEntries()
    {
        var passScenario = await RunScenarioAsync(15m);
        var oosScenario = await RunScenarioAsync(5m);

        Assert.Equal(QcTestStatus.Approved, passScenario.Test.Status);
        Assert.Equal(LotStatus.Released, passScenario.Lot!.Status);
        Assert.True(passScenario.Response.CoaStarted);
        Assert.False(passScenario.Response.NotificationQueued);
        Assert.All(passScenario.Response.Parameters, parameter => Assert.True(parameter.IsWithinSpecification));
        Assert.Contains(passScenario.AuditLogs, log => log.Actie == "QC_RESULTS_SUBMITTED");
        Assert.Single(passScenario.AuditLogs);

        Assert.Equal(QcTestStatus.Rejected, oosScenario.Test.Status);
        Assert.Equal(LotStatus.Rejected, oosScenario.Lot!.Status);
        Assert.False(oosScenario.Response.CoaStarted);
        Assert.True(oosScenario.Response.NotificationQueued);
        Assert.All(oosScenario.Response.Parameters, parameter => Assert.False(parameter.IsWithinSpecification));
        Assert.Contains(oosScenario.AuditLogs, log => log.Actie == "QC_RESULTS_SUBMITTED");
        Assert.Contains(oosScenario.AuditLogs, log => log.Actie == "QA_MANAGER_ALERT");
        Assert.Equal(2, oosScenario.AuditLogs.Count);
    }

    [Fact]
    public async Task SubmitResultsAsync_WithWithinAndOutOfSpecificationValues_ApprovesOrRejectsTestAndBatchAndWritesAuditEntries()
    {
        var passScenario = await RunBatchScenarioAsync(15m);
        var oosScenario = await RunBatchScenarioAsync(5m);

        Assert.Equal(QcTestStatus.Approved, passScenario.Test.Status);
        Assert.Equal(BmrStatus.Completed, passScenario.Bmr!.Status);
        Assert.True(passScenario.Response.CoaStarted);
        Assert.False(passScenario.Response.NotificationQueued);
        Assert.All(passScenario.Response.Parameters, parameter => Assert.True(parameter.IsWithinSpecification));
        Assert.Contains(passScenario.AuditLogs, log => log.Actie == "QC_RESULTS_SUBMITTED");
        Assert.Single(passScenario.AuditLogs);

        Assert.Equal(QcTestStatus.Rejected, oosScenario.Test.Status);
        Assert.Equal(BmrStatus.Rejected, oosScenario.Bmr!.Status);
        Assert.False(oosScenario.Response.CoaStarted);
        Assert.True(oosScenario.Response.NotificationQueued);
        Assert.All(oosScenario.Response.Parameters, parameter => Assert.False(parameter.IsWithinSpecification));
        Assert.Contains(oosScenario.AuditLogs, log => log.Actie == "QC_RESULTS_SUBMITTED");
        Assert.Contains(oosScenario.AuditLogs, log => log.Actie == "QA_MANAGER_ALERT");
        Assert.Equal(2, oosScenario.AuditLogs.Count);
    }

    private static async Task<QcScenarioResult> RunScenarioAsync(decimal measuredValue)
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
            MaxSpecificationLimit = 1.05m,
        };

        var lot = new Lot
        {
            Id = 1001,
            RawMaterialId = rawMaterial.Id,
            LotNumber = "LOT-QC-UT-0001",
            ReceivedDateUtc = new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc),
            Quantity = 125.5m,
            Status = LotStatus.Quarantine,
            ExpiryDateUtc = new DateTime(2027, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            PurchaseOrderNumber = "PO-778899",
            AnalysisCertificate = "COA-778899"
        };

        var qcTest = new QcTest
        {
            TestObjectType = "Lot",
            TestObjectId = lot.Id,
            Status = QcTestStatus.InBehandeling,
            CreatedAt = new DateTimeOffset(2026, 5, 24, 9, 0, 0, TimeSpan.Zero),
            CreatedBy = "qc-user-1",
            Parameters =
            [
                new QcTestParameter
                {
                    Name = "Assay",
                    Unit = "%",
                    Min = 10m,
                    Max = 20m,
                }
            ]
        };

        dbContext.RawMaterials.Add(rawMaterial);
        dbContext.Lots.Add(lot);
        dbContext.QcTests.Add(qcTest);
        await dbContext.SaveChangesAsync(cancellationToken);

        var auditService = new TestAuditService(dbContext);
        var qcService = new QcTestService(dbContext, new TestAuthService(), auditService);

        var parameterId = qcTest.Parameters.Single().Id;
        var response = await qcService.SubmitResultsAsync(
            qcTest.Id,
            new SubmitQcTestResultsRequest
            {
                Password = "correct-password",
                Parameters =
                [
                    new SubmitQcTestResultParameterRequest
                    {
                        ParameterId = parameterId,
                        MeasuredValue = measuredValue,
                    }
                ]
            },
            "qc-user-1",
            cancellationToken);

        Assert.True(response.IsSuccess);
        var submitResult = Assert.IsType<SubmitQcTestResultsResponse>(response.Value);

        var savedTest = await dbContext.QcTests
            .Include(test => test.Parameters)
            .SingleAsync(test => test.Id == qcTest.Id, cancellationToken);

        var savedLot = await dbContext.Lots.SingleAsync(lotEntity => lotEntity.Id == lot.Id, cancellationToken);
        var auditLogs = await dbContext.AuditLogs
            .OrderBy(log => log.Id)
            .ToListAsync(cancellationToken);

        return new QcScenarioResult(submitResult, savedTest, savedLot, null, auditLogs);
    }

    private static async Task<QcScenarioResult> RunBatchScenarioAsync(decimal measuredValue)
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

        var masterRecipe = new MasterRecipe
        {
            Id = Guid.NewGuid(),
            RecipeName = "Seed Batch Recipe",
            Version = "1.0",
            IsApproved = true,
        };

        var productionLine = new ProductionLine
        {
            Id = Guid.NewGuid(),
            LineName = "Seed Line",
            Location = "Main",
            IsActive = true,
        };

        var bmr = new Bmr
        {
            Id = Guid.NewGuid(),
            BatchNumber = "BATCH-QC-SEED-777",
            MasterRecipeId = masterRecipe.Id,
            BatchSize = 250m,
            ProductionLineId = productionLine.Id,
            Status = BmrStatus.InQc,
            CreatedById = "qc-user-1",
            CreatedAt = new DateTime(2026, 5, 24, 9, 0, 0, DateTimeKind.Utc),
        };

        dbContext.MasterRecipes.Add(masterRecipe);
        dbContext.ProductionLines.Add(productionLine);
        dbContext.Bmrs.Add(bmr);
        await dbContext.SaveChangesAsync(cancellationToken);

        var auditService = new TestAuditService(dbContext);
        var qcService = new QcTestService(dbContext, new TestAuthService(), auditService);

        var qcTest = new QcTest
        {
            TestObjectType = "Batch",
            TestObjectId = 777,
            Status = QcTestStatus.InBehandeling,
            CreatedAt = new DateTimeOffset(2026, 5, 24, 9, 0, 0, TimeSpan.Zero),
            CreatedBy = "qc-user-1",
            Parameters =
            [
                new QcTestParameter
                {
                    Name = "Assay",
                    Unit = "%",
                    Min = 10m,
                    Max = 20m,
                }
            ]
        };

        dbContext.QcTests.Add(qcTest);
        await dbContext.SaveChangesAsync(cancellationToken);

        var parameterId = qcTest.Parameters.Single().Id;
        var response = await qcService.SubmitResultsAsync(
            qcTest.Id,
            new SubmitQcTestResultsRequest
            {
                Password = "correct-password",
                Parameters =
                [
                    new SubmitQcTestResultParameterRequest
                    {
                        ParameterId = parameterId,
                        MeasuredValue = measuredValue,
                    }
                ]
            },
            "qc-user-1",
            cancellationToken);

        Assert.True(response.IsSuccess);
        var submitResult = Assert.IsType<SubmitQcTestResultsResponse>(response.Value);

        var savedTest = await dbContext.QcTests
            .Include(test => test.Parameters)
            .SingleAsync(test => test.Id == qcTest.Id, cancellationToken);

        var savedBmr = await dbContext.Bmrs.SingleAsync(entity => entity.Id == bmr.Id, cancellationToken);
        var auditLogs = await dbContext.AuditLogs
            .OrderBy(log => log.Id)
            .ToListAsync(cancellationToken);

        return new QcScenarioResult(submitResult, savedTest, null, savedBmr, auditLogs);
    }

    private sealed record QcScenarioResult(
        SubmitQcTestResultsResponse Response,
        QcTest Test,
        Lot? Lot,
        Bmr? Bmr,
        IReadOnlyList<AuditLog> AuditLogs);

    private sealed class TestAuthService : IAuthService
    {
        public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> VerifyPasswordAsync(string userId, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(password == "correct-password");

        public Task<string> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> GetUsernameByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult($"user-{userId}");

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
                IPAdres = string.Empty,
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