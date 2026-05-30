using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.Infrastructure;
using PharmaQMS.API.Models.DTOs.Audit;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Services;

namespace PlaywrightTests;

public sealed class AuditServiceTests
{
    [Fact]
    public async Task UpdateAsync_AndDeleteAsync_OnExistingAuditLog_ThrowAndLeaveRecordUntouched()
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
            seedContext.AuditLogs.Add(new AuditLog
            {
                Tijdstip = new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc),
                GebruikerId = "audit-user-1",
                Actie = "CREATE",
                EntiteitType = "Lot",
                EntiteitId = "LOT-UT-001",
                OudWaarde = "{\"status\":\"Quarantine\"}",
                NieuweWaarde = "{\"status\":\"Released\"}",
                IPAdres = "127.0.0.1"
            });

            await seedContext.SaveChangesAsync(cancellationToken);
        }

        var updateRequest = new AuditLogUpdateRequest(
            EntiteitType: "Lot",
            Actie: "UPDATE",
            OudWaarde: "{\"status\":\"Released\"}",
            NieuweWaarde: "{\"status\":\"Rejected\"}",
            GebruikerId: "audit-user-2",
            Tijdstip: new DateTime(2026, 5, 24, 11, 0, 0, DateTimeKind.Utc));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var updateContext = CreateContext(connection, useInterceptor: true);
            var auditService = new AuditService(updateContext, new HttpContextAccessor());
            await auditService.UpdateAsync(1, updateRequest, cancellationToken);
        });

        await using (var verifyAfterUpdateContext = CreateContext(connection, useInterceptor: false))
        {
            var auditLog = await verifyAfterUpdateContext.AuditLogs.AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal("Lot", auditLog.EntiteitType);
            Assert.Equal("CREATE", auditLog.Actie);
            Assert.Equal("{\"status\":\"Quarantine\"}", auditLog.OudWaarde);
            Assert.Equal("{\"status\":\"Released\"}", auditLog.NieuweWaarde);
            Assert.Equal("audit-user-1", auditLog.GebruikerId);
            Assert.Equal(new DateTime(2026, 5, 24, 10, 0, 0, DateTimeKind.Utc), auditLog.Tijdstip);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await using var deleteContext = CreateContext(connection, useInterceptor: true);
            var auditService = new AuditService(deleteContext, new HttpContextAccessor());
            await auditService.DeleteAsync(1, cancellationToken);
        });

        await using (var verifyAfterDeleteContext = CreateContext(connection, useInterceptor: false))
        {
            Assert.Equal(1, await verifyAfterDeleteContext.AuditLogs.CountAsync(cancellationToken));
            var auditLog = await verifyAfterDeleteContext.AuditLogs.AsNoTracking().SingleAsync(cancellationToken);
            Assert.Equal(1, auditLog.Id);
        }
    }

    private static DomainDbContext CreateContext(SqliteConnection connection, bool useInterceptor)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DomainDbContext>()
            .UseSqlite(connection);

        if (useInterceptor)
        {
            optionsBuilder.AddInterceptors(new AuditTrailInterceptor(new HttpContextAccessor()));
        }

        return new DomainDbContext(optionsBuilder.Options);
    }
}