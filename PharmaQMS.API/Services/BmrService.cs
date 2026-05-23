namespace PharmaQMS.API.Services;

using System.Linq;
using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.MasterRecipe;
using PharmaQMS.API.DTOs.Productionline;
using PharmaQMS.API.Models.DTOs.Bmr;
using PharmaQMS.API.Models.DTOs.Common;
using PharmaQMS.API.Models.Entities;
using PharmaQMS.API.Models.Enums;
using PharmaQMS.API.Services.Interfaces;

public sealed class BmrService(
    DomainDbContext db,
    IAuthService _authService
) : IBmrService
{
    public async Task<BmrDetailResponse> CreateAsync(
    CreateBmrRequest request,
    string userId,
    CancellationToken ct = default)
    {
        var recipe = await db.MasterRecipes
            .AsNoTracking()
            .Include(r => r.Steps.OrderBy(s => s.StepNumber))
            .FirstOrDefaultAsync(r => r.Id == request.MasterRecipeId && r.IsApproved, ct)
            ?? throw new InvalidOperationException("Master recipe not found or not approved.");

        var lineExists = await db.ProductionLines
            .AsNoTracking()
            .AnyAsync(l => l.Id == request.ProductionLineId && l.IsActive, ct);

        if (!lineExists)
            throw new InvalidOperationException("Production line not found or inactive.");

        var approvedLotIds = await db.Lots
            .AsNoTracking()
            // .Where(l => request.LotIds.Contains(l.Id) && l.Status == LotStatus.Approved)
            .Where(l => ((ICollection<int>)request.LotIds).Contains(l.Id) && l.Status == LotStatus.Released)
            .Select(l => l.Id)
            .ToListAsync(ct);


        var missingLots = ((ICollection<int>)request.LotIds).Except(approvedLotIds).ToList();
        if (missingLots.Count > 0)
            throw new InvalidOperationException("One or more lots are not found or not released.");

        var batchNumber = GenerateBatchNumber();

        var bmr = new Bmr
        {
            Id = Guid.NewGuid(),
            BatchNumber = batchNumber,
            MasterRecipeId = recipe.Id,
            BatchSize = request.BatchSize,
            ProductionLineId = request.ProductionLineId,
            Status = BmrStatus.InProgress,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        bmr.Steps = recipe.Steps.Select(s => new BmrStep
        {
            Id = Guid.NewGuid(),
            BmrId = bmr.Id,
            MasterRecipeStepId = s.Id,
            Status = BmrStepStatus.Open,
            EnteredById = userId,
            EnteredAt = DateTime.UtcNow
        }).ToList();

        bmr.LotLinks = approvedLotIds.Select(lotId => new BmrLotLink
        {
            Id = Guid.NewGuid(),
            BmrId = bmr.Id,
            LotId = lotId
        }).ToList();

        db.Bmrs.Add(bmr);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(bmr.Id, ct);
    }

    public async Task<PagedResponse<BmrSummaryResponse>> GetAllAsync(
        BmrListQuery query,
        CancellationToken ct = default)
    {
        var q = db.Bmrs.AsNoTracking();

        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(b => b.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new BmrSummaryResponse(
                b.Id,
                b.BatchNumber,
                b.MasterRecipe!.RecipeName,
                b.ProductionLine!.LineName,
                b.CreatedAt,
                b.Status,
                b.Steps.Count,
                b.Steps.Count(s => s.Status == BmrStepStatus.Verified)
            ))
            .ToListAsync(ct);

        return new PagedResponse<BmrSummaryResponse>(items, query.Page, query.PageSize, total);
    }

    public async Task<BmrDetailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var bmr = await db.Bmrs
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new BmrDetailResponse(
                b.Id,
                b.BatchNumber,
                b.MasterRecipe!.RecipeName,
                b.MasterRecipe.Version,
                b.ProductionLine!.LineName,
                b.BatchSize,
                b.Status,
                b.CreatedAt,
                b.LotLinks.Select(ll => new BmrLotLinkResponse(
                    ll.LotId,
                    ll.Lot!.LotNumber,
                    ll.Lot.RawMaterial!.Name
                )).ToList(),
                b.Steps
                    .OrderBy(s => s.MasterRecipeStep!.StepNumber)
                    .Select(s => new BmrStepResponse(
                        s.Id,
                        s.MasterRecipeStep!.StepNumber,
                        s.MasterRecipeStep.StepName,
                        s.MasterRecipeStep.IsCritical,
                        s.Status,
                        s.EnteredData,
                        s.EnteredById,
                        s.EnteredAt == default ? null : s.EnteredAt,
                        s.VerifiedById,
                        s.VerifiedAt,
                        s.DeviationNote
                    )).ToList()
            ))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"BMR {id} not found.");

        return bmr;
    }

    public async Task ConfirmStepAsync(
        Guid bmrId,
        Guid stepId,
        ConfirmStepRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var bmr = await db.Bmrs
            .Include(b => b.Steps)
                .ThenInclude(s => s.MasterRecipeStep)
            .FirstOrDefaultAsync(b => b.Id == bmrId, ct)
            ?? throw new KeyNotFoundException("BMR not found.");

        if (bmr.Status != BmrStatus.InProgress)
            throw new InvalidOperationException("BMR is not in progress.");

        var step = bmr.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException("Step not found.");

        if (step.Status != BmrStepStatus.Open)
            throw new InvalidOperationException("Step is not open.");

        // Enforce sequential order — all prior steps must be Verified
        var priorStepsIncomplete = bmr.Steps
            .Where(s => s.MasterRecipeStep!.StepNumber < step.MasterRecipeStep!.StepNumber)
            .Any(s => s.Status != BmrStepStatus.Verified);

        if (priorStepsIncomplete)
            throw new InvalidOperationException("All preceding steps must be verified before confirming this step.");

        step.EnteredData = request.EnteredData;
        step.DeviationNote = request.DeviationNote;
        step.EnteredById = userId;
        step.EnteredAt = DateTime.UtcNow;
        step.Status = step.MasterRecipeStep!.IsCritical
            ? BmrStepStatus.AwaitingVerification
            : BmrStepStatus.Verified;

        // Auto-complete BMR when last step is done
        if (step.Status == BmrStepStatus.Verified && bmr.Steps.All(s => s.Status == BmrStepStatus.Verified))
            bmr.Status = BmrStatus.Completed;

        db.Bmrs.Update(bmr);

        await db.SaveChangesAsync(ct);
    }

    public async Task VerifyStepAsync(
        Guid bmrId,
        Guid stepId,
        VerifyStepRequest request,
        string userId,
        CancellationToken ct = default)
    {
        var bmr = await db.Bmrs
            .Include(b => b.Steps)
                .ThenInclude(s => s.MasterRecipeStep)
            .FirstOrDefaultAsync(b => b.Id == bmrId, ct)
            ?? throw new KeyNotFoundException("BMR not found.");

        var step = bmr.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new KeyNotFoundException("Step not found.");

        if (step.Status != BmrStepStatus.AwaitingVerification)
            throw new InvalidOperationException("Step is not awaiting verification.");

        // Four-eyes principle
        if (step.EnteredById == userId)
            throw new InvalidOperationException(
                "Verificateur mag niet dezelfde gebruiker zijn als de uitvoerende (vier-ogen-principe).");

        // Electronic signature — verify password
        var user = await _authService.GetUserByIdAsync(userId, ct);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var passwordValid = await _authService.VerifyPasswordAsync(user, request.Password, ct);
        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Electronic signature rejected: incorrect password.");
        }

        var utcNow = DateTime.UtcNow;
        var amsterdamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, amsterdamTimeZone);

        if (localTime < step.EnteredAt)
            throw new InvalidOperationException("Verification time cannot be before the step was entered.");

        step.VerifiedById = userId;
        step.VerifiedAt = localTime;
        step.Status = BmrStepStatus.Verified;

        if (bmr.Steps.All(s => s.Status == BmrStepStatus.Verified))
            bmr.Status = BmrStatus.Completed;

        db.Bmrs.Update(bmr);

        await db.SaveChangesAsync(ct);
    }

    private static string GenerateBatchNumber()
    {
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"BATCH-{datePart}-{randomPart}";
    }


    public async Task<IReadOnlyList<ProductionLineResponse>> GetProductionLinesAsync(CancellationToken ct = default)
    {
        try
        {
            return await db.ProductionLines
            .AsNoTracking()
            .Where(l => l.IsActive)
            .Select(l => new ProductionLineResponse(l.Id, l.LineName))
            .ToListAsync(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
    }
}