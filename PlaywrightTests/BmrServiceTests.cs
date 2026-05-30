using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.Auth;
using PharmaQMS.API.Infrastructure;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.DTOs.Bmr;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services;

namespace PlaywrightTests;

public sealed class BmrServiceTests
{
    [Fact]
    [Trait("TestId", "UT-UC03-01")]
    [Trait("TestId", "UT-UC03-02")]
    public async Task CreateAsync_WithApprovedMasterRecipeAndReleasedLot_CreatesBmrInProgressWithLinkedLotsAndAuditEntries()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        await using (var seedContext = new DomainDbContext(
            new DbContextOptionsBuilder<DomainDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync(cancellationToken);

            var rawMaterial = new RawMaterial
            {
                Id = 1,
                Name = "Amoxicillin Trihydrate",
                PharmaceuticalApi = "Amoxicillin Trihydrate",
                Unit = "kg",
                Supplier = "ACME Pharma",
                MinSpecificationLimit = 0.95m,
                MaxSpecificationLimit = 1.05m
            };

            var productionLineId = Guid.NewGuid();
            var masterRecipeId = Guid.NewGuid();
            var masterRecipeStepId = Guid.NewGuid();
            var lotId = 1001;

            seedContext.RawMaterials.Add(rawMaterial);
            seedContext.ProductionLines.Add(new ProductionLine
            {
                Id = productionLineId,
                LineName = "Line Alpha",
                Location = "Hall 1",
                IsActive = true
            });
            seedContext.MasterRecipes.Add(new MasterRecipe
            {
                Id = masterRecipeId,
                RecipeName = "BMR TEST RECIPE",
                Version = "1.0",
                IsApproved = true,
                Steps =
                [
                    new MasterRecipeStep
                    {
                        Id = masterRecipeStepId,
                        StepNumber = 1,
                        StepName = "Dispense raw material",
                        IsCritical = true,
                        ExpectedFields = "Weight"
                    }
                ]
            });
            seedContext.Lots.Add(new Lot
            {
                Id = lotId,
                RawMaterialId = rawMaterial.Id,
                LotNumber = "LOT-APPROVED-0001",
                ReceivedDateUtc = new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc),
                Quantity = 125.5m,
                Status = LotStatus.Released,
                ExpiryDateUtc = new DateTime(2027, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                PurchaseOrderNumber = "PO-778899",
                AnalysisCertificate = "COA-778899"
            });

            await seedContext.SaveChangesAsync(cancellationToken);

            var httpContextAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") },
                            authenticationType: "Test"))
                }
            };

            await using var dbContext = new DomainDbContext(
                new DbContextOptionsBuilder<DomainDbContext>()
                    .UseSqlite(connection)
                    .AddInterceptors(new AuditTrailInterceptor(httpContextAccessor))
                    .Options);

            var bmrService = new BmrService(dbContext, new TestAuthService());

            var request = new CreateBmrRequest
            {
                MasterRecipeId = masterRecipeId,
                BatchSize = 250m,
                ProductionLineId = productionLineId,
                LotIds = [lotId]
            };

            var firstResult = await bmrService.CreateAsync(request, "user-123", cancellationToken);
            var secondResult = await bmrService.CreateAsync(request, "user-123", cancellationToken);

            Assert.Equal(BmrStatus.InProgress, firstResult.Status);
            Assert.Equal(BmrStatus.InProgress, secondResult.Status);
            Assert.False(string.IsNullOrWhiteSpace(firstResult.BatchNumber));
            Assert.False(string.IsNullOrWhiteSpace(secondResult.BatchNumber));
            Assert.NotEqual(firstResult.BatchNumber, secondResult.BatchNumber);
            Assert.Equal(new[] { lotId }, firstResult.Lots.Select(l => l.LotId));
            Assert.Equal(new[] { lotId }, secondResult.Lots.Select(l => l.LotId));
            Assert.Single(firstResult.Steps);
            Assert.Single(secondResult.Steps);

            var bmrEntities = await dbContext.Bmrs.Include(b => b.LotLinks).ToListAsync(cancellationToken);
            Assert.Equal(2, bmrEntities.Count);
            Assert.All(bmrEntities, bmr => Assert.Equal(BmrStatus.InProgress, bmr.Status));
            Assert.All(bmrEntities, bmr => Assert.Single(bmr.LotLinks));

            var auditLogs = await dbContext.AuditLogs.ToListAsync(cancellationToken);
            Assert.Contains(auditLogs, log => log.EntiteitType == nameof(Bmr));
            Assert.Equal(2, auditLogs.Count(log => log.EntiteitType == nameof(Bmr)));
        }
    }

    [Fact]
    [Trait("TestId", "UT-UC03-01")]
    public async Task VerifyStepAsync_WithSelfVerificationRejectsAndWithQaManagerMarksStepVerifiedAndWritesAuditEntry()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var productionLineId = Guid.NewGuid();
        var masterRecipeId = Guid.NewGuid();
        var lotId = 1002;

        await using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync(cancellationToken);

        await using (var seedContext = new DomainDbContext(
            new DbContextOptionsBuilder<DomainDbContext>()
                .UseSqlite(connection)
                .Options))
        {
            await seedContext.Database.EnsureCreatedAsync(cancellationToken);

            var rawMaterial = new RawMaterial
            {
                Id = 1,
                Name = "Amoxicillin Trihydrate",
                PharmaceuticalApi = "Amoxicillin Trihydrate",
                Unit = "kg",
                Supplier = "ACME Pharma",
                MinSpecificationLimit = 0.95m,
                MaxSpecificationLimit = 1.05m
            };

            var firstStepId = Guid.NewGuid();
            var criticalStepId = Guid.NewGuid();
            var finalStepId = Guid.NewGuid();

            seedContext.RawMaterials.Add(rawMaterial);
            seedContext.ProductionLines.Add(new ProductionLine
            {
                Id = productionLineId,
                LineName = "Line Alpha",
                Location = "Hall 1",
                IsActive = true
            });
            seedContext.MasterRecipes.Add(new MasterRecipe
            {
                Id = masterRecipeId,
                RecipeName = "BMR STEP VALIDATION RECIPE",
                Version = "1.0",
                IsApproved = true,
                Steps =
                [
                    new MasterRecipeStep
                    {
                        Id = firstStepId,
                        StepNumber = 1,
                        StepName = "Dispense raw material",
                        IsCritical = false,
                        ExpectedFields = "Weight"
                    },
                    new MasterRecipeStep
                    {
                        Id = criticalStepId,
                        StepNumber = 2,
                        StepName = "Critical blend verification",
                        IsCritical = true,
                        ExpectedFields = "Mixing parameters"
                    },
                    new MasterRecipeStep
                    {
                        Id = finalStepId,
                        StepNumber = 3,
                        StepName = "Packaging release",
                        IsCritical = false,
                        ExpectedFields = "Checklist"
                    }
                ]
            });
            seedContext.Lots.Add(new Lot
            {
                Id = lotId,
                RawMaterialId = rawMaterial.Id,
                LotNumber = "LOT-VALIDATION-0001",
                ReceivedDateUtc = new DateTime(2026, 5, 20, 8, 30, 0, DateTimeKind.Utc),
                Quantity = 125.5m,
                Status = LotStatus.Released,
                ExpiryDateUtc = new DateTime(2027, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                PurchaseOrderNumber = "PO-778899",
                AnalysisCertificate = "COA-778899"
            });

            await seedContext.SaveChangesAsync(cancellationToken);
        }

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = CreateHttpContext("ProductionAnalyst1")
        };

        await using var dbContext = new DomainDbContext(
            new DbContextOptionsBuilder<DomainDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(new AuditTrailInterceptor(httpContextAccessor))
                .Options);

        var bmrService = new BmrService(dbContext, new StepVerificationTestAuthService());

        var createResult = await bmrService.CreateAsync(
            new CreateBmrRequest
            {
                MasterRecipeId = masterRecipeId,
                BatchSize = 250m,
                ProductionLineId = productionLineId,
                LotIds = [lotId]
            },
            "ProductionAnalyst1",
            cancellationToken);

        var firstStep = createResult.Steps.Single(step => step.StepNumber == 1);
        var criticalStep = createResult.Steps.Single(step => step.StepNumber == 2);
        var finalStep = createResult.Steps.Single(step => step.StepNumber == 3);

        await bmrService.ConfirmStepAsync(
            createResult.Id,
            firstStep.Id,
            new ConfirmStepRequest
            {
                EnteredData = "125.5 kg"
            },
            "ProductionAnalyst1",
            cancellationToken);

        await bmrService.ConfirmStepAsync(
            createResult.Id,
            criticalStep.Id,
            new ConfirmStepRequest
            {
                EnteredData = "Mixing parameters within tolerance"
            },
            "ProductionAnalyst1",
            cancellationToken);

        var awaitingVerification = await dbContext.BmrSteps.AsNoTracking().SingleAsync(step => step.Id == criticalStep.Id, cancellationToken);
        Assert.Equal(BmrStepStatus.AwaitingVerification, awaitingVerification.Status);
        Assert.Equal("ProductionAnalyst1", awaitingVerification.EnteredById);

        var selfVerificationException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => bmrService.VerifyStepAsync(
                createResult.Id,
                criticalStep.Id,
                new VerifyStepRequest
                {
                    Password = "QaManager1!"
                },
                "ProductionAnalyst1",
                cancellationToken));

        Assert.Contains("vier-ogen-principe", selfVerificationException.Message, StringComparison.OrdinalIgnoreCase);

        var afterRejectedVerification = await dbContext.BmrSteps.AsNoTracking().SingleAsync(step => step.Id == criticalStep.Id, cancellationToken);
        Assert.Equal(BmrStepStatus.AwaitingVerification, afterRejectedVerification.Status);
        Assert.Null(afterRejectedVerification.VerifiedById);
        Assert.Null(afterRejectedVerification.VerifiedAt);

        httpContextAccessor.HttpContext = CreateHttpContext("QaManager1");

        await bmrService.VerifyStepAsync(
            createResult.Id,
            criticalStep.Id,
            new VerifyStepRequest
            {
                Password = "QaManager1!"
            },
            "QaManager1",
            cancellationToken);

        var verifiedStep = await dbContext.BmrSteps.AsNoTracking().SingleAsync(step => step.Id == criticalStep.Id, cancellationToken);
        Assert.Equal(BmrStepStatus.Verified, verifiedStep.Status);
        Assert.Equal("QaManager1", verifiedStep.VerifiedById);
        Assert.NotNull(verifiedStep.VerifiedAt);
        Assert.Equal("ProductionAnalyst1", verifiedStep.EnteredById);

        var firstConfirmedStep = await dbContext.BmrSteps.AsNoTracking().SingleAsync(step => step.Id == firstStep.Id, cancellationToken);
        var finalOpenStep = await dbContext.BmrSteps.AsNoTracking().SingleAsync(step => step.Id == finalStep.Id, cancellationToken);
        Assert.Equal(BmrStepStatus.Verified, firstConfirmedStep.Status);
        Assert.Equal(BmrStepStatus.Open, finalOpenStep.Status);
        Assert.Equal(BmrStatus.InProgress, (await dbContext.Bmrs.AsNoTracking().SingleAsync(bmr => bmr.Id == createResult.Id, cancellationToken)).Status);

        var auditLogs = await dbContext.AuditLogs.AsNoTracking().ToListAsync(cancellationToken);
        Assert.Contains(auditLogs, log =>
            log.EntiteitType == nameof(BmrStep) &&
            log.Actie == "UPDATE" &&
            log.EntiteitId == criticalStep.Id.ToString());
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

    private static DefaultHttpContext CreateHttpContext(string userId) => new()
    {
        User = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                authenticationType: "Test"))
    };

    private sealed class StepVerificationTestAuthService : IAuthService
    {
        public Task<AuthenticationResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<AuthenticationResult> RevokeAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> VerifyPasswordAsync(string userId, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId switch
            {
                "ProductionAnalyst1" => password == "ProductionAnalyst1!",
                "QaManager1" => password == "QaManager1!",
                _ => false
            });

        public Task<string> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId switch
            {
                "ProductionAnalyst1" => "ProductionAnalyst1",
                "QaManager1" => "QaManager1",
                _ => throw new KeyNotFoundException($"User {userId} not found.")
            });

        public Task<string> GetUsernameByIdAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> GetUserFullNameAsync(string userId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}