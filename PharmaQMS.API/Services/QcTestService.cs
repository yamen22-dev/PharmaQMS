using Microsoft.EntityFrameworkCore;
using PharmaQMS.Application.QualityControl;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.QualityControl;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services;
using PharmaQMS.API.Services.Interfaces;
using System.Globalization;

namespace PharmaQMS.Infrastructure.QualityControl;

public sealed class QcTestService(
    DomainDbContext db,
    IAuthService authService,
    IAuditService auditService) : IQcTestService
{
    public async Task<Result<QcTestSummaryResponse>> CreateAsync(
        CreateQcTestRequest request,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<QcTestSummaryResponse>.Failure(
                "Electronic signature rejected: password is required.");
        }

        var signatureValid = await authService.VerifyPasswordAsync(userId, request.Password, ct);
        if (!signatureValid)
        {
            return Result<QcTestSummaryResponse>.Failure(
                "Electronic signature rejected: incorrect password.");
        }

        var normalizedType = request.TestObjectType.Trim().ToLowerInvariant() switch
        {
            "lot" => "Lot",
            "batch" => "Batch",
            _ => string.Empty,
        };

        if (string.IsNullOrWhiteSpace(normalizedType))
        {
            return Result<QcTestSummaryResponse>.Failure(
                "Test object type must be either 'Lot' or 'Batch'.");
        }

        if (request.Parameters.Count == 0)
        {
            return Result<QcTestSummaryResponse>.Failure(
                "At least one test parameter is required.");
        }

        // Validate parameters: min must be < max
        foreach (var p in request.Parameters)
        {
            if (string.IsNullOrWhiteSpace(p.Name) || string.IsNullOrWhiteSpace(p.Unit))
            {
                return Result<QcTestSummaryResponse>.Failure(
                    "Each parameter must contain a name and a unit.");
            }

            if (p.Min >= p.Max)
                return Result<QcTestSummaryResponse>.Failure(
                    $"Parameter '{p.Name}': min must be less than max.");
        }

        // Guard: verify test object exists and has correct status
        var objectExists = normalizedType switch
        {
            "Lot" => await db.Lots
                            .AsNoTracking()
                            .AnyAsync(l => l.Id == request.TestObjectId
                                         && l.Status == LotStatus.Quarantine, ct),
            "Batch" => await IsEligibleBatchAsync(request.TestObjectId, ct),
            _ => false
        };

        if (!objectExists)
            return Result<QcTestSummaryResponse>.Failure(
                "Test object not found or not eligible for QC.");

        var now = DateTimeOffset.UtcNow;

        var test = new QcTest
        {
            TestObjectType = normalizedType,
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
            NieuweWaarde = $"TestObjectType={normalizedType}, "
                           + $"TestObjectId={request.TestObjectId}, "
                           + $"ParameterCount={request.Parameters.Count}",
            IPAdres = string.Empty,
        });


        await db.SaveChangesAsync(ct);

        var createdByUsername = await ResolveCreatedByUsernameAsync(test.CreatedBy, ct);

        return Result<QcTestSummaryResponse>.Success(new(
            test.Id,
            test.TestObjectType,
            test.TestObjectId,
            test.Status,
            test.CreatedAt,
            createdByUsername
        ));
    }

    private async Task<bool> IsEligibleBatchAsync(int testObjectId, CancellationToken ct)
    {
        var idText = testObjectId.ToString(CultureInfo.InvariantCulture);
        var paddedIdText = testObjectId.ToString("D3", CultureInfo.InvariantCulture);

        var batchNumbers = await db.Bmrs
            .AsNoTracking()
            .Where(b => b.Status == BmrStatus.InQc)
            .Select(b => b.BatchNumber)
            .ToListAsync(ct);

        return batchNumbers.Any(batchNumber => MatchesBatchId(batchNumber, idText, paddedIdText));
    }

    private static bool MatchesBatchId(string batchNumber, string idText, string paddedIdText)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            return false;
        }

        if (batchNumber.Equals(idText, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (batchNumber.EndsWith($"-{idText}", StringComparison.OrdinalIgnoreCase)
            || batchNumber.EndsWith($"-{paddedIdText}", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var segments = batchNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        var lastSegment = segments[^1];
        if (!int.TryParse(lastSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSegment))
        {
            return false;
        }

        if (!int.TryParse(idText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
        {
            return false;
        }

        return parsedSegment == parsedId;
    }

    public async Task<Result<QcEligibleObjectsResponse>> GetEligibleObjectsAsync(
        CancellationToken ct = default)
    {
        var lots = await db.Lots
            .AsNoTracking()
            .Where(l => l.Status == LotStatus.Quarantine)
            .OrderByDescending(l => l.Id)
            .Select(l => new QcEligibleObjectResponse(
                "Lot",
                l.Id,
                $"Lot #{l.Id} - {l.LotNumber}"))
            .ToListAsync(ct);

        var inQcBatchNumbers = await db.Bmrs
            .AsNoTracking()
            .Where(b => b.Status == BmrStatus.InQc)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => b.BatchNumber)
            .ToListAsync(ct);

        var batches = inQcBatchNumbers
            .Select(batchNumber => new
            {
                BatchNumber = batchNumber,
                ParsedId = ExtractBatchObjectId(batchNumber)
            })
            .Where(x => x.ParsedId is not null)
            .GroupBy(x => x.ParsedId!.Value)
            .Select(g => g.First())
            .Select(x => new QcEligibleObjectResponse(
                "Batch",
                x.ParsedId!.Value,
                x.BatchNumber))
            .ToList();

        return Result<QcEligibleObjectsResponse>.Success(
            new QcEligibleObjectsResponse(lots, batches));
    }

    private static int? ExtractBatchObjectId(string batchNumber)
    {
        if (string.IsNullOrWhiteSpace(batchNumber))
        {
            return null;
        }

        var segments = batchNumber.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return null;
        }

        var lastSegment = segments[^1];
        if (!int.TryParse(lastSegment, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
        {
            return null;
        }

        return parsedId > 0 ? parsedId : null;
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

        var createdByUserIds = items
            .Select(x => x.CreatedBy)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var usernamesById = await ResolveCreatedByUsernamesAsync(createdByUserIds, ct);

        var mappedItems = items
            .Select(item => item with
            {
                CreatedBy = usernamesById.TryGetValue(item.CreatedBy, out var username)
                    ? username
                    : item.CreatedBy
            })
            .ToList();

        return Result<PagedResponse<QcTestSummaryResponse>>.Success(
            new(mappedItems, query.Page, query.PageSize, total));
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

        if (test is null)
        {
            return Result<QcTestSummaryResponse>.Failure("QC test not found.");
        }

        var createdByUsername = await ResolveCreatedByUsernameAsync(test.CreatedBy, ct);

        return Result<QcTestSummaryResponse>.Success(test with
        {
            CreatedBy = createdByUsername
        });
    }

    public async Task<Result<QcTestDetailResponse>> GetDetailAsync(
        int id,
        CancellationToken ct = default)
    {
        var test = await db.QcTests
            .AsNoTracking()
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (test is null)
        {
            return Result<QcTestDetailResponse>.Failure("QC test not found.");
        }

        var createdByUsername = await ResolveCreatedByUsernameAsync(test.CreatedBy, ct);

        return Result<QcTestDetailResponse>.Success(new QcTestDetailResponse(
            test.Id,
            test.TestObjectType,
            test.TestObjectId,
            await ResolveTestObjectLabelAsync(test.TestObjectType, test.TestObjectId, ct),
            test.Status,
            test.CreatedAt,
            createdByUsername,
            test.Parameters
                .OrderBy(parameter => parameter.Id)
                .Select(ToParameterResponse)
                .ToList()));
    }

    public async Task<Result<SubmitQcTestResultsResponse>> SubmitResultsAsync(
        int id,
        SubmitQcTestResultsRequest request,
        string userId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<SubmitQcTestResultsResponse>.Failure(
                "Electronic signature rejected: password is required.");
        }

        var signatureValid = await authService.VerifyPasswordAsync(userId, request.Password, ct);
        if (!signatureValid)
        {
            return Result<SubmitQcTestResultsResponse>.Failure(
                "Electronic signature rejected: incorrect password.");
        }

        var test = await db.QcTests
            .Include(t => t.Parameters)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (test is null)
        {
            return Result<SubmitQcTestResultsResponse>.Failure("QC test not found.");
        }

        if (test.Status is not QcTestStatus.InBehandeling and not QcTestStatus.InProgress)
        {
            return Result<SubmitQcTestResultsResponse>.Failure("QC test is already finalized.");
        }

        if (request.Parameters.Count != test.Parameters.Count)
        {
            return Result<SubmitQcTestResultsResponse>.Failure(
                "All parameter results must be entered before saving.");
        }

        var parametersById = test.Parameters.ToDictionary(parameter => parameter.Id);

        foreach (var submittedParameter in request.Parameters)
        {
            if (!parametersById.TryGetValue(submittedParameter.ParameterId, out var parameter))
            {
                return Result<SubmitQcTestResultsResponse>.Failure(
                    $"Parameter {submittedParameter.ParameterId} not found in this test.");
            }

            parameter.MeasuredValue = submittedParameter.MeasuredValue;
        }

        if (test.Parameters.Any(parameter => !parameter.MeasuredValue.HasValue))
        {
            return Result<SubmitQcTestResultsResponse>.Failure(
                "All parameter results must be entered before saving.");
        }

        var allWithinSpecification = test.Parameters.All(parameter =>
            parameter.MeasuredValue.HasValue
            && parameter.MeasuredValue.Value >= parameter.Min
            && parameter.MeasuredValue.Value <= parameter.Max);

        test.Status = allWithinSpecification
            ? QcTestStatus.Approved
            : QcTestStatus.Rejected;

        var testObjectUpdated = await ApplyTestObjectStatusAsync(
            test.TestObjectType,
            test.TestObjectId,
            allWithinSpecification,
            ct);

        if (!testObjectUpdated)
        {
            return Result<SubmitQcTestResultsResponse>.Failure(
                "Test object could not be updated for this QC result.");
        }

        await db.SaveChangesAsync(ct);

        await auditService.LogAsync(
            entityName: nameof(QcTest),
            entityId: test.Id.ToString(CultureInfo.InvariantCulture),
            action: "QC_RESULTS_SUBMITTED",
            oldValue: null,
            newValue: $"TestObjectType={test.TestObjectType};TestObjectId={test.TestObjectId};Status={test.Status};WithinSpec={allWithinSpecification}",
            performedByUserId: userId,
            ct: ct);

        if (!allWithinSpecification)
        {
            await auditService.LogAsync(
                entityName: "Notification",
                entityId: test.Id.ToString(CultureInfo.InvariantCulture),
                action: "QA_MANAGER_ALERT",
                oldValue: null,
                newValue: "OOS detected. QA manager notification queued.",
                performedByUserId: userId,
                ct: ct);
        }

        var responseParameters = test.Parameters
            .OrderBy(parameter => parameter.Id)
            .Select(ToParameterResponse)
            .ToList();

        return Result<SubmitQcTestResultsResponse>.Success(new SubmitQcTestResultsResponse(
            test.Id,
            test.TestObjectType,
            test.TestObjectId,
            await ResolveTestObjectLabelAsync(test.TestObjectType, test.TestObjectId, ct),
            test.Status,
            allWithinSpecification
                ? "Alle resultaten liggen binnen specificatie. CoA-generatie is gestart."
                : "OOS gedetecteerd. Er dient een deviatierapport te worden aangemaakt (BR03).",
            allWithinSpecification,
            !allWithinSpecification,
            responseParameters));
    }

    private async Task<Dictionary<string, string>> ResolveCreatedByUsernamesAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken ct)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var userId in userIds)
        {
            map[userId] = await ResolveCreatedByUsernameAsync(userId, ct);
        }

        return map;
    }

    private async Task<string> ResolveCreatedByUsernameAsync(string userId, CancellationToken ct)
    {
        try
        {
            return await authService.GetUsernameByIdAsync(userId, ct);
        }
        catch (KeyNotFoundException)
        {
            return userId;
        }
    }

    private async Task<string> ResolveTestObjectLabelAsync(
        string testObjectType,
        int testObjectId,
        CancellationToken ct)
    {
        if (testObjectType.Equals("Lot", StringComparison.OrdinalIgnoreCase))
        {
            var lot = await db.Lots
                .AsNoTracking()
                .Where(lot => lot.Id == testObjectId)
                .Select(lot => new { lot.Id, lot.LotNumber })
                .FirstOrDefaultAsync(ct);

            return lot is null
                ? $"Lot #{testObjectId}"
                : $"Lot #{lot.Id} - {lot.LotNumber}";
        }

        if (testObjectType.Equals("Batch", StringComparison.OrdinalIgnoreCase))
        {
            var batch = await db.Bmrs
                .AsNoTracking()
                .Select(bmr => new { bmr.BatchNumber })
                .ToListAsync(ct);

            var batchNumber = batch.FirstOrDefault(item => MatchesBatchId(item.BatchNumber, testObjectId))?.BatchNumber;
            return string.IsNullOrWhiteSpace(batchNumber)
                ? $"Batch #{testObjectId}"
                : $"Batch #{testObjectId} - {batchNumber}";
        }

        return $"{testObjectType} #{testObjectId}";
    }

    private async Task<bool> ApplyTestObjectStatusAsync(
        string testObjectType,
        int testObjectId,
        bool passed,
        CancellationToken ct)
    {
        if (testObjectType.Equals("Lot", StringComparison.OrdinalIgnoreCase))
        {
            var lot = await db.Lots.FirstOrDefaultAsync(l => l.Id == testObjectId, ct);
            if (lot is null)
            {
                return false;
            }

            lot.Status = passed ? LotStatus.Released : LotStatus.Rejected;
            return true;
        }

        if (testObjectType.Equals("Batch", StringComparison.OrdinalIgnoreCase))
        {
            var bmrList = await db.Bmrs.ToListAsync(ct);
            var matchingBmr = bmrList.FirstOrDefault(bmr => MatchesBatchId(bmr.BatchNumber, testObjectId));
            if (matchingBmr is null)
            {
                return false;
            }

            matchingBmr.Status = passed ? BmrStatus.Completed : BmrStatus.Rejected;
            return true;
        }

        return false;
    }

    private static QcTestParameterResponse ToParameterResponse(QcTestParameter parameter)
    {
        var measuredValue = parameter.MeasuredValue;
        var withinSpecification = measuredValue.HasValue
            && measuredValue.Value >= parameter.Min
            && measuredValue.Value <= parameter.Max;

        return new QcTestParameterResponse(
            parameter.Id,
            parameter.Name,
            parameter.Unit,
            parameter.Min,
            parameter.Max,
            parameter.MeasuredValue,
            withinSpecification);
    }

    private static bool MatchesBatchId(string batchNumber, int testObjectId)
    {
        var idText = testObjectId.ToString(CultureInfo.InvariantCulture);
        var paddedIdText = testObjectId.ToString("D3", CultureInfo.InvariantCulture);
        return MatchesBatchId(batchNumber, idText, paddedIdText);
    }
}