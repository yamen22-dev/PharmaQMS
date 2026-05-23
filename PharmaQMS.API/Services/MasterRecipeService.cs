using Microsoft.EntityFrameworkCore;
using PharmaQMS.API.Data;
using PharmaQMS.API.DTOs.MasterRecipe;
using PharmaQMS.API.Services.Interfaces.MasterRecipe;
namespace PharmaQMS.API.Services.MasterRecipe;


public sealed class MasterRecipeService : IMasterRecipeService
{
    private readonly DomainDbContext _db;

    public MasterRecipeService(DomainDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MasterRecipeSummaryResponse>> GetApprovedMasterRecipesAsync(
        string? status,
        CancellationToken ct = default)
    {
        var masterRecipes = await _db.MasterRecipes.ToListAsync(ct);

        if (!string.IsNullOrEmpty(status))
        {
            masterRecipes = masterRecipes.Where(mr => mr.IsApproved).ToList();
        }

        return masterRecipes.Select(mr => new MasterRecipeSummaryResponse(
            mr.Id,
            mr.RecipeName,
            mr.Version,
            mr.IsApproved
        )).ToList();
    }
}