using Microsoft.EntityFrameworkCore;
using PharmaQMS.Application.QualityControl;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.QualityControl;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services.Interfaces;

namespace PharmaQMS.Infrastructure.QualityControl;

public sealed class QcTestService(DomainDbContext db) : IQcTestService
{
    public async Task<Result<QcTestSummaryResponse>> CreateAsync(
        CreateQcTestRequest request,
        string userId,
        CancellationToken ct = default)
    {
        // Validate parameters: min must be < max
        foreach (var p in request.Parameters)
        {
            if (p.Min >= p.Max)
                return Result<QcTestSummaryResponse>.Failure(
                    $"Parameter '{p.Name}': min must be less than max.");
        }

        // Guard: verify test object exists and has correct status
        var objectExists = request.TestObjectType switch
        {
            "Lot" => await db.Lots
                            .AsNoTracking()
                            .AnyAsync(l => l.Id == request.TestObjectId
                                         && l.Status == LotStatus.Quarantine, ct),
            "Batch" => await db.Bmrs
                            .AsNoTracking()
                            .AnyAsync(b => b.BatchNumber == request.TestObjectId.ToString()
                                         && b.Status == BmrStatus.InQc, ct),
            _ => false
        };

        if (!objectExists)
            return Result<QcTestSummaryResponse>.Failure(
                "Test object not found or not eligible for QC.");

        var now = DateTimeOffset.UtcNow;

        var test = new QcTest
        {
            TestObjectType = request.TestObjectType,
            TestObjectId = request.TestObjectId,
            Status = QcTestStatus.InBehandeling,
            CreatedAt = now,
            CreatedBy = userId,
            Parameters = request.Parameters
                .Select(p => new QcTestParameter
                {
                    Name = p.Name,
                    Min = p.Min,
                    Max = p.Max,
                    Unit = p.Unit,
                })
                .ToList(),
        };

        db.QcTests.Add(test);

        // INSERT-only audit log (ALCOA+)
        await db.SaveChangesAsync(ct);

        db.AuditLogs.Add(new AuditLog
        {
            Tijdstip = now.UtcDateTime,
            GebruikerId = userId,
            Actie = "QcTest.Create",
            EntiteitType = nameof(QcTest),
            EntiteitId = test.Id.ToString(),
            OudWaarde = null,
            NieuweWaarde = $"TestObjectType={request.TestObjectType}, "
                           + $"TestObjectId={request.TestObjectId}, "
                           + $"ParameterCount={request.Parameters.Count}",
            IPAdres = string.Empty,
        });


        await db.SaveChangesAsync(ct);

        return Result<QcTestSummaryResponse>.Success(new(
            test.Id,
            test.TestObjectType,
            test.TestObjectId,
            test.Status,
            test.CreatedAt,
            test.CreatedBy
        ));
    }

    public async Task<Result<PagedResponse<QcTestSummaryResponse>>> GetAllAsync(
        QcTestListQuery query,
        CancellationToken ct = default)
    {
        var q = db.QcTests.AsNoTracking();

        if (query.Status is not null)
            q = q.Where(t => t.Status == query.Status);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new QcTestSummaryResponse(
                t.Id, t.TestObjectType, t.TestObjectId,
                t.Status, t.CreatedAt, t.CreatedBy))
            .ToListAsync(ct);

        return Result<PagedResponse<QcTestSummaryResponse>>.Success(
            new(items, query.Page, query.PageSize, total));
    }

    public async Task<Result<QcTestSummaryResponse>> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        var test = await db.QcTests
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new QcTestSummaryResponse(
                t.Id, t.TestObjectType, t.TestObjectId,
                t.Status, t.CreatedAt, t.CreatedBy))
            .FirstOrDefaultAsync(ct);

        return test is null
            ? Result<QcTestSummaryResponse>.Failure("QC test not found.")
            : Result<QcTestSummaryResponse>.Success(test);
    }
}