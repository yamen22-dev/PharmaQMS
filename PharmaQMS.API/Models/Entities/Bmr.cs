namespace PharmaQMS.API.Models.Entities;

using PharmaQMS.API.Models.Enums;

public class Bmr
{
    public Guid Id { get; set; }
    public required string BatchNumber { get; set; }
    public Guid MasterRecipeId { get; set; }
    public decimal BatchSize { get; set; }
    public Guid ProductionLineId { get; set; }
    public BmrStatus Status { get; set; }
    public required string CreatedById { get; set; }  // FK → ApplicationUser.Id
    public DateTime CreatedAt { get; set; }

    public MasterRecipe? MasterRecipe { get; set; }
    public ProductionLine? ProductionLine { get; set; }
    public ICollection<BmrStep> Steps { get; set; } = [];
    public ICollection<BmrLotLink> LotLinks { get; set; } = [];
}