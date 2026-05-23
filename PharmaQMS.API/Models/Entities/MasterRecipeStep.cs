namespace PharmaQMS.API.Models.Entities;

public class MasterRecipeStep
{
    public Guid Id { get; set; }
    public Guid MasterRecipeId { get; set; }
    public int StepNumber { get; set; }
    public required string StepName { get; set; }
    public bool IsCritical { get; set; }
    public string? ExpectedFields { get; set; }  // JSON description of fields to fill

    public MasterRecipe? MasterRecipe { get; set; }
    public ICollection<BmrStep> BmrSteps { get; set; } = [];
}