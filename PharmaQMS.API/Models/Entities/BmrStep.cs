namespace PharmaQMS.API.Models.Entities;


public class BmrStep
{
    public Guid Id { get; set; }
    public Guid BmrId { get; set; }
    public Guid MasterRecipeStepId { get; set; }
    public BmrStepStatus Status { get; set; }
    public string? EnteredData { get; set; }
    public required string EnteredById { get; set; }  // FK → ApplicationUser.Id
    public DateTime EnteredAt { get; set; }
    public string? VerifiedById { get; set; }         // FK → ApplicationUser.Id
    public DateTime? VerifiedAt { get; set; }
    public string? DeviationNote { get; set; }

    public Bmr? Bmr { get; set; }
    public MasterRecipeStep? MasterRecipeStep { get; set; }
}