namespace PharmaQMS.API.DTOs.MasterRecipe;
public record MasterRecipeSummaryResponse(
    Guid Id,
    string RecipeName,
    string Version,
    bool IsApproved
);