using PharmaQMS.API.DTOs.MasterRecipe;

namespace PharmaQMS.API.Services.Interfaces.MasterRecipe;

public interface IMasterRecipeService
{
    Task<IReadOnlyList<MasterRecipeSummaryResponse>> GetApprovedMasterRecipesAsync(
        string? status,
        CancellationToken ct = default);
}