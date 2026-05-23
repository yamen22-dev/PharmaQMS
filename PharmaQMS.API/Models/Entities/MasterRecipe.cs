namespace PharmaQMS.API.Models.Entities;

public class MasterRecipe
{
    public Guid Id { get; set; }
    public required string RecipeName { get; set; }
    public required string Version { get; set; }
    public bool IsApproved { get; set; }

    public ICollection<MasterRecipeStep> Steps { get; set; } = [];
    public ICollection<Bmr> Bmrs { get; set; } = [];
}